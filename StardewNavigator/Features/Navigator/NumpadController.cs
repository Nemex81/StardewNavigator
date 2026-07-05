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
        public static bool HandleButton(
            ButtonPressedEventArgs e, 
            Navigator navigator, 
            DestinationRegistry registry, 
            RouteEngine routeEngine)
        {
            if (!ModEntry.Config.NumpadControlsActive) return false;
            if (!Context.IsWorldReady) return false;

            // 1. Esplorazione tramite Ctrl + Numpad (se stardew-access è attiva)
            bool ctrlPressed = ModEntry.Helper.Input.IsDown(SButton.LeftControl) || ModEntry.Helper.Input.IsDown(SButton.RightControl);
            bool shiftPressed = ModEntry.Helper.Input.IsDown(SButton.LeftShift) || ModEntry.Helper.Input.IsDown(SButton.RightShift);

            if (ctrlPressed)
            {
                if (e.Button == SButton.NumPad8)
                {
                    MoveTileViewerCursor(new Vector2(0, shiftPressed ? -4 : -64), shiftPressed);
                    return true;
                }
                if (e.Button == SButton.NumPad2)
                {
                    MoveTileViewerCursor(new Vector2(0, shiftPressed ? 4 : 64), shiftPressed);
                    return true;
                }
                if (e.Button == SButton.NumPad4)
                {
                    MoveTileViewerCursor(new Vector2(shiftPressed ? -4 : -64, 0), shiftPressed);
                    return true;
                }
                if (e.Button == SButton.NumPad6)
                {
                    MoveTileViewerCursor(new Vector2(shiftPressed ? 4 : 64, 0), shiftPressed);
                    return true;
                }
                if (e.Button == SButton.NumPad0)
                {
                    TriggerAutoWalk();
                    return true;
                }
            }

            // 2. Comandi standard del Numpad (senza Ctrl)
            if (e.Button == SButton.NumPad8)
            {
                MoveGrid(0); // Up
                return true;
            }
            if (e.Button == SButton.NumPad6)
            {
                MoveGrid(1); // Right
                return true;
            }
            if (e.Button == SButton.NumPad2)
            {
                MoveGrid(2); // Down
                return true;
            }
            if (e.Button == SButton.NumPad4)
            {
                MoveGrid(3); // Left
                return true;
            }
            if (e.Button == SButton.NumPad5)
            {
                bool altPressed = ModEntry.Helper.Input.IsDown(SButton.LeftAlt) || ModEntry.Helper.Input.IsDown(SButton.RightAlt);
                ReadTile(altPressed);
                return true;
            }
            if (e.Button == SButton.NumPad7)
            {
                ReadCoords();
                return true;
            }
            if (e.Button == SButton.NumPad9)
            {
                OpenMenu(registry, navigator, routeEngine);
                return true;
            }
            if (e.Button == SButton.NumPad1)
            {
                ReadNavStatus(navigator);
                return true;
            }
            if (e.Button == SButton.NumPad3)
            {
                CancelNav(navigator);
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
                    return helper.Translation.Get("numpad-read-tile-fruit-tree", new { stage = fruitTree.growthStage.Value }).ToString();
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
    }
}
