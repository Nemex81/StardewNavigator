# StardewNavigator

StardewNavigator is a standalone pathfinding and navigation mod for Stardew Valley. It helps players navigate to various points of interest (buildings, areas, coordinates) across maps.

## Features

- **Automatic Pathfinding**: Computes shortest-path routes across multiple maps using BFS.
- **Dynamic Routing**: Adapts paths when you warp or change locations.
- **Numpad Navigation & Grid Movement**: Classic numeric keypad layout for manual movement, inspection, coordinates, menu, and scanner controls (active when `NumLock` is ON).
- **stardew-access Reflection Bridge**: Direct, dynamic integration with `stardew-access`'s TileViewer cursor and Auto-Walk scanner features via numeric keypad.
- **Mouse & Resize Support**: Recalculates menu layout on window resize, highlights items on hover, and allows click-to-confirm POI selections for sighted players.
- **Accessibility Integration**: If `Stardew Access` is installed, navigation instructions and numpad inspects are read aloud via NVDA/SAPI.
- **Visual Fallback**: If `Stardew Access` is not installed, temporary HUD messages are displayed on-screen.
- **Customizable**:
  - Keybind to open the navigation menu (default: `G`).
  - Toggle Numpad controls active/inactive in config (default: `true`).
  - Duration of on-screen HUD messages.
  - Fully compatible with Generic Mod Config Menu.

