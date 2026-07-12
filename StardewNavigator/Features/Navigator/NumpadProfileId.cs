namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Identifiers for the available numpad binding profiles.
    /// </summary>
    public enum NumpadProfileId
    {
        /// <summary>
        /// Orientation profile for blind players relying heavily on screen-reader and auditory cues.
        /// Maps coordinates, tile inspection, TileViewer movement, etc.
        /// </summary>
        Blind,

        /// <summary>
        /// Orientation profile for sighted players who use visual HUD indicators.
        /// Minimised profile that reduces auditory spam.
        /// </summary>
        Sighted
    }
}
