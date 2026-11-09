# GildedChronicles - Block Colors

https://mods.vintagestory.at/show/mod/73809

Client-side mod. `.blockcolors` writes one color per block to
`ModData/Nf3tBlockColors.json` in the game's data folder:

```json
{ "game:soil-medium-none": { "hex": "#6B5A45", "alpha": 255 } }
```

Optional wildcard filter on the block code: `.blockcolors game:rock-*`.

Blocks with no color texture (for example invisible or specially rendered blocks) are written as white (`#FFFFFF`).

The color comes from `ITextureAtlasAPI.GetAverageColor` on the block's color texture
(`Block.TextureSubIdForBlockColor`), which is what the game uses for the world map. It is a quick sample
of the texture, not an exact mean of every pixel, and it does not include climate or season tinting.

## Used for the wiki

`Nf3tBlockColors.json` is how the [Vintage Story wiki](https://jgwoolley.github.io/vintage-story-wiki/) gets its block colors:
the file is committed there as `apps/vs-wiki/site/assets/Nf3tBlockColors.json` and colors the schematic viewer and 3D
previews; vsdev bundles the same file for `vsdev render` and `vsdev screenshots` schematics. It is refreshed by hand: after a
game update or a change in the mod list, run `.blockcolors` (no filter) in a game with those mods loaded and replace the
wiki's copy with the output (see "Block colors" in the wiki's README).

Note: the `hex` values are actually written in BGR order (soil comes out as `#1D3954`), and `alpha` is 0; the wiki swaps
red and blue when it loads the file. If this mod is fixed to write RGB, remove that swap in the wiki at the same time.
