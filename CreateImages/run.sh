set -euo pipefail

docker build -t favicon-gen .

docker run --rm \
    -u $(id -u):$(id -g) \
    -v "$(pwd)/..":/app \
    favicon-gen