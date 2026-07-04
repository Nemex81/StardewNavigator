# StardewNavigator

StardewNavigator is a standalone pathfinding and navigation mod for Stardew Valley. It helps players navigate to various points of interest (buildings, areas, coordinates) across maps.

## Features

- **Automatic Pathfinding**: Computes shortest-path routes across multiple maps using BFS.
- **Dynamic Routing**: Adapts paths when you warp or change locations.
- **Accessibility Integration**: If `Stardew Access` is installed, navigation instructions are read aloud via your screen reader.
- **Visual Fallback**: If `Stardew Access` is not installed, temporary HUD messages are displayed on-screen.
- **Customizable**:
  - Keybind to open the navigation menu (default: `G`).
  - Duration of on-screen HUD messages.
  - Fully compatible with Generic Mod Config Menu.

## Configuration

You can configure the mod via the `config.json` file generated in the mod folder after running the game once, or in-game via the **Generic Mod Config Menu**:

- `NavigatorMenuKey`: The keybind used to open the navigation menu (default: `G`).
- `HudMessageDuration`: The time (in seconds) that on-screen fallback messages remain visible (default: `4.0` seconds).
