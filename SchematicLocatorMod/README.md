# Nf3t - Schematic Locator (Not published)

[![A sereph in front of a Translocator](Screenshots/Main.png)](https://mods.vintagestory.at/schematicsearch)

Mod URL: [https://mods.vintagestory.at/show/mod/60760](https://mods.vintagestory.at/show/mod/39775)

You know when you are watching a YouTuber explore a structure you have never seen before? Well now if you can identify a few unique blocks, you can search for a particular structure starting with a specific block!

Default to "Schematic Locator" open Ctrl + Y. Can be changed in settings.
For blocks like tapestries, and you know anything about the BlockEntity's AttributeTree values, you can further refine your search!

## How parsing is done

| Data Structure    | Key Type              | Value Type            | Purpose                                                  |
|:------------------|:----------------------|:----------------------|:---------------------------------------------------------|
| **BlockCodes**    | `int` (Palette ID)    | `AssetLocation`       | Defines **what** the block is (e.g., `game:log`).        |
| **BlockIds**      | `int` (Index $i$)     | `int` (Palette ID)    | Stores the ID of the block at specific position $i$.     |
| **Indices**       | `int` (Index $i$)     | `uint` (Packed Coord) | Stores the X/Y/Z coordinate at specific position $i$.    |
| **BlockEntities** | `uint` (Packed Coord) | `string` (JSON)       | Stores custom data for a block at a specific coordinate. |

## Current changelog
- Chat wasn't sent due to rate

## TODO

- Readopt Utils.MatchAssetLocation when it does caching. I'm thinking there should be a search object
- Search Prefix / Matched Block / Block + Entity Mode
- Make it so that tapestries render their blockentity type stuff so they show up!
- Arrow for scrolling through text that has been reduced. Or maybe clicking on "..." swaps whether its the start or end.
- Flicker through different block types?
