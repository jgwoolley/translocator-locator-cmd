"""Generates the Entropy Rift mod's models and textures from the vanilla game files.

    python3 -I CreateImages/entropyrift/generate.py

Run from the repository root. Reads ./vintagestory (the game) and writes into EntropyRiftMod/assets/nf3tentropyrift/:
  textures/block/entropyglow.png        the vanilla rusty glow, hue-shifted to the storm's red-orange
  textures/item/entropygear-edge.png    the temporal gear texture shrouded in darkness, only its highlights glowing orange
  textures/item/entropygear-body.png    the same, fully dark (used on the gear's flat faces)
  shapes/item/entropygear.json          the vanilla temporal gear model with those two textures
  shapes/block/stormsiphon-{empty,temporal,entropy}.json   the Storm Siphon with no gear, a temporal gear, an entropy gear
  shapes/block/entropyrift-{empty,powered}.json            the Entropy Rift, dormant or powered by an entropy gear
Animations: "gearspin" / "gearspin-reverse" (the gear in the cradle) and "turn" (the Entropy Rift's tower).
"""
import colorsys, copy, json, math, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import png_io  # noqa: E402

GAME = "vintagestory/assets/survival"
OUT = "EntropyRiftMod/assets/nf3tentropyrift"
DOMAIN = "nf3tentropyrift"


# ---------------------------------------------------------------- textures

def hue_shift(src, dst, hue_deg, sat_mul, val_mul):
    w, h, px = png_io.read_png(src)
    out = []
    for r, g, b, a in px:
        hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        r2, g2, b2 = colorsys.hsv_to_rgb((hh + hue_deg / 360) % 1, min(1, s * sat_mul), min(1, v * val_mul))
        out.append((round(r2 * 255), round(g2 * 255), round(b2 * 255), a))
    png_io.write_png(dst, w, h, out)


def shroud(src, dst, threshold, body, hue_deg=12, floor=0.85):
    """Near-black body; pixels brighter than `threshold` (0-1 of the texture's range) glow orange."""
    w, h, px = png_io.read_png(src)
    vals = [max(p[:3]) for p in px if p[3] > 0]
    lo, hi = min(vals), max(vals)
    out = []
    for r, g, b, a in px:
        v = (max(r, g, b) - lo) / max(1, hi - lo)
        if v >= threshold:
            t = (v - threshold) / max(1e-6, 1 - threshold)
            rr, gg, bb = colorsys.hsv_to_rgb(hue_deg / 360, 1 - 0.35 * t, floor + (1 - floor) * t)
        else:
            rr, gg, bb = colorsys.hsv_to_rgb(330 / 360, 0.35, body * (0.6 + 0.4 * v / max(threshold, 1e-6)))
        out.append((round(rr * 255), round(gg * 255), round(bb * 255), a))
    png_io.write_png(dst, w, h, out)


# ---------------------------------------------------------------- shape helpers

TEX = {
    "plate": "game:block/machine/device-plate",
    "copper": "game:block/metal/ingot/copper",
    "brass": "game:block/metal/ingot/brass",
    "rusty": "game:block/metal/tarnished/rusty-iron",
    "stone": "game:block/stone/agedbrick/basalt1",
    "glow": "game:block/machine/statictranslocator/rustyglow",
}


def box(name, f, t, tex, rot=None, origin=None):
    """A cuboid with one texture on all faces; `rot` = {"x": deg, "y": deg, ...} around `origin` (default: its centre)."""
    sx, sy, sz = (abs(t[i] - f[i]) for i in range(3))

    def uv(w, h):
        return [0, 0, min(max(w, 0.5), 16), min(max(h, 0.5), 16)]
    faces = {side: {"texture": "#" + tex, "uv": uv(w, h)} for side, (w, h) in
             {"north": (sx, sy), "south": (sx, sy), "east": (sz, sy), "west": (sz, sy), "up": (sx, sz), "down": (sx, sz)}.items()}
    e = {"name": name, "from": [round(v, 4) for v in f], "to": [round(v, 4) for v in t], "faces": faces}
    if rot:
        e["rotationOrigin"] = list(origin or [(f[i] + t[i]) / 2 for i in range(3)])
        for axis, deg in rot.items():
            e["rotation" + axis.upper()] = deg
    return e


