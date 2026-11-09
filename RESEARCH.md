# Research notes

Findings and suggestions for four open questions. Nothing here has been implemented.

**How to read this.** Sources are the game C# in `/home/ubuntu/workspace/vintage-story-wiki`
(`vsapi`, `vsessentialsmod`, `vssurvivalmod`) and the shipped assets in `vintagestory/assets`.
`VintagestoryLib.dll` (the mod loader, the server chat pipeline) is closed source and not in the
wiki checkout. Anything that depends on it is marked **UNVERIFIED**, with a cheap way to check it.

Contents:

1. [Sharing map coordinates in chat](#1-sharing-map-coordinates-in-chat)
2. [Turning Translocator Navigator into a GUI](#2-turning-translocator-navigator-into-a-gui)
3. [Translocator Repair vs. other mods that change translocator classes](#3-translocator-repair-vs-other-mods-that-change-translocator-classes)
4. [What to do about Nf3tVSCommon](#4-what-to-do-about-nf3tvscommon)

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
