using System;
using StardewModdingAPI;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Represents a physical key combination: a primary button plus any active modifier keys.
    /// Designed for use as a dictionary key in <see cref="DefaultBindingTable"/>.
    ///
    /// Equality is based on both <see cref="Key"/> and <see cref="Modifiers"/>,
    /// so <c>NumPad5</c> and <c>Ctrl+NumPad5</c> are distinct chords.
    /// </summary>
    public readonly struct InputChord : IEquatable<InputChord>
    {
        /// <summary>The primary button of this chord.</summary>
        public readonly SButton Key;

        /// <summary>Active modifier keys at the time of the key press.</summary>
        public readonly ModifierFlags Modifiers;

        public InputChord(SButton key, ModifierFlags modifiers = ModifierFlags.None)
        {
            Key       = key;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Creates an <see cref="InputChord"/> from a pressed button and the current
        /// modifier state read from SMAPI's input helper.
        ///
        /// Only <c>LeftAlt</c> is recognised as the Alt modifier.
        /// <c>RightAlt</c> and <c>AltGr</c> are intentionally ignored to avoid
        /// conflicts on international keyboards (see <c>docs/input-management.md</c> §1.A).
        /// </summary>
        public static InputChord FromCurrentInput(SButton key, IModHelper helper)
        {
            var modifiers = ModifierFlags.None;

            if (helper.Input.IsDown(SButton.LeftControl) || helper.Input.IsDown(SButton.RightControl))
                modifiers |= ModifierFlags.LeftCtrl;

            // RightAlt / AltGr excluded intentionally — see docs/input-management.md §1.A
            if (helper.Input.IsDown(SButton.LeftAlt))
                modifiers |= ModifierFlags.LeftAlt;

            if (helper.Input.IsDown(SButton.LeftShift) || helper.Input.IsDown(SButton.RightShift))
                modifiers |= ModifierFlags.LeftShift;

            return new InputChord(key, modifiers);
        }

        public bool Equals(InputChord other) => Key == other.Key && Modifiers == other.Modifiers;
        public override bool Equals(object? obj) => obj is InputChord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)Key, (int)Modifiers);

        public static bool operator ==(InputChord left, InputChord right) => left.Equals(right);
        public static bool operator !=(InputChord left, InputChord right) => !left.Equals(right);

        public override string ToString() => Modifiers == ModifierFlags.None
            ? Key.ToString()
            : $"{Modifiers}+{Key}";
    }

    /// <summary>
    /// Modifier keys recognised by the numpad subsystem.
    /// <c>RightAlt</c> is deliberately absent to prevent AltGr conflicts on
    /// international keyboards (see <c>docs/input-management.md</c> §1.A).
    /// </summary>
    [Flags]
    public enum ModifierFlags
    {
        None      = 0,
        LeftCtrl  = 1,
        LeftAlt   = 2,
        LeftShift = 4,
    }
}