def strut(name, p0, p1, w, tex, seg=1.6):
    """An angled rod from p0 to p1 made of short axis-aligned segments."""
    n = max(1, round(math.dist(p0, p1) / seg))
    out = []
    for i in range(n):
        a = [p0[j] + (p1[j] - p0[j]) * i / n for j in range(3)]
        b = [p0[j] + (p1[j] - p0[j]) * (i + 1) / n for j in range(3)]
        out.append(box(f"{name}-{i}", [min(a[0], b[0]) - w / 2, min(a[1], b[1]), min(a[2], b[2]) - w / 2],
                       [max(a[0], b[0]) + w / 2, max(a[1], b[1]) + 0.01, max(a[2], b[2]) + w / 2], tex))
    return out


def embed_gear(gear_shape, y, k, texture_keys):
    """The gear model (lying flat, centred on the block) raised to height y and scaled by k around its centre."""
    els = copy.deepcopy(gear_shape["elements"])

    def walk(es, root):
        for e in es:
            for f in e["faces"].values():
                f["texture"] = "#" + texture_keys[f["texture"][1:]]
            for key in ("from", "to", "rotationOrigin"):
                if key in e:
                    c = (8, 0.5, 8)
                    e[key] = [round(c[i] + (e[key][i] - c[i]) * k, 4) for i in range(3)] if root else [round(v * k, 4) for v in e[key]]
            walk(e.get("children", []), False)
    walk(els, True)
    return {"name": "gear", "from": [0, y, 0], "to": [0, y, 0], "rotationOrigin": [8, y + 0.5 * k, 8], "faces": {}, "children": els}


def spin(code, element, frames, reverse=False):
    """A looping full turn of `element` around Y over `frames` frames (30 frames = 1 second at speed 1). Like the rift ward's
    spin, the last keyframe stops one step short of 360 degrees so the loop back to frame 0 is seamless."""
    end = 360 * (frames - 1) / frames * (-1 if reverse else 1)
    return {"name": code, "code": code, "quantityframes": frames, "onActivityStopped": "Stop", "onAnimationEnd": "Repeat",
            # Every keyframe sets all three rotations: the game interpolates X, Y and Z together and crashes on a missing one
            # (Animation.lerpKeyFrameElement: "Nullable object must have a value").
            "keyframes": [{"frame": 0, "elements": {element: {"rotationX": 0, "rotationY": 0, "rotationZ": 0}}},
                          {"frame": frames - 1, "elements": {element: {"rotationX": 0, "rotationY": round(end, 3), "rotationZ": 0}}}]}


def base_and_cradle(e, y):
    """Stone feet, the device plate, and a brass cradle ring at height y on struts around a copper column."""
    for i, (x, z) in enumerate([(0.5, 0.5), (11.5, 0.5), (0.5, 11.5), (11.5, 11.5)]):
        e.append(box(f"foot{i}", (x, 0, z), (x + 4, 2.5, z + 4), "stone"))
    e.append(box("base", (2, 2, 2), (14, 4, 14), "plate"))
    e.append(box("column", (6.5, 4, 6.5), (9.5, y, 9.5), "copper"))
    for n, (f, t) in {"N": ((3, y, 3), (13, y + 1, 4)), "S": ((3, y, 12), (13, y + 1, 13)),
                      "W": ((3, y, 4), (4, y + 1, 12)), "E": ((12, y, 4), (13, y + 1, 12))}.items():
        e.append(box("ring" + n, f, t, "brass"))
    for i, (x, z) in enumerate([(3, 3), (12, 3), (3, 12), (12, 12)]):
        e.append(box(f"strut{i}", (x, 4, z), (x + 1, y, z + 1), "brass"))


