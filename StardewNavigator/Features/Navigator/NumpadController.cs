using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;
using StardewNavigator.Integration;

namespace StardewNavigator.Features.Navigator
{
    public static class NumpadController
    {
        private static SButton _activeDirectionKey = SButton.None;
        private static int _activeDirection = -1;
        private static long _lastStepTick = 0;
        private static long _keyPressStartTick = 0;
        private static bool _useFallbackMovement = false;

        // Cooldown per l'uso dell'attrezzo (NumPad1): evita attivazioni multiple ravvicinate.
        // ~20 ticks = ~333ms a 60 FPS, pari alla durata di una singola oscillazione attrezzo.
        private static int _lastUseToolTick = 0;
        private const int UseToolCooldownTicks = 20;


        private static SButton _activeCursorKey = SButton.None;
        private static Vector2 _activeCursorDelta = Vector2.Zero;
        private static long _lastCursorStepTick = 0;

        public static void AddNumpadToGameOptions()
        {
            try
            {
                Game1.options.moveUpButton = AddKeyToOptionArray(Game1.options.moveUpButton, Microsoft.Xna.Framework.Input.Keys.NumPad8);
                Game1.options.moveRightButton = AddKeyToOptionArray(Game1.options.moveRightButton, Microsoft.Xna.Framework.Input.Keys.NumPad6);
                Game1.options.moveDownButton = AddKeyToOptionArray(Game1.options.moveDownButton, Microsoft.Xna.Framework.Input.Keys.NumPad2);
                Game1.options.moveLeftButton = AddKeyToOptionArray(Game1.options.moveLeftButton, Microsoft.Xna.Framework.Input.Keys.NumPad4);
            }
            catch (Exception ex)
            {
                Log.Error($"[StardewNavigator] Errore nell'aggiunta dei tasti Numpad alle opzioni di gioco: {ex.Message}");
            }
        }

        private static InputButton[] AddKeyToOptionArray(InputButton[] currentArray, Microsoft.Xna.Framework.Input.Keys key)
        {
            var button = new InputButton(key);
            if (currentArray == null)
            {
                return new InputButton[] { button };
            }
            if (currentArray.Contains(button)) return currentArray;

            var newArray = new InputButton[currentArray.Length + 1];
            currentArray.CopyTo(newArray, 0);
            newArray[currentArray.Length] = button;
            return newArray;
        }

        private static void ResetCursorMovement()
        {
            _activeCursorKey = SButton.None;
            _activeCursorDelta = Vector2.Zero;
            _lastCursorStepTick = 0;
        }

        private static bool TryDelegateGridMovement(int direction, SButton sButton)
        {
            if (_useFallbackMovement) return false;

            if (StardewAccessBridge.TryHandleGridMovement(direction, sButton))
            {
                return true;
            }

            Log.Warn("[StardewNavigator] Metodo HandleGridMovement non eseguito su stardew-access. Attivo fallback locale.");
            _useFallbackMovement = true;
            return false;
        }

