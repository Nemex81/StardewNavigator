# Changelog - StardewNavigator

All notable changes to this project will be documented in this file.

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
