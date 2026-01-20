#!/bin/sh
set -eu

for project_path in */; do
    if [ -f "$project_path/modinfo.json" ]; then
        moddb_path="$project_path/ModDB"
        screenshots_path="$project_path/Screenshots"
        modicon_svg_path="$project_path/modicon.svg"
        modicon_png_path="$project_path/modicon.png"
        logo_png_path="$moddb_path/logo.png"
        mkdir -p "$moddb_path"
        if [ -d "$screenshots_path" ]; then
            if command -v mogrify >/dev/null 2>&1; then
                mogrify -path "${moddb_path}" \
                    -format jpg \
                    -resize "1920x1080>" \
                    -define jpeg:extent=2MB \
                    "${screenshots_path}"/*
            fi
        fi
        if [ -f "$modicon_svg_path" ]; then
            if command -v convert >/dev/null 2>&1; then
                convert -density 1200 \
                    -background none \
                    "$modicon_svg_path" \
                    -resize "128x128" \
                    -colorspace sRGB \
                    "$modicon_png_path"
                convert -density 1200 \
                    -background none \
                    "$modicon_svg_path" \
                    -resize "480x480" \
                    -colorspace sRGB \
                    "$logo_png_path"
            fi
        fi
    fi
done

echo "✅ Done!"