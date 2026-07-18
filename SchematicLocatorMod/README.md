# Nf3t - Schematic Locator (Not published)

[![A sereph in front of a Translocator](Screenshots/Main.png)](https://mods.vintagestory.at/show/mod/39775)

Mod URL: [https://mods.vintagestory.at/show/mod/39775](https://mods.vintagestory.at/show/mod/39775)

A server side mod that allows you to search for schematics in worldgen by block code and/or properties.

| Data Structure | Key Type | Value Type | Purpose |
| :--- | :--- | :--- | :--- |
| **BlockCodes** | `int` (Palette ID) | `AssetLocation` | Defines **what** the block is (e.g., `game:log`). |
| **BlockIds** | `int` (Index $i$) | `int` (Palette ID) | Stores the ID of the block at specific position $i$. |
| **Indices** | `int` (Index $i$) | `uint` (Packed Coord) | Stores the X/Y/Z coordinate at specific position $i$. |
| **BlockEntities** | `uint` (Packed Coord) | `string` (JSON) | Stores custom data for a block at a specific coordinate. |