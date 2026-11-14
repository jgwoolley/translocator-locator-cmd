# Reliquiarum: development notes

**Spoilers.** This lists every relic. The player-facing page is [README.md](README.md), which names none of them: keep it
that way (the same goes for `modinfo.json`'s description and any ModDB page).

## Status

Test build (0.1.0). The nineteen relics are placeable blocks (`nf3treliquiarum:relic-<type>-<side>`) in the creative inventory,
in their own **Reliquiarum** tab (and *Everything*). A placed relic faces the player who placed it (the `<side>` is the
direction the player looked, as with the game's chests, so the model is turned the other way). They are excluded from the
handbook (`attributes.handbook.exclude`), so nothing in game lists them.

The other thirty-six relics the generators still make (painted wing, rift gun, toppled sentinel, crate, sun disc, sun idol, skull plaque, eye core, boots, brass scroll,
colossus, artificers, skull drone, withered throne, kneeling and guarding giants, crawling deathless, gilt statue, ember crystal,
brazier, saint's head, skull pillar, angels, war casings, suited effigy, target bots, animatronics) were cut and kept in
`ReliquiarumModBackup/`. The generators below still write all of them (and their textures) into this mod, so build only
what changed with `--only` (`vsdev shape CreateImages/reliquiarum-grimdark.shapes.mjs --only lastclash`, `vsdev texture ...
--only armor-blue-worn`), or delete the extras again after a full run.

The imperial reliquary and three shrines are placed by worldgen so far (the last clash is out of worldgen for now) (see [Worldgen](#worldgen)), and there is no ModDB page (`vsdev moddb` reports it as not published, no page).

## The relics

| Shape | What | Size |
| --- | --- | --- |
| `fallengiant.json` | A giant slumped against a wall, paint gone, armor eaten by rust and verdigris, one lens still faintly red, a wound in the chest, a purity seal (wax and parchment strips) on the left shoulder plate | about 2 x 2.5 x 2 blocks, back to the north |
| `reliquary.json` | Gothic worn-bronze shrine: a bone behind glass framed by rune bands and skull roundels, keyhole plate, chains, a pediment with a gilt two-headed eagle in a dark niche between two pinnacles, a winged skull on top | 14 x 7 px footprint, about 1.8 blocks tall |
| `stasispod.json` | A destroyed, long-dead automaton spilled from a shattered coffin-shaped stasis pod: no light left, its blackened frame against the pod's dull grey inside, shards in the frame and on the floor, the upper right of the frame broken off, the spilled fluid dried to a crust, one canister drained and one knocked over; the automaton on one knee, an arm torn off, a leg snapped | about 2 x 3 x 1.6 blocks, back to the north |
| `scarab.json` | A broken mechanical beetle rolled onto its side: grey domed shell cracked open (the chunk lies beside it), dark sensor eye under vent slats, one leg torn off and lying nearby, one snapped, one mandible broken short | about 1.4 x 0.7 x 1.6 blocks |
| `giantprone.json` | An armored giant stripped to rust and verdigris, fallen face down, one arm reaching forward, the gun out of reach, a game skull nearby | about 2 x 1 x 2.5 blocks, head to the south |
| `deathlessstanding.json` | A deathless warrior (a skeleton of dark living metal with a ridged skull, dim green eyes and core) standing dormant, its rifle with a blade under the barrel held across its body | about 0.8 x 2 x 1.3 blocks, facing south |
| `deathlesskneeling.json` | A deathless warrior collapsed on its knees, slumped forward, arms hanging, its rifle dropped beside it | about 0.9 x 1.6 x 1.3 blocks, facing south |
| `lectern.json` | A stone pedestal with a slanted dark-wood desk and a great red-bound book open on it, a skull and a candle at its foot (19 boxes) | about 0.9 x 1 x 0.7 blocks |
| `candelabrum.json` | A tall iron stand on three feet with five candles in brass cups at different heights, wax run down onto the floor (35 boxes) | about 0.7 x 1.5 x 0.6 blocks |
| `machinealtar.json` | A block of old basalt brick under a red runner with a fringe, a brass cog on its front, a skull on top between three candles (26 boxes) | about 0.9 x 1 x 0.6 blocks |
| `cogitator.json` | An iron console with a dark cracked screen, a slanted board of brass keys, a cog on its side, a purity seal, cables to the floor (30 boxes) | about 1.2 x 0.9 x 0.6 blocks |
| `brokenpew.json` | A high-backed bench of rotten ebony, leaning where its east legs gave way, the back cracked, a plank and a leg fallen (9 boxes) | about 1.1 x 0.9 x 0.7 blocks |
| `skullniche.json` | A section of basalt brick wall with an arched opening backed by skull-set cobble, four skulls stacked inside, one fallen to the floor (29 boxes) | about 1 x 1.1 x 1 blocks |
| `munitionscrate.json` | A riveted iron crate, its lid shoved aside over brass shells, a stencil band and a purity seal on the front (13 boxes) | about 1.1 x 0.5 x 0.7 blocks |
| `dataslates.json` | Floor clutter: three iron data-slates with dead screens, a rolled scroll, a red-bound book, a loose purity seal and a toppled candle (22 boxes) | about 0.9 x 0.1 x 1 blocks |
| `aquila.json` | A small gilded statue: a two-headed eagle with spread, layered wings (flight feathers and coverts), scalloped breast plumage, hooked beaks, fanned tail and clawed legs, on a post over a stepped round pedestal | about 1 block wide (wing tips reach half a pixel past each side), 0.9 tall |
| `eaglebanner.json` | A standing banner: tattered black cloth with gold trim hung by loops from a crossbar, embroidered with the two-headed eagle, a line of glyphs and a skull; a small gilt eagle on top of the bronze pole, gilt cords with tassels at the bar's ends | cloth 1 block wide, 2.9 blocks tall with the finial |
| `cogbanner.json` | The same stand in rusted iron and bronze: rust-red cloth with black and bone trim, a cog wheel with a skull on it split down the middle (black wheel and bone skull on one side, bone wheel and a black machine skull with a lens eye on the other), three rows of punched-tape marks; a toothed wheel on top of the pole, chains with weights at the bar's ends | as the eagle banner |
| `lastclash.json` | The last clash: an armored giant in faded blue armor and a deathless warrior who killed each other and never fell (125 boxes). The giant, down on one knee and facing north, has run his sword through the deathless's glowing core, the point out of its back, and grips its rifle with his other hand; the deathless, braced over him, has driven its rifle blade into his chest. His gun lies beside his knee | about 1.5 x 1.9 x 2.9 blocks, the deathless to the north |

## Original designs

The designs are original homages to tropes (grimdark sci-fi, the abandoned test chamber, lost temple treasure), not copies
of any franchise's models, logos or names, and nothing in the mod uses a trademarked name:

- The two-headed eagle is drawn from the old heraldic eagle.
- The cog banner's emblem is drawn in code from circles and lines (a square-toothed wheel, a skull that is half bone and
  half machine, split and counterchanged down the middle). It is not traced from any existing logo.
- The sun idol (a gold sun with ball-tipped rays and a face of two black eyes) and the skull plaque (a gold tile with studs and a
  crowned skull) are homages to treasure-chest props, built from scratch out of boxes and pixel art.
- The eye core (a white boxy body with one big red eye and a U-shaped handle) is a homage to robot-companion props, built from scratch;
  the iris texture is the mod's own spoked drawing.
- The rift gun (a white handheld emitter with three prongs round a lens) and the spring-heeled boots (long steel spring heels on
  black boots) are homages to puzzle-game props, built from scratch out of boxes under their own names.
- The brass scroll (a long brass case with gemmed cuffs and a spiral-bound rod) is an homage to fantasy-game artifact scrolls, built from
  scratch under its own name.
- The bronze colossus (a crested bronze giant with wheel discs and a spiked fist) is an homage to fantasy-game ancient automatons,
  built from scratch under its own name; the meander and wheel textures are the mod's own.
- The dead artificer (a robed cyborg priest with a drill arm, hoses, lantern and skull) and the skull drone (a skull with a bionic
  eye and a hover unit) are homages to grimdark sci-fi tech-priests and their servants, built from scratch under their own names.
- The withered throne (a corpse-king on a gilt throne with a halo of cables) is an homage to the same setting, built from scratch
  under its own name.
- The armored giants, the artificers and the deathless warriors (metal skeletons) are homages to grimdark sci-fi, built from
  scratch under their own names. One builder per kind takes a pose (joint angles), so another pose is a few lines.
- The skulls on the new figures are the game's own human skull (`survival/shapes/item/clutter/skull/humanoid.json`, copied
  element for element into `reliquiarum-grimdark.shapes.mjs`), wearing the pale bone of the game's carcass skull
  (`game:block/creature/skullhumanoid` and `insideskull-small`, the same UV layout) instead of the clutter skull's brown. It cannot be scaled,
  so the smaller skulls (the artificer's lap, the throne's armrest) stay the mod's own boxes.
- The hunched gilt statue (a masked hunchback in orange gold with a staff) is an homage to a cartoon-gothic survival game's
  ancient statues, built from scratch under its own name.
- The ember crystal (red crystals round a core with a flame emblem) is an homage to fantasy-game element crystals; the emblem is
  the mod's own pixel drawing.
- The painted wing (a glyph-painted horned shell with rainbow feather fans and a sun gem) is an homage to a fantasy
  game's divine relics; the glyphs are the mod's own lines.
- The ruin furniture and curios (lectern, candelabrum, machine altar, cogitator, pew, skull niche, munitions crate, brazier, saint's
  head, skull pillar, data-slates) use the game's own textures wherever one fits: basalt polished rock and aged brick, skull
  cobble, white marble, rotten ebony planks, riveted iron, rusty corroded metal, tarnished brass, basic red and black cloth,
  paper, and the candle texture mapped as the game's candle maps it (`shapes/block/wax/candle.json`). Only the dead screen, wax
  seals and parchment strips are the mod's own. `texFor` in the spec keeps only the textures each piece uses.
- The angel statues are the game's player model (`game:shapes/entity/humanoid/seraph-faceless.json`) made into a statue the way
  the Player Statues mod does it (`StatueMod/StatueMesh.cs`, `StatuePose.cs`): a keyframe of one of the seraph's own animations
  baked into the limbs, every face on one marble tile sized by the face, the hidden tools and attachment points dropped, the
  model scaled and turned to face south, wings added to the upper torso: vsdev's `gameShape` and `statue` helpers do it.
  `gameShape` reads the seraph from the game install (`./vintagestory`, `$VINTAGE_STORY` or the installed game, as `vsdev render`
  finds it), so building `reliquiarum-grimdark.shapes.mjs` needs one. The war casing's eight-sided sections are vsdev's `prism`.
- The domed war casing (a pepper-pot war machine with rows of hemispheres, an eyestalk and two arms) is an homage to a classic
  science-fiction television villain, built from scratch under its own name.
- The scowling suited effigy (a blue-headed figure in a check suit with a chart badge) and the broken target bots (cuboid bots with
  goggle eyes on hooked posts) are homages to game characters from pictures the user supplied, built from scratch out of boxes under
  their own names; the face, badge and paint textures are the mod's own.
- The ruined animatronics (boar drummer, hare, moth usher, goat ringmaster) are the mod's own cast for the abandoned
  party-hall trope: own animals, colours, costumes and props, deliberately not any existing game's characters.
- The crate's emblem (a pale diamond on a teal disc), the sentinel's stepped shell and the sun disc's sleeping face are
  the mod's own.

## How the models are made

Never edit the shape JSON or the PNGs: edit the generator and run it again. Shapes go to
`assets/nf3treliquiarum/shapes/block/`, textures to `assets/nf3treliquiarum/textures/block/`.

| Relics | Shapes | Textures |
| --- | --- | --- |
| fallen giant, reliquary, stasis pod, scarab, eagle statue, both banners | `vsdev shape CreateImages/reliquiarum-relics.shapes.mjs` | `vsdev texture CreateImages/reliquiarum-relics.textures.mjs` |
| crate, sentinel, sun disc, sun idol, skull plaque, eye core, rift gun, boots, brass scroll, colossus, artificer, skull drone, withered throne, hunched gilt statue, ember crystal, painted wing, suited effigy, the three target bots, the four animatronics | `vsdev shape CreateImages/reliquiarum-ruins.shapes.mjs` | `vsdev texture CreateImages/reliquiarum-ruins.textures.json5` |
| armored giants, artificers on staff and kneeling, deathless warriors, the last clash, ruin furniture and curios | `vsdev shape CreateImages/reliquiarum-grimdark.shapes.mjs` | `vsdev texture CreateImages/reliquiarum-grimdark.textures.json5` |

The sun idol and the skull plaque keep to pixel steps and stacked slices on purpose: a version with discs made of rotated slabs (`fan()`)
looked worse in render (ridges, radial seams, many more elements), so it was dropped.

Everything is made with `vsdev` now. The first relics were made with Python scripts (`reliquiarum_shapes.py`,
`reliquiarum_textures.py`); `reliquiarum-relics.*` is their port, checked against them before they were deleted: the same
shape JSON element for element (within 0.0001 where a piece is set on the ground) and every PNG identical pixel for pixel.

Notes on the first relics (`reliquiarum-relics.*`):

- A box there turns about its `from` corner unless an origin is given (the Python scripts' rule; `B` in the spec), not
  about its middle as vsdev's `box` does.
- Every eagle comes from the one `eagle()` builder (exported by the shape spec): scaled down and flattened for the
  reliquary's pediment, small on the eagle banner's pole, and seen from the front as the banner's embroidery (the texture
  spec imports it and draws it with vsdev's `project`). A change to it shows on all of them.
- The textures are the Python algorithms on Python's own random numbers (`PyRandom` in the texture spec is Python's
  `random.Random`), each texture drawn by a vsdev `custom` layer. The stasis pod's glass shards keep the places the
  Python script drew for them, as numbers in the spec.
- The banners' cloth is a row of one-wide strips, each torn off at its own length (`rags`). Its texture is 4 px per unit
  (64 x 112), finer than the metals so the emblems read, and the same picture is on both sides.

`ground: true` drops a model or a loose piece onto the ground (after a pose change nothing sinks into the floor or floats
above it), and `vsdev shape` prints each model's bounds. The crate and the sun disc are sunk a little on purpose.

## Worldgen

`worldgen/structures.json` adds four surface ruins: the imperial reliquary and three shrines. The last clash's schematic is out of
worldgen for now (its sizes looked off in game); its builder stays in the generator, and its relic in the creative inventory. Their schematics (`worldgen/schematics/surface/`) are made by
`node CreateImages/reliquiarum.schematics.mjs`: never edit them by hand. It also writes `.vsdev/reliquiarum-previews.json`, so
`vsdev screenshots --manifest .vsdev/reliquiarum-previews.json` draws each one with the relics' real models (a plain `vsdev render`
of a schematic draws one colored cell per block).

| Structure | Schematic | Chance |
| --- | --- | --- |
| `nf3treliquiarum-imperialreliquary` | `imperialreliquary.json` (9 x 6 x 13): a roofless basalt-brick chapel, its south front fallen. At the north end the reliquary between two candelabra, an eagle banner either side and a gilded eagle on a pedestal in each corner; a lectern before it facing two rows of broken pews, a skull niche in each side wall, data-slates on the floor, a munitions crate by the door, rubble, grass through the broken floor; outside the fallen front the fallen giant sits slumped against a wall stub beside the door, legs out to the south | 0.02 |
| `nf3treliquiarum-smallshrine` | `smallshrine/{white,red,green}.json` (9 x 6 x 9): a stepped white marble platform, its base level with the ground; four marble pillars, a fountain with a paper lantern, four collapsed chests | 0.01 |
| `nf3treliquiarum-ancientshrine` | `ancientshrine/{white,red,green}.json` (19 x 18 x 19): a marble plaza at ground level with a pool, rings of marble pillars and a domed canopy; under it a sealed marble hall (pillars, water channels, a paper lantern), found only by digging | 0.012 |
| `nf3treliquiarum-desertshrine` | `desertshrine/{white,red,green}.json` (9 x 11 x 9): buried whole, a marble hall (pillars, a fountain, a paper lantern, four collapsed chests) under three layers of soil; only the tops of five pillars show in the ground. Warm, dry land only (`minTemp` 15, `maxRain` 0.35) | 0.012 |

- The shrines were built in game (copies of the vintage-story-wiki schematic viewer's samples, kept in
  `CreateImages/reliquiarum-shrines/`) and are placed as built: the generator only adds meta-filler, inside their buried halls (a
  schematic stores no air, so worldgen would leave them full of dirt and rock) and above their ground layer, and fills their
  chests with loot (below). They were built in quartz and white marble; the generator makes each one all marble, once in each of the game's three
  marbles (white, red, green; `marbleize`), into a folder per shrine that its structure lists (`surface/smallshrine/*`), so
  worldgen picks one of the three at random each time (no more often than before). In it:
  every upright run of quartz pillar blocks becomes a pillar of chiseled marble after the two-block pillar built in game
  (`CreateImages/reliquiarum-shrines/marble_pillar.json`: a base with a full-width slab, a capital with one, a 12 x 12 shaft
  between; a shaft-only piece fills taller columns and a one-block piece has both slabs), and every other quartz block (ornate
  caps, beams and dome, the desert shrine's plain walls) and the white marble they were built with polished marble of that colour. The chiseled blocks are written by the
  generator; its encoder rebuilds the in-game base piece byte for byte. Marble wherever they land, on purpose: a shrine that
  plainly does not belong. In logs (Structure Log) the schematic shows as its file, e.g.
  `nf3treliquiarum:red.json/nf3treliquiarum-smallshrine`: match the structure code after the `/`.
- Loot: each shrine chest (the collapsed kind, 8 slots) holds one to three of the game's `stackrandomizer` items, as the
  game's own ruins do; each turns into a random stack from its pool (`survival/itemtypes/meta/stackrandomizer.json`) when the
  chest is first opened. Pools: `accessory-mediumstatus` / `accessory-highstatus` (jewellery), `ingot` (mostly lead, tin,
  copper; rarely silver, gold), `gear` (rusty gears; rarely temporal), `lantern`. Better the harder the shrine is to reach:

  | Shrine | Chests |
  | --- | --- |
  | small (in the open) | its 4: medium jewellery + ingot, gear, medium jewellery, ingot + lantern |
  | desert (buried) | its 4: high jewellery + ingot, medium jewellery + gear, ingot + gear, high jewellery |
  | ancient (sealed hall) | 2 added against the hall's west and east walls: high + medium jewellery + ingot, gear + ingot + lantern |

  The generator writes the chests' block entities itself (`lootChest`: vsapi's TreeAttribute bytes, Ascii85-encoded) and the
  schematic's `ItemCodes`; the wiki's `decodeBlockEntity` reads them back as written. A dedicated server placed all three with
  no warnings (opening a chest, where the loot is rolled, needs a player).
- Soil (`soil-low-normal`) is swapped for the local soil and grass and granite for the local rock; the chapel's basalt is kept,
  so it looks the same anywhere. `meta-filler` (placed as air) clears terrain and plants out of the chapel. Worldgen turns a
  schematic one of four ways; the relics' variants turn with it.
- A relic's variant is the side a player placing it looked to, so the model faces the other way (as the game's chests); the
  generator's `relic(x, y, z, type, facing)` takes the way it should face.
- `chance` is per 32 x 32 chunk column, times the game's `chanceMultiplier` (0.46; a mod's own multiplier is ignored): 0.015 is
  about one try in 145 chunk columns. The game's tiny house ruins have 0.03, its rare ruins (monuments, the
  witch hut) 0.005. Tries on uneven ground, near water or over another structure fail, so all are rarer than that.
