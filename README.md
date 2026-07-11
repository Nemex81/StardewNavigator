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
- **`1`**: Use Tool — calls `pressUseToolButton()` with 20-tick cooldown. Works for all players (sighted or screen-reader, with or without stardew-access). Triggers axe, hoe, fishing rod, sword, watering can, etc.
- **`3`**: Action/Interact — calls `location.checkAction()` directly on the facing tile (opens chests, talks to NPCs, uses machines). Falls back to `pressActionButton` for special doors and events.
- **`5`**: Read tile in front of the player (facing tile).
- **`7`**: Select previous hotbar slot (circular wrapping over 12 slots).
- **`9`**: Select next hotbar slot (circular wrapping over 12 slots).
- **`0`**: Read current coordinates and location display name (like `K`).
- **`.` (NumPad Decimal)**: Alias for `Enter` in **all contexts** (world, inventory, Navigator Menu, any open menu). No modifier needed. Numpad `.` is distinct from the main keyboard `.` — no conflict.

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

For a detailed history of all changes across versions, please refer to the [Changelog](changelog.md).
