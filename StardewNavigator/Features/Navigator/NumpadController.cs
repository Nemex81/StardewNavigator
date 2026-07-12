using System;
using System.Collections.Generic;
using System.Linq;
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

            // Se viene premuto un tasto scanner (Add/Subtract), verifica la presenza dell'Object Tracker di stardew-access.
            // Divide e Multiply sono funzioni standalone (menu/inventario) e non richiedono stardew-access.
            bool isScannerKey = e.Button == SButton.Add || e.Button == SButton.Subtract;
            if (isScannerKey && !StardewAccessBridge.IsObjectTrackerAvailable())
            {
                return false;
            }

            bool ctrlPressed = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressed = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
            bool altPressed = ModEntry.Helper.Input.IsDown(SButton.LeftAlt);
            bool isPlayerFree = Context.IsPlayerFree;
            bool isInMenuBuilder = IsInMenuBuilderViewport();

            // ─── NumPad Decimal (.) = alias di Enter in qualsiasi contesto ──────────────────────
            // Inserito PRIMA di qualsiasi blocco menu/guard: funziona nel mondo, nell'inventario,
            // nel NavigatorMenu e in qualsiasi altro menu. Non richiede modificatori.
            // SButton.Decimal (110) = '.' del tastierino numerico; diverso da OemPeriod (190).
            if (e.Button == SButton.Decimal)
            {
                ModEntry.Helper.Input.Press(SButton.Enter); // '.' numpad ≡ Enter in tutti i contesti
                return true;
            }

            // ─── Gestione tasti numerici quando il NavigatorMenu di StardewNavigator è aperto ──
            // Questo blocco è eseguito PRIMA della guardia !isPlayerFree, poiché il menu
            // blocca Context.IsPlayerFree. Intercetta solo i tasti pertinenti al menu.
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
                return false; // altri tasti numpad non intercettati nel NavigatorMenu
            }

            // ─── LeftCtrl+NumPad5 come alias di LeftCtrl+Enter in qualsiasi altro menu aperto ──
            // Gestisce l'inventario (GameMenu) e altri menu generici: quando il giocatore non è
            // libero ma è in un menu, simula Enter così da confermare selezioni / interazioni,
            // replicando il comportamento di LeftCtrl+NumPad0 ma con il tasto 5 centrale.
            if (!isPlayerFree && Game1.activeClickableMenu != null && ctrlPressed && e.Button == SButton.NumPad5)
            {
                ModEntry.Helper.Input.Press(SButton.Enter); // Ctrl+5 ≡ Ctrl+Enter nel menu
                return true;
            }

            // Blocca l'intercettazione se il giocatore non è libero (nei menu/chat/cutscene),
            // a meno che non si stia usando Ctrl o Shift (esplorazione) all'interno di un menu di costruzione (es. CarpenterMenu).
            if (!isPlayerFree && !(isInMenuBuilder && (ctrlPressed || shiftPressed)))
            {
                return false;
            }

            // 1. Comandi del tastierino per Menu e Inventario (senza modificatori)
            if (e.Button == SButton.Divide && !ctrlPressed && !shiftPressed && !altPressed)
            {
                OpenMenu(registry, navigator, routeEngine); // NumPadDivide = open destinations menu (like G)
                return true;
            }
            if (e.Button == SButton.Multiply && !ctrlPressed && !shiftPressed && !altPressed)
            {
                Game1.activeClickableMenu = new StardewValley.Menus.GameMenu(); // NumPadMultiply = open inventory
                return true;
            }
            if (e.Button == SButton.Add)
            {
                if (ctrlPressed)
                {
                    StardewAccessBridge.TryCycleObjectTracker(0, back: true); // CATEGORY Up (corresponds to Ctrl + PageUp)
                }
                else if (shiftPressed)
                {
                    StardewAccessBridge.TryCycleObjectTracker(2, back: true); // IN_GROUP Up (corresponds to Shift + PageUp)
                }
                else
                {
                    StardewAccessBridge.TryCycleObjectTracker(1, back: true); // OBJECT_GROUP Up (corresponds to PageUp)
                }
                return true;
            }
            if (e.Button == SButton.Subtract)
            {
                if (ctrlPressed)
                {
                    StardewAccessBridge.TryCycleObjectTracker(0, back: false); // CATEGORY Down (corresponds to Ctrl + PageDown)
                }
                else if (shiftPressed)
                {
                    StardewAccessBridge.TryCycleObjectTracker(2, back: false); // IN_GROUP Down (corresponds to Shift + PageDown)
                }
                else
                {
                    StardewAccessBridge.TryCycleObjectTracker(1, back: false); // OBJECT_GROUP Down (corresponds to PageDown)
                }
                return true;
            }

            // 2. Livello LeftCtrl (Micro-movimento preciso ed esplorazione fisica)
            if (ctrlPressed)
            {
                if (e.Button == SButton.NumPad8 || e.Button == SButton.NumPad2 || e.Button == SButton.NumPad4 || e.Button == SButton.NumPad6)
                {
                    if (navigator.IsActive)
                    {
                        navigator.CancelNavigation("input movimento manuale");
                    }
                    return false; // Non sopprimiamo, lasciamo passare il tasto per il movimento nativo
                }
                if (e.Button == SButton.NumPad5)
                {
                    StardewAccessBridge.TryMoveToSelectedObject(); // Ctrl + 5 = Auto-Walk all'oggetto selezionato dell'Object Tracker (≡ LeftCtrl + Home)
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    CancelNav(navigator); // Ctrl + 9 = Annulla navigazione
                    return true;
                }
                if (e.Button == SButton.NumPad0)
                {
                    ModEntry.Helper.Input.Press(SButton.Enter); // Ctrl + 0 = Alias di LeftCtrl + Enter (Auto-Walk / Left Click)
                    return true;
                }
            }

            // 3. Livello LeftAlt (TileViewer, Coordinate, Stato Vitale, Oggetto Selezionato & Tile sotto i piedi)
            else if (altPressed)
            {
                if (e.Button == SButton.NumPad8 || e.Button == SButton.NumPad2 || e.Button == SButton.NumPad4 || e.Button == SButton.NumPad6 ||
                    e.Button == SButton.Up || e.Button == SButton.Down || e.Button == SButton.Left || e.Button == SButton.Right)
                {
                    Vector2 delta = e.Button switch
                    {
                        SButton.NumPad8 => new Vector2(0, -64),
                        SButton.Up => new Vector2(0, -64),
                        SButton.NumPad2 => new Vector2(0, 64),
                        SButton.Down => new Vector2(0, 64),
                        SButton.NumPad4 => new Vector2(-64, 0),
                        SButton.Left => new Vector2(-64, 0),
                        SButton.NumPad6 => new Vector2(64, 0),
                        SButton.Right => new Vector2(64, 0),
                        _ => Vector2.Zero
                    };

                    if (delta != Vector2.Zero)
                    {
                        // Rimuove immediatamente ogni stato di movimento a griglia pendente in stardew-access
                        // così che non si muova il personaggio basandosi sul tasto soppresso.
                        StardewAccessBridge.TryResetGridMovementState();

                        _activeCursorKey = e.Button;
                        _activeCursorDelta = delta;
                        _lastCursorStepTick = Game1.ticks;
                        MoveTileViewerCursor(delta, false);

                        // Supprimiamo sia il tasto fisico del tastierino che la freccia direzionale corrispondente
                        // per prevenire movimenti del personaggio dovuti alla traduzione degli input da parte del driver/OS.
                        if (e.Button == SButton.NumPad8 || e.Button == SButton.Up)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad8);
                            ModEntry.Helper.Input.Suppress(SButton.Up);
                        }
                        else if (e.Button == SButton.NumPad2 || e.Button == SButton.Down)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad2);
                            ModEntry.Helper.Input.Suppress(SButton.Down);
                        }
                        else if (e.Button == SButton.NumPad4 || e.Button == SButton.Left)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad4);
                            ModEntry.Helper.Input.Suppress(SButton.Left);
                        }
                        else if (e.Button == SButton.NumPad6 || e.Button == SButton.Right)
                        {
                            ModEntry.Helper.Input.Suppress(SButton.NumPad6);
                            ModEntry.Helper.Input.Suppress(SButton.Right);
                        }

                        return true;
                    }
                }
                if (e.Button == SButton.NumPad3)
                {
                    ReadTile(true); // Alt + 3 = Leggi tile sotto i piedi (≡ LeftAlt + J)
                    return true;
                }
                if (e.Button == SButton.NumPad5)
                {
                    ReadCoords(); // Alt + 5 = Leggi coordinate / posizione
                    return true;
                }
                if (e.Button == SButton.NumPad7)
                {
                    ReadCurrentItem(); // Alt + 7 = Leggi oggetto impugnato
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    ReadNavStatus(navigator); // Alt + 9 = Stato navigazione
                    return true;
                }
                if (e.Button == SButton.NumPad0)
                {
                    ReadHealthAndStamina(); // Alt + 0 = Leggi salute / energia
                    return true;
                }
            }

            // 5. Livello Base (Nessun modificatore)
            else
            {
                if (e.Button == SButton.NumPad8 || e.Button == SButton.NumPad6 || e.Button == SButton.NumPad2 || e.Button == SButton.NumPad4)
                {
                    if (navigator.IsActive)
                    {
                        navigator.CancelNavigation("input movimento manuale");
                    }

                    int dir = e.Button switch
                    {
                        SButton.NumPad8 => 0,
                        SButton.NumPad6 => 1,
                        SButton.NumPad2 => 2,
                        SButton.NumPad4 => 3,
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
                }

                if (e.Button == SButton.NumPad1)
                {
                    // NumPad1 = usa attrezzo (≡ X) — valido per tutti i giocatori,
                    // con e senza stardew-access, con e senza screen reader.
                    // Il cooldown (20 ticks ≈ 333ms a 60 FPS) impedisce attivazioni a raffica
                    // replicando la cadenza naturale di una singola oscillazione dell'attrezzo.
                    int currentTick = Game1.ticks;
                    if (currentTick - _lastUseToolTick >= UseToolCooldownTicks)
                    {
                        _lastUseToolTick = currentTick;
                        Game1.pressUseToolButton();
                    }
                    return true;
                }
                if (e.Button == SButton.NumPad3)
                {
                    // NumPad3 = azione / interazione (≡ tasto azione, default X)
                    // checkAction / checkForAction sono intercettati da stardew-access 1.7.0-beta.2
                    // e restituiscono false per i bauli. La strategia corretta è simulare la
                    // pressione del tasto azione configurato (X) tramite SMAPI Input.Press(),
                    // che lo inietta nello stato SMAPI. Al tick successivo, il loop nativo del gioco
                    // vedrà X come premuto, eseguirà pressActionButton con il keyState corretto,
                    // e tutta la pipeline di interazione (bauli, NPC, porte, oggetti) funzionerà.
                    // Simula il tasto azione nativo (default: X) tramite SMAPI Input.Press().
                    // Viene iniettato nello stato input; il loop nativo lo elabora al tick successivo
                    // con la pipeline corretta: bauli, NPC, porte, oggetti interattivi.
                    // Evita checkAction/checkForAction che vengono intercettati da stardew-access
                    // 1.7.0-beta.2 e restituiscono false per i bauli.
                    ModEntry.Helper.Input.Press(SButton.X);
                    return true;
                }
                if (e.Button == SButton.NumPad5)
                {
                    ReadTile(false); // Numpad5 = leggi tile di fronte
                    return true;
                }
                if (e.Button == SButton.NumPad7)
                {
                    // NumPad7 = slot precedente della hotbar attiva (wrapping circolare su 12 slot)
                    Game1.player.CurrentToolIndex = (Game1.player.CurrentToolIndex + 11) % 12;
                    return true;
                }
                if (e.Button == SButton.NumPad0)
                {
                    ReadCoords(); // NumPad0 = leggi coordinate (K)
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    // NumPad9 = slot successivo della hotbar attiva (wrapping circolare su 12 slot)
                    Game1.player.CurrentToolIndex = (Game1.player.CurrentToolIndex + 1) % 12;
                    return true;
                }
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
                : GetFacingTile(player);
            
            targetTile.X = (int)targetTile.X;
            targetTile.Y = (int)targetTile.Y;

            string description = GetTileDescription(location, targetTile);
            NavigatorSpeaker.Say(description, true);
        }

        private static string GetTileDescription(GameLocation location, Vector2 tile)
        {
            int x = (int)tile.X;
            int y = (int)tile.Y;
            var helper = ModEntry.Helper;

            // 1. Personaggi (NPC, Mostri)
            NPC? npc = location.characters.FirstOrDefault(c => c.Tile == tile);
            if (npc != null)
            {
                return npc.displayName;
            }

            // 2. Oggetti (foraggio, bauli, colture)
            if (location.objects.TryGetValue(tile, out var obj))
            {
                string objName = obj.DisplayName;
                if (location.terrainFeatures.TryGetValue(tile, out var tfCrop) && tfCrop is StardewValley.TerrainFeatures.HoeDirt hoeDirt && hoeDirt.crop != null)
                {
                    try
                    {
                        var cropObj = new StardewValley.Object(hoeDirt.crop.indexOfHarvest.Value, 1);
                        return $"{objName} " + helper.Translation.Get("numpad-read-tile-crop", new { crop = cropObj.DisplayName }).ToString();
                    }
                    catch { }
                }
                return objName;
            }

            // 3. Edifici
            var building = location.getBuildingAt(tile);
            if (building != null)
            {
                string bName = building.GetData()?.Name ?? building.buildingType.Value;
                return helper.Translation.Get("numpad-read-tile-building", new { name = bName }).ToString();
            }

            // 4. Elementi del terreno (Alberi, Erba, Terra zappata)
            if (location.terrainFeatures.TryGetValue(tile, out var tf))
            {
                if (tf is StardewValley.TerrainFeatures.Tree tree)
                {
                    string tName = tree.treeType.Value switch
                    {
                        "1" => "Oak",
                        "2" => "Maple",
                        "3" => "Pine",
                        "6" => "Mahogany",
                        _ => "Tree"
                    };
                    var trans = helper.Translation.Get($"numpad-read-tile-tree-type-{tree.treeType.Value}");
                    string resolvedTreeName = trans.HasValue() ? trans.ToString() : tName;
                    return helper.Translation.Get("numpad-read-tile-tree", new { tree_name = resolvedTreeName, stage = tree.growthStage.Value }).ToString();
                }
                if (tf is StardewValley.TerrainFeatures.FruitTree fruitTree)
                {
                    string ftName = fruitTree.GetDisplayName();
                    return helper.Translation.Get("numpad-read-tile-fruit-tree-with-name", new { name = ftName, stage = fruitTree.growthStage.Value }).ToString();
                }
                if (tf is StardewValley.TerrainFeatures.Grass)
                {
                    return helper.Translation.Get("numpad-read-tile-grass").ToString();
                }
                if (tf is StardewValley.TerrainFeatures.HoeDirt hd)
                {
                    if (hd.crop != null)
                    {
                        try
                        {
                            var cropObj = new StardewValley.Object(hd.crop.indexOfHarvest.Value, 1);
                            return helper.Translation.Get("numpad-read-tile-hoedirt").ToString() + " " + helper.Translation.Get("numpad-read-tile-crop", new { crop = cropObj.DisplayName }).ToString();
                        }
                        catch { }
                    }
                    return helper.Translation.Get("numpad-read-tile-hoedirt").ToString();
                }
                if (tf is StardewValley.TerrainFeatures.Flooring)
                {
                    return helper.Translation.Get("numpad-read-tile-flooring").ToString();
                }
            }

            // 5. Grandi risorse (Tronchi, Massi, Ceppi)
            foreach (var clump in location.resourceClumps)
            {
                Rectangle bounds = new Rectangle((int)clump.Tile.X * 64, (int)clump.Tile.Y * 64, clump.width.Value * 64, clump.height.Value * 64);
                if (bounds.Contains(x * 64 + 32, y * 64 + 32))
                {
                    string clumpType = clump.parentSheetIndex.Value switch
                    {
                        600 => helper.Translation.Get("numpad-read-tile-stump").ToString(),
                        602 => helper.Translation.Get("numpad-read-tile-trunk").ToString(),
                        672 => helper.Translation.Get("numpad-read-tile-boulder").ToString(),
                        _ => helper.Translation.Get("numpad-read-tile-clump").ToString()
                    };
                    return clumpType;
                }
            }

            // 6. Warp o Porte
            Rectangle position = new Rectangle(x * 64, y * 64, 64, 64);
            Warp warp = location.isCollidingWithWarpOrDoor(position, Game1.player);
            if (warp != null)
            {
                return helper.Translation.Get("numpad-read-tile-warp", new { target = warp.TargetName }).ToString();
            }

            // 7. Acqua
            if (location.isWaterTile(x, y))
            {
                return helper.Translation.Get("numpad-read-tile-water").ToString();
            }

            // 8. Stato di collisione base
            Rectangle playerBox = Game1.player.GetBoundingBox();
            Rectangle targetBox = new Rectangle(x * 64, y * 64, playerBox.Width, playerBox.Height);
            bool isColliding = location.isCollidingPosition(targetBox, Game1.viewport, true, 0, false, Game1.player);

            if (isColliding)
            {
                return helper.Translation.Get("numpad-nav-blocked").ToString();
            }

            return helper.Translation.Get("numpad-nav-free").ToString();
        }

        private static void ReadCoords()
        {
            if (!Context.IsWorldReady) return;

            Farmer player = Game1.player;
            GameLocation location = Game1.currentLocation;
            if (player == null || location == null) return;

            int x = (int)player.Tile.X;
            int y = (int)player.Tile.Y;
            string text = ModEntry.Helper.Translation.Get("numpad-coords", new { location_name = location.DisplayName, x = x, y = y }).ToString();
            NavigatorSpeaker.Say(text, true);
        }

        private static void OpenMenu(DestinationRegistry registry, Navigator navigator, RouteEngine routeEngine)
        {
            if (!Context.IsPlayerFree) return;

            Game1.activeClickableMenu = new NavigatorMenu(
                registry.Maps,
                (map, poi) => navigator.StartNavigation(map, poi, routeEngine),
                text => NavigatorSpeaker.Say(text, true)
            );
        }

        private static void ReadNavStatus(Navigator navigator)
        {
            if (navigator.IsActive)
            {
                string poi = navigator.PoiDisplayName;
                string map = navigator.MapDisplayName;
                int steps = navigator.RemainingSteps;
                string text = ModEntry.Helper.Translation.Get("numpad-nav-status-active", new { poi_name = poi, location_name = map, steps = steps }).ToString();
                NavigatorSpeaker.Say(text, true);
            }
            else
            {
                NavigatorSpeaker.Say(ModEntry.Helper.Translation.Get("numpad-nav-status-inactive").ToString(), true);
            }
        }

        private static void CancelNav(Navigator navigator)
        {
            if (navigator.IsActive)
            {
                navigator.CancelNavigation("Comando tastierino");
            }
            else
            {
                NavigatorSpeaker.Say(ModEntry.Helper.Translation.Get("numpad-nav-status-inactive").ToString(), true);
            }
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

        private static Vector2 GetFacingTile(Farmer player)
        {
            int x = player.GetBoundingBox().Center.X;
            int y = player.GetBoundingBox().Center.Y;
            const int offset = 64;

            switch (player.FacingDirection)
            {
                case 0: y -= offset; break; // Su
                case 1: x += offset; break; // Destra
                case 2: y += offset; break; // Giù
                case 3: x -= offset; break; // Sinistra
            }

            return new Vector2(x / 64, y / 64);
        }

        private static void ReadHealthAndStamina()
        {
            if (!Context.IsWorldReady) return;
            Farmer player = Game1.player;
            if (player == null) return;

            int healthPercent = player.maxHealth > 0 ? (int)Math.Round((double)player.health / player.maxHealth * 100) : 0;
            int staminaPercent = player.MaxStamina > 0 ? (int)Math.Round((double)player.Stamina / player.MaxStamina * 100) : 0;

            string text = ModEntry.Helper.Translation.Get("numpad-health-stamina", new { health = healthPercent, stamina = staminaPercent }).ToString();
            NavigatorSpeaker.Say(text, true);
        }

        private static void ReadCurrentItem()
        {
            if (!Context.IsWorldReady) return;
            Farmer player = Game1.player;
            if (player == null) return;

            Item? currentItem = player.CurrentItem;
            string text;
            if (currentItem == null)
            {
                text = ModEntry.Helper.Translation.Get("numpad-current-item-empty").ToString();
            }
            else
            {
                text = ModEntry.Helper.Translation.Get("numpad-current-item", new { item_name = currentItem.DisplayName }).ToString();
            }
            NavigatorSpeaker.Say(text, true);
        }
    }
}
