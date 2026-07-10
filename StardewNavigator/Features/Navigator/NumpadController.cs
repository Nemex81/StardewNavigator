using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewNavigator.Features.Navigator
{
    public static class NumpadController
    {
        private static SButton _activeDirectionKey = SButton.None;
        private static int _activeDirection = -1;
        private static long _lastStepTick = 0;
        private static long _keyPressStartTick = 0;
        private static bool _useFallbackMovement = false;

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

        private static object? GetGridMovementInstance()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "stardew-access");
                if (assembly == null) return null;

                var type = assembly.GetType("stardew_access.Features.GridMovement");
                if (type == null) return null;

                var prop = type.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                return prop?.GetValue(null);
            }
            catch { return null; }
        }

        private static bool TryDelegateGridMovement(int direction, SButton sButton)
        {
            if (_useFallbackMovement) return false;

            var instance = GetGridMovementInstance();
            if (instance == null) return false;

            try
            {
                var method = instance.GetType().GetMethod("HandleGridMovement", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method == null)
                {
                    Log.Warn("[StardewNavigator] Metodo HandleGridMovement non trovato su stardew-access GridMovement. Attivo fallback locale.");
                    _useFallbackMovement = true;
                    return false;
                }

                var key = (Microsoft.Xna.Framework.Input.Keys)sButton;
                var inputButton = new StardewValley.InputButton(key);

                method.Invoke(instance, new object[] { direction, inputButton });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewNavigator] Errore nell'invocazione di HandleGridMovement: {ex.Message}. Attivo fallback locale.");
                _useFallbackMovement = true;
                return false;
            }
        }

        public static void OnUpdateTicked(UpdateTickedEventArgs e)
        {
            if (!ModEntry.Config.NumpadControlsActive) return;
            if (!IsNumLockActive()) return;
            if (!Context.IsWorldReady) return;

            // 2. Gestione Repeat Cursore (LeftShift)
            if (_activeCursorKey != SButton.None)
            {
                bool shiftPressed = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
                if (!Context.IsPlayerFree || !ModEntry.Helper.Input.IsDown(_activeCursorKey) || !shiftPressed)
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

            // Se viene premuto Ctrl o Shift, interrompiamo il movimento continuo locale a griglia
            bool ctrlPressedNow = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressedNow = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
            if (ctrlPressedNow || shiftPressedNow)
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

            // Se viene premuto un tasto scanner matematico, verifica la presenza dell'Object Tracker di stardew-access.
            // Se non è installata, lasciamo passare il tasto al gioco.
            bool isScannerKey = e.Button == SButton.Divide || e.Button == SButton.Multiply || 
                                e.Button == SButton.Add || e.Button == SButton.Subtract;
            if (isScannerKey && GetObjectTrackerInstance() == null)
            {
                return false;
            }

            bool ctrlPressed = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressed = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);
            bool altPressed = ModEntry.Helper.Input.IsDown(SButton.LeftAlt) || ModEntry.Helper.Input.IsDown(SButton.RightAlt);
            bool isPlayerFree = Context.IsPlayerFree;
            bool isInMenuBuilder = IsInMenuBuilderViewport();

            // Blocca l'intercettazione se il giocatore non è libero (nei menu/chat/cutscene),
            // a meno che non si stia usando Ctrl o Shift (esplorazione) all'interno di un menu di costruzione (es. CarpenterMenu).
            if (!isPlayerFree && !(isInMenuBuilder && (ctrlPressed || shiftPressed)))
            {
                return false;
            }

            // 1. Comandi dello Scanner (Object Tracker) tramite tasti matematici
            if (e.Button == SButton.Divide)
            {
                if (ctrlPressed)
                {
                    TriggerObjectTrackerAction("MoveToCurrentlySelectedObject");
                }
                else
                {
                    TriggerObjectTrackerAction("ReadCurrentlySelectedObject", new object[] { false });
                }
                return true;
            }
            if (e.Button == SButton.Multiply)
            {
                TriggerObjectTrackerAction("ReadCurrentlySelectedObject", new object[] { true });
                return true;
            }
            if (e.Button == SButton.Add)
            {
                if (ctrlPressed)
                {
                    TriggerObjectTrackerCycle(1, back: true); // OBJECT_GROUP Up
                }
                else if (shiftPressed)
                {
                    TriggerObjectTrackerCycle(0, back: true); // CATEGORY Up
                }
                else
                {
                    TriggerObjectTrackerCycle(2, back: true); // IN_GROUP Up
                }
                return true;
            }
            if (e.Button == SButton.Subtract)
            {
                if (ctrlPressed)
                {
                    TriggerObjectTrackerCycle(1, back: false); // OBJECT_GROUP Down
                }
                else if (shiftPressed)
                {
                    TriggerObjectTrackerCycle(0, back: false); // CATEGORY Down
                }
                else
                {
                    TriggerObjectTrackerCycle(2, back: false); // IN_GROUP Down
                }
                return true;
            }

            // 2. Livello LeftShift (Cursore TileViewer ed esplorazione spaziale)
            if (shiftPressed)
            {
                if (e.Button == SButton.NumPad8 || e.Button == SButton.NumPad2 || e.Button == SButton.NumPad4 || e.Button == SButton.NumPad6)
                {
                    Vector2 delta = e.Button switch
                    {
                        SButton.NumPad8 => new Vector2(0, -64),
                        SButton.NumPad2 => new Vector2(0, 64),
                        SButton.NumPad4 => new Vector2(-64, 0),
                        SButton.NumPad6 => new Vector2(64, 0),
                        _ => Vector2.Zero
                    };

                    if (delta != Vector2.Zero)
                    {
                        _activeCursorKey = e.Button;
                        _activeCursorDelta = delta;
                        _lastCursorStepTick = Game1.ticks;
                        MoveTileViewerCursor(delta, false);
                        return true;
                    }
                }
                if (e.Button == SButton.NumPad5)
                {
                    ReadCoords(); // Shift + 5 = Leggi coordinate / posizione
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    ReadNavStatus(navigator); // Shift + 9 = Stato navigazione
                    return true;
                }
                if (e.Button == SButton.NumPad0)
                {
                    TriggerAutoWalk(); // Shift + 0 = Auto-Walk al cursore TileViewer
                    return true;
                }
            }

            // 3. Livello LeftCtrl (Micro-movimento preciso ed esplorazione fisica)
            else if (ctrlPressed)
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
                    ReadTile(true); // Ctrl + 5 = Leggi tile sotto i piedi
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    CancelNav(navigator); // Ctrl + 9 = Annulla navigazione
                    return true;
                }
            }

            // 4. Livello LeftAlt (Stato vitale del personaggio)
            else if (altPressed)
            {
                if (e.Button == SButton.NumPad5)
                {
                    ReadHealthAndStamina(); // Alt + 5 = Leggi salute / energia
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
                    // Numpad1 = usa attrezzo (X)
                    Game1.pressUseToolButton();
                    return true;
                }
                if (e.Button == SButton.NumPad3)
                {
                    // Numpad3 = azione / interazione (C)
                    Game1.pressActionButton(Game1.input.GetKeyboardState(), Game1.input.GetMouseState(), Game1.input.GetGamePadState());
                    return true;
                }
                if (e.Button == SButton.NumPad5)
                {
                    ReadTile(false); // Numpad5 = leggi tile di fronte
                    return true;
                }
                if (e.Button == SButton.NumPad7)
                {
                    ReadCoords(); // Numpad7 = leggi coordinate (K) come fallback o tasto base alternativo
                    return true;
                }
                if (e.Button == SButton.NumPad9)
                {
                    OpenMenu(registry, navigator, routeEngine); // Numpad9 = menu navigatore
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

            Vector2 targetTile = standing 
                ? player.Tile 
                : (player.GetToolLocation(ignoreClick: true) / 64f);
            
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
                        "1" => "Quercia",
                        "2" => "Acero",
                        "3" => "Pino",
                        "6" => "Albero di Mogano",
                        _ => "Albero"
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

        private static object? GetTileViewerInstance()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "stardew-access");
                if (assembly == null) return null;

                var tileViewerType = assembly.GetType("stardew_access.Features.TileViewer");
                if (tileViewerType == null) return null;

                var instanceProp = tileViewerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                return instanceProp?.GetValue(null);
            }
            catch { return null; }
        }

        private static void MoveTileViewerCursor(Vector2 delta, bool precise)
        {
            var tvInstance = GetTileViewerInstance();
            if (tvInstance == null) return;

            try
            {
                var method = tvInstance.GetType().GetMethod("CursorMoveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(tvInstance, new object[] { delta, precise });
            }
            catch { }
        }

        private static void TriggerAutoWalk()
        {
            var tvInstance = GetTileViewerInstance();
            if (tvInstance == null) return;

            try
            {
                var method = tvInstance.GetType().GetMethod("StartAutoWalking", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(tvInstance, null);
            }
            catch { }
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

        private static object? GetObjectTrackerInstance()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "stardew-access");
                if (assembly == null) return null;

                var trackerType = assembly.GetType("stardew_access.Features.ObjectTracker");
                if (trackerType == null) return null;

                var instanceProp = trackerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                return instanceProp?.GetValue(null);
            }
            catch { return null; }
        }

        private static void TriggerObjectTrackerCycle(int cycleTypeVal, bool back)
        {
            var otInstance = GetObjectTrackerInstance();
            if (otInstance == null) return;

            try
            {
                var cycleType = otInstance.GetType().GetNestedType("CycleType", System.Reflection.BindingFlags.NonPublic);
                if (cycleType == null) return;

                var cycleVal = Enum.ToObject(cycleType, cycleTypeVal);
                bool wrapAround = GetOTWrapLists();

                var method = otInstance.GetType().GetMethod("Cycle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(otInstance, new object[] { cycleVal, back, wrapAround });
            }
            catch { }
        }

        private static void TriggerObjectTrackerAction(string methodName, object[]? args = null)
        {
            var otInstance = GetObjectTrackerInstance();
            if (otInstance == null) return;

            try
            {
                var method = otInstance.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(otInstance, args);
            }
            catch { }
        }

        private static bool GetOTWrapLists()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "stardew-access");
                if (assembly == null) return false;

                var mainClassType = assembly.GetType("stardew_access.MainClass");
                if (mainClassType == null) return false;

                var configProp = mainClassType.GetProperty("Config", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    ?? (object?)mainClassType.GetField("Config", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic) as System.Reflection.MemberInfo;

                object? configObj = null;
                if (configProp is System.Reflection.PropertyInfo pInfo) configObj = pInfo.GetValue(null);
                else if (configProp is System.Reflection.FieldInfo fInfo) configObj = fInfo.GetValue(null);

                if (configObj == null) return false;
                var wrapListsProp = configObj.GetType().GetProperty("OTWrapLists");
                return (bool)(wrapListsProp?.GetValue(configObj) ?? false);
            }
            catch { return false; }
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
    }
}
