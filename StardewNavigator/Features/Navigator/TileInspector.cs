using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Provides standalone tile inspection logic for the numpad fallback read-tile path.
    ///
    /// This class is used only when stardew-access is NOT loaded (or its TryReadTile returns false).
    /// It replicates a subset of stardew-access tile-reading behaviour so that the mod remains
    /// functional in a standalone install.
    ///
    /// Design notes:
    ///   • No state — all methods are pure functions over their arguments.
    ///   • No dependency on NumpadController state, tick loop, or input handling.
    ///   • Extracted from NumpadController.GetTileDescription (lines 501-625) and
    ///     NumpadController.GetFacingTile (lines 691-706) without behavioural changes.
    /// </summary>
    internal static class TileInspector
    {
        /// <summary>
        /// Returns a localised human-readable description of the tile at <paramref name="tile"/>
        /// in <paramref name="location"/>, probing the following layers in priority order:
        /// NPC/characters → objects → buildings → terrain features → resource clumps →
        /// warps/doors → water → collision state → free tile.
        /// </summary>
        public static string GetTileDescription(GameLocation location, Vector2 tile)
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

        /// <summary>
        /// Returns the tile coordinates of the tile directly in front of the player,
        /// based on their current facing direction.
        /// </summary>
        public static Vector2 GetFacingTile(Farmer player)
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
    }
}
