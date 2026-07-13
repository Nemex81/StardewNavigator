using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewNavigator.Integration;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Catalogue of executable numpad actions and their concrete implementations.
    ///
    /// Each action is identified by a <see cref="NumpadActionId"/> value.
    /// Handlers receive an <see cref="ActionContext"/> snapshot for any contextual data
    /// they need (navigator state, registry references, etc.).
    ///
    /// Pre-conditions (IsPlayerFree, SA availability, cooldown guards) are enforced by
    /// the caller (<c>NumpadController.HandleButton</c>); this catalogue trusts the caller
    /// to invoke handlers only when appropriate.
    ///
    /// Actions not yet migrated here (GridMove*, MicroMove*, TileViewerMove*,
    /// ReadTileFacing, ReadTileStanding) remain implemented inline in
    /// <c>NumpadController</c> and are excluded from this catalogue until Phase 2.
    /// </summary>
    internal static class NumpadActionCatalog
    {
        /// <summary>
        /// Executes the specified action using the provided game-state snapshot.
        /// </summary>
        public static void Execute(NumpadActionId actionId, ActionContext context)
        {
            switch (actionId)
            {
                // ── Hotbar ─────────────────────────────────────────────────────────
                case NumpadActionId.SlotPrevious:
                    // Wrapping circular cycle backwards through 12 hotbar slots.
                    Game1.player.CurrentToolIndex = (Game1.player.CurrentToolIndex + 11) % 12;
                    break;

                case NumpadActionId.SlotNext:
                    // Wrapping circular cycle forwards through 12 hotbar slots.
                    Game1.player.CurrentToolIndex = (Game1.player.CurrentToolIndex + 1) % 12;
                    break;

                // ── Menu shortcuts ─────────────────────────────────────────────────
                case NumpadActionId.OpenInventory:
                    Game1.activeClickableMenu = new GameMenu();
                    break;

                case NumpadActionId.OpenNavigatorMenu:
                    // Defensive IsPlayerFree check — the caller's guard already ensures this,
                    // but the original OpenMenu() also had it.
                    if (context.IsPlayerFree &&
                        context.Registry  != null &&
                        context.Navigator != null &&
                        context.RouteEngine != null)
                    {
                        Game1.activeClickableMenu = new NavigatorMenu(
                            context.Registry.Maps,
                            (map, poi) => context.Navigator.StartNavigation(map, poi, context.RouteEngine),
                            text => NavigatorSpeaker.Say(text, true)
                        );
                    }
                    break;

                // ── Input aliases ──────────────────────────────────────────────────
                case NumpadActionId.AliasEnter:
                case NumpadActionId.AliasCtrlEnter:
                    // Both press SButton.Enter. The "Ctrl" part of AliasCtrlEnter is
                    // achieved by LeftCtrl being physically held while Enter is simulated,
                    // which the game reads as Ctrl+Enter.
                    ModEntry.Helper.Input.Press(SButton.Enter);
                    break;

                // ── Direct interaction ─────────────────────────────────────────────
                case NumpadActionId.UseTool:
                    // Simulates the native use tool button (default: C). Required for melee weapons
                    // since Game1.pressUseToolButton() only triggers Tool subclasses (axe, pickaxe)
                    // but not MeleeWeapon subclasses (swords).
                    var useToolButtons = Game1.options.useToolButton;
                    if (useToolButtons != null && useToolButtons.Length > 0)
                    {
                        foreach (var btn in useToolButtons)
                        {
                            if (btn.key != Microsoft.Xna.Framework.Input.Keys.None)
                            {
                                if (Enum.TryParse<SButton>(btn.key.ToString(), out var sBtn))
                                {
                                    ModEntry.Helper.Input.Press(sBtn);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        ModEntry.Helper.Input.Press(SButton.C);
                    }
                    break;

                case NumpadActionId.Interact:
                    // Simulates the native action button (default: X). Required because
                    // stardew-access 1.7.0-beta.2 intercepts checkAction() and blocks chests.
                    // See docs/input-management.md §3.A and docs/stardew-access-integration.md §2.B.
                    ModEntry.Helper.Input.Press(SButton.X);
                    break;

                // ── Reading / inspection ───────────────────────────────────────────
                case NumpadActionId.ReadCoords:
                    ExecuteReadCoords();
                    break;

                case NumpadActionId.ReadHealthStamina:
                    ExecuteReadHealthStamina();
                    break;

                case NumpadActionId.ReadCurrentItem:
                    ExecuteReadCurrentItem();
                    break;

                case NumpadActionId.ReadNavStatus:
                    ExecuteReadNavStatus(context.Navigator);
                    break;

                // ── Navigation control ─────────────────────────────────────────────
                case NumpadActionId.CancelNavigation:
                    // If navigation is not active, speaks "navigation inactive" as feedback.
                    // This preserves the behaviour of the original CancelNav() method.
                    if (context.Navigator != null)
                    {
                        if (context.Navigator.IsActive)
                            context.Navigator.CancelNavigation("Comando tastierino");
                        else
                            NavigatorSpeaker.Say(ModEntry.Helper.Translation.Get("numpad-nav-status-inactive").ToString(), true);
                    }
                    break;

                case NumpadActionId.AutoWalkToObject:
                    StardewAccessBridge.TryMoveToSelectedObject();
                    break;

                // ── Object Tracker scanner ─────────────────────────────────────────
                case NumpadActionId.ScannerObjectGroupUp:
                    StardewAccessBridge.TryCycleObjectTracker(1, back: true);
                    break;

                case NumpadActionId.ScannerObjectGroupDown:
                    StardewAccessBridge.TryCycleObjectTracker(1, back: false);
                    break;

                case NumpadActionId.ScannerCategoryUp:
                    StardewAccessBridge.TryCycleObjectTracker(0, back: true);
                    break;

                case NumpadActionId.ScannerCategoryDown:
                    StardewAccessBridge.TryCycleObjectTracker(0, back: false);
                    break;

                case NumpadActionId.ScannerInGroupUp:
                    StardewAccessBridge.TryCycleObjectTracker(2, back: true);
                    break;

                case NumpadActionId.ScannerInGroupDown:
                    StardewAccessBridge.TryCycleObjectTracker(2, back: false);
                    break;

                // ── Navigator Menu context actions ─────────────────────────────────
                // Not yet reached via the binding table dispatch. Present here for
                // completeness — will be wired in Phase 2.
                case NumpadActionId.NavMenuCursorUp:
                    context.ActiveNavigatorMenu?.NumpadMoveCursor(-1);
                    break;

                case NumpadActionId.NavMenuCursorDown:
                    context.ActiveNavigatorMenu?.NumpadMoveCursor(1);
                    break;

                case NumpadActionId.NavMenuConfirm:
                    context.ActiveNavigatorMenu?.NumpadConfirm();
                    break;

                // ── Actions not yet migrated — handled inline in NumpadController ──
                // GridMove*, MicroMove*, TileViewerMove*, ReadTileFacing, ReadTileStanding
                default:
                    Log.Warn($"[NumpadActionCatalog] Execute called for action not yet handled: {actionId}");
                    break;
            }
        }

        // ── Reading helpers (migrated from NumpadController) ──────────────────────

        private static void ExecuteReadCoords()
        {
            if (!Context.IsWorldReady) return;

            Farmer player       = Game1.player;
            GameLocation location = Game1.currentLocation;
            if (player == null || location == null) return;

            int x    = (int)player.Tile.X;
            int y    = (int)player.Tile.Y;
            string text = ModEntry.Helper.Translation.Get(
                "numpad-coords",
                new { location_name = location.DisplayName, x, y }
            ).ToString();
            NavigatorSpeaker.Say(text, true);
        }

        private static void ExecuteReadHealthStamina()
        {
            if (!Context.IsWorldReady) return;
            Farmer player = Game1.player;
            if (player == null) return;

            int healthPercent  = player.maxHealth  > 0 ? (int)Math.Round((double)player.health  / player.maxHealth  * 100) : 0;
            int staminaPercent = player.MaxStamina > 0 ? (int)Math.Round((double)player.Stamina / player.MaxStamina * 100) : 0;

            string text = ModEntry.Helper.Translation.Get(
                "numpad-health-stamina",
                new { health = healthPercent, stamina = staminaPercent }
            ).ToString();
            NavigatorSpeaker.Say(text, true);
        }

        private static void ExecuteReadCurrentItem()
        {
            if (!Context.IsWorldReady) return;
            Farmer player = Game1.player;
            if (player == null) return;

            Item? currentItem = player.CurrentItem;
            string text = currentItem == null
                ? ModEntry.Helper.Translation.Get("numpad-current-item-empty").ToString()
                : ModEntry.Helper.Translation.Get("numpad-current-item", new { item_name = currentItem.DisplayName }).ToString();
            NavigatorSpeaker.Say(text, true);
        }

        private static void ExecuteReadNavStatus(Navigator? navigator)
        {
            if (navigator == null) return;

            if (navigator.IsActive)
            {
                string text = ModEntry.Helper.Translation.Get(
                    "numpad-nav-status-active",
                    new
                    {
                        poi_name      = navigator.PoiDisplayName,
                        location_name = navigator.MapDisplayName,
                        steps         = navigator.RemainingSteps,
                    }
                ).ToString();
                NavigatorSpeaker.Say(text, true);
            }
            else
            {
                NavigatorSpeaker.Say(ModEntry.Helper.Translation.Get("numpad-nav-status-inactive").ToString(), true);
            }
        }
    }
}
