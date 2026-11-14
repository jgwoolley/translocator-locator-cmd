# Use a lightweight Linux distribution
FROM debian:bookworm-slim

# Install ImageMagick, SVG support, and ICNS tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    imagemagick \
    librsvg2-bin \
    icnsutils \
    && rm -rf /var/lib/apt/lists/*

# Set the working directory
WORKDIR /app

COPY generate.sh /usr/local/bin/generate.sh

# Make the script executable
RUN chmod +x /usr/local/bin/generate.sh

# Don't run as root. run.sh passes -u $(id -u):$(id -g) so the generated files belong to the caller; without it this
# unprivileged user can't write to the bind mount, so the run fails instead of leaving root-owned files.
USER nobody

# Default command: 
# It takes 'logo.svg' and generates a multi-resolution favicon.ico
# and a high-res apple-touch-icon.png
CMD ["/usr/local/bin/generate.sh"]