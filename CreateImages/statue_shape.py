#!/usr/bin/env python3
"""Bakes a finished statue into plain shape files that `vsdev screenshots` can draw as "model" parts.

The game has no shape file for a finished statue: StatueMod builds it in code (StatueMesh.Build). This script repeats the
steps that matter for a picture, from the game's own assets:
  1. the seraph-faceless body (with its animations, so the scene entry picks the pose with "animation" and "frame"),
  2. worn items step-parented onto the body's bones, hiding the body parts they disable (AddGear),
  3. every face reset to one 16 px tile sized by its element (StatuePose.UseSingleTile),
  4. every texture replaced by the block's stone texture (StoneTextureSource).
The scale (BlockEntityStatue.SmallScale) and the lift onto the pedestal (2/16) are left to the scene entry in vs-manifest.json.
It also writes the pedestal with the same stone. Usage (from the repo root, after ./build.sh fetched ./vintagestory):

    python3 CreateImages/statue_shape.py [rock]      # default whitemarble; writes StatueMod/Screenshots/statue-<rock>*.json
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "vintagestory" / "assets"
BODY = ASSETS / "game/shapes/entity/humanoid/seraph-faceless.json"
ARMOR_DIR = ASSETS / "survival/shapes/entity/humanoid/seraph/armor/antique/forlorn-pristine"
ARMOR = ["head", "body", "legs"]
# survival/itemtypes/wearable/seraph/armor.json, disableElementsByType "armor-head-antique-*-pristine".
HIDDEN = {"hidehat", "closehat", "hoodshoulder", "hoodupperover", "Hair tile upper part", "ponytailhigh", "ponytaillow", "Hair",
          "hair-uncovered", "hidefacialhair", "sidehigh", "sidelow", "bangs"}
PEDESTAL = ROOT / "StatueMod/assets/nf3tstatue/shapes/block/pedestal.json"
FACE_SIZE = 16  # StatuePose.FaceSize


def walk(elements):
    for el in elements:
        yield el
        yield from walk(el.get("children") or [])


def single_tile(elements):
    """StatuePose.UseSingleTile: each face shows one tile of the texture, as big as the element's side (1..16 px)."""
    clamp = lambda v: min(max(abs(v), 1), FACE_SIZE)
    for el in walk(elements):
        f, t = el.get("from"), el.get("to")
        if not f or not t:
            continue
        sx, sy, sz = (clamp(t[i] - f[i]) for i in range(3))
        size = {"north": (sx, sy), "south": (sx, sy), "east": (sz, sy), "west": (sz, sy), "up": (sx, sz), "down": (sx, sz)}
        for side, face in (el.get("faces") or {}).items():
            if face is not None and side in size:
                face["uv"] = [0, 0, *size[side]]


def in_stone(shape, texture):
    keys = set(shape.get("textures") or {})
    for el in walk(shape["elements"]):
        for face in (el.get("faces") or {}).values():
            if face and isinstance(face.get("texture"), str):
                keys.add(face["texture"].lstrip("#"))
    shape["textures"] = {k: texture for k in sorted(keys)}
    shape["textureWidth"] = shape["textureHeight"] = FACE_SIZE
    shape.pop("textureSizes", None)
    single_tile(shape["elements"])


def statue(texture):
    body = json.loads(BODY.read_text())
    by_name = {el["name"]: el for el in walk(body["elements"])}
    for part in ARMOR:
        piece = json.loads((ARMOR_DIR / f"{part}.json").read_text())
        for el in piece["elements"]:
            parent = by_name.get(el.get("stepParentName", ""))
            (parent.setdefault("children", []) if parent else body["elements"]).append(el)

    def drop(elements):
        return [dict(el, children=drop(el.get("children") or [])) for el in elements if el.get("name") not in HIDDEN]

    body["elements"] = drop(body["elements"])
    in_stone(body, texture)
    return body


def main():
    rock = sys.argv[1] if len(sys.argv) > 1 else "whitemarble"
    texture = f"game:block/stone/rock/{rock}1"  # statue.json: "stone": "game:block/stone/rock/{rock}1"
    out = ROOT / "StatueMod/Screenshots"
    out.mkdir(parents=True, exist_ok=True)
    pedestal = json.loads(PEDESTAL.read_text())
    in_stone(pedestal, texture)
    for name, shape in [(f"statue-{rock}.json", statue(texture)), (f"statue-{rock}-pedestal.json", pedestal)]:
        (out / name).write_text(json.dumps(shape, separators=(",", ":")) + "\n")
        print(out / name)


if __name__ == "__main__":
    main()
