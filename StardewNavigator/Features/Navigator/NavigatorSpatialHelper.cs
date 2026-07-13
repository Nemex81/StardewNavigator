using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Helper statico che incapsula le logiche spaziali, geometriche e di collisione.
    /// Estratto da Navigator.cs per garantire la separazione delle responsabilità.
    /// </summary>
    internal static class NavigatorSpatialHelper
    {
        public static int GetMapWidth(GameLocation location)
        {
            try
            {
                if (location?.Map?.Layers?.Count > 0)
                    return location.Map.Layers[0].LayerWidth;
            }
            catch { }

            return location?.Name switch
            {
                "Farm" => 80,
                "BusStop" => 35,
                "Town" => 120,
                "Beach" => 104,
                "Mountain" => 135,
                "Forest" => 120,
                "Backwoods" => 50,
                "Woods" => 60,
                "Railroad" => 70,
                _ => -1
            };
        }

        public static int GetMapHeight(GameLocation location)
        {
            try
            {
                if (location?.Map?.Layers?.Count > 0)
                    return location.Map.Layers[0].LayerHeight;
            }
            catch { }

            return location?.Name switch
            {
                "Farm" => 80,
                "BusStop" => 30,
                "Town" => 110,
                "Beach" => 50,
                "Mountain" => 45,
                "Forest" => 120,
                "Backwoods" => 40,
                "Woods" => 32,
                "Railroad" => 65,
                _ => -1
            };
        }

        public static bool IsTileCollidingInGame(GameLocation location, int x, int y)
        {
            try
            {
                Rectangle pb = Game1.player.GetBoundingBox();
                Rectangle tb = new Rectangle(x * 64, y * 64, pb.Width, pb.Height);
                return location.isCollidingPosition(tb, Game1.viewport, true, 0, false, Game1.player);
            }
            catch { return true; }
        }

        public static Point GetNearestPassableTile(GameLocation location, Point targetTile, int mapW, int mapH)
        {
            if (!IsTileCollidingInGame(location, targetTile.X, targetTile.Y))
                return targetTile;

            Point[] offsets =
            {
                new Point(0,1), new Point(0,-1),
                new Point(1,0), new Point(-1,0),
                new Point(1,1), new Point(-1,1),
                new Point(1,-1), new Point(-1,-1)
            };

            for (int r = 1; r <= 3; r++)
            {
                foreach (Point o in offsets)
                {
                    int nx = targetTile.X + o.X * r;
                    int ny = targetTile.Y + o.Y * r;

                    if (nx < 0 || ny < 0) continue;
                    if (mapW > 0 && nx >= mapW) continue;
                    if (mapH > 0 && ny >= mapH) continue;

                    if (!IsTileCollidingInGame(location, nx, ny))
                    {
                        Log.Debug($"[Nav] Tile warp ({targetTile.X},{targetTile.Y}) bloccato. Fallback: ({nx},{ny})");
                        return new Point(nx, ny);
                    }
                }
            }

            Log.Warn($"[Nav] Nessun tile libero vicino a ({targetTile.X},{targetTile.Y}).");
            return targetTile;
        }
    }
}
