using System.Collections.Generic;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Represents a specific set of physical-to-logical bindings for the numpad.
    /// Parametrises chord lookup for different user profiles (Blind, Sighted).
    /// </summary>
    public class NumpadProfile
    {
        public NumpadProfileId Id { get; }
        
        private readonly Dictionary<InputChord, NumpadActionId> _lookup;

        public NumpadProfile(NumpadProfileId id, IEnumerable<NumpadBinding> bindings)
        {
            Id = id;
            _lookup = new Dictionary<InputChord, NumpadActionId>();
            foreach (var b in bindings)
            {
                _lookup[b.Chord] = b.ActionId;
            }
        }

        public bool TryGetAction(InputChord chord, out NumpadActionId actionId)
        {
            return _lookup.TryGetValue(chord, out actionId);
        }
    }
}
