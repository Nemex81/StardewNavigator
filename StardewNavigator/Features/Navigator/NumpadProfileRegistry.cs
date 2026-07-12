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
        /// Sighted profile: contains only base movement, interaction, hotbar slots,
        /// inventory, navigator menu, cancel navigation, and navigation status feedback.
        /// Excludes all other screen-reader specific readouts, Object Tracker controls,
        /// and TileViewer movements.
        /// </summary>
        public static readonly NumpadProfile Sighted = new NumpadProfile(
            NumpadProfileId.Sighted,
            GetSightedBindings()
        );

        private static IEnumerable<NumpadBinding> GetSightedBindings()
        {
            // Filtro inclusivo (whitelist) basato sull'analisi architetturale approvata
            return DefaultBindingTable.Bindings.Where(b =>
                b.ActionId == NumpadActionId.GridMoveUp ||
                b.ActionId == NumpadActionId.GridMoveDown ||
                b.ActionId == NumpadActionId.GridMoveLeft ||
                b.ActionId == NumpadActionId.GridMoveRight ||
                b.ActionId == NumpadActionId.UseTool ||
                b.ActionId == NumpadActionId.Interact ||
                b.ActionId == NumpadActionId.SlotPrevious ||
                b.ActionId == NumpadActionId.SlotNext ||
                b.ActionId == NumpadActionId.OpenInventory ||
                b.ActionId == NumpadActionId.OpenNavigatorMenu ||
                b.ActionId == NumpadActionId.CancelNavigation ||
                b.ActionId == NumpadActionId.ReadNavStatus
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
