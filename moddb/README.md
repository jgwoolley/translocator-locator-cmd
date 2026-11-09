# moddb

Source-controlled copy of what the mod database (https://mods.vintagestory.at) shows for each mod. The
ModDB API is read-only for us, so changes are still made by hand in the web UI; this folder records them
and lets `vsdev moddb` tell you when the site and the repo disagree.

## Layout (`moddb/<modid>/`)

| File | Content |
| --- | --- |
| `page.json` | `modid`, `moddbId`, optional `moddbPageId` (draft pages), `name`, `side`, `type`, `urlalias`, link fields (`homepageurl`, `sourcecodeurl`, `trailervideourl`, `issuetrackerurl`, `wikiurl`), sorted `tags`, `logo` (`logo.<ext>`), `screenshots` (file names, in order) |
| `description.html` | The page description, as the HTML the site stores |
| `releases/<version>.json` | `version`, `gameVersions`, `filename` of a release |
| `releases/<version>.html` | That release's changelog HTML |
| `images/` | The logo (`logo.<ext>`) and screenshots, downloaded from the site by `pull` |

`page.json` is described by `page.schema.json`; VS Code picks it up through `.vscode/settings.json`.

Volatile values (downloads, follows, comments, timestamps, CDN urls) are deliberately not stored.
Mods without a live page have no folder until you create one by hand.

## Workflow

The tool is `vsdev moddb` from the npm package [`@nf3t/vsdev`](https://www.npmjs.com/package/@nf3t/vsdev)
(`npm install -g @nf3t/vsdev`, or `npx @nf3t/vsdev moddb <command>`; from here on `moddb`). This folder is
vsdev's default pages folder, so no `--pages-dir` is needed.
It reads the modids from the zips in `Releases/`, so build first (`./build.sh`).

```
moddb check                   # which zips are already deployed
moddb scaffold [modid ...]    # placeholder pages for mods that are not published yet
moddb pull [modid ...]        # overwrite moddb/ with the live state
moddb diff [modid ...]        # live vs moddb/; exit 1 if different or page.json is invalid
moddb serve                   # local web page: checklist, live vs local, copy buttons
moddb status                  # local version vs newest listed, changelog drafted, page authored
moddb export                  # static site of the pages with the newest zips (.vsdev/site, see compose.yml)
```

Shared options: `--releases-dir`, `--pages-dir`, `--json`, `--offline` (network commands exit 0 doing nothing).
Exit codes: 0 ok, 1 a condition was met (a difference, `--fail-if-*`), 2 a lookup failed or the input was unusable.

First upload of a new mod:
1. `moddb scaffold` creates `moddb/<modid>/` with `moddbId: null`, the name/side/description from the zip's
   `modinfo.json`, and `TODO:` placeholders for the description and the changelog.
2. Fill in the placeholders (and tags, links); `moddb diff --fail-if-placeholders` exits 1 until none are left.
3. Create the page and upload the zip in the web UI.
4. `moddb pull <modid>` replaces the scaffold with what the site stored (it now has a `moddbId`, logo and screenshots).

Publishing a new version of a published mod:
1. Write `moddb/<modid>/releases/<version>.html` (the changelog) and edit `description.html` / `page.json`
   if the page changes; commit.
2. Upload the zip and paste the changelog/description in the web UI.
3. `moddb pull <modid>` (answer `y` if it lists the files you just wrote); `git diff` should then show only normalization noise, if anything.

`diff` lists local release drafts that are not live yet as `pending`, and does not treat them as a difference.
Everything else (edited locally and not uploaded, or edited on the site and not pulled) makes `diff` exit 1.
Dotfiles (`.gitignore`) in a mod folder are ignored.
