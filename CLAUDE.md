# Notes for Claude

Vintage Story mods (C#, .NET 10, solution `Nf3tMods.sln`). Each `*Mod/` folder is one mod with a `modinfo.json`;
`./build.sh` (Cake, `ZZCakeBuild/`) builds and packages them into `Releases/<modid>_<game>_<version>.zip`.

## Mod database pages (mods.vintagestory.at)

The ModDB API cannot create releases or edit pages, so the pages are edited by hand in the web UI. Their content
(description, links, tags, release changelogs, logo and screenshots) is kept in `moddb/<modid>/` and compared with
the live site by `vsdev moddb` (npm package `@nf3t/vsdev`, see `moddb/README.md`). The repo uses vsdev's default
layout (`moddb/`, `Releases/`), so no folder options are needed.

```
vsdev moddb <command>        # or: npx @nf3t/vsdev moddb <command>
```

The tool takes its modids from the zips in `Releases/`, so run `./build.sh` first when mods changed. Only
unauthenticated reads are made; never try to log in or write to the site.

### When to check whether mod info needs updating
After changing a mod's behavior, commands, version or README, and before telling the user a mod is ready to upload:

1. `vsdev moddb check` – is this version already uploaded (`deployed` / `new-version` / `new-mod`)? A warning means the
   version number skips (1.2.3 -> 1.2.4, 1.3.0 or 2.0.0 are the natural next versions).
2. `vsdev moddb status` – for each mod: uploaded or not, changelog written, page authored (no `TODO:` left).
3. `vsdev moddb diff` – exit 1 means `moddb/` differs from the live page (someone edited the site, or a local edit is not
   uploaded yet). Read the diff before changing anything; `vsdev moddb pull <modid>` overwrites local files with the live
   ones, so only run it when the live site is the intended truth (e.g. right after an upload) and review `git diff`. It asks `[y/N]`
   before replacing differing local files and needs `-y` without a terminal; don't pass `-y` unless the user said to discard local edits.
4. `vsdev moddb diff --fail-if-placeholders` – must exit 0 before an upload.

For the user's manual site updates, `vsdev moddb serve` starts a local read-only page with a per-mod checklist and copy buttons.

### What to update in `moddb/<modid>/`
- New or changed mod behavior: update `description.html` (HTML subset the site accepts: `p`, `ul/li`, `strong`, `code`, `pre`)
  and keep it consistent with the mod's `README.md`.
- New version: add `releases/<version>.html` (changelog) and `releases/<version>.json`. Never edit versions that are
  already live; `diff` shows local-only files as `pending`, which is expected until the upload.
- `page.json` must validate against `moddb/page.schema.json` (fields documented there; `diff` checks it).
  Leave `moddbId` null until the mod is published.
- Unpublished mod without a `moddb/` folder: `vsdev moddb scaffold <modid>`, then replace every `TODO:`.
- After the user uploads to the site: `vsdev moddb pull <modid>` then `vsdev moddb diff <modid>` should be clean.

### Limits
Draft pages are not visible to the API, so `vsdev moddb` reports those mods as not published; their `moddb/` files are
written by hand. Images in `moddb/<modid>/images/` are downloaded from the live page, not generated
(`CreateImages/` makes the files to upload).

## Statue rendering is mirrored in vsdev (keep in sync)

`StatueMod` builds a statue's mesh in code (`StatueMesh.cs`, `StatuePose.cs`). The same geometry is ported to TypeScript in the
vintage-story-wiki repo (`../vintage-story-wiki/libs/vs-schematic/src/statue.ts`, tested by `libs/vs-schematic/test/statue.test.ts`), so
`vsdev screenshots` can draw a statue (a `"statue"` scene part in `vs-manifest.json`) and `vsdev render` can draw statues inside
schematics. When you change how a statue looks or what its block entity stores (`StatueMesh.cs`, `StatuePose.cs`, `StatueAppearance.cs`
attribute names, `BlockEntityStatue.cs` scales, `StatueTypes.cs` specials, `StatueTack.cs`, `StatuePoses.cs`, `StatuePlatform.cs`,
`StatueMolding.cs`, or the textures in `assets/nf3tstatue/blocktypes/statue*.json`), make the same change in `statue.ts` and its test,
and say so to the user; the wiki repo's CLAUDE.md says the same in the other direction. `CreateImages/statue_shape.py` (the baked
render-only shape for the store images) predates the port: a `"statue"` scene part does the same without it.
