using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Accessible three-level text-based menu for consulting, switching profiles, and mapping numpad bindings.
    /// Follows the same navigation and screen-reader announcement patterns as <see cref="NavigatorMenu"/>.
    /// </summary>
    public class NumpadConfigMenu : IClickableMenu
    {
        private enum MenuLevel { Level1, Level2, Level3 }

        private MenuLevel _currentLevel = MenuLevel.Level1;
        private int _categoryIndex = 0; // selected item in Level1
        private int _detailIndex = 0;   // selected item in Level2
        private int _actionIndex = 0;   // selected item in Level3

        // Level 1 category identifiers
        private const int CatActive = 0;
        private const int CatInactive = 1;
        private const int CatGlobal = 2;
        private const int CatProfile = 3;
        private const int CatReset = 4;
        private const int CategoryCount = 5;

        // Configuration helper lists
        private readonly List<NumpadBinding> _visibleBindings = new();
        private NumpadBinding _selectedBinding;

        // List of logical actions available for custom assignment (excluding protected aliases)
        private static readonly NumpadActionId[] AssignableActions = {
            NumpadActionId.GridMoveUp, NumpadActionId.GridMoveDown, NumpadActionId.GridMoveLeft, NumpadActionId.GridMoveRight,
            NumpadActionId.UseTool, NumpadActionId.Interact,
            NumpadActionId.ReadTileFacing, NumpadActionId.ReadTileStanding, NumpadActionId.ReadCoords, NumpadActionId.ReadHealthStamina, NumpadActionId.ReadCurrentItem, NumpadActionId.ReadNavStatus,
            NumpadActionId.SlotPrevious, NumpadActionId.SlotNext,
            NumpadActionId.OpenInventory, NumpadActionId.OpenNavigatorMenu,
            NumpadActionId.CancelNavigation, NumpadActionId.AutoWalkToObject,
            NumpadActionId.ScannerObjectGroupUp, NumpadActionId.ScannerObjectGroupDown,
            NumpadActionId.ScannerCategoryUp, NumpadActionId.ScannerCategoryDown,
            NumpadActionId.ScannerInGroupUp, NumpadActionId.ScannerInGroupDown,
            NumpadActionId.TileViewerMoveUp, NumpadActionId.TileViewerMoveDown, NumpadActionId.TileViewerMoveLeft, NumpadActionId.TileViewerMoveRight
        };

        // Layout measurements (isomorphic to NavigatorMenu)
        private const int ItemHeight = 56;
        private const int MenuPadding = 32;
        private const int TitleHeight = 64;
        private const int MaxVisibleItems = 10;

        private int _lastMouseX = -1;
        private int _lastMouseY = -1;

        public NumpadConfigMenu()
            : base(
                x: Game1.uiViewport.Width / 2 - 350,
                y: Game1.uiViewport.Height / 2 - (MaxVisibleItems * ItemHeight / 2) - MenuPadding,
                width: 700,
                height: MaxVisibleItems * ItemHeight + MenuPadding * 2 + TitleHeight,
                showUpperRightCloseButton: true)
        {
            AnnounceCurrentView();
        }

        /// <summary>
        /// Handles keyboard and numpad shortcuts for navigation.
        /// </summary>
        public override void receiveKeyPress(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    MoveCursor(-1);
                    break;

                case Keys.Down:
                case Keys.S:
                    MoveCursor(1);
                    break;

                case Keys.Enter:
                    ConfirmSelection();
                    break;

                case Keys.Escape:
                    HandleEscape();
                    break;
            }
        }

        /// <summary>
        /// Support for controller D-Pad + A/B.
        /// </summary>
        public override void receiveGamePadButton(Buttons b)
        {
            switch (b)
            {
                case Buttons.DPadUp:
                    MoveCursor(-1);
                    break;
                case Buttons.DPadDown:
                    MoveCursor(1);
                    break;
                case Buttons.A:
                    ConfirmSelection();
                    break;
                case Buttons.B:
                    HandleEscape();
                    break;
            }
        }

        /// <summary>
        /// Custom text-based rendering identical in aesthetic to NavigatorMenu.
        /// </summary>
        public override void draw(SpriteBatch b)
        {
            // Menu background
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            // Title
            string title = GetTitleText();
            Utility.drawTextWithShadow(b, title,
                Game1.dialogueFont,
                new Vector2(xPositionOnScreen + MenuPadding, yPositionOnScreen + MenuPadding),
                Color.Black);

            // Current visible items
            List<string> items = GetCurrentItems();
            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            
            int currentIndex = _currentLevel switch
            {
                MenuLevel.Level1 => _categoryIndex,
                MenuLevel.Level2 => _detailIndex,
                MenuLevel.Level3 => _actionIndex,
                _ => 0
            };

            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                int idx = i + scrollOffset;
                if (idx >= items.Count) break;

                bool isSelected = idx == currentIndex;
                Color textColor = isSelected ? Color.DarkBlue : Color.Black;
                string prefix = isSelected ? "▶ " : "  ";

                Utility.drawTextWithShadow(b,
                    prefix + items[idx],
                    Game1.dialogueFont,
                    new Vector2(xPositionOnScreen + MenuPadding, startY + i * ItemHeight),
                    textColor);
            }

            // Scroll arrows
            if (items.Count > MaxVisibleItems)
            {
                if (scrollOffset > 0)
                    Utility.drawTextWithShadow(b, "▲", Game1.dialogueFont,
                        new Vector2(xPositionOnScreen + width - MenuPadding - 20, startY - 20), Color.Gray);
                if (scrollOffset + MaxVisibleItems < items.Count)
                    Utility.drawTextWithShadow(b, "▼", Game1.dialogueFont,
                        new Vector2(xPositionOnScreen + width - MenuPadding - 20, startY + visibleCount * ItemHeight), Color.Gray);
            }

            // Bottom-right indicator
            string levelIndicator = _currentLevel switch
            {
                MenuLevel.Level1 => ModEntry.Helper.Translation.Get("numpad-config-category-indicator").ToString(),
                MenuLevel.Level2 => ModEntry.Helper.Translation.Get("numpad-config-binding-indicator").ToString(),
                MenuLevel.Level3 => "[Edit]",
                _ => string.Empty
            };
            Utility.drawTextWithShadow(b, levelIndicator, Game1.smallFont,
                new Vector2(xPositionOnScreen + width - MenuPadding - 100,
                            yPositionOnScreen + height - MenuPadding - 20),
                Color.Gray);

            base.draw(b);
            drawMouse(b);
        }

        /// <summary>
        /// Basic mouse hover/scroll navigation.
        /// </summary>
        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            if (x == _lastMouseX && y == _lastMouseY) return;
            _lastMouseX = x;
            _lastMouseY = y;

            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            List<string> items = GetCurrentItems();
            
            int currentIndex = _currentLevel switch
            {
                MenuLevel.Level1 => _categoryIndex,
                MenuLevel.Level2 => _detailIndex,
                MenuLevel.Level3 => _actionIndex,
                _ => 0
            };

            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                var rect = new Rectangle(
                    xPositionOnScreen + MenuPadding,
                    startY + i * ItemHeight,
                    width - MenuPadding * 2,
                    ItemHeight
                );

                if (rect.Contains(x, y))
                {
                    int targetIdx = i + scrollOffset;
                    if (targetIdx < items.Count && targetIdx != currentIndex)
                    {
                        switch (_currentLevel)
                        {
                            case MenuLevel.Level1:
                                _categoryIndex = targetIdx;
                                break;
                            case MenuLevel.Level2:
                                _detailIndex = targetIdx;
                                break;
                            case MenuLevel.Level3:
                                _actionIndex = targetIdx;
                                break;
                        }
                        AnnounceCurrentSelection();
                    }
                    break;
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            List<string> items = GetCurrentItems();
            
            int currentIndex = _currentLevel switch
            {
                MenuLevel.Level1 => _categoryIndex,
                MenuLevel.Level2 => _detailIndex,
                MenuLevel.Level3 => _actionIndex,
                _ => 0
            };

            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                var rect = new Rectangle(
                    xPositionOnScreen + MenuPadding,
                    startY + i * ItemHeight,
                    width - MenuPadding * 2,
                    ItemHeight
                );

                if (rect.Contains(x, y))
                {
                    int targetIdx = i + scrollOffset;
                    if (targetIdx < items.Count)
                    {
                        switch (_currentLevel)
                        {
                            case MenuLevel.Level1:
                                _categoryIndex = targetIdx;
                                break;
                            case MenuLevel.Level2:
                                _detailIndex = targetIdx;
                                break;
                            case MenuLevel.Level3:
                                _actionIndex = targetIdx;
                                break;
                        }

                        ConfirmSelection();
                    }
                    break;
                }
            }
        }

        // ─── Internal Logic ───────────────────────────────────────────────────────

        private string GetTitleText()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                string baseTitle = ModEntry.Helper.Translation.Get("numpad-config-title").ToString();
                string profileKey = ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind 
                    ? "numpad-config-profile-blind" 
                    : "numpad-config-profile-sighted";
                string profileName = ModEntry.Helper.Translation.Get(profileKey).ToString();
                return $"{baseTitle} ({profileName})";
            }

            if (_currentLevel == MenuLevel.Level2)
            {
                return _categoryIndex switch
                {
                    CatActive => ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                    CatInactive => ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                    CatGlobal => ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                    CatProfile => ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString(),
                    _ => string.Empty
                };
            }

            return $"{_selectedBinding.Chord}";
        }

        private List<string> GetCurrentItems()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                return new List<string>
                {
                    ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString(),
                    "* " + ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString() + " *"
                };
            }

            var currentProfile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);

            if (_currentLevel == MenuLevel.Level2)
            {
                switch (_categoryIndex)
                {
                    case CatActive:
                        // Cache bindings of current profile
                        _visibleBindings.Clear();
                        foreach (var b in DefaultBindingTable.Bindings)
                        {
                            if (NumpadProfileRegistry.TryResolveAction(b.Chord, currentProfile, ModEntry.Config.NumpadOverrides, out var act) && act == b.ActionId)
                            {
                                _visibleBindings.Add(b);
                            }
                        }

                        // Also include any user-overridden keys that are not in the default table
                        if (ModEntry.Config.NumpadOverrides != null)
                        {
                            foreach (var ov in ModEntry.Config.NumpadOverrides)
                            {
                                if (ov.Value != "None" && Enum.TryParse<NumpadActionId>(ov.Value, out var actId))
                                {
                                    // Add to visible if not already in it
                                    var chord = InputChord.TryParse(ov.Key);
                                    if (chord.HasValue && !_visibleBindings.Any(b => b.Chord == chord.Value))
                                    {
                                        _visibleBindings.Add(new NumpadBinding(chord.Value, actId));
                                    }
                                }
                            }
                        }

                        return _visibleBindings
                            .Select(b => $"{b.Chord} → {NumpadActionMetadata.GetDescription(GetRuntimeAction(b.Chord, currentProfile))}")
                            .ToList();

                    case CatInactive:
                        _visibleBindings.Clear();
                        foreach (var b in DefaultBindingTable.Bindings)
                        {
                            var currentAct = GetRuntimeAction(b.Chord, currentProfile);
                            if (currentAct == NumpadActionId.None)
                            {
                                _visibleBindings.Add(b);
                            }
                        }

                        if (_visibleBindings.Count == 0)
                            return new List<string> { ModEntry.Helper.Translation.Get("numpad-config-no-inactive").ToString() };

                        return _visibleBindings
                            .Select(b => $"{b.Chord} → ({NumpadActionMetadata.GetDescription(b.ActionId)})")
                            .ToList();

                    case CatGlobal:
                        return NumpadActionMetadata.GlobalBindings
                            .Select(b => $"{b.Keys} → {b.Description}")
                            .ToList();

                    case CatProfile:
                        string blindLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind ? "[X] " : "[  ] ") + ModEntry.Helper.Translation.Get("numpad-config-profile-blind").ToString();
                        string sightedLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Sighted ? "[X] " : "[  ] ") + ModEntry.Helper.Translation.Get("numpad-config-profile-sighted").ToString();
                        return new List<string> { blindLabel, sightedLabel };

                    default:
                        return new List<string>();
                }
            }

            // Level 3: Dropdown list of assignable actions
            var list = new List<string>(AssignableActions.Length + 2);
            var currentAssigned = GetRuntimeAction(_selectedBinding.Chord, currentProfile);

            foreach (var act in AssignableActions)
            {
                string activeMarker = (currentAssigned == act) ? "[X] " : "[  ] ";
                list.Add(activeMarker + NumpadActionMetadata.GetDescription(act));
            }

            string noneMarker = (currentAssigned == NumpadActionId.None) ? "[X] " : "[  ] ";
            list.Add(noneMarker + "[" + ModEntry.Helper.Translation.Get("numpad-config-disable-label").ToString() + "]");
            
            // Revert option
            bool isOverridden = ModEntry.Config.NumpadOverrides.ContainsKey(_selectedBinding.Chord.ToString());
            string defaultMarker = isOverridden ? "    " : "[X] ";
            list.Add(defaultMarker + "[" + ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString() + "]");

            return list;
        }

        private NumpadActionId GetRuntimeAction(InputChord chord, NumpadProfile baseProfile)
        {
            if (NumpadProfileRegistry.TryResolveAction(chord, baseProfile, ModEntry.Config.NumpadOverrides, out var act))
            {
                return act;
            }
            return NumpadActionId.None;
        }

        private void MoveCursor(int direction)
        {
            List<string> items = GetCurrentItems();
            if (items.Count == 0) return;

            switch (_currentLevel)
            {
                case MenuLevel.Level1:
                    _categoryIndex = (_categoryIndex + direction + CategoryCount) % CategoryCount;
                    break;
                case MenuLevel.Level2:
                    _detailIndex = (_detailIndex + direction + items.Count) % items.Count;
                    break;
                case MenuLevel.Level3:
                    _actionIndex = (_actionIndex + direction + items.Count) % items.Count;
                    break;
            }

            AnnounceCurrentSelection();
        }

        private void ConfirmSelection()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                if (_categoryIndex == CatReset)
                {
                    // Global Reset
                    ModEntry.Config.NumpadOverrides.Clear();
                    ModEntry.Helper.WriteConfig(ModEntry.Config);

                    string msg = ModEntry.Helper.Translation.Get("numpad-info-reset-complete").ToString();
                    NavigatorSpeaker.Say(msg, true);
                    return;
                }

                _currentLevel = MenuLevel.Level2;
                _detailIndex = 0;
                AnnounceCurrentView();
            }
            else if (_currentLevel == MenuLevel.Level2)
            {
                if (_categoryIndex == CatProfile)
                {
                    // Switch profile
                    var newProfile = _detailIndex == 0 ? NumpadProfileId.Blind : NumpadProfileId.Sighted;
                    ModEntry.Config.ActiveNumpadProfile = newProfile;
                    ModEntry.Helper.WriteConfig(ModEntry.Config);

                    string profileName = newProfile.ToString();
                    string msg = ModEntry.Helper.Translation.Get("numpad-config-profile-changed", new { profile = profileName }).ToString();
                    NavigatorSpeaker.Say(msg, true);

                    // Return to Level 1
                    _currentLevel = MenuLevel.Level1;
                    _detailIndex = 0;
                    AnnounceCurrentView();
                }
                else if (_categoryIndex == CatActive || _categoryIndex == CatInactive)
                {
                    if (_visibleBindings.Count > 0 && _detailIndex < _visibleBindings.Count)
                    {
                        _selectedBinding = _visibleBindings[_detailIndex];
                        _currentLevel = MenuLevel.Level3;
                        _actionIndex = 0;
                        AnnounceCurrentView();
                    }
                }
            }
            else
            {
                // Level 3 Confirm option
                int totalActions = AssignableActions.Length;
                string targetActionName = "None";

                if (_actionIndex < totalActions)
                {
                    targetActionName = AssignableActions[_actionIndex].ToString();
                }
                else if (_actionIndex == totalActions)
                {
                    targetActionName = "None";
                }
                else if (_actionIndex == totalActions + 1)
                {
                    targetActionName = "Default";
                }

                if (ValidateAndApplyOverride(_selectedBinding.Chord, targetActionName))
                {
                    // Successful mapping -> vocal confirmation
                    string actionNameLocalized = targetActionName == "None" 
                        ? ModEntry.Helper.Translation.Get("numpad-config-disable-label").ToString()
                        : targetActionName == "Default" 
                            ? ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString()
                            : NumpadActionMetadata.GetDescription(AssignableActions[_actionIndex]);

                    string keyLabel = _selectedBinding.Chord.ToString();
                    string infoMsg = ModEntry.Helper.Translation.Get("numpad-info-binding-moved", new { action = actionNameLocalized, newKey = keyLabel }).ToString();
                    NavigatorSpeaker.Say(infoMsg, true);

                    // Go back to Level 2
                    _currentLevel = MenuLevel.Level2;
                    _detailIndex = 0;
                    AnnounceCurrentView();
                }
            }
        }

        private void HandleEscape()
        {
            if (_currentLevel == MenuLevel.Level3)
            {
                _currentLevel = MenuLevel.Level2;
                _actionIndex = 0;
                AnnounceCurrentView();
            }
            else if (_currentLevel == MenuLevel.Level2)
            {
                _currentLevel = MenuLevel.Level1;
                _detailIndex = 0;
                AnnounceCurrentView();
            }
            else
            {
                exitThisMenu();
            }
        }

        private int GetScrollOffset(int index, int totalCount)
        {
            if (totalCount <= MaxVisibleItems) return 0;
            if (index < MaxVisibleItems / 2) return 0;
            if (index >= totalCount - MaxVisibleItems / 2) return totalCount - MaxVisibleItems;
            return index - MaxVisibleItems / 2;
        }

        // ─── Verification & Overrides Validation ─────────────────────────────────

        private bool ValidateAndApplyOverride(InputChord chord, string targetActionName)
        {
            string chordString = chord.ToString();
            
            // Simula lo stato futuro degli overrides
            var tempOverrides = new Dictionary<string, string>(ModEntry.Config.NumpadOverrides);
            
            // Swap automatico nel dizionario temporaneo
            if (targetActionName != "None" && targetActionName != "Default")
            {
                var keysToRemove = tempOverrides.Where(kvp => kvp.Value == targetActionName && kvp.Key != chordString).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    tempOverrides[key] = "None"; // disattivato per non ereditare il vecchio default conflittuale
                }
            }

            if (targetActionName == "Default")
            {
                tempOverrides.Remove(chordString);
            }
            else
            {
                tempOverrides[chordString] = targetActionName;
            }

            // Validazione delegata al resolver centralizzato (disaccoppiamento logica di business da UI)
            var baseProfile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);
            if (!NumpadProfileRegistry.ValidateOverrides(baseProfile, tempOverrides, out var unmappedCriticalAction))
            {
                // Errore critico bloccante
                string actionLabel = NumpadActionMetadata.GetDescription(unmappedCriticalAction);
                string errMsg = ModEntry.Helper.Translation.Get("numpad-err-critical-unmapped", new { action = actionLabel }).ToString();
                NavigatorSpeaker.Say(errMsg, true);
                return false;
            }

            // Consolidamento modifiche
            ModEntry.Config.NumpadOverrides = tempOverrides;
            ModEntry.Helper.WriteConfig(ModEntry.Config);
            return true;
        }

        // ─── Accessibility Vocalizations ──────────────────────────────────────────

        private void AnnounceCurrentView()
        {
            string viewName = GetTitleText();
            string profileKey = ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind 
                ? "numpad-config-profile-blind" 
                : "numpad-config-profile-sighted";
            string profileName = ModEntry.Helper.Translation.Get(profileKey).ToString();
            string profileIndicator = ModEntry.Helper.Translation.Get("numpad-config-current-profile", new { profile = profileName }).ToString();
            
            if (_currentLevel == MenuLevel.Level1)
            {
                string help = ModEntry.Helper.Translation.Get("numpad-config-choose-category").ToString();
                NavigatorSpeaker.Say($"{viewName}. {profileIndicator}. {help}", true);
            }
            else
            {
                List<string> items = GetCurrentItems();
                string countAnnouncement = ModEntry.Helper.Translation.Get("numpad-config-item-count", new { count = items.Count }).ToString();
                int idx = _currentLevel == MenuLevel.Level2 ? _detailIndex : _actionIndex;

                if (items.Count > 0 && idx >= 0 && idx < items.Count)
                {
                    NavigatorSpeaker.Say($"{viewName}. {countAnnouncement}. {items[idx]}", true);
                }
                else
                {
                    NavigatorSpeaker.Say($"{viewName}. " + ModEntry.Helper.Translation.Get("numpad-config-empty").ToString(), true);
                }
            }
        }

        private void AnnounceCurrentSelection()
        {
            List<string> items = GetCurrentItems();
            int idx = _currentLevel switch
            {
                MenuLevel.Level1 => _categoryIndex,
                MenuLevel.Level2 => _detailIndex,
                MenuLevel.Level3 => _actionIndex,
                _ => 0
            };

            if (idx >= 0 && idx < items.Count)
            {
                NavigatorSpeaker.Say(items[idx], true);
            }
        }
    }
}
