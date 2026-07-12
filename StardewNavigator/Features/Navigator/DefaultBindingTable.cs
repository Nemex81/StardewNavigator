using System.Collections.Generic;
using StardewModdingAPI;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Declarative representation of the current default numpad binding set.
    /// Maps each physical <see cref="InputChord"/> to its logical <see cref="NumpadActionId"/>,
    /// as defined by the current behaviour of <c>NumpadController.HandleButton</c>.
    ///
    /// This table is NOT yet used to drive dispatch. Its purpose in this phase is:
    ///   1. Make the binding set machine-readable and auditable.
    ///   2. Act as the future basis for the <c>BlindProfile</c> preset.
    ///   3. Detect drifts between code and documentation (compare against NUMPAD_MAP.md).
    ///
    /// Special cases EXCLUDED from this table (handled as explicit logic in the controller):
    ///   • <c>Decimal</c> → AliasEnter  (pre-guard, fires in ALL contexts without exception)
    ///   • <c>NumPad8</c> in NavigatorMenu → NavMenuCursorUp   (context-gated override)
    ///   • <c>NumPad2</c> in NavigatorMenu → NavMenuCursorDown  (context-gated override)
    ///   • <c>Ctrl+NumPad5</c> in NavigatorMenu → NavMenuConfirm (context-gated override)
    ///   • <c>Ctrl+NumPad5</c> in any other menu → AliasCtrlEnter (context-gated override)
    ///   • <c>Ctrl+NumPad8/2/4/6</c> → MicroMove* (returns false, does not suppress input)
    /// </summary>
    public static class DefaultBindingTable
    {
        private static readonly NumpadBinding[] _bindings =
        {
            // ── Layer Base (no modifier) ────────────────────────────────────────────
            new(SButton.NumPad8,  NumpadActionId.GridMoveUp),
            new(SButton.NumPad2,  NumpadActionId.GridMoveDown),
            new(SButton.NumPad4,  NumpadActionId.GridMoveLeft),
            new(SButton.NumPad6,  NumpadActionId.GridMoveRight),
            new(SButton.NumPad1,  NumpadActionId.UseTool),
            new(SButton.NumPad3,  NumpadActionId.Interact),
            new(SButton.NumPad5,  NumpadActionId.ReadTileFacing),
            new(SButton.NumPad7,  NumpadActionId.SlotPrevious),
            new(SButton.NumPad9,  NumpadActionId.SlotNext),
            new(SButton.NumPad0,  NumpadActionId.ReadCoords),
            new(SButton.Multiply, NumpadActionId.OpenInventory),
            new(SButton.Divide,   NumpadActionId.OpenNavigatorMenu),
            new(SButton.Add,      NumpadActionId.ScannerObjectGroupUp),
            new(SButton.Subtract, NumpadActionId.ScannerObjectGroupDown),

            // ── Layer Ctrl ──────────────────────────────────────────────────────────
            // NOTE: Ctrl+NumPad8/2/4/6 (MicroMove*) are excluded — they return false
            //       (do not suppress input) and require special dispatch handling.
            // NOTE: Ctrl+NumPad5 maps to AutoWalkToObject in world context only;
            //       in any open menu it overrides to AliasCtrlEnter (context-gated, not in table).
            new(SButton.NumPad5,  ModifierFlags.LeftCtrl, NumpadActionId.AutoWalkToObject),
            new(SButton.NumPad9,  ModifierFlags.LeftCtrl, NumpadActionId.CancelNavigation),
            new(SButton.NumPad0,  ModifierFlags.LeftCtrl, NumpadActionId.AliasCtrlEnter),
            new(SButton.Add,      ModifierFlags.LeftCtrl, NumpadActionId.ScannerCategoryUp),
            new(SButton.Subtract, ModifierFlags.LeftCtrl, NumpadActionId.ScannerCategoryDown),

            // ── Layer Alt ───────────────────────────────────────────────────────────
            new(SButton.NumPad8, ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveUp),
            new(SButton.NumPad2, ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveDown),
            new(SButton.NumPad4, ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveLeft),
            new(SButton.NumPad6, ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveRight),
            // Arrow key aliases for TileViewer cursor (same action, different physical keys)
            new(SButton.Up,    ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveUp),
            new(SButton.Down,  ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveDown),
            new(SButton.Left,  ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveLeft),
            new(SButton.Right, ModifierFlags.LeftAlt, NumpadActionId.TileViewerMoveRight),
            new(SButton.NumPad3, ModifierFlags.LeftAlt, NumpadActionId.ReadTileStanding),
            new(SButton.NumPad5, ModifierFlags.LeftAlt, NumpadActionId.ReadCoords),
            new(SButton.NumPad7, ModifierFlags.LeftAlt, NumpadActionId.ReadCurrentItem),
            new(SButton.NumPad9, ModifierFlags.LeftAlt, NumpadActionId.ReadNavStatus),
            new(SButton.NumPad0, ModifierFlags.LeftAlt, NumpadActionId.ReadHealthStamina),

            // ── Layer Shift ─────────────────────────────────────────────────────────
            // Only Add/Subtract have Shift bindings; numerics are excluded per NVDA conflict rules.
            // See docs/input-management.md §1.B.
            new(SButton.Add,      ModifierFlags.LeftShift, NumpadActionId.ScannerInGroupUp),
            new(SButton.Subtract, ModifierFlags.LeftShift, NumpadActionId.ScannerInGroupDown),
        };

        /// <summary>All default bindings as a flat ordered list.</summary>
        public static IReadOnlyList<NumpadBinding> Bindings => _bindings;

        private static readonly Dictionary<InputChord, NumpadActionId> _lookup = BuildLookup();

        private static Dictionary<InputChord, NumpadActionId> BuildLookup()
        {
            var dict = new Dictionary<InputChord, NumpadActionId>(_bindings.Length);
            foreach (var b in _bindings)
                dict[b.Chord] = b.ActionId;
            return dict;
        }

        /// <summary>
        /// Returns the action associated with <paramref name="chord"/> in the default binding set,
        /// or <c>false</c> if the chord has no default binding.
        /// Not yet used by dispatch; available for future Phase 2 integration.
        /// </summary>
        public static bool TryGetAction(InputChord chord, out NumpadActionId actionId)
            => _lookup.TryGetValue(chord, out actionId);
    }
}
