#!/usr/bin/env bash
# Builds and packages every mod (see ZZCakeBuild/). Bootstraps what the build needs first:
#   1. vsdev (npm package @nf3t/vsdev): used from PATH, else run through npx
#   2. ./vintagestory: the game's DLLs and assets the csproj files reference ($(VINTAGE_STORY)), fetched with `vsdev game`
#      (the dedicated server; an existing install there is left alone)
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

if command -v vsdev >/dev/null 2>&1; then
  VSDEV=(vsdev)
else
  command -v npx >/dev/null || { echo "vsdev not found and no npx to run it; install node (22+) or: npm install -g @nf3t/vsdev" >&2; exit 1; }
  VSDEV=(npx --yes @nf3t/vsdev)
fi

"${VSDEV[@]}" game --server --game-dir "$PWD/vintagestory"

dotnet run --project ./ZZCakeBuild/CakeBuild.csproj -- "$@"
