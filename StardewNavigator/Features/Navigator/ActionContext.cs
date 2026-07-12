namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Snapshot of the relevant game state at the moment a key-press event is processed.
    /// Built once per event by <c>NumpadController.BuildContext()</c> and passed to
    /// action handlers in <see cref="NumpadActionCatalog"/>.
    ///
    /// Separating game-state interrogation from handler logic avoids repeated queries
    /// and makes the context available to all handlers in a single object.
    ///
    /// Note: modifier keys (Ctrl/Alt/Shift) are intentionally excluded — they are input
    /// state, not game state, and are captured separately as local bool vars in HandleButton.
    /// </summary>
    internal sealed class ActionContext
    {
        /// <summary>True when the player is free (not in a menu, chat or cutscene).</summary>
        public bool IsPlayerFree { get; init; }

        /// <summary>True when the StardewNavigator <see cref="NavigatorMenu"/> is the active menu.</summary>
        public bool IsInNavigatorMenu { get; init; }

        /// <summary>True when any menu other than <see cref="NavigatorMenu"/> is active.</summary>
        public bool IsInOtherMenu { get; init; }

        /// <summary>
        /// True when a viewport-cursor menu is active (CarpenterMenu, PurchaseAnimalsMenu, AnimalQueryMenu).
        /// These menus allow Ctrl/Shift numpad input even when the player is not technically free.
        /// </summary>
        public bool IsInMenuBuilder { get; init; }

        /// <summary>True when stardew-access is loaded in the current game session.</summary>
        public bool IsStardewAccessLoaded { get; init; }

        /// <summary>True when the stardew-access ObjectTracker feature is available at runtime.</summary>
        public bool IsObjectTrackerAvailable { get; init; }

        /// <summary>The active <see cref="NavigatorMenu"/> instance, or <c>null</c> if not open.</summary>
        public NavigatorMenu? ActiveNavigatorMenu { get; init; }

        // ── Runtime dependencies for action handlers ───────────────────────────────
        // These are not "game state" but are included here to avoid threading them
        // as extra parameters through every Execute() call.

        /// <summary>Active Navigator state machine. Required by navigation-related handlers.</summary>
        public Navigator? Navigator { get; init; }

        /// <summary>Destination registry. Required by the OpenNavigatorMenu handler.</summary>
        public DestinationRegistry? Registry { get; init; }

        /// <summary>Route engine. Required by the OpenNavigatorMenu handler.</summary>
        public RouteEngine? RouteEngine { get; init; }
    }
}
