# Research notes

Findings and suggestions for five open questions. Nothing here has been implemented.

**How to read this.** Sources are the game C# in `/home/ubuntu/workspace/vintage-story-wiki`
(`vsapi`, `vsessentialsmod`, `vssurvivalmod`) and the shipped assets in `vintagestory/assets`.
`VintagestoryLib.dll` (the mod loader, the server chat pipeline) is closed source and not in the
wiki checkout. Anything that depends on it is marked **UNVERIFIED**, with a cheap way to check it.

Contents:

1. [Sharing map coordinates in chat](#1-sharing-map-coordinates-in-chat)
2. [Turning Translocator Navigator into a GUI](#2-turning-translocator-navigator-into-a-gui)
3. [Translocator Repair vs. other mods that change translocator classes](#3-translocator-repair-vs-other-mods-that-change-translocator-classes)
4. [What to do about Nf3tVSCommon](#4-what-to-do-about-nf3tvscommon)
5. [A reverse rift ward: a device that lowers stability and draws boss-tier rust creatures](#5-a-reverse-rift-ward-a-device-that-lowers-stability-and-draws-boss-tier-rust-creatures)

---

## 1. Sharing map coordinates in chat

Goal: click the map, press "Share", and everyone sees a clickable link in chat.

### What the game already gives us

| Piece | Where | Notes |
| --- | --- | --- |
| Clickable `worldmap://` links | `WorldMapManager.cs:140` (`onWorldMapLinkClicked`), registered at `:206` | Format is `worldmap://=X=Y=Z=optional text`. Used by the auction house (`AuctionPackets.cs:119`). Opens the map, centres it, **and adds a steelblue "circle" waypoint if none exists there** by sending `/waypoint addati ...`. |
| Custom link protocols | `ICoreClientAPI.RegisterLinkProtocol(name, handler)` | What `CenterMapMod` already uses for `panmap://`. |
| Map click interception | `MapLayer.OnMouseUpClient(MouseEvent, GuiElementMap)` | `GuiDialogWorldMap.OnMouseUp` (`:369`) calls every layer's `OnMouseUpClient` first and stops if `args.Handled` is set. Only after that does it open the vanilla right-click "add waypoint" dialog. |
| Screen to world coordinates | `GuiElementMap.TranslateViewPosToWorldPos` (`:231`) | The dialog wraps it as the private `loadWorldPos` (`:325-335`). |
| Adding controls to the map dialog | `MapLayer.ComposeDialogExtras(GuiDialogWorldMap, GuiComposer)` | Called once per layer when the dialog composes, only for `EnumDialogType.Dialog` (`GuiDialogWorldMap.cs:180-184`). This is the supported place to add a "Share" button. |
| Registering a layer | `WorldMapManager.RegisterMapLayer<T>(code, position)` | The registry is a plain dictionary (`:47`, `:88`). A layer registered only on the client is fine as long as it never syncs data with the server. The `MapLayerUpdate` packet looks layers up by code, so keep it client-only. |
| Server hook for chat | `IServerEventAPI.PlayerChat(IServerPlayer, int channelId, ref string message, ref string data, BoolRef consumed)` | Lets a server mod rewrite or swallow a message. |
| Server to client messages | `IServerPlayer.SendMessage(groupId, message, EnumChatType, data)` and `IServerAPI.SendMessageToGroup` | Server-composed text, so the server controls the markup. |
| Client to server custom packets | `api.Network.RegisterChannel(...)` | Already used by `SchematicLocatorMod`. `IClientNetworkChannel.Connected` lets the client detect a missing server counterpart. |

### Why it might need a server companion

`CenterMapMod` today does `capi.SendChatMessage("<a href=\"panmap://...\">...")`
(`CenterMapModSystem.cs:84`). That is a player chat message carrying raw markup. **UNVERIFIED:**
whether the server escapes markup in player-sent chat. If it does, the link renders as literal
text for everyone else. If it does not, any player could inject markup into other players' chat,
which would be a bug in the game.

Cheap test: with two clients on a server, run `.panmap` (or send `<a href="worldmap://=0=100=0=test">x</a>`
as a normal message) and see what the other client displays. The result decides everything below.

Separately, `SendPanmapLink` builds a `LinkTextComponent` and never uses it. It also sends and
locally echoes the same HTML. That is dead code to remove.

### Design options

**A. Client-only mod, no server.** Works only if the server passes markup through. No new network
surface, but it depends on the behaviour above, and the mod is not guaranteed to keep working. The
sender can also forge the markup.

**B. Client mod plus a small server mod (recommended).** One mod ID with `side: Universal` and a
`ModSystem` that behaves differently per side:

1. Client: a `MapLayer` subclass (`ShareMapLayer`). It adds a "Share location" button in
   `ComposeDialogExtras`, and optionally treats Ctrl+right-click as "share this point" in
   `OnMouseUpClient` (set `args.Handled = true` so the add-waypoint dialog does not open).
2. Client sends a `ShareLocationPacket { int X, Y, Z; string Label; }` on a custom channel.
3. Server validates (clamp the label length, strip markup from the label, optional per-player rate
   limit) and builds the chat line itself with
   `string.Format("{0} shared a location: <a href=\"worldmap://={1}={2}={3}={4}\">[{1}, {2}, {3}]</a>", ...)`.
   It sends that with `SendMessageToGroup(GlobalConstants.GeneralChatGroup, ..., EnumChatType.OthersMessage)`.
   The player name and label are escaped by the server.
4. Anyone, including players without the mod, gets a working link, because `worldmap://` is vanilla.

The cost is that the mod must be installed on the server for sharing to work. Clients should check
`channel.Connected` and show "this server does not have the share mod" instead of failing
silently. Mod-wise: `requiredOnServer: true`, `requiredOnClient: false`, because vanilla clients
can still read the output. The trade-off is that the sender needs the mod to originate shares, while
the receivers do not.

**C. A chat command only (`.share` or `/sharepos`).** The smallest version. It shares the player's
own position or a given position with the same server packet as B. It is a useful fallback and
covers players who never open the map.

### `worldmap://` or a custom `panmap://`?

* `worldmap://` works for everyone and needs no mod on the receiving side. Its side effect is the
  auto-created waypoint, which some users will dislike.
* A custom protocol (`panmap://`) can centre the map without adding a waypoint. It is only
  understood by players with the mod. For others, an unknown protocol is **UNVERIFIED** (it may do
  nothing, or it may try to open as a URL). I would not risk that.

Recommendation: use `worldmap://` on the wire. A mod-aware client can still intercept the link and
offer "pan only" (for example Shift-click) via `RegisterLinkProtocol("worldmap", ...)`, but that
would replace the vanilla handler and needs a closer look first.

### Suggested work order

1. Run the two-client test above and record the result here.
2. Move the sharing code out of `CenterMapMod` into a new `MapShareMod`. Do not keep stretching a
   client-only "centre map" mod. `CenterMapMod` can stay as-is or be folded in (see §2: the map
   click infrastructure is shared with the Navigator GUI).
3. Add the server half behind the same mod ID.

---

## 2. Turning Translocator Navigator into a GUI

### Current state

`TranslocatorNavigatorMod` is a client-only mod with three commands: `.pathtl`, `.pathtlhist`
and `.counttl`. It records translocators by polling `CurrentBlockSelection` every 200 ms, and
stores them in `Nf3tData.json` via `Nf3tVSCommon.Context`. Pathing is `TranslocatorPathResult`
(Dijkstra.NET). There is already a GUI precedent in this repo: `SchematicLocatorMod/SchematicSearchDialog.cs`
(a `GuiDialog`, a hotkey on Ctrl+Y, a text-input, button and table layout).

### Available building blocks (all in `vsapi/Client/UI`)

`GuiDialog` + `GuiComposer`; elements `AddTextInput`, `AddNumberInput`, `AddSmallButton`,
`AddDropDown`, `AddToggleButton`, `AddVerticalScrollbar`, `AddCellList<T>`, `AddDynamicText`,
`AddRichtext` (VTML, so links to `worldmap://` work inline), `AddHoverText`.

### Proposed UI

A dialog bound to a hotkey (suggest Ctrl+T, avoid clashing with Ctrl+Y):

```
+--- Translocator Navigator -----------------------+
| Goal   [ x ] [ y ] [ z ]   [Use map pin] [Waypoint v] |
| Start  [ x ] [ y ] [ z ]   [Use my position]           |
| [Find path]                    Known translocators: 128 |
|----------------------------------------------------|
| Next:  Translocator at (+120, 40, -300)  ↗ 85 m     |
| Path distance: 640   Bird's-eye: 910   Hops: 3      |
|  1  (+120, 40, -300) => (+4210, 52, 80)   [show]    |
|  2  ...                                              |
|----------------------------------------------------|
| History  [ list of previous goals ]    [Re-run]      |
+----------------------------------------------------+
```

* **Waypoint dropdown** is fed from the player's own waypoints. `WaypointMapLayer.ownWaypoints`
  (`WaypointMapLayer.cs:61`) is reachable through the `WorldMapManager` mod system
  (`capi.ModLoader.GetModSystem<WorldMapManager>()`).
* **Step list** is a scrolling `AddCellList` or `AddRichtext` with clickable steps (`worldmap://`
  links, which also solves "how do I get there": click and the map centres on that hop).
* **History** comes from `SaveData.LastTranslocatorPathPerSavegame` today (only the single last path).
  A GUI wants a short list (say 10) per savegame, so that structure needs a schema change (see §4).

### Map integration (the part that makes it feel native)

Put a `MapLayer` in the Navigator, like the share mod in §1:

* `ComposeDialogExtras` adds a "Navigate here" control to the world map dialog.
* `OnMouseUpClient` with a modifier click (for example Ctrl+right-click) sets the goal at the
  clicked point. This is the same mechanism as sharing, so one "map actions" layer could serve both.
* `Render(GuiElementMap, float)` draws the computed route over the map. `GuiElementMap` gives
  `TranslateWorldPosToViewPos`. For line drawing, `IRenderAPI` only has a world-space
  `RenderLine(BlockPos origin, ...)`; for 2D map overlays the usual approach is to draw into a
  texture via Cairo in a `MapComponent` (see `WaypointMapComponent`) or render thin quads with
  `Render2DTexture`. Picking one needs a prototype. Start with markers on each hop
  (`MapComponent` per translocator) and add connecting lines later.

### Practical concerns

* **Threading.** Dijkstra over a few thousand nodes is fine on the main thread. If the graph grows,
  compute off-thread and hand the result back with `capi.Event.EnqueueMainThreadTask`.
* **Context.Save and the shared file** (see §4): a GUI makes the "two mods overwrite
  `Nf3tData.json`" problem more visible. Fix that first.
* **Translocators are only learned by looking at them.** A GUI should say so. The empty state
  ("Known translocators: 0, look at a translocator to record it") will be the first thing a new
  user sees. A "scan nearby" button that reuses the Locator mod's chunk scan would help but
  creates a Navigator-Locator dependency (§4).
* **Localization.** The existing commands use English string literals. A GUI should go through
  `Lang.Get` and a `lang/en.json` from the start.

### Suggested work order

1. Fix persistence (§4).
2. Dialog with goal and start inputs, the Find button and text results. This reuses `Context`
   unchanged.
3. Hotkey plus the history list.
4. Map layer: click to set goal, hop markers, then route lines.

---

## 3. Translocator Repair vs. other mods that change translocator classes

### How the mod works today

`TranslocatorRepairMod/assets/survival/patches/translocator_class_patch.json` does a `replace`
of `/classByType` on `game:blocktypes/machine/statictranslocator`, mapping `"*"` to
`BlockRepairableStaticTranslocator` (a subclass of `BlockStaticTranslocator`).

Vanilla is:

```json
classByType: { "*-unrepairable": "Block", "*": "BlockStaticTranslocator" }
```

### Problems

1. **Last patch wins.** Any other mod that replaces `/classByType` (or `/class`) on the same file
   silently overwrites ours, or is overwritten by ours, depending on load order. There is no error.
   Whoever loses just stops working.
2. **We widened the scope.** We replaced the whole map, so `*-unrepairable` (which vanilla gives
   plain `Block`) now also gets our class. It works, but it makes the class "own" every state.
3. **A subclass can only be a subclass of one parent.** If another mod subclasses
   `BlockStaticTranslocator` too, only one of the two classes can be in use.

### Recommended fix: use a block behavior instead of a block class

Behaviors compose. The game builds a list of `BlockBehavior`s per block, and several mods can each
add one without a conflict:

* Vanilla patches do exactly this: `{"op": "add", "path": "/behaviors/-", "value": {"name": ...}}`
  (see `assets/survival/patches/fallingblocks.json`).
* `Block.OnBlockInteractStart` (`Block.cs:1436`) iterates behaviors and calls
  `behavior.OnBlockInteractStart(world, byPlayer, blockSel, ref handled)` (`BlockBehavior.cs:231`).
* `Block.GetPlacedBlockInteractionHelp` (`Block.cs:2179`) also asks behaviors
  (`BlockBehavior.cs:256`) for hints.
* The `*-unrepairable` variant is a plain `Block` in vanilla, so behaviors fire there with no custom
  class at all. This is the only state we care about repairing.

Sketch:

```csharp
public class BlockBehaviorTranslocatorRepair(Block block) : BlockBehavior(block)
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (block.Variant["state"] != "unrepairable") return false; // leave handling as PassThrough
        // ... same repair logic as BlockRepairableStaticTranslocator ...
        handling = EnumHandling.PreventDefault;
        return true;
    }
}
```

```json
[
  {
    "file": "game:blocktypes/machine/statictranslocator",
    "op": "add",
    "path": "/behaviors/-",
    "value": { "name": "TranslocatorRepair" }
  }
]
```

Register with `api.RegisterBlockBehaviorClass("TranslocatorRepair", typeof(...))`. No class is
replaced, so the mod no longer fights with other translocator mods over `class` or `classByType`.

Caveats:

* **Block info is behavior-aware.** `Block.GetPlacedBlockInfo` appends each behavior's
  `GetPlacedBlockInfo` (`Block.cs:2330`), so the "Seems to be missing a cupronickel rod." text can
  move into the behavior. Check the ordering and duplication against the vanilla text.
* **A mod that gives the unrepairable state its own class** which does not call `base` would skip
  behaviors. Unlikely, and the failure is only "repair does nothing".
* **Another mod that makes translocators repairable** (for example by adding its own interaction on
  the same item) would race with ours. `EnumHandling.PreventDefault` ends the chain, so order by
  mod load order. Document it, or add a config option.
* The patch must be applied on both sides. It already is: the mod is `Universal`, required on both.

### Alternative: Harmony

Harmony (`0Harmony.dll`, already in `$(VINTAGE_STORY)/Lib`) can postfix
`BlockStaticTranslocator.OnBlockInteractStart` and `GetPlacedBlockInfo`. It works regardless of
other mods' classes, but only if their subclasses call `base`. A subclass that overrides without
calling `base` bypasses a patch on the parent entirely. Harmony is also a global patch that can
conflict with other Harmony users, and it is harder to debug. It is the fallback if behaviors prove
insufficient. The general community guidance is to prefer data (JSON patches, behaviors) and
subclassing before Harmony. I have not checked this against the wiki.

### Suggested work order

1. Prototype the behavior approach in a copy of the mod. Test the `unrepairable` interaction, the
   hint text and what happens with the vanilla `BlockStaticTranslocator` interaction.
2. Test against a mod that replaces `classByType` on the same file (write a 10-line dummy mod for it)
   to confirm both load.
3. Ship it as a new version of `nf3ttranslocatorrepair`. Keep the old class registered for a
   release so worlds and patches that reference `BlockRepairableStaticTranslocator` still load.

---

## 4. What to do about Nf3tVSCommon

### What actually happens today

`Nf3tVSCommon` is a `ProjectReference` from `TranslocatorLocatorCmdMod` and
`TranslocatorNavigatorMod`. Reference copy-local is on by default, so **each mod zip ships its own
copy of `Nf3tVSCommon.dll` (33 280 bytes) and `Dijkstra.NET.dll`**. Verified by listing the zips
in `Releases/`.

**UNVERIFIED but likely:** when two mods ship an assembly with the same simple name, .NET resolves
the name once per load context. Whichever mod loads first supplies the `Nf3tVSCommon` type
definitions for both. If the two zips contain different builds (say the user updated only one), the
mod loaded second runs against the other mod's version. That fits "weird issues when versions
differ". The check is a one-liner: log `typeof(Context).Assembly.Location` from each mod at startup
and see whether they print the same path. (`ModAssemblyLoader` in `VintagestoryLib.dll` does the
loading, and I cannot read it from here.)

Two other problems showed up in the code:

1. **The two mods clobber each other's save file.** Both create their own `Context`, which loads
   `ModData/Nf3tData.json` once at startup and rewrites the entire file on save
   (`Context.Save`). `Context` holds `TranslocatorsPerSavegame`, `WayPointsPerSavegame`, and so on.
   Whichever mod saves last overwrites whatever the other added since startup. Today this mostly
   works because the Locator's scan and the Navigator's look-at tracking both feed
   `TranslocatorsPerSavegame`, but each only has what it saw itself.
2. **Common has no version of its own.** `SerializedSaveData` has no schema version, so any change
   to its shape has to guess what old files look like.

### Options

| Option | Fixes version skew | Fixes the save file | User cost | Effort |
| --- | --- | --- | --- | --- |
| **A. Shared source, not a shared assembly.** Compile the Common `.cs` files into each mod (`<Compile Include="..\Nf3tVSCommon\**\*.cs" Link="Common\%(RecursiveDir)%(Filename)%(Extension)" />`). Make the types `internal`, or give each mod its own root namespace. | Yes, each mod has its own private copy and nothing is shared across mods. | No, still two writers. | None | Small |
| **B. Real dependency mod.** Ship `nf3tcommon` as its own mod. The other mods list it under `dependencies` and reference it with `<Private>false</Private>`. | Mostly. The game enforces that the dependency is present and meets the minimum version (`ModDependency.Version` is a minimum), and only one copy exists. | Only if the library owns the file (see below). | Users install two mods | Medium |
| **C. Merge into one mod** (`Translocator Tools`: Locator, Navigator, Repair behind one modid). | Yes, since there is only one assembly. | Yes, one `Context`. | One download. Also loses independent install. | Medium-large. Changes mod IDs, so it needs a migration story. |
| **D. IL-merge** (ILRepack or similar) into each mod's DLL. | Yes. | No. | None | Small build change, but adds a tool to the build and signing/debugging quirks. |

### Recommendation

1. **Short term: option A plus fixing the file.** It is a build-only change that removes the shared
   assembly and the "which copy loaded" question. Make `Context` and the types `internal`. Give
   each mod its own file (`Nf3tLocator.json`, `Nf3tNavigator.json`) or, better, make the writer
   merge on save: re-read the file, apply this mod's changes and write back. For translocator edges
   the merge is trivial (a union keyed by `SimplePos`). Add a `"version": 1` field to
   `SerializedSaveData` now, while there is only one old format to migrate from.
2. **If the mods are going to share runtime state** (the Navigator GUI wants the Locator's scan, §2),
   then **B or C**. Pick C if the mods will mostly be used together. Pick B if there is value in
   people installing only one. Do not use A and then re-introduce sharing through files.
3. **For client/server version skew in general:**
   * Client-only mods already say `side: Client`, `requiredOnServer: false`. Keep it. Version
     mismatches with the server do not matter there.
   * For mods with network messages or patches that must match (`TranslocatorRepairMod`,
     `SchematicLocatorMod`, and any future share mod), set `networkVersion` in `modinfo.json`
     (`ModInfo.NetworkVersion`, `ModInfo.cs:79`). It is separate from `version` and tells the
     game when an old client must not connect to a newer server. Bump it only when the protocol or
     patch contract changes, not on every release.
   * Do not put message classes in a shared library that is a separate mod. Keep them in the mod
     that owns the channel.

### Things to verify before choosing

* The two-mods-one-assembly behaviour above (log the assembly location).
* Whether the Cake `Package` task should keep putting `Nf3tVSCommon.dll` and `Dijkstra.NET.dll`
  inside both zips. It currently does. With option A the shared DLL disappears from the zips.
  `Dijkstra.NET` is only used by `TranslocatorPathResult` in Common, so the Locator ships it only
  because Common references it. Moving the path code into the Navigator (or compiling it only there
  under option A) drops it from the Locator zip.

---

## 5. A reverse rift ward: a device that lowers stability and draws boss-tier rust creatures

**Built** as the Entropy Rift (`EntropyRiftMod`): its own spawner as in "Design" step 3 (with a boss guarantee and a boss
refund), the stability drain of step 1 through `OnGetTemporalStability` on both sides, plus a server-side floor (0.15) on the
players' own stability near it outside storms, since the drain alone takes them all the way to 0. See the mod's README.

Goal: a placeable Jonas-style device that does the opposite of stabilising. Around it, temporal
stability drops and the rust creatures that spawn are the top tier, up to the "gearfoot" tier.

Sources: game version 1.22.7. Survival code is in `vssurvivalmod/Systems/TemporalStability/`. The
runtime spawner (`ServerSystemEntitySpawner`, `EntitySpawnerResult`) is in the closed
`VintagestoryLib.dll` and was read with `ilspycmd`.

### What "gearfoot tier" means in the game files

*Gearfoot* is a **bowtorn** variant (`bowtorn-gearfoot`), not a drifter. It sits one step above
nightmare. Each rust-creature family has an equivalent:

| Family | Normal tiers (storm/upgrade order) | Above nightmare | Max HP (nightmare → special) |
| --- | --- | --- | --- |
| Drifter | normal, deep, tainted, corrupt, nightmare | `drifter-double-headed` | 40 → 54 |
| Shiver | surface, deep, tainted, corrupt, nightmare | `shiver-deepsplit` (also stilt, bellhead) | 45 → 60 |
| Bowtorn | surface, deep, tainted, corrupt, nightmare | `bowtorn-gearfoot` | 32 → 50 |

How each one appears in vanilla:

* **Gearfoot never spawns on its own.** Its runtime `maxQuantity` is `0` (`entities/lore/bowtorn.json`,
  `spawnconditions.runtime.maxQuantityByType`). It is not in `config/mobextraspawns.json` and no
  worldgen asset names it. Only commands or creative spawning produce one. Its drops are 1 ±0.4
  temporal gears plus about 5 rusty gears, and a kill restores 0.35 of the killer's stability
  (`onDeathStabilityRecoveryByType`).
* **Double-headed drifter and deepsplit shiver** spawn only during temporal storms, as
  `rareSpawns` (`chancePerStorm: 1`, group `boss`, at most one per player per storm,
  `TemporalStability.cs:575-586`).
* **Nightmare** spawns naturally only very deep (`maxY 0.23` × sea level). It can also appear
  anywhere through the low-stability upgrade below.

So the design has two parts. Lowering stability is enough to get **nightmare** tier everywhere.
**Gearfoot/boss** tier needs the device to spawn mobs itself.

### How stability and spawning work now

1. **Location stability.** `SystemTemporalStability.GetTemporalStability(x,y,z)`
   (`TemporalStability.cs:852`) is a noise value (surface 0.8–1.5, lower deeper down) minus storm
   strength. At the end it calls the **public event `OnGetTemporalStability`** and returns the
   handler's result **without clamping it**. The game already uses this hook:
   `StoryStructuresSpawnConditions` (`Lore/StoryStructuresSpawnConditions.cs:27,76`) forces 1.0
   inside the Resonance Archives. It subscribes in `Start()`, so **both sides** use it.
2. **Player stability** (`BehaviorTemporalStabilityAffected.cs:333-344`). This behavior runs on
   client and server. Each tick it moves the player's own stability (`WatchedAttributes["temporalStability"]`)
   by `(here - 1)`. Below 1 it loses about `(1 - here) / 800` per second. Above 1 it gains about
   `(here - 1) / 200` per second. Worked out: in a spot with stability 0, a player at 100% reaches
   0.25 in about 10 minutes and 0 in about 13 minutes. A value of −3 (allowed, because the event
   isn't clamped) is 4× faster. Rifts use a separate `stabilityOffset` of up to −20
   (`Rifts.cs:181`) but reset it to 0 every 100 ms, so a mod shouldn't use that field.
3. **What low player stability does already.** Below 0.3: fog and rust-coloured rain. Below 0.2:
   glitch shader. Below 0.13: poison damage every 4 s (`:376-387`).
4. **The runtime spawner** (closed source) picks a type that passes its depth band
   (`minY`/`maxY` relative to sea level), its `chance` and its caps. Then:
   * `CanSpawnAt` asks the **nearest player's** `EntityPlayer.OnCanSpawnNearby`. The stability system
     sets that to `SystemTemporalStability.CanSpawnNearby` (`:788`). For types with
     `spawnCloserDuringLowStability` (all three families), low **player** stability scales away the
     light-level limit, so they spawn next to torches. Below 0.125 it forces spawns within 10 blocks.
     **However, on the open surface** (sunlight 16) they still need a rift within 20 blocks at night,
     or within 6 blocks by day at 7%.
   * `EntitySpawnerResult.Spawn` then fires `OnTrySpawnEntity` with `ref EntityProperties`. The
     stability system's handler (`:640-693`) upgrades the type when the nearest player's stability
     (or the storm) is below 0.25: `index += round((0.25 - stab) * 15)`, capped at nightmare. A
     player at ≤ 0.017 stability turns every surface drifter into a nightmare one. The family can also
     change (drifter → shiver/bowtorn) according to the current storm pattern weights. The spawner
     records the original code in `Attributes["originaltype"]`, so upgraded mobs still count toward
     the **original** type's cap.
   * The upgrade never goes past index 4. Bosses come only from the storm code's own spawner
     (`trySpawnForPlayer`/`DoSpawn`, `:505-636`). That spawner ignores spawn caps and
     `CanSpawnNearby`, tags mobs `origin = "timedistortion"`, sets `ignoreDaylightFlee`, and
     schedules a despawn through `timeddespawn`.

### Design

One mod (`side: Universal`, required on both sides). It would be modelled on `BlockEntityRiftWard`
(`BlockEntity/BERiftWard.cs`): temporal-gear fuel counted in days, toggled by right click with an
empty hand, an animated spinning model and a looping ambient sound. Working names: *Rift lure* or
*Temporal destabilizer*.

**1. Lower stability around the device (cheap, no patches).**

* `ModSystemDestabilizer.Start()` (both sides) subscribes to `OnGetTemporalStability`. The handler
  returns `min(stability, deviceValue(dist))` for the nearest active device. Example falloff: −1 at
  the core, rising to the vanilla value at radius R (say 24–32 blocks). Keep the value ≥ 0 at first;
  going negative is a balance decision.
* The device list has to exist **on the client as well**. Otherwise the client predicts other
  values and the stability gear, sounds and glitch effects disagree with the server. The block entity
  adds itself in `Initialize` and removes itself in `OnBlockUnloaded`/`OnBlockRemoved` on both sides.
  `On` and fuel are synced through `ToTreeAttributes`/`MarkDirty`, like the rift ward.
* The handler runs **every tick for every player**, on both sides, and also off-thread from the
  spawner. Use a small immutable array that is swapped on change, and compare squared distances.
  Don't take locks or do LINQ in the handler.

What this gives for free: player stability drains near the device, along with the glitch, fog,
rust-rain and poison effects, and vanilla spawns become nightmare tier once a player is near 0. The
mobs ignore light underground and spawn close. Killing them restores stability, so the player has to
hold a balance.

**2. Upgrades based on the device, not the player (optional).** A second `OnTrySpawnEntity`
handler can upgrade by distance to the device, so mobs reach the top tier before the player's own
stability is gone. It runs after vanilla's handler because survival registers earlier. Check the
order with a log line before relying on it.

**3. Boss tier: the device's own spawner (needed for gearfoot).** This copies the storm spawner
pattern (`trySpawnForPlayer` + `DoSpawn`):

* Server tick every few seconds while the device is on, fuelled, and a player is within R.
* Tier table in a JSON config (for example `config/nf3t…-spawns.json`) with weights by device
  strength or fuel tier: nightmare variants commonly; `drifter-double-headed`, `shiver-deepsplit`,
  `bowtorn-gearfoot` rarely.
* Before spawning: count this mod's mobs in range through `EntityPartitioning.WalkEntities` with a
  marker attribute (for example `origin = "nf3tdestabilizer"`) and enforce a cap, at most one boss alive.
  Use `CollisionTester` against `SpawnCollisionBox` and walk down to solid ground, as in `:596-605`.
* After spawning: `Attributes.SetBool("ignoreDaylightFlee", true)`, so the mobs work on the surface by
  day. Optionally set a timed despawn. The vanilla `despawn` behaviors (more than 32 blocks away
  for 30 s) already remove them when players leave.
* This path never calls `CanSpawnNearby`, so the **surface/rift rule doesn't apply** and the device
  works outdoors. The alternative, injecting a permanent rift (`ModSystemRifts.riftsById` +
  `NextRiftId` + `BroadCastRifts`, all public), depends on how `ServerRifts` and `KillOldRifts`
  treat foreign rifts. It is unverified and more fragile.

**4. Cost and risk (balance).** Gearfoot drops a temporal gear, so the device is a gear farm. The
gear fuel cost per day should be higher than the expected gear yield, or bosses should drop nothing
while the device is involved. A drop-table override would need a Harmony patch or a separate entity
variant. Device-spawned mobs ignore vanilla caps, so the mod's own cap is the only limit. Rift
wards block rifts but don't touch stability, so they don't counter this device. Server owners
probably want a world-config switch to disable it.

### Things to verify before building

* **The rate figures are worked out from the code, not measured.** Check them with
  `/entity cmd` or by reading the player's `temporalStability` over time near a test device.
* **Handler order** of `OnTrySpawnEntity` (survival first, then this mod) if step 2 depends on it.
* **Client/server agreement.** With the client hook missing, the stability gear should visibly
  disagree. That proves the hook needs to be registered on the client.
* **Negative stability values.** Only `BehaviorTemporalStabilityAffected` reads
  `GetTemporalStability`, and it clamps the player's own value. Check that sounds and effects in
  `updateSoundsAndEffects` don't misbehave below 0 before allowing it.
* **Interaction with storms.** During a storm, vanilla already lowers stability and runs its own
  boss spawns. The device cap should probably count `origin = "timedistortion"` mobs too.

### Is the stability loss permanent?

No. There are two separate values:

* **The area around the device.** This is computed fresh on every call (`GetTemporalStability`), and
  nothing is written to the world. When the device is turned off, runs out of fuel or is broken, the
  area is back to its normal value straight away.
* **The player's own stability** (`WatchedAttributes["temporalStability"]`) is saved and changes
  slowly. Vanilla restores it in three ways:
  * Standing where location stability is **above 1** (much of the surface; the noise gives 0.8–1.5).
    It gains `(here - 1) / 200` per second, so at 1.5 it goes from 0 to full in about 7 minutes.
    Where the area value is between 0.8 and 1 it keeps falling slowly, as in vanilla.
  * Killing rust creatures: `onDeathStabilityRecovery`, from +0.01 (surface tier) to +0.3 (nightmare
    bowtorn) and +0.35 (gearfoot).
  * The knife ritual: knife in the main hand and a temporal gear in the off hand restores +0.30
    (`ItemKnife.cs:146`).
  * **UNVERIFIED:** whether dying resets it. Check in game.

### Simpler alternative: a spawner block that rarely rolls a boss

This drops the stability change entirely. A placed block spawns drifters (or a mix of rust
creatures) and has a small chance per spawn of a boss-tier mob. The game already has most of this:

* **`BlockEntitySpawner`** (`BlockEntity/BESpawner.cs`) is the creative "meta spawner" block
  (`game/blocktypes/meta.json`, `meta-spawner`). It is a public class with `virtual OnGameTick`,
  `virtual DoSpawn` and `virtual GetNextHerdId`, and public `Data` (`BESpawnerData`). It already
  provides:
  * `EntityCodes` (one is picked at random each attempt)
  * `MaxCount` (the most it keeps alive; dead or despawned mobs are removed from its list)
  * `InGameHourInterval` and `GroupSize`
  * `SpawnArea` (a cuboid relative to the block)
  * `SpawnRangeMode` with `MinPlayerRange`/`MaxPlayerRange` (for example: spawn only while a player is within N blocks)
  * a charge system: `InternalCapacity` + `RechargePerHour`. A mob that despawns alive gives its
    charge back, which works as a fuel or charge mechanic.
* What stops it being a survival block as it is: the settings dialog opens only in creative
  (`BlockSpawner.cs:17`, `BESpawner.cs:526`). `SpawnOnlyAfterImport` defaults to `true`, so a
  hand-placed spawner does nothing unless it came from a schematic.
* **The hook:** each attempt fires `sapi.Event.PushEvent("onattemptspawnerspawn", tree)` with
  `type` and `pos`, then reads `type` back (`BESpawner.cs:252-256`). A mod can rewrite `type` there.
  The event is global, though, so it also reaches spawners in vanilla structures. Filter on `pos`.

**Suggested build:** a survival block whose block entity **subclasses `BlockEntitySpawner`**.

1. In `Initialize`, fill `Data` from the block's JSON attributes, so the numbers can be tuned
   without code changes:
   * `EntityCodes`: `drifter-normal` … `drifter-nightmare`
   * `MaxCount` ≈ 3
   * `InGameHourInterval` ≈ 0.25
   * `SpawnOnlyAfterImport = false`
   * `SpawnRangeMode = WhenInRange` with `MinPlayerRange` ≈ 24
   * optional charge settings
2. Rare boss roll: override `DoSpawn(entityType, pos, herdId)`. With probability p (e.g. 0.02),
   swap `entityType` for one entry of a `bossCodes` list (`bowtorn-gearfoot`, `drifter-double-headed`,
   `shiver-deepsplit`) before calling `base.DoSpawn`. Keep a separate limit of one boss alive. A pity
   counter (raise p after each miss) makes it feel fair. Note that the base class checks for space
   using the **original** type's collision box. The gearfoot is 0.7 × 2.3 blocks, a little taller than
   a drifter, so it could occasionally clip a ceiling. A collision re-check in the override avoids that.
3. After `base.DoSpawn`, tag the entity (for example `origin = "nf3tdriftspawner"`). For daytime use on
   the surface also set `Attributes["ignoreDaylightFlee"] = true`, as the storm spawner does.
4. Activation: a survival right-click (toggle, or feed temporal or rusty gears for charge), modelled
   on the rift ward. Don't use the creative dialog.

**Compared with the stability device:**

| | Stability device (§5 design) | Spawner block |
| --- | --- | --- |
| Players hurt by low stability | yes, recoverable as above | no |
| Code size | event hook + client sync + own spawner | one block entity subclass + block JSON |
| Patches/Harmony | none | none |
| Respects vanilla spawn caps, light, rift rules | partly | no, the block is the only limit |
| Boss tier | needs its own spawner anyway | built into the `DoSpawn` override |

The spawner is the smaller and safer build. The balance concern from §4 still applies: each
gearfoot drops a temporal gear, so p, the interval, and any fuel cost set the gear-per-day rate.

Checked: `BlockEntitySpawner` saves its spawned-entity IDs (`BESpawner.cs:572-583`). But each
tick it drops any ID that isn't in `LoadedEntities` (`:296-303`), so a mob that wandered into an
unloaded chunk stops counting toward `MaxCount`. The cap is "alive and loaded", not a hard limit.
With the vanilla `despawn` behaviors (32+ blocks from players) this rarely matters.
