#!/usr/bin/env python3
"""Checks which zips in Releases/ are already deployed on mods.vintagestory.at.

For every zip, the modid and version are read from the modinfo.json inside it (falling back to the
`<modid>_<gameversion>_<version>.zip` file name), then compared with the releases the mod database
lists for that modid (https://mods.vintagestory.at/api/mod/<modid>).

Status per zip:
  deployed     this exact mod version is already listed
  new-version  the mod is listed, but not this version
  new-mod      no listing exists for this modid
  error        the lookup failed (network/API problem); nothing is assumed about the zip

A `new-version` zip also gets a warning when it skips a version number relative to the newest listed
version. Allowed next versions of 1.2.3 are 1.2.4, 1.3.0 and 2.0.0 (so 1.0.0 -> 1.2.0 warns).

Usage:
  ./check_deployed.py                          # table, exit 0
  ./check_deployed.py --json                   # machine readable
  ./check_deployed.py --fail-if-undeployed     # exit 1 if anything is not deployed
  ./check_deployed.py --fail-if-deployed       # exit 1 if anything already is (guard against re-publishing)
  ./check_deployed.py --fail-if-skipped        # exit 1 if a version number is skipped

Exit codes: 0 ok, 1 a --fail-if-* condition was met, 2 a lookup failed or the input was unusable.
Version warnings do not change the exit code unless --fail-if-skipped is given.

In GitHub Actions it also writes `undeployed` (JSON list of file names) and `any_undeployed`
to $GITHUB_OUTPUT, and a table to $GITHUB_STEP_SUMMARY. Only the standard library is used.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from dataclasses import asdict, dataclass, field
from pathlib import Path

DEFAULT_API = "https://mods.vintagestory.at/api/mod/"
FILENAME_PATTERN = re.compile(r"^(?P<modid>[a-z0-9]+)_(?P<game>[^_]+)_(?P<version>.+)\.zip$")

DEPLOYED, NEW_VERSION, NEW_MOD, ERROR = "deployed", "new-version", "new-mod", "error"


@dataclass
class Result:
    file: str
    modid: str = ""
    version: str = ""
    status: str = ERROR
    listed_versions: list[str] = field(default_factory=list)
    detail: str = ""
    warnings: list[str] = field(default_factory=list)


def parse_version(text: str) -> tuple[int, int, int] | None:
    """Parses MAJOR.MINOR.PATCH (a pre-release/build suffix is ignored). None if it doesn't fit."""
    match = re.match(r"^(\d+)\.(\d+)\.(\d+)", text.strip())
    return (int(match[1]), int(match[2]), int(match[3])) if match else None


def version_warnings(version: str, listed: list[str]) -> list[str]:
    """Warns if `version` is not the natural successor of the newest listed version.

    From X.Y.Z the natural successors are X.Y.(Z+1), X.(Y+1).0 and (X+1).0.0.
    """
    new = parse_version(version)
    known = [(parsed, text) for text in listed if (parsed := parse_version(text))]
    if new is None:
        return [f"version '{version}' is not MAJOR.MINOR.PATCH, so the numbering was not checked"]
    if not known:
        return []

    latest, latest_text = max(known)
    if new <= latest:
        return [f"{version} is not newer than the newest listed version {latest_text}"]

    major, minor, patch = latest
    allowed = [(major, minor, patch + 1), (major, minor + 1, 0), (major + 1, 0, 0)]
    if new in allowed:
        return []
    options = ", ".join(".".join(map(str, a)) for a in allowed)
    return [f"{version} skips version numbers after {latest_text} (expected one of: {options})"]


def read_modinfo(zip_path: Path) -> tuple[str, str]:
    """Returns (modid, version) from the zip's modinfo.json, falling back to the file name."""
    modid = version = ""
    try:
        with zipfile.ZipFile(zip_path) as archive:
            with archive.open("modinfo.json") as handle:
                info = json.loads(handle.read().decode("utf-8-sig"))
        # The game treats keys case-insensitively.
        info = {key.lower(): value for key, value in info.items()}
        modid = str(info.get("modid", "")).strip().lower()
        version = str(info.get("version", "")).strip()
    except (KeyError, zipfile.BadZipFile, json.JSONDecodeError):
        pass

    if not modid or not version:
        match = FILENAME_PATTERN.match(zip_path.name)
        if match:
            modid = modid or match["modid"]
            version = version or match["version"]
    return modid, version