def finish(e, gear, gear_textures, gear_y, finished, animations=(), dormant=False):
    textures = dict(TEX)
    if finished:  # the gear has drunk the storm: the device glows the storm's red-orange
        textures["glow"] = f"{DOMAIN}:block/entropyglow"
    if dormant:   # unpowered: the parts that glow are dull rusty iron
        textures["glow"] = TEX["rusty"]
    if gear is not None:
        e.append(embed_gear(gear, gear_y, 1.15, {k: v[0] for k, v in gear_textures.items()}))
        textures.update({v[0]: v[1] for v in gear_textures.values()})
    shape = {"textureWidth": 16, "textureHeight": 16, "textures": textures, "elements": e}
    if animations:
        shape["animations"] = list(animations)
    return shape


DISH_Y = 25          # height of the dish's hub (the device is 32 tall)
DISH_PETALS = 8      # separate petals with gaps between them, not a solid disc
DISH_CUP = 30        # how far each petal rises from the hub (degrees)
DISH_TILT = 25       # the whole dish leans this far toward the south (the front), so its bowl shows


def dish(centre):
    """The copper dish: petals radiating from the hub, each cupped upward, the whole leaning toward the front. Built as
    nested elements so each rotation is about the right axis: the dish leans (x), each arm turns its petal to its place round
    the hub (y), and the petal rises from its own inner end (x). (One element with both a y and an x rotation does not do
    this: the game turns it about its own axes, and twelve such petals all landed in one tilted plane, a solid disc.)"""
    arms = []
    for i in range(DISH_PETALS):
        petal = box(f"petal{i}", (-1.6, -0.4, 1.4), (1.6, 0.4, 7), "copper", rot={"x": -DISH_CUP}, origin=(0, 0, 1.4))
        arms.append({"name": f"dishArm{i}", "from": [0, 0, 0], "to": [0, 0, 0], "rotationOrigin": [0, 0, 0],
                     "rotationY": i * 360 / DISH_PETALS, "faces": {}, "children": [petal]})
    return {"name": "dish", "from": list(centre), "to": list(centre), "rotationOrigin": list(centre),
            "rotationX": DISH_TILT, "faces": {}, "children": arms}


def siphon(gear, gear_textures, finished):
    """The Storm Siphon (storm dish design): an upturned copper dish on a crane arm collects the storm, and a brass spike
    funnels it down into the gear in the cradle. `gear` is the gear model to show, or None."""
    e = []
    base_and_cradle(e, 7)
    hy = DISH_Y
    # The mast stands at the back (north), so the arm holds the dish from behind and the dish's bowl faces the front.
    e.append(box("mast", (7.3, 8, 1.6), (8.7, hy + 0.6, 3), "rusty"))
    e.append(box("arm", (7.3, hy - 0.8, 1.6), (8.7, hy + 0.6, 8), "rusty"))
    e.append(dish((8, hy, 8)))
    e.append(box("hub", (6.5, hy - 1, 6.5), (9.5, hy + 1, 9.5), "brass"))
    e.append(box("spike", (7.5, 11, 7.5), (8.5, hy - 1, 8.5), "brass"))
    e.append(box("spark", (7.1, 10, 7.1), (8.9, 11.8, 8.9), "glow", rot={"y": 45}))
    anims = [] if gear is None else [spin("gearspin-reverse" if finished else "gearspin", "gear", 90, reverse=finished)]
    return finish(e, gear, gear_textures, 7.6, finished, anims)