## Installation
1. Installa SMAPI (https://smapi.io/)
2. Scarica l'ultima release dalla pagina Releases di GitHub
3. Decomprimi e copia la cartella StardewNavigator in Stardew Valley/Mods/
4. Avvia il gioco via SMAPI
Opzionale: installa Stardew Access per integrazione screen reader completa.

## Compatibility
- Stardew Valley 1.6+
- SMAPI 4.0+
- Stardew Access (opzionale — https://github.com/stardew-access/stardew-access)
- Generic Mod Config Menu (opzionale)

## Configuration

You can configure the mod via the `config.json` file generated in the mod folder after running the game once, or in-game via the **Generic Mod Config Menu**:

- `NavigatorMenuKey`: The keybind used to open the navigation menu (default: `G`).
- `NumpadControlsActive`: Toggle the Numpad navigation shortcut controls (default: `true`).
- `HudMessageDuration`: The time (in seconds) that on-screen fallback messages remain visible (default: `4.0` seconds).

## Numpad Controls (NumLock ON)

> [!NOTE]
> For a fully detailed map, including all modifier layers, scanner interactions, and emulation details, please read the complete [Numpad Mapping Documentation](NUMPAD_MAP.md).

When **NumLock is ON**, the numeric keypad acts as a layered interface for movement, cursors, actions, and readings:

### 1. Base Level (No Modifiers)
- **`8, 2, 4, 6`**: Move grid up, down, left, right (delegates to `stardew-access` if available, otherwise fallback locale).
- **`1`**: Use Tool — with `stardew-access`: simulates native `X` key (rate-limited by XNA). Without it: `pressUseToolButton()` with 20-tick cooldown (~333 ms).
- **`3`**: Action/Interact — calls `location.checkAction()` directly on the facing tile (opens chests, talks to NPCs, uses machines). Falls back to `pressActionButton` for special doors and events.
- **`5`**: Read tile in front of the player (facing tile).
- **`7`**: Select previous hotbar slot (circular wrapping over 12 slots).
- **`9`**: Select next hotbar slot (circular wrapping over 12 slots).
- **`0`**: Read current coordinates and location display name (like `K`).

### Navigator Menu (when open via `G` or `/`)
When the Navigator Menu is active, the numpad provides direct navigation without switching to the main keyboard:
- **`8`**: Move cursor up in the list (previous destination or POI).
- **`2`**: Move cursor down in the list (next destination or POI).
- **`LeftCtrl + 5`**: Confirm the current selection and start the automated route.

> **`LeftCtrl + NumPad5` is context-aware**: it confirms the Navigator Menu selection when that menu is open, acts as `LeftCtrl + Enter` in any other menu (inventory, shops, dialogues…), and triggers Object Tracker Auto-Walk when no menu is open.

### 2. LeftCtrl Level (Character Physics & Actions)
- **`LeftCtrl + 8, 2, 4, 6`**: Micro-movement precise (player walks fluidly pixel-per-pixel).
- **`LeftCtrl + 5`**: Context-aware — confirms Navigator Menu selection when that menu is open; acts as `LeftCtrl + Enter` in any other menu (inventory, shops…); triggers Object Tracker Auto-Walk in the world (equivalent to `LeftCtrl + Home`).
- **`LeftCtrl + 9`**: Cancel active navigation.
- **`LeftCtrl + 0`**: Alias of `LeftCtrl + Enter` (simulates Left Click in menus / Carpenter construction, and triggers Auto-Walk in the world).

### 3. LeftShift Level (Tile Cursor & Navigation)
- **`LeftShift + 8, 2, 4, 6`**: Move `stardew-access` TileViewer cursor.
- **`LeftShift + 5`**: Read current coordinates and location display name.
- **`LeftShift + 9`**: Read active navigation route status.

### 4. LeftAlt Level (Player Vitals, Held Item & Standing Tile)
- **`LeftAlt + 5`**: Read standing tile (equivalent to `LeftAlt + J` in stardew-access).
- **`LeftAlt + 0`**: Read player health and stamina (vital stats).
- **`LeftAlt + 7`**: Read selected hotbar item (alias).
- **`LeftAlt + 9`**: Read selected hotbar item (alias).

### 5. Utility & Scanner Controls
- **`*` (Multiply)**: Open Inventory.
- **`/` (Divide)**: Open Navigator Menu.
- **`+` (Add)**: Cycle upwards (Group without modifier, Category with `Ctrl`, Item in group with `Shift`).
- **`-` (Subtract)**: Cycle downwards (Group without modifier, Category with `Ctrl`, Item in group with `Shift`).

When **NumLock is OFF**, the keys behave as standard keyboard navigation keys. Numpad keys are also ignored during chat typing or in menus to avoid layout conflicts.

## Credits & Acknowledgements
Questo mod è un'estrazione standalone del modulo Navigator sviluppato 
originariamente come contributo al progetto stardew-access:
  https://github.com/stardew-access/stardew-access

Il modulo Navigator è stato progettato e implementato da Nemex81
e proposto tramite PR #549:
  https://github.com/stardew-access/stardew-access/pull/549

Ringraziamenti speciali al team di stardew-access, a Pathoschild
per SMAPI e ModBuildConfig, e alla community di modding di Stardew Valley.

## Changelog

### v1.2.4
- **Object Tracker Auto-Walk**: Remapped `LeftCtrl + NumPad5` to trigger Auto-Walk directly to the Object Tracker selected object (equivalent to `LeftCtrl + Home`), separating it from `LeftCtrl + NumPad0` which remains the TileViewer cursor Auto-Walk (equivalent to `LeftCtrl + Enter`).

### v1.2.3
- **Alias LeftCtrl + Enter**: Reassigned `LeftCtrl + NumPad0` to act as a virtual key press of `Enter` while `LeftCtrl` is held. This makes `LeftCtrl + NumPad0` a true alias of `LeftCtrl + Enter`, triggering Auto-Walk in the world and simulating Left Click in menus (like chest slot confirmation or Carpenter construction).

### v1.2.2
- **Key Binding Optimization**: Reassigned `LeftCtrl + NumPad5` to trigger `stardew-access` Auto-Walk to the TileViewer cursor (equivalent to `LeftCtrl + Home`, alias of `LeftCtrl + NumPad0`). This eliminates the redundancy where both `Ctrl + 5` and `Alt + 5` were mapped to reading the tile under the player's feet.

### v1.2.1
- **ReadTile Reflection & Alignment**: Delegated `NumPad5` (facing tile) and `LeftAlt + NumPad5` (standing tile) to `stardew-access` native `ReadTile` feature via reflection when present.
- **Improved Standalone Fallback**: Aligned standalone `NumPad5` facing tile detection to mirror `stardew-access` `FacingTile` bounding box calculations exactly.
- **Removed Italian Fallbacks**: Fixed C# tree fallback labels to use English terminology (`Oak`, `Maple`, etc.) for non-Italian language play.
- **Cleaned Vitals Bindings**: Reserved `LeftAlt + NumPad0` as the vital statistics command, freeing `LeftAlt + NumPad5` to read the standing tile (matching `LeftAlt + J`).

### v1.2.0
- **Active Hotbar Item Reader**: Added `LeftAlt + NumPad7` and `LeftAlt + NumPad9` (alias) to announce the currently held item (TTS/HUD fallback, empty slot detection).
- **Hotbar Slot Cycling**: Reassigned `NumPad7` and `NumPad9` base level to select the previous/next hotbar slot with circular wrapping.
- **Utility Key Reassignments**: Mapped `NumPad0` to coordinates, `LeftCtrl+NumPad0` to Auto-Walk, `LeftAlt+NumPad0` to Vitals, `*` to Inventory, and `/` to Destinations Menu.
- **Scanner Key Alignments**: Matched cycle controls of `+` and `-` to `PageUp` and `PageDown` modifiers of `stardew-access`.
- **Bypassed Double Movement**: Appended Numpad keys to game settings on load for smooth native micro-movement under `LeftCtrl`.

### v1.1.0
- **Numpad Controls**: Added comprehensive keypad-driven layout for manual movement, inspection, coordinates, menu, and scanner controls (active when `NumLock` is ON).
- **stardew-access Bridge**: Added reflection bridge to support `stardew-access` TileViewer exploration cursor and Auto-Walk scanner functionality.
- **Usability Enhancements**: Added full mouse click support, hover state detection, and dynamic menu resizing for `NavigatorMenu`.
- **Stability Improvement**: Wrapped JSON file reading in a generic try-catch block to prevent game startup crashes if files are locked.
