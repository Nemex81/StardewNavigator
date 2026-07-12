namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Identifies each logical action that the numpad subsystem can perform.
    /// This enum is the semantic vocabulary of the numpad — independent of physical key assignments.
    ///
    /// Adding a new action here is the first step to extending the system.
    /// Each value must have a corresponding handler in <see cref="NumpadActionCatalog"/>
    /// before it can be used from dispatch code.
    /// </summary>
    public enum NumpadActionId
    {
        // ── Special value for unmapped / disabled keys ───────────────────────────
        None = 0,

        // ── Grid movement (delegated to stardew-access, with local fallback) ─────
        GridMoveUp,
        GridMoveDown,
        GridMoveLeft,
        GridMoveRight,

        // ── Micro-movement (pixel-level, bypasses stardew-access grid snap) ───────
        MicroMoveUp,
        MicroMoveDown,
        MicroMoveLeft,
        MicroMoveRight,

        // ── TileViewer cursor (stardew-access only) ───────────────────────────────
        TileViewerMoveUp,
        TileViewerMoveDown,
        TileViewerMoveLeft,
        TileViewerMoveRight,

        // ── Direct interaction ────────────────────────────────────────────────────
        UseTool,
        Interact,

        // ── Reading / inspection ──────────────────────────────────────────────────
        ReadTileFacing,
        ReadTileStanding,
        ReadCoords,
        ReadHealthStamina,
        ReadCurrentItem,
        ReadNavStatus,

        // ── Hotbar management ─────────────────────────────────────────────────────
        SlotPrevious,
        SlotNext,

        // ── Menu shortcuts ────────────────────────────────────────────────────────
        OpenInventory,
        OpenNavigatorMenu,

        // ── Navigation control ────────────────────────────────────────────────────
        CancelNavigation,
        AutoWalkToObject,

        // ── Input simulation aliases ──────────────────────────────────────────────
        AliasEnter,
        AliasCtrlEnter,

        // ── Object Tracker scanner (stardew-access only) ──────────────────────────
        ScannerObjectGroupUp,
        ScannerObjectGroupDown,
        ScannerCategoryUp,
        ScannerCategoryDown,
        ScannerInGroupUp,
        ScannerInGroupDown,

        // ── Navigator Menu context-specific ───────────────────────────────────────
        // These are dispatched explicitly by HandleButton when NavigatorMenu is open.
        // They are present in the catalogue for completeness but not yet reached
        // via the binding table (see DefaultBindingTable notes on special cases).
        NavMenuCursorUp,
        NavMenuCursorDown,
        NavMenuConfirm,
    }
}
