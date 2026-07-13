using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Provides the fallback grid-movement step used when stardew-access is unavailable
    /// or its HandleGridMovement call returns false.
    ///
    /// Responsible for a single tile step in a given cardinal direction:
    ///   • facing correction with audio feedback;
    ///   • collision detection;
    ///   • door/action interaction on blocked tiles;
    ///   • warp handling on both blocked and free destination tiles;
    ///   • position update and terrain sound on free tiles.
    ///
    /// Design notes:
    ///   • No state — all reads come from Game1.player / Game1.currentLocation.
    ///   • No dependency on NumpadController state (tick loop, repeat timers, etc.).
    ///   • Single entry point: <see cref="MoveGrid"/>, called with a NSEW int direction:
    ///     0 = Up, 1 = Right, 2 = Down, 3 = Left.
    ///   • Extracted verbatim from NumpadController.MoveGrid (lines 408-475) without
    ///     behavioural changes.
    /// </summary>
    internal static class GridMovement
    {
        /// <summary>
        /// Executes one tile step in the specified cardinal direction.
        /// </summary>
        /// <param name="direction">0 = Up, 1 = Right, 2 = Down, 3 = Left.</param>
        public static void MoveGrid(int direction)
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
    }
}