- All four use `"placement": "surface"`, not the default ruin placement. The ruin placement checks for water only in the layer just
  above the schematic's start, which for anything set into the ground is the lakebed, so it put the last clash in a lake (when it was in worldgen). The
  surface placement checks for water at the terrain's own height whatever the offset, but wants level ground (the centre and
  the four corners at one height), so big schematics land less often: the chapel and the ancient shrine have their chances
  raised for it (in a test with every chance at 30, they placed about a third as often as with the ruin placement).
- The surface placement starts one block above the terrain, so `offsetY` is -(ground layer + 1): -2 for the chapel, -1
  for the small shrine, -8 for the ancient shrine, -11 for the desert shrine.
- A mod's `structures.json` needs `rocktypeRemapGroups` and `schematicYOffsets`, even empty: without them the game's
  structure loader throws and no surface structures generate at all (vanilla ones included).
- Tested on a dedicated server (1.22.7) with the chances raised and the Structure Log mod: both generated around spawn, in all
  turns, with no errors in the log.

## Inventory icons

`guiTransformByType` in `blocktypes/relic.json` centres each relic on its bounds and scales it to fit the slot. The three
shape specs keep it up to date (their `gui` setting): every `vsdev shape` run moves each relic's `origin` to its bounds'
centre (for the south-facing variant, `rotateY: 180`), and gives a new relic a scale that fits it in the slot. A scale
already in the file stays (`keepScale`: several were tuned by eye); to have one worked out again, delete its line and run
`vsdev shape`.

