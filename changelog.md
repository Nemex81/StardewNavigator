# Changelog - StardewNavigator

All notable changes to this project will be documented in this file.

## [1.2.7] - 2026-07-10

### Fixed
- **NumPad3 (interact/action) — chests and interactive objects**: replaced `pressActionButton` with a direct call to `location.checkAction()` on the tile in front of the player, mirroring the approach used by `stardew-access` in `GridMovement.cs`. This correctly triggers interactions with chests, NPCs, signs, machines, and all other tile-based interactive objects. A fallback to `pressActionButton` (using the raw XNA `GetKeyboardState()`) handles edge cases such as festival events and special doors.
- **NumPad1 (use tool) — rapid-fire on repeated presses**: with `stardew-access` installed, now simulates `SButton.X` via `ModEntry.Helper.Input.Press`, delegating rate limiting to the native XNA input loop (one event per frame). Without `stardew-access`, a cooldown guard of 20 ticks (~333 ms at 60 FPS) prevents multiple tool activations from rapid key presses.

---

## [1.2.6] - 2026-07-10


### Added
- **`LeftCtrl + NumPad5` in any open menu (e.g. inventory/GameMenu)**: now acts as an alias of `LeftCtrl + Enter`, confirming selections inside menus without requiring the main keyboard. This extends the context-aware behaviour of `LeftCtrl + NumPad5` to all generic `IClickableMenu` instances (not just the Navigator Menu).

---

## [1.2.5] - 2026-07-10


### Added
- **Numpad navigation inside Navigator Menu**: when the menu is open (via `G` or `/`), `NumPad8` moves the cursor to the previous entry and `NumPad2` moves it to the next entry. This allows full keyboard-numpad-only menu navigation without switching hands to the main keyboard.
- **`LeftCtrl + NumPad5` in Navigator Menu**: when the Navigator Menu is open, this combo confirms the selected destination/POI and starts the automated route — dual-use binding, context-aware (Object Tracker auto-walk when in world, menu confirm when menu is open).

---

## [1.2.4] - 2026-07-10

### Added
- **Object Tracker Auto-Walk**: Remapped `LeftCtrl + NumPad5` to invoke `MoveToCurrentlySelectedObject` on the `ObjectTracker` instance (equivalent to `LeftCtrl + Home`).
  - Separates it from the TileViewer cursor Auto-Walk (equivalent to `LeftCtrl + Enter` / `LeftCtrl + NumPad0`).

## [1.2.3] - 2026-07-10

### Added
- **LeftCtrl + NumPad0 Alias of LeftCtrl + Enter**: Reassigned `LeftCtrl + NumPad0` to virtually press `SButton.Enter` when triggered. When combined with the active `LeftCtrl` modifier, this acts as a direct alias of `LeftCtrl + Enter` in all contexts:
  - Triggers Auto-Walk to the TileViewer cursor in the world.
  - Simulates a Left Click in menus (such as chest items, menu buttons, and Carpenter building construction confirmation).

## [1.2.2] - 2026-07-10

### Added
- **Auto-Walk Mapping Optimization**: Reassigned `LeftCtrl + NumPad5` to trigger `stardew-access` Auto-Walk (equivalent to `LeftCtrl + Home` or `LeftCtrl + NumPad0`).
- **Redundancy Clean-Up**: Removed the redundant standing tile reading from `LeftCtrl + NumPad5`, leaving `LeftAlt + NumPad5` as the sole standing tile reader on the number 5 key (matching `LeftAlt + J` in stardew-access).

## [1.2.1] - 2026-07-10

### Added
- **ReadTile Reflection Bridge**: Implemented a reflection lookup to delegate `NumPad5` (facing tile) and `LeftAlt + NumPad5` (standing tile) directly to `stardew-access`'s native `ReadTile` feature when installed.
  - Matches the exact formatting and translation output of `J` and `LeftAlt + J`.
- **Improved Standalone Fallback**: Replaced mouse-sensitive tool location with bounding box directional math (mirroring `stardew-access` `FacingTile` property) for accurate standalone face-tile reading.
- **Alias Support**: Maintained `LeftCtrl + NumPad5` as an intentional alias of the standing tile reader for backwards compatibility.

### Fixed
- **Italian Hardcoded Fallbacks**: Replaced hardcoded Italian tree names (`Quercia`, `Acero`, etc.) in C# code with standard English fallbacks (`Oak`, `Maple`, etc.), ensuring proper non-Italian game play formatting when translation files are absent.
- **Vitals Redundancy**: Removed vitals narration from `LeftAlt + NumPad5` to align it to standing tile reading, leaving `LeftAlt + NumPad0` as the dedicated vital statistics command.

## [1.2.0] - 2026-07-10

### Added
- **Alt + NumPad7 / Alt + NumPad9 (Alias)**: Added a new dual-shortcut command that reads the currently selected/held item in the active hotbar slot.
  - Integration with `stardew-access` allows spoken TTS feedback.
  - Fallback temporary HUD message is shown on-screen if `stardew-access` is not loaded.
  - Explicit feedback is given if the active slot is empty.
- **NumPad7 / NumPad9 base**: Reassigned to cycle through hotbar slots (previous / next) with circular wrapping over 12 slots.
- **NumPad0 base**: Added coordinate reading (equivalent of `K`).
- **LeftCtrl + NumPad0**: Added `stardew-access` Auto-Walk trigger (equivalent of `LeftCtrl + Home`).
- **LeftAlt + NumPad0**: Added vitals (Health & Stamina) reading (equivalent of `H`).
- **NumPadMultiply (*)**: Added quick inventory opening (equivalent of `E`).
- **NumPadDivide (/)**: Added quick navigator destination menu opening (equivalent of `G`).
- **NUMPAD_MAP.md**: Created a comprehensive Numpad Key mapping documentation file detailing all modifier levels.

### Fixed
- **Scanner Key Guard**: Corrected the scanner key list in `NumpadController.cs` to exclude `Divide` and `Multiply`. Now, inventory and destination menu shortcuts work standalone without requiring `stardew-access`.
- **Scanner Direction Cycling**: Aligned `NumPad +` and `NumPad -` cycles to mimic `PageUp` and `PageDown` modifiers of `stardew-access` exactly (Unmodified -> Object Group, Ctrl -> Category, Shift -> In Group).
- **Movement Options Injection**: Patched option bindings dynamically on save load to prevent double-movement conflicts and ensure smooth native micro-movement using `LeftCtrl + Numpad`.
- **Carpenter Viewport Cursor**: Allowed cursor key event handling during menu construction layout planning.

## [1.1.0] - 2026-07-04

### Added
- **Numpad Controls**: Added comprehensive keypad-driven layout for manual movement, inspection, coordinates, menu, and scanner controls (active when `NumLock` is ON).
- **stardew-access Bridge**: Added reflection bridge to support `stardew-access` TileViewer exploration cursor and Auto-Walk scanner functionality.
- **Usability Enhancements**: Added full mouse click support, hover state detection, and dynamic menu resizing for `NavigatorMenu`.

### Fixed
- **Locale Reloading**: Added automatic reload of translated POI and map names on the `Content.LocaleChanged` event.
- **Co-op Double Translation**: Prevented double translation of destination display names by reloading registry keys.
- **Fruit Tree Inspection**: Improved fruit tree inspection to announce specific tree display name.
