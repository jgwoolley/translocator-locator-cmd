#!/usr/bin/env bash
# Downloads and unpacks the Vintage Story linux-x64 build into ./vs_server.
#
# Usage:   ./extract.sh [branch] [version]
#          VS_BRANCH=unstable VS_VERSION=1.22.0-rc.1 ./extract.sh
#
# branch:  stable (default) or unstable. Arguments win over the environment variables.
# version: e.g. 1.22.0 (default). It must exist on the chosen branch.
set -euo pipefail

VS_BRANCH="${1:-${VS_BRANCH:-stable}}"
VS_VERSION="${2:-${VS_VERSION:-1.22.7}}"

VS_SERVER_URL="https://cdn.vintagestory.at/gamefiles/${VS_BRANCH}/vs_client_linux-x64_${VS_VERSION}.tar.gz"

echo "Downloading ${VS_SERVER_URL}"
# Download first, so a bad branch/version doesn't delete the existing ./vs_server.
# -f: fail on HTTP errors (e.g. 404) instead of saving the error page; -L: follow redirects.
curl -fL --progress-bar -o ./vs_server.tar.gz "$VS_SERVER_URL"

rm -rf ./vs_server
mkdir -p ./vs_server
tar -xzf ./vs_server.tar.gz -C ./vs_server --strip-components=1
rm ./vs_server.tar.gz