def fetch_listing(api_base: str, modid: str, attempts: int = 3, timeout: float = 20.0) -> list[str] | None:
    """Returns the listed versions (newest first), or None if the mod is not listed.

    The API answers HTTP 200 even for unknown mods; those responses have no "mod" key.
    Raises OSError after the last failed attempt.
    """
    url = api_base + urllib.parse.quote(modid)
    last_error: Exception | None = None
    for attempt in range(1, attempts + 1):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": "nf3t-check-deployed"})
            with urllib.request.urlopen(request, timeout=timeout) as response:
                data = json.loads(response.read().decode("utf-8"))
            mod = data.get("mod")
            if not mod:
                return None
            return [str(release["modversion"]) for release in mod.get("releases", [])]
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return None
            last_error = error
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError) as error:
            last_error = error
        if attempt < attempts:
            time.sleep(2 * attempt)
    raise OSError(f"{url}: {last_error}")


def check(releases_dir: Path, api_base: str) -> list[Result]:
    results: list[Result] = []
    listings: dict[str, list[str] | None] = {}

    for zip_path in sorted(releases_dir.glob("*.zip")):
        modid, version = read_modinfo(zip_path)
        result = Result(file=zip_path.name, modid=modid, version=version)
        results.append(result)

        if not modid or not version:
            result.detail = "could not determine modid/version from modinfo.json or the file name"
            continue

        try:
            if modid not in listings:
                listings[modid] = fetch_listing(api_base, modid)
        except OSError as error:
            result.detail = str(error)
            continue

        listed = listings[modid]
        if listed is None:
            result.status = NEW_MOD
        else:
            result.listed_versions = listed
            result.status = DEPLOYED if version in listed else NEW_VERSION
            if result.status == NEW_VERSION:
                result.warnings = version_warnings(version, listed)
    return results


def render_table(results: list[Result]) -> str:
    rows = [("FILE", "MODID", "VERSION", "STATUS", "LISTED (newest first)")]
    for r in results:
        listed = ", ".join(r.listed_versions[:4]) + (", ..." if len(r.listed_versions) > 4 else "")
        rows.append((r.file, r.modid or "?", r.version or "?", r.status, listed or r.detail or "-"))
    widths = [max(len(row[i]) for row in rows) for i in range(len(rows[0]))]
    table = "\n".join("  ".join(cell.ljust(widths[i]) for i, cell in enumerate(row)).rstrip() for row in rows)
    warnings = [f"WARNING {r.file}: {w}" for r in results for w in r.warnings]
    return table + ("\n\n" + "\n".join(warnings) if warnings else "")


def write_github_outputs(results: list[Result]) -> None:
    undeployed = [r.file for r in results if r.status in (NEW_VERSION, NEW_MOD)]

    output_path = os.environ.get("GITHUB_OUTPUT")
    if output_path:
        with open(output_path, "a", encoding="utf-8") as handle:
            handle.write(f"undeployed={json.dumps(undeployed)}\n")
            handle.write(f"any_undeployed={'true' if undeployed else 'false'}\n")

    if os.environ.get("GITHUB_ACTIONS"):
        for r in results:
            for warning in r.warnings:
                print(f"::warning title=Version number::{r.file}: {warning}")

    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        lines = ["| File | Mod | Version | Status | Warnings |", "| --- | --- | --- | --- | --- |"]
        lines += [f"| {r.file} | {r.modid} | {r.version} | {r.status} | {'; '.join(r.warnings)} |" for r in results]
        with open(summary_path, "a", encoding="utf-8") as handle:
            handle.write("\n".join(lines) + "\n")


def main() -> int:
    parser = argparse.ArgumentParser(description="Check which zips in Releases/ are already deployed.")
    parser.add_argument("--releases-dir", type=Path, default=Path("Releases"))
    parser.add_argument("--api-base", default=DEFAULT_API, help=f"default: {DEFAULT_API}")
    parser.add_argument("--json", action="store_true", help="print JSON instead of a table")
    parser.add_argument("--fail-if-undeployed", action="store_true", help="exit 1 if any zip is not deployed")
    parser.add_argument("--fail-if-deployed", action="store_true", help="exit 1 if any zip is already deployed")
    parser.add_argument("--fail-if-skipped", action="store_true", help="exit 1 if any zip skips a version number")
    args = parser.parse_args()

    if not args.releases_dir.is_dir():
        print(f"Releases directory not found: {args.releases_dir}", file=sys.stderr)
        return 2

    results = check(args.releases_dir, args.api_base)
    if not results:
        print(f"No zip files in {args.releases_dir}", file=sys.stderr)
        return 2

    print(json.dumps([asdict(r) for r in results], indent=2) if args.json else render_table(results))
    write_github_outputs(results)

    if any(r.status == ERROR for r in results):
        print("\nSome lookups failed; see the detail column.", file=sys.stderr)
        return 2
    if args.fail_if_undeployed and any(r.status in (NEW_VERSION, NEW_MOD) for r in results):
        return 1
    if args.fail_if_deployed and any(r.status == DEPLOYED for r in results):
        return 1
    if args.fail_if_skipped and any(r.warnings for r in results):
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
