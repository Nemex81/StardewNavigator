using System.Collections.Generic;
using System.Linq;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Registry providing pre-defined binding profiles.
    /// </summary>
    public static class NumpadProfileRegistry
    {
        /// <summary>
        /// Blind profile: contains all bindings defined in DefaultBindingTable.
        /// </summary>
        public static readonly NumpadProfile Blind = new NumpadProfile(
            NumpadProfileId.Blind,
            DefaultBindingTable.Bindings
        );

        /// <summary>
        /// Sighted profile placeholder: contains only base movement, tool usage,
        /// interaction, toolbar, inventory, and navigator menu. Excludes all screen-reader
        /// reading keys and Object Tracker scanner shortcuts.
        /// </summary>
        public static readonly NumpadProfile Sighted = new NumpadProfile(
            NumpadProfileId.Sighted,
            GetSightedBindings()
        );

        private static IEnumerable<NumpadBinding> GetSightedBindings()
        {
            // Filtriamo i binding di DefaultBindingTable per escludere quelli specifici per ciechi.
            // Riteniamo specifici per non vedenti (SR-only) i binding del visualizzatore cursori, coordinate,
            // letture di stato e scanner dell'Object Tracker.
            return DefaultBindingTable.Bindings.Where(b => 
                b.ActionId != NumpadActionId.ReadCoords &&
                b.ActionId != NumpadActionId.ReadTileFacing &&
                b.ActionId != NumpadActionId.ReadTileStanding &&
                b.ActionId != NumpadActionId.ReadCurrentItem &&
                b.ActionId != NumpadActionId.ReadNavStatus &&
                b.ActionId != NumpadActionId.ReadHealthStamina &&
                b.ActionId != NumpadActionId.ScannerObjectGroupUp &&
                b.ActionId != NumpadActionId.ScannerObjectGroupDown &&
                b.ActionId != NumpadActionId.ScannerCategoryUp &&
                b.ActionId != NumpadActionId.ScannerCategoryDown &&
                b.ActionId != NumpadActionId.ScannerInGroupUp &&
                b.ActionId != NumpadActionId.ScannerInGroupDown &&
                b.ActionId != NumpadActionId.TileViewerMoveUp &&
                b.ActionId != NumpadActionId.TileViewerMoveDown &&
                b.ActionId != NumpadActionId.TileViewerMoveLeft &&
                b.ActionId != NumpadActionId.TileViewerMoveRight &&
                b.ActionId != NumpadActionId.AutoWalkToObject
            );
        }

        /// <summary>
        /// Resolves a profile instance from its identifier.
        /// </summary>
        public static NumpadProfile GetProfile(NumpadProfileId id)
        {
            return id switch
            {
                NumpadProfileId.Blind => Blind,
                NumpadProfileId.Sighted => Sighted,
                _ => Blind
            };
        }
    }
}
