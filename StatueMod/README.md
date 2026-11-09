# GildedChronicles - Player Statues

https://mods.vintagestory.at/playerstatues

Statues are carved, not crafted finished. Craft a **statue block** from stone bricks of one rock type: a small one (one
block tall) from four (2x2 in the crafting grid), a large one (about the size of the Tobias statue) from nine (3x3), or a
bust (just the head and neck, on a pedestal) from two (a row of 2). Marble comes from polished marble. Place the block, hold
a **chisel** in your hand and a **hammer** in the off hand, and right-click it to open the design dialog: choose a **player**
(from the players online) and a **pose**. The statue is drawn as that player's seraph (including hair, face and beard),
frozen in the most expressive moment of the pose and carved from the same stone. The dialog shows a turning preview and can
play the pose; the frame is a real frame number of the animation ("Best frame" picks the most expressive one) and is saved
as that frame. The player must be online when the statue is carved to copy their look; otherwise the statue has no hair or
face. Required on both client and server.

When you press **Commit** the block becomes the finished statue (the chisel and hammer wear down) and it **cannot be carved
again**. The statue keeps its design when broken and picked up, and can be placed again.

**Remove the pedestal** (off by default) makes a statue stand directly on the ground, without the stone pedestal, or, with an elk, the platform under it.

**Copy design from** lists the finished statues in your hotbar; picking one fills in every setting of the dialog (player,
pose, layers, hands, elk and tack), which you can commit as it is or change first. The statue itself is not used up.

The edit dialog has three layer switches, applied inner to outer: **hair, face and beard**, then **clothing**, then
**armor**. Only the layers you switch on are copied from the player, and worn items hide the body parts they replace
(a helmet hides hair), as on the player. Clothing that only changes the skin texture has no shape, so it does not
show on a stone statue.

Each hand can hold an item, carved from the same stone as the statue. In the dialog, choose for the right and left hand
one of the items in your hotbar slots, nothing, or (after copying a statue) the copied statue's item. The item is only
copied; nothing is taken from you. A bust has no hands.

The preview turns on its own; switch off "Spin preview" to hold it still. Under it are two rows of < > buttons. **Turn
statue (saved)** turns the statue in steps of 22.5 degrees, and the compass direction it now has (for example
"south-southwest") is written between the buttons; that direction is saved as the way the statue faces, and a newly
placed statue looks at the player who placed it. A line above says which way the block faced when you opened the
dialog, since picking a direction only takes effect when you Commit. **Turn preview (unsaved)** turns only the picture in the
preview (a line says which direction it shows), so you can look at the statue from other sides. These two buttons are
greyed out while "Spin preview" is on, since the preview is turning by itself; switch spin off to use them.

The player and the elk are posed separately. **Player pose** is Standing, one of the player's emotes, or **Riding**.
Small and large statues can also have an **Elk** (a cow, or a bull with antlers), carved from the same stone, with its own
**Elk pose**: standing still, walking or galloping. With **Riding**, the player looks like they are really riding: the
game's riding animation that goes with the elk's gait is used, so the player's legs, body and arms follow the stride.
With an emote instead, the player sits in the saddle and the emote takes over the arms and head (a wave is a full wave, not
half-way between waving and holding the reins).

The **Pose frame** picks the frame of the player's emote, and the **Elk frame** (shown when there is an elk) picks the
frame of the elk's stride. Each has its own **Best frame** button, and "Play the animation" steps both. With the
**Riding** pose the rider is kept at the elk's frame, so the legs, body and arms follow the stride. The default elk frame
is the one where the rider leans furthest into the stride.

When there is an elk, it can also wear **tack** from the game's own elk gear: a bridle, a face mask (16 designs), a saddle and
saddlebags. You choose them from lists; you do not need to own the items. **Copy from a nearby elk** lists the loaded elk around you (closest first; **Refresh** looks again), and picking one fills in cow or bull, its antlers and the tack it wears. A bull's antlers can also be chosen by hand, from none to the biggest of the eight antler models. The tack is carved from the statue's material.

Statues can also be made of **metal**, in the same three sizes, but metal is not chiseled: it is **cast**. Make a **statue
mold** by packing fire clay around a finished stone statue: place the statue, right-click it with fire clay in your hand, one
piece at a time. The clay is packed around the statue and rises with every piece (a bust takes five, a small statue eight,
a large statue sixteen) until the statue is buried; then right-click it with an empty hand to lift off the mold. The mold
remembers that statue's design, and the statue is left unharmed. Place the mold (sneaking, on solid ground): a small or bust mold is a block of clay with a hole in the top, and a large mold fills a 3x3 area four blocks tall, with the hole in the middle block. Pour molten metal
into the hole (a bust takes 300 units, a small statue 600, a large one 1800); once the metal has cooled, break the mold
to get the metal statue, which has exactly the same design as the stone one. The mold breaks in the process, so each
cast needs a new mold. Metal statues are carved in the game's plain sheet texture for that metal (gold uses the smooth plate
texture instead, without the speckles) and shine like the game's metal blocks do (iron and lead, which do not shine in the
game, do not shine here either). The metals are bismuth, bismuth bronze, black bronze, brass, copper, cupronickel, electrum,
gold, iron, lead, meteoric iron, molybdochalkos, silver, steel and tin bronze.

**Statue types.** The dialog has a **Statue type** list at the top. It is **Player** by default; the other types are special
statues with a fixed shape and no pose, currently the **robot** (a retro tin robot: a small wide head with two eyes on a thin
stalk, an egg-shaped body with a chest dial, a yellow band and a plug at the belt, jointed arms with pincer claws and jointed
legs). A special statue is not a block of its own: it is carved, copied, packed in clay and cast exactly like a player statue,
in the same blocks (small, large or bust, in every rock and metal), with only the player settings greyed out. The figure is
scaled to fill its block: a small robot stands about one block tall, a large one fills the 3x3 area. The pedestal switch and the
facing buttons still apply. **Copy design from** lists special statues in the hotbar too: copying one switches the dialog to its
type, and choosing Player (or copying a player statue) switches back. A new special statue is added in `StatueTypes.cs` with a
shape (its front on the south face, and its own pedestal slab as an element named `Pedestal`, which is left out) and a lang name
`nf3tstatue:type-<code>`. The robot's shape is built from boxes in `assets/nf3tstatue/shapes/block/robot.json`.

A **large statue** fills a 3x3 area of blocks and is four blocks tall (the game's multiblock system, as large slabs do in the StoneQuarry mod), standing on a wide plinth. Its block is drawn as a block of stone of that size. Place it in the middle of the area, which must be free. Breaking any part of it breaks all of it.
