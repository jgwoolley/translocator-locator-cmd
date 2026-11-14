# GildedChronicles - Entropy Rift

Adds two devices: the **storm siphon** makes entropy gears, and the **entropy rift** runs on them.

The **storm siphon** is a Jonas device (1 block wide, 2 tall) that draws a temporal storm into its copper dish and down into a
temporal gear and turns it into an **entropy gear**: a temporal gear burned down to dark embers, its edges glowing orange; on the ground it sheds orange and black motes, as the temporal gear sheds cyan ones. Required on both client and
server.

**The storm siphon** (right-click it with):
- a **temporal gear**: sets the gear in the cradle. The siphon tells you in chat when the next temporal storm comes (or how
  long the current one has left);
- an **empty hand**: takes the gear out. Taking out a temporal gear early loses the storm it has gathered.

During a temporal storm, with **open sky** above it, the siphon draws in the storm (particles run down the brass spike, teal
turning red as the gear fills). After **2 in-game hours of storm** the gear becomes an entropy gear and the siphon's glow
turns red. The shortest storms last 2.4 hours, so one full storm of any strength is enough; time carries over between storms
as long as the gear stays in. Storm creatures still come for you while it works.

Look at the siphon to see how full the gear is, when the next storm comes, and whether it can reach the sky. If the world
has temporal storms turned off, it says so.

**Crafting** (grid, 3×3):

| | | |
|---|---|---|
| | Cupronickel rod | |
| Cupronickel rod | Connector (Jonas part) | Cupronickel rod |
| Cupronickel plate | Metal parts | Cupronickel plate |

**The entropy rift** (1 block wide, 2 tall) is the siphon's dark twin, a copper tower with an ember-red tip. Right-click
it with an **entropy gear** to open it: its tower slowly turns and the gear spins backwards in the cradle. **The gear cannot
be taken out again**, and breaking an open rift loses it.

While a player is within 24 blocks, the open rift **pulses** about every 45 seconds and rust creatures crawl out of it, 3-10
blocks away: one or two corrupt or nightmare drifters, shivers and bowtorns per pulse, and now and then a **boss**, one of
the kinds above nightmare that never come on their own outside storms (gearfoot bowtorn, double-headed drifter, deepsplit,
stilt and bellhead shivers). They work on the surface by day too.

- An entropy gear holds **12 pulses**. Each pulse has a 20% chance of a boss, and **every gear brings at least 2** (on
  average 2.7). Bosses drop about one temporal gear each, so a gear gives back about **3.7 temporal gears** on average
  (with the corrupt and nightmare drops), for the one that went into it.
- At most 4 of the rift's creatures and 1 boss are alive at once; a pulse waits until there is room, and waiting costs nothing.
- A boss that despawns alive (everyone walked away) goes back into the gear: you do not lose it.
- **Temporal stability sinks** within 20 blocks of an open rift (nothing left at the rift itself): fog, rust-red rain,
  glitches, and the game's own night and underground spawns there turn nightmare. Outside storms it never lets a player
  near it sink below 0.15 stability, so no poison; during a storm it is no shelter.
- When the pulses are spent (and its last boss is gone) the gear crumbles and the rift goes dormant.

The numbers are the server's to change, in `ModConfig/nf3tentropyrift.json` (written with the defaults on first start):
`Enabled`, `PulsesPerGear`, `PulseIntervalSeconds`, `PlayerRange`, `CreaturesPerPulseMin`/`Max`, `BossChance`,
`GuaranteedBosses`, `MaxAlive`, `MaxBossesAlive`, `SpawnDistanceMin`/`Max`, and the `Creatures` and `Bosses` lists (entity
codes with weights). The stability drain is fixed.

**Crafting the entropy rift** (grid, 3×3):

| | | |
|---|---|---|
| | Cupronickel rod | |
| Cupronickel rod | Cylinder (Jonas part) | Cupronickel rod |
| Cupronickel plate | Metal parts | Cupronickel plate |

**Animations:** the temporal gear in the siphon turns only while a storm is filling it (faster while it draws the storm in);
the entropy gear turns the other way all the time it is in the cradle, in the siphon and the rift, and the powered rift's tower
turns.

The models and textures are generated from the game's own files by `CreateImages/entropyrift/generate.py`.