def rift_tower(gear, gear_textures, powered):
    """The Entropy Rift (lightning rod design): three copper rods meet at a mast with coils and a glowing tip, and a feed
    line runs down into the entropy gear in the cradle. Powered (gear in), the tower turns slowly and the gear spins the
    other way; empty, it is dormant and its glow parts are dull."""
    e = []
    base_and_cradle(e, 8)
    tower = []
    for i in range(3):
        a = math.radians(i * 120 + 90)
        tower += strut(f"prong{i}", (8 + 5.5 * math.cos(a), 9, 8 + 5.5 * math.sin(a)), (8 + 1.2 * math.cos(a), 25, 8 + 1.2 * math.sin(a)), 1, "copper")
    tower.append(box("mast", (7.4, 18, 7.4), (8.6, 30, 8.6), "rusty"))
    for i, cy in enumerate((20, 23, 26)):
        tower.append(box(f"coil{i}", (6.5, cy, 6.5), (9.5, cy + 1, 9.5), "copper"))
    tower.append(box("tip", (6.8, 29.5, 6.8), (9.2, 31.9, 9.2), "glow", rot={"y": 45}))
    tower.append(box("feed", (7.7, 11, 7.7), (8.3, 18, 8.3), "glow"))
    e.append({"name": "tower", "from": [0, 0, 0], "to": [0, 0, 0], "rotationOrigin": [8, 0, 8], "faces": {}, "children": tower})
    anims = [spin("turn", "tower", 360), spin("gearspin-reverse", "gear", 90, reverse=True)] if gear is not None else []
    return finish(e, gear, gear_textures, 8.6, True, anims, dormant=gear is None)


def write_json(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as fh:
        json.dump(data, fh, indent=1)
    print(path)


def main():
    if not os.path.isdir(GAME):
        sys.exit(f"{GAME} not found: run from the repository root with the game in ./vintagestory")
    os.makedirs(f"{OUT}/textures/block", exist_ok=True)
    os.makedirs(f"{OUT}/textures/item", exist_ok=True)

    hue_shift(f"{GAME}/textures/block/machine/statictranslocator/rustyglow.png", f"{OUT}/textures/block/entropyglow.png", 210, 1.0, 0.8)
    gear_png = f"{GAME}/textures/item/resource/temporalgear.png"
    shroud(gear_png, f"{OUT}/textures/item/entropygear-edge.png", 0.76, 0.14)
    shroud(gear_png, f"{OUT}/textures/item/entropygear-body.png", 1.01, 0.14)
    print(f"{OUT}/textures written")

    # entropy gear item: vanilla model; flat faces (north/south of everything but the spikes) fully dark
    temporal = json.load(open(f"{GAME}/shapes/item/gear-temporal.json"))
    entropy = copy.deepcopy(temporal)
    entropy["textures"] = {"edge": f"{DOMAIN}:item/entropygear-edge", "body": f"{DOMAIN}:item/entropygear-body"}

    def retex(es):
        for el in es:
            spike = el["name"].startswith("Cube") and el["name"] != "Cube16"   # Cube1..12 are the spikes, Cube16 a tooth
            for side, f in el["faces"].items():
                f["texture"] = "#body" if side in ("north", "south") and not spike else "#edge"
                # The edge texture is near-black with orange highlights: glowing hard, the highlights burn orange while the
                # black stays black (glow lights a face's own colours). The temporal gear glows 35 all over.
                f["glow"] = 35 if f["texture"] == "#body" else 200
            retex(el.get("children", []))
    retex(entropy["elements"])
    write_json(f"{OUT}/shapes/item/entropygear.json", entropy)

    temporal_tex = {"temporal": ("gear", "game:item/resource/temporalgear")}
    entropy_tex = {"edge": ("gearedge", f"{DOMAIN}:item/entropygear-edge"), "body": ("gearbody", f"{DOMAIN}:item/entropygear-body")}
    write_json(f"{OUT}/shapes/block/stormsiphon-empty.json", siphon(None, {}, False))
    write_json(f"{OUT}/shapes/block/stormsiphon-temporal.json", siphon(temporal, temporal_tex, False))
    write_json(f"{OUT}/shapes/block/stormsiphon-entropy.json", siphon(entropy, entropy_tex, True))

    write_json(f"{OUT}/shapes/block/entropyrift-empty.json", rift_tower(None, {}, False))
    write_json(f"{OUT}/shapes/block/entropyrift-powered.json", rift_tower(entropy, entropy_tex, True))


if __name__ == "__main__":
    main()