## Review

```
vsdev texture CreateImages/reliquiarum-relics.textures.mjs   # also writes a preview sheet of the textures
vsdev texture CreateImages/reliquiarum-ruins.textures.json5
vsdev texture CreateImages/reliquiarum-grimdark.textures.json5
vsdev shape CreateImages/reliquiarum-relics.shapes.mjs        # prints each model's bounds; updates the icons in relic.json
vsdev shape CreateImages/reliquiarum-ruins.shapes.mjs
vsdev shape CreateImages/reliquiarum-grimdark.shapes.mjs      # needs the game install (./vintagestory) for the angels
vsdev render ReliquiarumMod/assets/nf3treliquiarum/shapes/block/*.json --out .vsdev/renders/reliquiarum --floor
```

`vsdev render` draws glass opaque. In game the reliquary's pane is drawn in the transparent render pass (`renderPass: 3`
on the element, like the game's display case), so the bone shows through; without it the see-through texture cut a hole
in the world behind the block.

## Next steps

1. Selection and collision boxes: every relic uses the default one-block box, though the giant and the pod are about two
   blocks wide and two to three tall (and the sentinel nearly two long). They need `multiblock` filler (or at least larger
   selection boxes) before they are used in the world.
2. Worldgen, the point of the mod (started: see [Worldgen](#worldgen)): small ruin schematics (a collapsed shrine around the giant, a crypt alcove for the
   reliquary, a tomb for the pod and scarabs, a buried test chamber for the crate and sentinel, a temple niche for the sun
   disc) made in game with WorldEdit, plus `worldgen/structures.json` entries with a low chance so they stay a rare find.
   Then remove the creative tab, or keep it for builders.
3. ModDB: `vsdev moddb scaffold nf3treliquiarum`, mod icon and screenshots (which should show a relic half-seen, not a
   catalogue), before any upload.
4. Optional loot: the reliquary as a container, or lore paper beside the giant.
