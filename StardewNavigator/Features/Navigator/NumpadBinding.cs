using StardewModdingAPI;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Associates a physical key combination with a logical numpad action.
    /// The primitive building block of a binding profile.
    ///
    /// A collection of <see cref="NumpadBinding"/> records defines a complete
    /// binding set (such as <see cref="DefaultBindingTable"/>).
    /// </summary>
    public readonly struct NumpadBinding
    {
        /// <summary>The physical key combination that triggers the action.</summary>
        public readonly InputChord Chord;

        /// <summary>The logical action to execute when the chord is pressed.</summary>
        public readonly NumpadActionId ActionId;

        public NumpadBinding(InputChord chord, NumpadActionId actionId)
        {
            Chord    = chord;
            ActionId = actionId;
        }

        /// <summary>Convenience constructor: key + explicit modifiers + action.</summary>
        public NumpadBinding(SButton key, ModifierFlags modifiers, NumpadActionId actionId)
            : this(new InputChord(key, modifiers), actionId) { }

        /// <summary>Convenience constructor: unmodified key + action.</summary>
        public NumpadBinding(SButton key, NumpadActionId actionId)
            : this(new InputChord(key), actionId) { }

        public override string ToString() => $"{Chord} → {ActionId}";
    }
}
