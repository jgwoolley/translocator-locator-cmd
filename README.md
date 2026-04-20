# GildedChronicles Vintage Story Mods

[![A sereph in front of a Translocator](Main.png)](https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE)

Mods URL: [https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE](https://mods.vintagestory.at/show/user/0FE64B20508C53BB01DE)

Various mods I have created for VintageStory

* [Schematic Preview](https://github.com/jgwoolley/vintage-story-schematic-preview/)
* [Translocator Locator](TranslocatorLocatorCmd)
* [Translocator Navigator](TranslocatorNavigator)
* [Water Sponge](Sponge)

## Other files

* [Nf3tVSCommon](Nf3tVSCommon): Contains the common code shared between the mods.
* [ZZCakeBuild](ZZCakeBuild): Builds the Mods.
* [CreateImages](CreateImages): These are some scripts that will generate VintageStory modicons as well as Screenshots in the correct resolution for ModDB.
* [SchematicCli](SchematicCli): A script for analyzing schematics for tapestries.
* [NEW_MODS.md](NEW_MODS.md): Some tips for adding new mods.

## Old files
Files that are no longer used.

* [CreateDocs](CreateDocs): Very basic project that used to build the command templates.
* Efforts to build the project without references to VINTAGESTORY HOME
    * [Libs](Libs): Experimentally working on adding git submodule support to get the .dlls instead.
    * Old Docker build scripts
        * [build.sh](build.sh): Builds the projects?
        * [run.sh](run.sh): Builds the projects?
        * [clean.sh](clean.sh): Runs project cleanup commands.
        * [extract.sh](extract.sh): Downloads VintageStory server, which it turns out, is not good enough to get the development environment setup.