        public static void OnUpdateTicked(UpdateTickedEventArgs e)
        {
            if (!ModEntry.Config.NumpadControlsActive) return;
            if (!IsNumLockActive()) return;
            if (!Context.IsWorldReady) return;

            // ── Soppressione proattiva Alt+Direzione ──────────────────────────────────────────
            // stardew-access carica prima di StardewNavigator, quindi il suo handler ButtonPressed
            // riceve l'evento prima del nostro. Per NumPad2/Down (movimento giù) questo causa
            // il movimento del personaggio prima che il nostro handler possa sopprimerlo.
            // Soluzione: soppressione ogni tick quando LeftAlt è premuto, PRIMA che ButtonPressed
            // venga distribuito. Questo blocca il tasto a livello di input state per tutti i mod.
            bool altDown = ModEntry.Helper.Input.IsDown(SButton.LeftAlt);
            if (altDown && Context.IsPlayerFree)
            {
                // Resetta lo stato di GridMovement di stardew-access per evitare che consideri
                // i tasti soppressi come ancora premuti (stardew-access controlla IsSuppressed).
                StardewAccessBridge.TryResetGridMovementState();

                // Per ogni direzione: se il tasto numpad O la freccia corrispondente è premuta,
                // sopprimi entrambi per bloccare il movimento prima che stardew-access li veda.
                if (ModEntry.Helper.Input.IsDown(SButton.NumPad2) || ModEntry.Helper.Input.IsDown(SButton.Down))
                {
                    ModEntry.Helper.Input.Suppress(SButton.NumPad2);
                    ModEntry.Helper.Input.Suppress(SButton.Down);
                }
                if (ModEntry.Helper.Input.IsDown(SButton.NumPad8) || ModEntry.Helper.Input.IsDown(SButton.Up))
                {
                    ModEntry.Helper.Input.Suppress(SButton.NumPad8);
                    ModEntry.Helper.Input.Suppress(SButton.Up);
                }
                if (ModEntry.Helper.Input.IsDown(SButton.NumPad4) || ModEntry.Helper.Input.IsDown(SButton.Left))
                {
                    ModEntry.Helper.Input.Suppress(SButton.NumPad4);
                    ModEntry.Helper.Input.Suppress(SButton.Left);
                }
                if (ModEntry.Helper.Input.IsDown(SButton.NumPad6) || ModEntry.Helper.Input.IsDown(SButton.Right))
                {
                    ModEntry.Helper.Input.Suppress(SButton.NumPad6);
                    ModEntry.Helper.Input.Suppress(SButton.Right);
                }
            }

            // 2. Gestione Repeat Cursore (LeftAlt)
            if (_activeCursorKey != SButton.None)
            {
                bool altPressed = ModEntry.Helper.Input.IsDown(SButton.LeftAlt);
                if (!Context.IsPlayerFree || !ModEntry.Helper.Input.IsDown(_activeCursorKey) || !altPressed)
                {
                    ResetCursorMovement();
                }
                else
                {
                    long currentTick = Game1.ticks;
                    if (currentTick - _lastCursorStepTick >= 12) // 200ms
                    {
                        MoveTileViewerCursor(_activeCursorDelta, false);
                        _lastCursorStepTick = currentTick;
                    }
                }
            }

            // 3. Gestione Movimento Griglia Base
            if (_activeDirectionKey == SButton.None) return;

            // Se il tasto non è più premuto, resettiamo
            if (!ModEntry.Helper.Input.IsDown(_activeDirectionKey))
            {
                ResetMovement();
                return;
            }

            // Se il giocatore non è più libero, resettiamo
            if (!Context.IsPlayerFree)
            {
                ResetMovement();
                return;
            }

            // Se viene premuto Ctrl, Shift o Alt, interrompiamo il movimento continuo locale a griglia
            bool ctrlPressedNow = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressedNow = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
            bool altPressedNow = ModEntry.Helper.Input.IsDown(SButton.LeftAlt);
            if (ctrlPressedNow || shiftPressedNow || altPressedNow)
            {
                ResetMovement();
                return;
            }

            long currentTickBase = Game1.ticks;
            long elapsedTicks = currentTickBase - _keyPressStartTick;

            // Ritardo iniziale per la ripetizione (circa 400ms = 24 tick a 60fps)
            const int initialDelayTicks = 24;
            if (elapsedTicks < initialDelayTicks)
            {
                return;
            }

            // Calcolo dell'intervallo di ripetizione dinamico basato sulla velocità del giocatore.
            float speed = Game1.player.getMovementSpeed();
            if (speed <= 0) speed = 4.6f;
            int repeatIntervalTicks = (int)Math.Max(5, Math.Round(55f / speed));

            if (currentTickBase - _lastStepTick >= repeatIntervalTicks)
            {
                MoveGrid(_activeDirection);
                _lastStepTick = currentTickBase;
            }
        }

        private static void StartContinuousMovement(SButton button, int direction, long currentTick)
        {
            _activeDirectionKey = button;
            _activeDirection = direction;
            _keyPressStartTick = currentTick;
            _lastStepTick = currentTick;

            // Eseguiamo il primo passo immediatamente
            MoveGrid(direction);
        }

        private static void ResetMovement()
        {
            _activeDirectionKey = SButton.None;
            _activeDirection = -1;
            _lastStepTick = 0;
            _keyPressStartTick = 0;
        }

