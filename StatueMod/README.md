# GildedChronicles - Statues

Craft a small statue (one block tall) from four stone bricks of one rock type (2x2 in the crafting grid), or a large
statue (about the size of the Tobias statue) from nine (3x3). Or craft a bust (just the head and neck, on a pedestal) from two (a row of 2). Place it, then right-click it to
choose a **player** (from the players online) and an **emote**. The statue is drawn as that player's seraph (including hair, face and beard),
frozen in the most expressive moment of the emote and carved from the same stone. The editor shows a turning
preview of the statue and can play the emote; the frame is a real frame number of the animation ("Best frame"
picks the most expressive one) and is saved as that frame. Marble statues are made from polished marble. The player must be online when the statue is edited to copy their look; otherwise the
statue has no hair or face. Required on both client and server.

The statue keeps its settings when broken and picked up again.

The edit dialog has three layer switches, applied inner to outer: **hair, face and beard**, then **clothing**, then
**armor**. Only the layers you switch on are copied from the player, and worn items hide the body parts they replace
(a helmet hides hair), as on the player. Clothing that only changes the skin texture has no shape, so it does not
show on a stone statue.

Each hand can hold an item, carved from the same stone as the statue. In the editor, choose for the right and left
hand: keep what it holds, copy the item you are holding (the main hand for the right, the off hand for the left), or
leave it empty. The item is only copied; nothing is taken from you. A bust has no hands.

The preview turns on its own; switch off "Spin preview" to hold it still. Under it are two rows of < > buttons. **Turn
statue (saved)** turns the statue in steps of 22.5 degrees, and the compass direction it now has (for example
"south-southwest") is written between the buttons; that direction is saved as the way the statue faces, and a newly
placed statue looks at the player who placed it. A line above says which way the placed statue faced when you opened the
editor, since picking a direction only takes effect on Save. **Turn preview (unsaved)** turns only the picture in the
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

Statues can also be made of metal, in the same three sizes: craft them the same way from the game's riveted metal
blocks (for example gold). Metal statues are carved from the game's plain sheet texture for that metal (gold uses the smooth plate texture instead, without the speckles) and shine like the
game's metal blocks do (iron and lead, which do not shine in the game, do not shine here either). The metals are
bismuth, bismuth bronze, black bronze, brass, copper, cupronickel, electrum, gold, iron, lead, meteoric iron,
molybdochalkos, silver, steel and tin bronze.
