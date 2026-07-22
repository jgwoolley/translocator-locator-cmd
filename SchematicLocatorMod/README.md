# Nf3t - Schematic Locator (Not published)

[![A sereph in front of a Translocator](Screenshots/Main.png)](https://mods.vintagestory.at/show/mod/39775)

Mod URL: [https://mods.vintagestory.at/show/mod/39775](https://mods.vintagestory.at/show/mod/39775)

A server side mod that allows you to search for schematics in worldgen by block code and/or properties. Default to "Schematic Locator" open Ctrl + Y.

| Data Structure    | Key Type              | Value Type            | Purpose                                                  |
|:------------------|:----------------------|:----------------------|:---------------------------------------------------------|
| **BlockCodes**    | `int` (Palette ID)    | `AssetLocation`       | Defines **what** the block is (e.g., `game:log`).        |
| **BlockIds**      | `int` (Index $i$)     | `int` (Palette ID)    | Stores the ID of the block at specific position $i$.     |
| **Indices**       | `int` (Index $i$)     | `uint` (Packed Coord) | Stores the X/Y/Z coordinate at specific position $i$.    |
| **BlockEntities** | `uint` (Packed Coord) | `string` (JSON)       | Stores custom data for a block at a specific coordinate. |

## TODO

- Double << and >> for navigation
- Show Block in Cell
- Clear on search
- Select button doesn't work
- arrow weird
- packet to get full JSON asset
- Dummy item stack
- open gui with key rather than command
- Search Prefix / Matched Block / Block + Entity Mode