        public static bool HandleButton(
            ButtonPressedEventArgs e,
            Navigator navigator,
            DestinationRegistry registry,
            RouteEngine routeEngine)
        {
            if (!ModEntry.Config.NumpadControlsActive) return false;

            // Intercetta solo se il NumLock è attivo fisicamente
            if (!IsNumLockActive()) return false;

            if (!Context.IsWorldReady) return false;

            bool ctrlPressed = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressed = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
            bool isPlayerFree = Context.IsPlayerFree;
            bool isInMenuBuilder = IsInMenuBuilderViewport();

            // ─── SPECIAL CASE: NumPad Decimal (.) = alias di Enter in qualsiasi contesto ─────────
            if (e.Button == SButton.Decimal)
            {
                NumpadActionCatalog.Execute(NumpadActionId.AliasEnter, new ActionContext());
                return true;
            }

            // ─── SPECIAL CASE: tasti numerici quando NavigatorMenu è aperto ──────────────────────
            if (Game1.activeClickableMenu is NavigatorMenu navMenu)
            {
                if (e.Button == SButton.NumPad8)
                {
                    navMenu.NumpadMoveCursor(-1); // Su nella lista
                    return true;
                }
                if (e.Button == SButton.NumPad2)
                {
                    navMenu.NumpadMoveCursor(1);  // Giù nella lista
                    return true;
                }
                if (ctrlPressed && e.Button == SButton.NumPad5)
                {
                    navMenu.NumpadConfirm(); // Conferma selezione e avvia percorso
                    return true;
                }
                return false;
            }

            // ─── SPECIAL CASE: Ctrl+NumPad5 = alias Ctrl+Enter in qualsiasi altro menu aperto ────
            if (!isPlayerFree && Game1.activeClickableMenu != null && ctrlPressed && e.Button == SButton.NumPad5)
            {
                NumpadActionCatalog.Execute(NumpadActionId.AliasCtrlEnter, new ActionContext());
                return true;
            }

            // ─── SPECIAL CASE: Ctrl+NumPad8/2/4/6 = micro-movimento ──────────────────────────────
            if (ctrlPressed && (e.Button == SButton.NumPad8 || e.Button == SButton.NumPad2 || e.Button == SButton.NumPad4 || e.Button == SButton.NumPad6))
            {
                if (navigator.IsActive)
                {
                    navigator.CancelNavigation("input movimento manuale");
                }
                return false;
            }

            // Blocca l'intercettazione se il giocatore non è libero (nei menu/chat/cutscene),
            // a meno che non si stia usando Ctrl o Shift all'interno di un menu di costruzione.
            if (!isPlayerFree && !(isInMenuBuilder && (ctrlPressed || shiftPressed)))
            {
                return false;
            }

            // Build a snapshot of the current game state for use by action handlers below.
            ActionContext context = BuildContext(navigator, registry, routeEngine);

            // Costruiamo l'InputChord corrente
            InputChord chord = InputChord.FromCurrentInput(e.Button, ModEntry.Helper);

            // Risolviamo l'azione dal profilo attivo corrente
            var profile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);
            if (NumpadProfileRegistry.TryResolveAction(chord, profile, ModEntry.Config.NumpadOverrides, out NumpadActionId actionId))
            {
                if (actionId == NumpadActionId.None)
                {
                    return IsNumpadKey(chord.Key);
                }
                // 1. UseTool (NumPad1): richiede cooldown check e aggiornamento ticks nel controller
                if (actionId == NumpadActionId.UseTool)
                {
                    int currentTick = Game1.ticks;
                    if (currentTick - _lastUseToolTick >= UseToolCooldownTicks)
                    {
                        _lastUseToolTick = currentTick;
                        NumpadActionCatalog.Execute(actionId, context);
                    }
                    return true;
                }

                // 2. GridMove (NumPad8/2/4/6 base)
                if (actionId == NumpadActionId.GridMoveUp || actionId == NumpadActionId.GridMoveDown ||
                    actionId == NumpadActionId.GridMoveLeft || actionId == NumpadActionId.GridMoveRight)
                {
                    if (navigator.IsActive)
                    {
                        navigator.CancelNavigation("input movimento manuale");
                    }

                    int dir = actionId switch
                    {
                        NumpadActionId.GridMoveUp => 0,
                        NumpadActionId.GridMoveRight => 1,
                        NumpadActionId.GridMoveDown => 2,
                        NumpadActionId.GridMoveLeft => 3,
                        _ => -1
                    };

                    if (dir >= 0)
                    {
                        if (TryDelegateGridMovement(dir, e.Button))
                        {
                            ResetMovement();
                            return true;
                        }

                        StartContinuousMovement(e.Button, dir, Game1.ticks);
                        return true;
                    }
                    return false;
                }

                // 3. TileViewerMove (Alt+NumPad8/2/4/6/frecce)
                if (actionId == NumpadActionId.TileViewerMoveUp || actionId == NumpadActionId.TileViewerMoveDown ||
                    actionId == NumpadActionId.TileViewerMoveLeft || actionId == NumpadActionId.TileViewerMoveRight)
                {
                    Vector2 delta = actionId switch
                    {
                        NumpadActionId.TileViewerMoveUp => new Vector2(0, -64),
                        NumpadActionId.TileViewerMoveDown => new Vector2(0, 64),
                        NumpadActionId.TileViewerMoveLeft => new Vector2(-64, 0),
                        NumpadActionId.TileViewerMoveRight => new Vector2(64, 0),
                        _ => Vector2.Zero
                    };

                    if (delta != Vector2.Zero)
                    {
                        StardewAccessBridge.TryResetGridMovementState();

                        _activeCursorKey = e.Button;
                        _activeCursorDelta = delta;
                        _lastCursorStepTick = Game1.ticks;
                        MoveTileViewerCursor(delta, false);

                        if (actionId == NumpadActionId.TileViewerMoveUp)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad8);
                            ModEntry.Helper.Input.Suppress(SButton.Up);
                        }
                        else if (actionId == NumpadActionId.TileViewerMoveDown)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad2);
                            ModEntry.Helper.Input.Suppress(SButton.Down);
                        }
                        else if (actionId == NumpadActionId.TileViewerMoveLeft)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad4);
                            ModEntry.Helper.Input.Suppress(SButton.Left);
                        }
                        else if (actionId == NumpadActionId.TileViewerMoveRight)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad6);
                            ModEntry.Helper.Input.Suppress(SButton.Right);
                        }

                        return true;
                    }
                    return false;
                }

                // 4. ReadTile (NumPad5 base e Alt+NumPad3)
                if (actionId == NumpadActionId.ReadTileFacing)
                {
                    ReadTile(false);
                    return true;
                }
                if (actionId == NumpadActionId.ReadTileStanding)
                {
                    ReadTile(true);
                    return true;
                }

                // 5. Azioni migrate standard
                NumpadActionCatalog.Execute(actionId, context);
                return true;
            }

            return false;
        }

        private static void MoveGrid(int direction)
        {
            if (!Context.IsPlayerFree) return;

            Farmer player = Game1.player;
            GameLocation location = Game1.currentLocation;
            if (player == null || location == null) return;

            // Giriamo il giocatore se guarda altrove
            if (player.FacingDirection != direction)
            {
                player.faceDirection(direction);
                Game1.playSound("dwop");
                return;
            }

            // Coordinate della casella target
            int targetX = (int)player.Tile.X;
            int targetY = (int)player.Tile.Y;
            switch (direction)
            {
                case 0: targetY--; break; // Up
                case 1: targetX++; break; // Right
                case 2: targetY++; break; // Down
                case 3: targetX--; break; // Left
            }

            // Collision check
            Rectangle pb = player.GetBoundingBox();
            Rectangle tb = new Rectangle(targetX * 64, targetY * 64, pb.Width, pb.Height);
            bool isColliding = location.isCollidingPosition(tb, Game1.viewport, true, 0, false, player);

            if (isColliding)
            {
                // Controllo porta o azione
                xTile.Dimensions.Location actionLoc = new xTile.Dimensions.Location(targetX * 64, targetY * 64);
                if (location.checkAction(actionLoc, Game1.viewport, player))
                {
                    return; // porta aperta o azione avviata
                }

                // Controllo warp diretto
                Rectangle position = new Rectangle(targetX * 64, targetY * 64, 64, 64);
                Warp warp = location.isCollidingWithWarpOrDoor(position, player);
                if (warp != null)
                {
                    Game1.playSound("doorOpen");
                    player.warpFarmer(warp);
                    return;
                }

                Game1.playSound("clank");
                return;
            }

            // Spostamento e allineamento
            player.Position = new Vector2(targetX * 64, targetY * 64);
            location.playTerrainSound(player.Tile);

            // Controllo warp sulla nuova posizione
            Rectangle newPosition = new Rectangle(targetX * 64, targetY * 64, 64, 64);
            Warp newWarp = location.isCollidingWithWarpOrDoor(newPosition, player);
            if (newWarp != null)
            {
                Game1.playSound("doorOpen");
                player.warpFarmer(newWarp);
            }
        }

        private static void ReadTile(bool standing)
        {
            if (!Context.IsPlayerFree) return;

            Farmer player = Game1.player;
            GameLocation location = Game1.currentLocation;
            if (player == null || location == null) return;

            // Ramo 1: stardew-access presente -> delega alla feature nativa.
            if (StardewAccessBridge.TryReadTile(standing)) return;

            // Ramo 2: fallback standalone -> replica la logica di stardew-access
            Vector2 targetTile = standing 
                ? player.Tile 
                : TileInspector.GetFacingTile(player);
            
            targetTile.X = (int)targetTile.X;
            targetTile.Y = (int)targetTile.Y;

            string description = TileInspector.GetTileDescription(location, targetTile);
            NavigatorSpeaker.Say(description, true);
        }


        /// <summary>
        /// Builds a snapshot of the relevant game state for the current key-press event.
        /// Called once per event, after the fast early-exit guards, before any dispatch logic.
        /// Centralises game-state interrogation so handlers do not repeat it individually.
        /// </summary>
        private static ActionContext BuildContext(Navigator navigator, DestinationRegistry registry, RouteEngine routeEngine)
        {
            var activeMenu = Game1.activeClickableMenu;
            var navMenu    = activeMenu as NavigatorMenu;

            return new ActionContext
            {
                IsPlayerFree             = Context.IsPlayerFree,
                IsInNavigatorMenu        = navMenu != null,
                IsInOtherMenu            = activeMenu != null && navMenu == null,
                IsInMenuBuilder          = IsInMenuBuilderViewport(),
                IsStardewAccessLoaded    = StardewAccessBridge.IsModLoaded,
                IsObjectTrackerAvailable = StardewAccessBridge.IsObjectTrackerAvailable(),
                ActiveNavigatorMenu      = navMenu,
                Navigator                = navigator,
                Registry                 = registry,
                RouteEngine              = routeEngine,
            };
        }

        // ─── Bridge Reflection per stardew-access ──────────────────────────────

        private static void MoveTileViewerCursor(Vector2 delta, bool precise)
        {
            StardewAccessBridge.TryMoveTileViewerCursor(delta, precise);
        }

        private static void TriggerAutoWalk()
        {
            StardewAccessBridge.TryStartAutoWalkToTileViewerCursor();
        }

        private static bool IsInMenuBuilderViewport()
        {
            return Game1.activeClickableMenu switch
            {
                StardewValley.Menus.CarpenterMenu => true,
                StardewValley.Menus.PurchaseAnimalsMenu => true,
                StardewValley.Menus.AnimalQueryMenu => true,
                _ => false
            };
        }

        private static bool IsNumLockActive()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return Console.NumberLock;
                }
                return true; // default a true su altre piattaforme (Linux/Mac)
            }
            catch
            {
                return true; // fallback sicuro se la piattaforma non lo supporta
            }
        }


        private static bool IsNumpadKey(SButton key)
        {
            return key >= SButton.NumPad0 && key <= SButton.NumPad9 ||
                   key == SButton.Add || key == SButton.Subtract ||
                   key == SButton.Multiply || key == SButton.Divide ||
                   key == SButton.Decimal;
        }
    }
}
