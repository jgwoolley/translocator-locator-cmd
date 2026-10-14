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
