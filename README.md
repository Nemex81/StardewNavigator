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
- `HudMessageDuration`: The time (in seconds) that on-screen fallback messages remain visible (default: `4.0` seconds).

## Credits & Acknowledgements
Questo mod è un'estrazione standalone del modulo Navigator sviluppato 
originariamente come contributo al progetto stardew-access:
  https://github.com/stardew-access/stardew-access

Il modulo Navigator è stato progettato e implementato da Nemex81
e proposto tramite PR #549:
  https://github.com/stardew-access/stardew-access/pull/549

Ringraziamenti speciali al team di stardew-access, a Pathoschild
per SMAPI e ModBuildConfig, e alla community di modding di Stardew Valley.
