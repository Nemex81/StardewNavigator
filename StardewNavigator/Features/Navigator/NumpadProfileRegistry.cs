using System.Collections.Generic;

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
        /// Sighted profile placeholder: currently initialized as a copy of the default binding set.
        /// It serves as a structural placeholder to prevent runtime errors and unexpected behavior,
        /// introducing no real behavioral difference until its product semantics are defined.
        /// </summary>
        public static readonly NumpadProfile Sighted = new NumpadProfile(
            NumpadProfileId.Sighted,
            DefaultBindingTable.Bindings
        );

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
