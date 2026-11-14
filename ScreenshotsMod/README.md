# GildedChronicles - Screenshots

Client-side mod. Every screenshot you take (F12, or a mega screenshot) is recorded with where you stood, which
world and server you were on, and the in-game date, season and weather. Use `.shots` to find them again, label them,
open them, or get a route back to the spot with Translocator Navigator.

## Commands

Screenshots are numbered per world, newest first (`#1` is your latest). Wherever a command takes a screenshot you
can give its number or its file name (`2026-11-11_14-02-33`).

| Command | Description |
| --- | --- |
| `.shots [page]` | Lists this world's screenshots, 8 per page, with position, distance and direction. |
| `.shots near [radius]` | Lists this world's screenshots within a radius (default 500 blocks), nearest first. |
| `.shots info <shot>` | Everything recorded: position, real and in-game date, season, temperature, server. |
| `.shots goto <shot>` | Plans a route there in Translocator Navigator and starts its on-screen guidance. |
| `.shots open <shot>` | Opens the image in your image viewer. |
| `.shots label <shot> [text]` | Names a screenshot (`.shots label 3 Lake house`); no text removes the name. |
| `.shots refresh` | Imports screenshots taken before the mod was installed, from the location data the game wrote into them (see below). |
| `.shots worlds` | How many screenshots you have per world or server. |

`.shots goto` needs [Translocator Navigator](https://mods.vintagestory.at/show/mod/39775) 2.2.0 or later. Without it,
the command prints the position, distance and direction instead.

## Compared with the game's own setting

The game can already do part of this without a mod. Its client setting `screenshotExifDataMode` (0, off, by default;
it has no entry in the settings menu) writes your position, the direction you look, the world seed and the world
config into each screenshot's EXIF data. Turn it on with:

```
.clientconfig screenshotExifDataMode 1
```

That only stores the data in the image: the game has no way to list, search or go back to those spots, and it records
no server, world ID or in-game date.

`.shots refresh` reads that metadata from the screenshots folder, straight from the image files (no library needed).
It imports the PNGs in the top-level screenshots folder that have the data and aren't already recorded, and ignores
the rest. The game's metadata has no savegame ID or server, so:

* a screenshot whose seed matches the world you're in is put in that world;
* any other is put in **Unknown world (imported)**. Those are counted by `.shots worlds` but not listed by `.shots`,
  `.shots near`, `.shots info` or `.shots goto`: their position can't be placed in a world, so the commands that need
  one skip them.

Subfolders such as `timelapse/` are not scanned. Running refresh again only adds new files. With both turned on,
this mod's `WriteExif` replaces the game's EXIF text (keeping the game's `Pos`, `RollYawPitch` and `WorldSeed`
fields, but without the world config).

## Privacy

* The record is kept in `VintagestoryData/ModData/Nf3tScreenshots.json` on your computer, not in the images, so a
  screenshot you share doesn't tell anyone where your base is or which server you play on.
* Never recorded: your player name, the server password or your login token.
* Servers that turn the coordinates HUD off: screenshots are still recorded, but the mod doesn't show their position,
  distance or direction, doesn't search them by distance and won't lead you to them.

## Settings

`ModConfig/nf3tscreenshots.json`:

| Setting | Default | Effect |
| --- | --- | --- |
| `WriteExif` | `false` | Also write the position, world and server into each image's EXIF data (as the game's own "screenshot location metadata" setting does, plus world and server). Anyone you share the image with can read it. Never written on servers that hide coordinates. |
| `IndexTimelapse` | `false` | Also record `.timelapse` frames. |
