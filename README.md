# GildedChronicles Vintage Story Mods

[![A sereph in front of a Translocator](Main.png)](https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE)

Mods URL: [https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE](https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE)

Various mods I have created for VintageStory

* [Schematic Preview](https://github.com/jgwoolley/vintage-story-schematic-preview/)
* [Translocator Locator](./TranslocatorLocatorCmdMod)
* [Translocator Navigator](./TranslocatorNavigatorMod)
* [Water Sponge](./WaterSpongeMod)
* [BlueprintCopyMod](./BlueprintCopyMod)
* [TranslocatorRepairMod](./TranslocatorRepairMod)
* [StructureLogMod](./StructureLogMod): Server-only; logs newly generated structures
* [TapestryRepairMod](./TapestryRepairMod): Repair ruined tapestries with flax twine
* [BlockColorMod](./BlockColorMod): Writes an average color for every block to a JSON file
* [StatueMod](./StatueMod): Stone statues of a player in an emote pose
* [SchematicLocatorMod](./SchematicLocatorMod): Not published
* [CenterMapMod](./CenterMapMod): Not published
* [ScreenshotsMod](./ScreenshotsMod): Not published; remembers where, in which world and on which server each screenshot was taken
* [ReliquiarumMod](./ReliquiarumMod): Not published; relics from other worlds, hidden across the map (test build: creative inventory only so far)
* [EntropyRiftMod](./EntropyRiftMod): Not published; the Storm Siphon turns a temporal gear into an entropy gear during a temporal storm

## Other files

* [Nf3tVSCommon](Nf3tVSCommon): Contains the common code shared between the mods.
* [ZZCakeBuild](ZZCakeBuild): Builds the Mods.
* [CreateImages](CreateImages): These are some scripts that will generate VintageStory modicons as well as Screenshots in the correct resolution for ModDB.
* [SchematicCli](SchematicCli): A script for analyzing schematics for tapestries. Being ported into [SchematicLocatorMod](SchematicLocatorMod)
* [NEW_MODS.md](NEW_MODS.md): Some tips for adding new mods.

## Old files
Files that are no longer used.

* [CreateDocs](CreateDocs): Very basic project that used to build the command templates.
* Efforts to build the project without references to VINTAGESTORY HOME
    * [Libs](Libs): Experimentally working on adding git submodule support to get the .dlls instead.
    * Old Docker build scripts
        * [build.sh](build.sh): Runs vsdev (from PATH, else via npx), installs the game files into `./vintagestory` with it, then builds and packages all mods.
        * [run.sh](run.sh): Builds the projects?
        * [clean.sh](clean.sh): Runs project cleanup commands.

## Mod page tracking

[`moddb/`](moddb) mirrors the mod database pages (description, links, tags, release changelogs) so they can be
versioned. `vsdev moddb diff` reports any difference from the live site; see `moddb/README.md`.
