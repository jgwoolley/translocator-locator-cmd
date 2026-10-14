# GildedChronicles - Structure Log

https://mods.vintagestory.at/show/mod/73807

Server-side only mod. Whenever world generation creates a new structure, a line is written to the server log:

`[StructureLog] Generated structure 'code' (group: g) at (x1, y1, z1) to (x2, y2, z2)`

## Filtering

Settings are stored in `ModConfig/nf3tstructurelog.json`:

```json
{ "Mode": "Denylist", "Patterns": ["*lakes", "*vugs"] }
```

* `Denylist` (default): log everything except structures matching a pattern. An empty list logs everything.
* `Allowlist`: log only structures matching a pattern. An empty list logs nothing.

Patterns are case-insensitive wildcards (`*`) that must match the whole structure code (for example
`s-kitchen1.json/undergroundruin`) or its group, so use `*ruin*` rather than `ruin`.

Admins (the `controlserver` privilege) can change this without editing the file. Changes apply immediately and are saved.

| Command | Effect |
| --- | --- |
| `/structurelog list` | Show the mode and patterns |
| `/structurelog mode allowlist\|denylist` | Switch mode |
| `/structurelog add <patterns>` | Add patterns (separated by spaces or commas) |
| `/structurelog remove <patterns>` | Remove patterns |
| `/structurelog set <patterns>` | Replace all patterns (same as `clear`, then `add`) |
| `/structurelog clear` | Remove all patterns |
