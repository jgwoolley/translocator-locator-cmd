// Shapes of the first Reliquiarum relics: the fallen giant, the reliquary, the stasis pod, the scarab, the gilded eagle and the two
// banners. Made with (textures first, the render reads them):
//   vsdev texture CreateImages/reliquiarum-relics.textures.mjs
//   vsdev shape CreateImages/reliquiarum-relics.shapes.mjs
// Ported from the Python generator these relics were first made with (reliquiarum_shapes.py), number for number: a box turns about its
// `from` corner unless an origin is given (`B`), a joint about itself (`J`). Units are the game's: 16 = one block, x east, y up, z south.
const T = "nf3treliquiarum:block/";
const FULL = [0, 0, 16, 16];

// Python's round(): halves to the even neighbour (the eagle's plumage UVs were sized with it).
const pyRound = (v) => {
  const f = Math.floor(v), d = v - f;
  return d > 0.5 ? f + 1 : d < 0.5 ? f : f % 2 === 0 ? f : f + 1;
};

/** The builders, on vsdev's box and joint: shared by the shapes here and the banner texture's embroidered eagle. */
export function builders({ box, joint }) {
  // faces: { side: "key" } for another texture, { side: ["key", uv] } or ["key", uv, turn] for a decorated face.
  const faceSpec = (faces) => Object.fromEntries(Object.entries(faces).map(([side, v]) =>
    [side, typeof v === "string" ? v : { texture: v[0], uv: v[1], ...(v[2] ? { rotation: v[2] } : {}) }]));
  const B = (name, f, t, tex, o = {}) => box(f, t, tex, {
    name,
    ...(o.rot && o.rot.some(Boolean) ? { rot: o.rot, origin: o.origin ?? f } : {}),
    ...(o.faces ? { faces: faceSpec(o.faces) } : {}),
    ...(o.children ? { children: o.children } : {}),
    ...(o.extra ? { extra: o.extra } : {}),
    ...(o.ground ? { ground: true } : {}),
  });
  const J = (name, at, rot, children = [], o = {}) => joint(at, { name, ...(rot && rot.some(Boolean) ? { rot } : {}), children, ...(o.ground ? { ground: true } : {}) });

  // An eight-sided slab of half-width r: four bars crossed at 45 degrees; their tops differ by a hair so overlapping faces don't flicker.
  const octagon = (name, [cx, cz], r, y0, y1, tex) => {
    const h = r * Math.tan((22.5 * Math.PI) / 180);
    return [0, 1, 2, 3].map((k) => B(`${name} ${k}`, [cx - r, y0, cz - h], [cx + r, y1 - 0.01 * k, cz + h], tex, k ? { rot: [0, 45 * k, 0], origin: [cx, y0, cz] } : {}));
  };

  // ---- The eagle: two heads looking apart, wings spread. One bird built to a scale: full size and round it is the gilded statue,
  // small and flattened it is the relief on the reliquary's gable. Feathers are single boxes fanned from the shoulder, each a step
  // behind the one above it, with a feather texture (lit top edge, dark bottom line) stretched over them.
  // Each wing's feathers, top to bottom: (height of its root on the body's side, angle above level, length); the coverts are a
  // second, shorter row lying over the roots.
  const FLIGHT = [[2.5, 22, 10.4], [1.3, 18, 10.6], [0.1, 14, 10.0], [-1.1, 10, 8.8], [-2.3, 6, 7.0]];
  const COVERTS = [[2.1, 21, 6.0], [0.9, 17, 5.6], [-0.3, 13, 4.8], [-1.5, 9, 3.6]];
  const TAIL = [-30, -15, 0, 15, 30];
  /** A two-headed eagle facing south, as one joint at `at` (the middle of its breast). `s` scales it (at 1 about 25 wide and 15
   * tall); `zs` scales its depth alone, for a relief. Only turns about the z axis are used, so flattening skews nothing. */
  const eagle = (name, at, s, zs = s) => {
    const feather = { south: ["feather", FULL], north: ["feather", FULL] };
    const p = (v) => [v[0] * s, v[1] * s, v[2] * zs];
    const b = (n, f, t, tex = "gilt", faces) => {
      const pf = p(f), pt = p(t);
      return B(`${name} ${n}`, pf.map((v, i) => Math.min(v, pt[i])), pf.map((v, i) => Math.max(v, pt[i])), tex, faces ? { faces } : {});
    };
    const j = (n, pivot, turn, children) => joint(p(pivot), { name: `${name} ${n}`, ...(turn ? { rot: [0, 0, turn] } : {}), children });
    // Breast feathers on the front and back of a w x h box: a scallop is a quarter of the texture, about 0.9 big at any scale.
    const plumage = (w, h) => {
      const uv = [0, 0, Math.min(16, Math.max(1, pyRound((w * s) / 0.9)) * 4), Math.min(16, Math.max(1, pyRound((h * s) / 0.9)) * 4)];
      return { south: ["plumage", uv], north: ["plumage", uv] };
    };
    const wing = (side) => {
      const w = side > 0 ? "east" : "west";
      const parts = [];
      FLIGHT.forEach(([root, angle, length], i) => {
        const z = 0.4 - 0.08 * i;
        parts.push(j(`${w} flight ${i}`, [0, root, 0], side * angle, [
          b(`${w} flight ${i} vane`, [0, -0.65, z - 0.45], [side * length, 0.65, z], "gilt", feather),
          b(`${w} flight ${i} tip`, [side * length, -0.4, z - 0.4], [side * (length + 0.55), 0.42, z - 0.05]),
          b(`${w} flight ${i} point`, [side * (length + 0.55), -0.15, z - 0.36], [side * (length + 1.0), 0.2, z - 0.09])]));
      });
      COVERTS.forEach(([root, angle, length], i) => {
        const z = 0.9 - 0.08 * i;
        parts.push(j(`${w} covert ${i}`, [0, root, 0], side * angle, [
          b(`${w} covert ${i} vane`, [0, -0.65, z - 0.42], [side * length, 0.65, z], "gilt", feather),
          b(`${w} covert ${i} tip`, [side * length, -0.38, z - 0.36], [side * (length + 0.45), 0.4, z - 0.05])]));
      });
      // The arm along the top edge, and the shoulder where the wing joins the breast.
      parts.push(
        j(`${w} arm`, [0, 2.9, 0], side * 23, [b(`${w} arm`, [0, -0.2, -0.15], [side * 7.4, 0.6, 1.2])]),
        b(`${w} shoulder`, [side * -0.5, -2.6, -0.5], [side * 1.2, 2.9, 1.35], "gilt", plumage(1.7, 5.5)),
        b(`${w} shoulder cap`, [side * -0.5, 2.9, -0.3], [side * 0.7, 3.5, 1.2]),
      );
      return j(`${w} wing`, [side * 2.2, 0, 0], 0, parts);
    };
    // Neck leaning outward, the head level on top of it, looking away from the other one.
    const head = (side) => {
      const w = side > 0 ? "east" : "west";
      const skull = [
        b(`${w} skull`, [side * -1.2, -0.3, -0.95], [side * 1.3, 1.7, 0.95]),
        b(`${w} crown`, [side * -0.9, 1.7, -0.7], [side * 0.9, 2.05, 0.7]),
        b(`${w} brow`, [side * 0.05, 1.1, -1.05], [side * 1.55, 1.7, 1.05], "giltdark"),
        b(`${w} eye`, [side * 0.35, 0.55, -1.0], [side * 0.95, 1.1, 1.0], "shadow"),
        b(`${w} beak`, [side * 1.3, 0.35, -0.55], [side * 2.9, 1.25, 0.55]),
        b(`${w} beak tip`, [side * 2.9, 0.2, -0.45], [side * 3.3, 0.95, 0.45]),
        b(`${w} beak hook`, [side * 2.75, -0.55, -0.4], [side * 3.3, 0.2, 0.4]),
        b(`${w} jaw`, [side * 1.3, -0.1, -0.45], [side * 2.3, 0.25, 0.45], "giltdark"),
        b(`${w} crest a`, [side * -1.65, 0.9, -0.6], [side * -1.2, 1.6, 0.6]),
        b(`${w} crest b`, [side * -1.6, 0.0, -0.6], [side * -1.2, 0.7, 0.6]),
      ];
      return j(`${w} neck`, [side * 1.0, 2.6, 0], -side * 22, [
        b(`${w} neck`, [-0.95, -0.8, -0.85], [0.95, 3.0, 0.85], "gilt", plumage(1.9, 3.8)),
        j(`${w} head`, [0, 3.0, 0], side * 22, skull)]);
    };
    // Heraldic: legs out to the sides, claws spread.
    const leg = (side) => {
      const w = side > 0 ? "east" : "west";
      const toes = [-40, 0, 40].map((a, k) => j(`${w} toe ${k}`, [0, -3.3, 0], a, [
        b(`${w} toe ${k}`, [-0.3, -1.4, -0.3], [0.3, 0.15, 0.3]),
        b(`${w} claw ${k}`, [-0.17, -2.0, -0.18], [0.17, -1.4, 0.18], "giltdark")]));
      return j(`${w} leg`, [side * 1.3, -2.5, 0.1], side * 40, [
        b(`${w} thigh`, [-0.9, -2.0, -0.75], [0.9, 0.5, 0.75], "gilt", plumage(1.8, 2.5)),
        b(`${w} shank`, [-0.42, -3.3, -0.4], [0.42, -2.0, 0.4], "giltdark"), ...toes]);
    };
    const quill = { south: ["feather", FULL, 90], north: ["feather", FULL, 90] };
    const tail = TAIL.map((angle, i) => {
      const away = Math.abs(i - Math.floor(TAIL.length / 2));
      const length = 4.6 - 0.5 * away, z = 0.5 - 0.14 * away;
      return j(`tail ${i}`, [0, 0, 0], angle, [
        b(`tail ${i} vane`, [-0.65, -length, z - 0.4], [0.65, -0.3, z], "gilt", quill),
        b(`tail ${i} tip`, [-0.36, -length - 0.55, z - 0.35], [0.36, -length, z - 0.05])]);
    });
    return joint(at, { name, children: [
      b("breast", [-2.2, -0.4, -1.1], [2.2, 2.8, 1.25], "gilt", plumage(4.4, 3.2)),
      b("keel", [-1.1, 0.2, 1.25], [1.1, 2.4, 1.6], "gilt", plumage(2.2, 2.2)),
      b("belly", [-1.75, -2.0, -1.0], [1.75, -0.4, 1.05], "gilt", plumage(3.5, 1.6)),
      b("hips", [-1.2, -3.2, -0.85], [1.2, -2.0, 0.85], "gilt", plumage(2.4, 1.2)),
      j("tail", [0, -2.9, 0], 0, tail),
      leg(-1), leg(1), wing(-1), wing(1), head(-1), head(1)] });
  };
  return { B, J, octagon, eagle };
}

export const GILT = { gilt: T + "gilt-worn", giltdark: T + "gilt-dark", feather: T + "gilt-feather", plumage: T + "gilt-plumage", shadow: "game:block/cloth/basic/black" };

export default (h) => {
  const { box, relief } = h;
  const { B, J, octagon, eagle } = builders(h);

  // ---- The fallen giant: a huge armored warrior slumped against a wall, its paint long gone, eaten by rust.
  const ARMOR = {
    plate: T + "armor-worn", trim: T + "armor-trim-worn", joint: T + "joint-worn",
    lens: "game:block/cloth/basic/red", deadlens: "game:block/cloth/basic/gray", wax: T + "wax-worn", paper: T + "parchment-worn",
  };
  // A wax disc with parchment strips hanging below it, on a surface facing south at `at` (the disc's centre).
  const puritySeal = (name, [x, y, z], strips = [9, 7, 10]) => [
    B(`${name} wax`, [x - 2, y - 2, z], [x + 2, y + 2, z + 1], "wax"),
    B(`${name} wax rim`, [x - 2.5, y - 1.5, z - 0.2], [x + 2.5, y + 1.5, z + 0.7], "wax"),
    B(`${name} wax rim2`, [x - 1.5, y - 2.5, z - 0.2], [x + 1.5, y + 2.5, z + 0.7], "wax"),
    ...strips.map((length, i) => {
      const sx = x - 1.8 + i * 1.3;
      return B(`${name} strip ${i}`, [sx, y - 1 - length, z + 0.7], [sx + 1.1, y - 1, z + 0.9], "paper", { rot: [0, 0, (i - 1) * 6], origin: [sx + 0.5, y - 1, z + 0.8] });
    }),
  ];
  const helmet = () => [
    B("helmet", [-4.5, 0, -4.5], [4.5, 9, 4.5], "plate"),
    B("helmet crown", [-3.5, 9, -3.5], [3.5, 10, 3.5], "plate"),
    B("brow", [-4.7, 5.5, 3.8], [4.7, 6.7, 5], "trim"),
    B("lens left", [-3.6, 3.6, 4.4], [-1.1, 5.4, 4.9], "deadlens"),
    B("lens right", [1.1, 3.6, 4.4], [3.6, 5.4, 4.9], "lens"),
    // The rebreather sits close to the face (1 px proud of it), so it reads as a grille, not a snout.
    B("rebreather", [-1.7, 0.4, 4.3], [1.7, 3.3, 5.5], "trim"),
    ...[0, 1, 2].map((i) => B(`grille slit ${i}`, [-1.25 + i * 1, 0.9, 5.5], [-0.75 + i * 1, 2.8, 5.6], "joint")),
    B("cheek left", [-4.7, 0, 1], [-3, 3.5, 4.7], "trim"),
    B("cheek right", [3, 0, 1], [4.7, 3.5, 4.7], "trim"),
  ];
  // A big rounded shoulder plate; side is -1 (west) or 1 (east).
  const pauldron = (s) => {
    const [lo, hi] = s < 0 ? [-6, 3] : [-3, 6];
    return [
      B("pad", [lo, -3, -7], [hi, 6, 7], "plate"),
      B("pad dome", [lo + 1, 6, -6], [hi - 1, 8, 6], "plate"),
      B("pad dome top", [lo + 2.5, 8, -4.5], [hi - 2.5, 9, 4.5], "plate"),
      B("pad rim", [lo - 0.5, -4, -7.5], [hi + 0.5, -2.5, 7.5], "trim"),
    ];
  };
  // Upper arm hanging from a shoulder joint, forearm and gauntlet below it.
  const arm = (name, foreRot, handRot) => {
    const gauntlet = [B(`${name} gauntlet`, [-3.2, -5, -3.2], [3.2, 0, 3.2], "plate"), B(`${name} fingers`, [-3, -8, -2.5], [3, -5, 2.5], "trim")];
    const fore = [B(`${name} forearm`, [-2.8, -9, -2.8], [2.8, 0, 2.8], "plate"), B(`${name} vambrace`, [-3.2, -8, -3.2], [3.2, -4, 3.2], "trim"),
      J(`${name} wrist`, [0, -9, 0], handRot, gauntlet)];
    return [B(`${name} upper`, [-2.5, -10, -2.5], [2.5, 0, 2.5], "joint"), B(`${name} elbow`, [-3, -11.5, -3], [3, -8.5, 3], "trim"),
      J(`${name} elbow joint`, [0, -10, 0], foreRot, fore)];
  };
  // Thigh pointing forward (+z) from the hip, shin and boot continuing from the knee.
  const leg = (name, shinRot, footRot) => {
    const boot = [B(`${name} boot`, [-3.6, -3, -3], [3.6, 3.5, 4.5], "plate"), B(`${name} toe`, [-3.4, -3, 4.5], [3.4, 2, 7], "plate"),
      B(`${name} sole`, [-3.8, -4, -3.2], [3.8, -3, 7.2], "trim")];
    const shin = [B(`${name} shin`, [-3, -3, 0], [3, 3.5, 13], "plate"), B(`${name} greave`, [-3.3, 1, 2], [3.3, 4, 11], "plate"),
      J(`${name} ankle`, [0, 0, 13], footRot, boot)];
    return [B(`${name} thigh`, [-3.5, -3.5, 0], [3.5, 4, 14], "plate"), B(`${name} hip plate`, [-4, -1, -1], [4, 4.5, 6], "trim"),
      B(`${name} knee`, [-3.2, -2.5, 12], [3.2, 4.5, 16.5], "trim"), J(`${name} knee joint`, [0, 0, 14], shinRot, shin)];
  };
  const fallenGiant = () => {
    const head = J("neck", [0, 21, 1], [16, 14, -20], helmet());
    const leftShoulder = J("left shoulder", [-10, 17, -0.5], [0, 0, 10], [...pauldron(-1), ...puritySeal("seal", [-1.5, 2, 7])]);
    const rightShoulder = J("right shoulder", [10, 17, -0.5], [0, 0, -10], pauldron(1));
    const torso = [
      B("abdomen", [-7, 0, -5], [7, 8, 5], "joint"),
      B("abdomen plates", [-6, 1, 4], [6, 7, 6], "plate"),
      B("belt", [-7.5, -1, -5.5], [7.5, 2, 5.5], "trim"),
      B("buckle", [-2, -1.5, 5.5], [2, 2.5, 6.5], "plate"),
      B("chest", [-9, 7, -6], [9, 21, 6], "plate"),
      B("chest plate", [-8, 9, 6], [8, 19, 7.5], "plate"),
      B("chest ridge", [-8.5, 15, 7], [8.5, 16.5, 8.5], "trim"),
      // The wound that killed it: a hole punched through the chest plate.
      B("wound", [2, 10.5, 7.4], [5.5, 13.5, 7.6], "joint"),
      B("wound edge", [1.5, 11, 7.5], [6, 13, 7.8], "trim"),
      B("collar", [-5, 20, -4], [5, 23, 4], "trim"),
      B("backpack", [-7.5, 8, -12], [7.5, 23, -6], "plate"),
      B("backpack trim", [-8, 12, -12.5], [8, 14, -5.5], "trim"),
      B("vent left", [-6.5, 20, -11], [-2.5, 27, -7], "trim"),
      B("vent right", [2.5, 20, -11], [6.5, 27, -7], "trim"),
      B("vent left mouth", [-6, 27, -10.5], [-3, 27.5, -7.5], "joint"),
      B("vent right mouth", [3, 27, -10.5], [6, 27.5, -7.5], "joint"),
      head, leftShoulder, rightShoulder,
      // The right arm has slid down beside the body; the left lies across the lap.
      J("right arm", [11.5, 15, 0], [-4, 0, -12], arm("right arm", [-78, 0, 0], [-12, 0, 0])),
      J("left arm", [-11.5, 15, 0], [-42, 0, 16], arm("left arm", [-55, 0, 0], [0, 0, 0])),
    ];
    const legs = [
      J("right leg", [5, 0, 0], [-2, 10, 0], leg("right leg", [4, 0, 0], [-80, 0, 0])),
      J("left leg", [-5, 0, 0], [-14, -12, 0], leg("left leg", [26, 0, 0], [-68, 0, 0])),
    ];
    // Hips above the ground (settled onto it), back against the north wall of a two-block-wide spot, leaning back on the wall.
    return [J("hips", [8, 5, 13], null, [J("waist", [0, 0, 0], [-8, 0, 5], torso), ...legs], { ground: true })];
  };

  // ---- The gilded eagle: the eagle on a post, on a stepped round pedestal. A little wider than its block.
  const aquila = () => [
    ...[[5.2, 0, 1.2], [4.1, 1.2, 2.2], [3.0, 2.2, 3.0]].flatMap(([r, y0, y1], i) => octagon(`pedestal ${i}`, [8, 8], r, y0, y1, i !== 1 ? "gilt" : "giltdark")),
    B("post foot", [7, 3, 6.8], [9, 3.7, 8.8], "giltdark"),
    B("post", [7.45, 3.7, 7.25], [8.55, 7.6, 8.05], "gilt"),
    B("post collar", [7.2, 6.9, 7.0], [8.8, 7.5, 8.2], "giltdark"),
    eagle("eagle", [8, 9.6, 8], 0.64),
  ];

  // ---- The banners: tattered cloth hung from a crossbar on a pole. The cloth is a row of one-wide strips, each torn off at its
  // own length; its texture (4 px per unit, so the emblem reads) is the same on both sides.
  const [CW, CH] = [16, 28]; // the cloth's width and height before the tears
  const BAR = 38; // height of the crossbar's underside, where the cloth starts
  const EAGLE_BANNER = { cloth: T + "banner-eagle", hem: T + "cloth-black", pole: T + "bronze-dark", fitting: T + "gilt-dark", ...GILT };
  const COG_BANNER = { cloth: T + "banner-cog", hem: T + "cloth-rust", pole: T + "iron-worn", iron: T + "iron-worn", fitting: T + "bronze-worn", shadow: "game:block/cloth/basic/black" };
  // The stand, the crossbar and the cloth; `rags`: for each strip from the west, how much of its lower end is torn away.
  const banner = (name, rags) => {
    const x0 = 8 - CW / 2, z = 9.0;
    const els = [...octagon(`${name} foot`, [8, 8], 3.6, 0, 1.0, "pole"), ...octagon(`${name} step`, [8, 8], 2.6, 1.0, 1.9, "pole")];
    els.push(B(`${name} socket`, [7.1, 1.9, 7.1], [8.9, 2.7, 8.9], "fitting"));
    // The pole in three lengths with a band between them, so no face is stretched over more than a tile.
    [[2.7, 14.5], [14.5, 26.5], [26.5, BAR + 3]].forEach(([lo, hi], i) => {
      els.push(B(`${name} pole ${i}`, [7.45, lo, 7.45], [8.55, hi, 8.55], "pole"));
      if (i) els.push(B(`${name} band ${i}`, [7.3, lo - 0.3, 7.3], [8.7, lo + 0.3, 8.7], "fitting"));
    });
    els.push(
      B(`${name} bracket`, [7.2, BAR - 0.3, 7.2], [8.8, BAR + 1.4, 8.7], "fitting"),
      B(`${name} bar west`, [0, BAR, 8.55], [8, BAR + 1.1, 9.65], "pole"),
      B(`${name} bar east`, [8, BAR, 8.55], [16, BAR + 1.1, 9.65], "pole"),
      B(`${name} cap`, [7.25, BAR + 3, 7.25], [8.75, BAR + 3.6, 8.75], "fitting"),
    );
    for (const [side, x] of [["west", -0.6], ["east", 16]]) els.push(B(`${name} knob ${side}`, [x, BAR - 0.2, 8.35], [x + 0.6, BAR + 1.3, 9.85], "fitting"));
    for (let i = 0; i < 4; i++) {
      const x = x0 + 0.6 + (i * (CW - 3.2)) / 3;
      els.push(B(`${name} loop ${i}`, [x, BAR - 0.1, 8.45], [x + 2, BAR + 1.2, 9.75], "hem"));
    }
    for (let i = 0; i < CW;) {
      let j = i;
      while (j < CW && rags[j] === rags[i]) j++;
      const v = ((CH - rags[i]) / CH) * 16;
      els.push(B(`${name} cloth ${i}`, [x0 + i, BAR - CH + rags[i], z], [x0 + j, BAR, z + 0.2], "hem", { faces: {
        south: ["cloth", [(i / CW) * 16, 0, (j / CW) * 16, v]],
        north: ["cloth", [((CW - j) / CW) * 16, 0, ((CW - i) / CW) * 16, v]] } }));
      i = j;
    }
    return els;
  };
  // A hanging chain: links alternately facing front and side.
  const chain = (name, x, z, top, bottom, tex = "iron") => {
    const links = [];
    for (let y = top, i = 0; y - 1.2 >= bottom; y -= 0.9, i++) {
      links.push(i % 2
        ? B(`${name} ${i}`, [x - 0.5, y - 1.2, z - 0.15], [x + 0.5, y, z + 0.15], tex)
        : B(`${name} ${i}`, [x - 0.15, y - 1.2, z - 0.5], [x + 0.15, y, z + 0.5], tex));
    }
    return links;
  };
  // Gold eagle on black. A small eagle stands on the pole; gilt cords with tassels hang from the bar's ends.
  const eagleBanner = () => [
    ...banner("banner", [2, 0, 1, 4, 3, 0, 0, 2, 5, 1, 0, 0, 3, 1, 0, 2]),
    ...[["west", -0.3], ["east", 16.3]].flatMap(([side, x]) => [
      B(`cord ${side}`, [x - 0.15, BAR - 5.2, 8.95], [x + 0.15, BAR - 0.2, 9.25], "giltdark"),
      B(`tassel head ${side}`, [x - 0.4, BAR - 5.9, 8.7], [x + 0.4, BAR - 5.2, 9.5], "gilt"),
      B(`tassel ${side}`, [x - 0.55, BAR - 7.6, 8.55], [x + 0.55, BAR - 5.9, 9.65], "gilt",
        { faces: Object.fromEntries(["north", "east", "south", "west"].map((sd) => [sd, ["feather", FULL, 90]])) }),
    ]),
    eagle("finial", [8, BAR + 5.9, 8], 0.3),
  ];
  // Bone and black cog and skull on rust red. A toothed wheel stands on the pole; chains with weights hang from the bar's ends.
  const cogBanner = () => {
    const els = banner("banner", [0, 3, 1, 0, 0, 2, 6, 4, 0, 1, 1, 0, 2, 3, 0, 1]);
    for (const [side, x] of [["west", -0.3], ["east", 16.3]]) {
      els.push(...chain(`chain ${side}`, x, 9.1, BAR - 0.2, BAR - 5.8));
      els.push(B(`weight ${side}`, [x - 0.6, BAR - 7.0, 8.8], [x + 0.6, BAR - 5.8, 9.4], "fitting", { rot: [0, 0, 45], origin: [x, BAR - 6.4, 9.1] }));
    }
    const cx = 8, cy = BAR + 5.5, r = 1.9, half = r * Math.tan((22.5 * Math.PI) / 180);
    for (let k = 0; k < 4; k++) els.push(B(`wheel ${k}`, [cx - r, cy - half, 7.6], [cx + r, cy + half, 8.4 - 0.01 * k], "fitting", k ? { rot: [0, 0, 45 * k], origin: [cx, cy, 8] } : {}));
    for (let k = 0; k < 8; k++) els.push(B(`tooth ${k}`, [cx - 0.45, cy + r - 0.1, 7.66], [cx + 0.45, cy + r + 0.65, 8.34], "fitting", k ? { rot: [0, 0, 45 * k], origin: [cx, cy, 8] } : {}));
    els.push(B("wheel hub", [cx - 0.75, cy - 0.75, 7.5], [cx + 0.75, cy + 0.75, 8.5], "shadow"));
    return els;
  };

  // ---- The reliquary: a gothic bronze shrine holding a bone behind glass. Rune bands and skull roundels round a framed window, a
  // keyhole plate in the base, chains down the front corners, a pediment with a gilded two-headed eagle relief, a stepped roof with a
  // winged skull on top.
  const RELIQUARY = {
    bronze: T + "bronze-worn", bronzedark: T + "bronze-dark", bronzebright: T + "bronze-bright", iron: T + "iron-worn",
    runeband: T + "rune-band", runecolumn: T + "rune-column", roundel: T + "skull-roundel", keyhole: T + "keyhole", backdrop: T + "backdrop",
    bone: "game:block/creature/bone", glass: "game:block/glass/plain", shadow: "game:block/cloth/basic/black",
    gilt: GILT.gilt, giltdark: GILT.giltdark, feather: GILT.feather, plumage: GILT.plumage,
  };
  // One wing of the crest's winged skull, pointing west; the east wing is its mirror.
  const WING = ["......####", "...#######", ".#########", "####.#####", "#.#.#.#.##"];
  // A thin decorated plate: the whole texture on its front (south), plain bronze round it.
  const plate = (name, f, t, key) => B(name, f, t, "bronzedark", { faces: { south: [key, FULL] } });
  const reliquary = () => {
    const els = [
      // Base with its front plates: skull, keyhole, skull.
      B("plinth", [1.5, 0, 4.5], [14.5, 1, 11.5], "bronzedark"),
      B("base", [2, 1, 5], [14, 5, 11], "bronze"),
      plate("base roundel left", [2.4, 1.4, 11], [5.6, 4.6, 11.3], "roundel"),
      plate("base keyhole", [6.2, 1.2, 11], [9.8, 4.8, 11.4], "keyhole"),
      plate("base roundel right", [10.4, 1.4, 11], [13.6, 4.6, 11.3], "roundel"),
      B("lower band", [1.8, 5, 4.8], [14.2, 6.5, 11.4], "bronzedark", { faces: { south: ["runeband", FULL], north: ["runeband", FULL] } }),
      // Window section: rune columns with skull roundels at the corners, the relic behind glass.
      B("column left", [2, 8.5, 5], [4, 13.5, 11.2], "bronze", { faces: { south: ["runecolumn", FULL] } }),
      B("column right", [12, 8.5, 5], [14, 13.5, 11.2], "bronze", { faces: { south: ["runecolumn", FULL] } }),
      B("back wall", [4, 6.5, 5.2], [12, 15.5, 6.2], "bronze", { faces: { south: ["backdrop", FULL] } }),
      B("frame bottom", [4, 6.5, 6.2], [12, 7.2, 11], "bronzedark"),
      B("frame top", [4, 14.8, 6.2], [12, 15.5, 11], "bronzedark"),
      B("frame left", [4, 6.5, 6.2], [4.6, 15.5, 11], "bronzedark"),
      B("frame right", [11.4, 6.5, 6.2], [12, 15.5, 11], "bronzedark"),
      B("upper band", [1.8, 15.5, 4.8], [14.2, 17, 11.4], "bronzedark", { faces: { south: ["runeband", FULL], north: ["runeband", FULL] } }),
    ];
    for (const x of [3, 13]) for (const y of [6.5, 13.5]) els.push(B(`corner roundel ${x} ${y}`, [x - 1.1, y, 4.9], [x + 1.1, y + 2, 11.4], "bronzedark", { faces: { south: ["roundel", FULL] } }));
    // The relic: a long bone lying aslant on the backdrop.
    els.push(J("relic", [8, 11, 7.2], [0, 0, 14], [
      B("bone shaft", [-2.6, -0.4, -0.4], [2.6, 0.4, 0.4], "bone"),
      B("bone end left a", [-3.5, 0.05, -0.55], [-2.4, 1, 0.55], "bone"),
      B("bone end left b", [-3.5, -1, -0.55], [-2.4, -0.05, 0.55], "bone"),
      B("bone end right a", [2.4, 0.05, -0.55], [3.5, 1, 0.55], "bone"),
      B("bone end right b", [2.4, -1, -0.55], [3.5, -0.05, 0.55], "bone"),
    ]));
    // The game's glass texture is see-through, so the pane goes in the transparent render pass (3), as in the game's display case;
    // in the default pass it shows the sky and caves behind the block instead of the relic.
    els.push(B("glass", [4.6, 7.2, 10.7], [11.4, 14.8, 10.9], "glass", { extra: { renderPass: 3 } }));
    // Pediment: a framed panel as wide as the shrine with the gilded two-headed eagle on it, a stepped roof with a pinnacle on each
    // corner, the winged skull on its tip.
    const top = 22.9;
    els.push(
      B("pediment", [2.6, 17, 5.5], [13.4, top, 10.8], "bronze", { faces: { south: "bronzedark" } }),
      B("pediment post left", [2.4, 17, 10.4], [3.1, top, 11.2], "bronze"),
      B("pediment post right", [12.9, 17, 10.4], [13.6, top, 11.2], "bronze"),
      B("pediment sill", [2.4, 17, 10.6], [13.6, 17.35, 11.3], "bronze"),
      B("pediment lintel", [2.2, top, 5.3], [13.8, top + 0.6, 11.3], "bronzedark"),
      B("roof 1", [4.2, top + 0.6, 5.8], [11.8, top + 1.5, 10.6], "bronze"),
      B("roof 2", [5.4, top + 1.5, 6.3], [10.6, top + 2.4, 10.1], "bronzedark"),
      B("roof 3", [6.6, top + 2.4, 6.8], [9.4, top + 3.3, 9.6], "bronze"),
      eagle("eagle", [8, 20.45, 11.05], 0.36, 0.2),
    );
    for (const x of [3.1, 12.9]) els.push(B(`pinnacle ${x}`, [x - 0.5, top + 0.6, 7.7], [x + 0.5, top + 2.4, 8.7], "bronze"), B(`finial ${x}`, [x - 0.75, top + 2.4, 7.45], [x + 0.75, top + 3.1, 8.95], "bronzedark"));
    // Crest: a winged skull on the roof's tip.
    const y = top + 3.3, wcell = 0.4;
    els.push(
      B("crest skull", [7.1, y, 7.3], [8.9, y + 2, 9.5], "bronzebright"),
      B("crest jaw", [7.4, y - 0.3, 8], [8.6, y + 0.4, 9.6], "bronzebright"),
      B("crest eye left", [7.3, y + 0.8, 9.5], [7.9, y + 1.4, 9.6], "shadow"),
      B("crest eye right", [8.1, y + 0.8, 9.5], [8.7, y + 1.4, 9.6], "shadow"),
      relief(WING, { at: [7.1 - WING[0].length * wcell, y + 2.2, 8.7], cell: wcell, depth: 0.5, tex: "bronzebright", name: "wing west" }),
      relief(WING.map((w) => [...w].reverse().join("")), { at: [8.9, y + 2.2, 8.7], cell: wcell, depth: 0.5, tex: "bronzebright", name: "wing east" }),
      // Chains down the front corners.
      ...chain("chain left", 1.5, 10.6, 16.5, 5.5), ...chain("chain right", 14.5, 10.6, 16.5, 5.5),
    );
    return els;
  };

  // ---- The stasis pod: a destroyed metal automaton slumped out of a shattered coffin-shaped pod. Back to the north wall. Long dead:
  // no light left anywhere. The pod's glass is gone but for shards, a piece of its frame lies beside it, the fluid has dried to a
  // crust, one canister is drained and the other knocked over; the blackened automaton has fallen forward onto one knee, one arm
  // torn off and one leg snapped, its eyes and core dark.
  const POD = {
    frame: T + "pod-frame", metal: T + "automaton-metal", dark: T + "automaton-dark", cloth: T + "cloth-tattered", interior: T + "pod-interior",
    residue: T + "residue", glass: T + "glass-grey", glyph: T + "glyph-panel-dead", black: "game:block/cloth/basic/black",
  };
  // A straight bar in the xy plane from p to q (as seen from the front), `thick` wide, from z0 to z1.
  const bar2 = (name, [px, py], [qx, qy], thick, z0, z1, tex) => {
    const cx = (px + qx) / 2, cy = (py + qy) / 2, length = Math.hypot(qx - px, qy - py);
    const angle = Math.round((-Math.atan2(qx - px, qy - py) * 180) / Math.PI * 100) / 100;
    return B(name, [cx - thick / 2, cy - length / 2, z0], [cx + thick / 2, cy + length / 2, z1], tex, { rot: [0, 0, angle], origin: [cx, cy, (z0 + z1) / 2] });
  };
  // The pod's outline, as seen from the front: bottom, shoulders, top (x from the centre, y up).
  const [POD_BOTTOM, POD_SHOULDER, POD_TOP] = [[5, 2], [11, 30], [6, 42]];
  const podHalfWidth = (y) => {
    const [[wb, yb], [ws, ys], [wt, yt]] = [POD_BOTTOM, POD_SHOULDER, POD_TOP];
    return y <= ys ? wb + ((ws - wb) * (y - yb)) / (ys - yb) : ws + ((wt - ws) * (y - ys)) / (yt - ys);
  };
  // The automaton, built upright from its pelvis and posed by its joints.
  const automaton = () => {
    const ribs = [];
    [4.5, 6, 7.5, 9, 10.5].forEach((y, i) => {
      ribs.push(B(`rib ${i} left`, [-3.6, y, -1.6], [-3, y + 0.7, 2], "metal"), B(`rib ${i} right`, [3, y, -1.6], [3.6, y + 0.7, 2], "metal"),
        B(`rib ${i} front left`, [-3.6, y, 1.4], [-0.6, y + 0.7, 2], "metal"));
      if (i !== 1 && i !== 2) ribs.push(B(`rib ${i} front right`, [0.6, y, 1.4], [3.6, y + 0.7, 2], "metal")); // two ribs on the right were smashed in
    });
    const head = [
      B("neck", [-0.6, 0, -0.6], [0.6, 1.5, 0.6], "dark"),
      B("skull", [-2.2, 1.5, -2.2], [2.2, 6, 2.4], "metal"),
      B("jaw", [-1.6, 0.6, -0.2], [1.6, 1.7, 2.2], "metal"),
      B("headdress left", [-2.7, 0.5, -2], [-2.2, 5.6, 1.6], "dark"),
      B("headdress right", [2.2, 0.5, -2], [2.7, 5.6, 1.6], "dark"),
      B("eye left", [-1.7, 3.6, 2.4], [-0.5, 4.4, 2.5], "black"),
      B("eye right", [0.5, 3.6, 2.4], [1.7, 4.4, 2.5], "black"),
    ];
    const hand = [0, 1, 2, 3].map((i) => B(`finger ${i}`, [-0.9 + i * 0.6, -3, -0.25], [-0.5 + i * 0.6, 0, 0.25], "metal"));
    const forearm = [B("forearm bone a", [-0.9, -7, -0.4], [-0.1, 0, 0.4], "metal"), B("forearm bone b", [0.1, -7, -0.4], [0.9, 0, 0.4], "metal"),
      B("wrist", [-1, -7.6, -0.6], [1, -6.8, 0.6], "dark"), J("hand", [0, -7.6, 0], [-60, 0, 0], hand)]; // bent back at the wrist: flat on the ground
    const rightArm = [B("upper arm", [-0.7, -8, -0.7], [0.7, 0, 0.7], "metal"), B("elbow", [-1, -8.6, -1], [1, -7.4, 1], "dark"),
      J("elbow joint", [0, -8, 0], [-20, 0, 0], forearm)];
    const cloak = [10, 13, 8, 12, 9, 14, 11].map((length, i) => B(`cloak strip ${i}`, [-4.5 + i * 1.5, -length, -2.2], [-3.2 + i * 1.5, 0, -1.8], "cloth",
      { rot: [0, 0, ((i % 3) - 1) * 4], origin: [-3.9 + i * 1.5, 0, -2] }));
    const torso = [
      B("spine", [-0.8, 0, -0.8], [0.8, 12, 0.8], "dark"),
      ...ribs,
      B("core", [-1, 7.6, -0.6], [1, 9.6, 1.2], "residue"),
      B("collarbone", [-5, 12, -1], [5, 13.2, 1.2], "metal"),
      B("shoulder plate left", [-6.6, 11, -2.2], [-3.4, 13.8, 2.2], "dark"),
      B("shoulder plate right", [3.4, 11, -2.2], [6.6, 13.8, 2.2], "dark"),
      J("cloak", [0, 12.5, 0], null, cloak),
      J("neck joint", [0, 13.2, 0], [14, 14, 16], head),
      J("right arm", [5, 12.3, 0], [-70, 0, -4], rightArm),
      // The left arm is gone: a stump and the wires that held it.
      B("left stump", [-5.8, 10.4, -0.7], [-4.4, 12.4, 0.7], "metal"),
      B("wire a", [-5.3, 6.5, 0], [-5.1, 10.5, 0.2], "black", { rot: [0, 0, 8], origin: [-5.2, 10.5, 0.1] }),
      B("wire b", [-4.9, 7.5, -0.3], [-4.7, 10.5, -0.1], "residue", { rot: [0, 0, -6], origin: [-4.8, 10.5, -0.2] }),
    ];
    const shin = [B("shin bone", [-0.7, -9, -0.7], [0.7, 0, 0.7], "metal"), B("knee cap", [-1.1, -1, -0.3], [1.1, 0.8, 1.2], "dark"),
      // Kneeling, the shin lies back along the ground, so the foot points along it instead of down.
      B("foot", [-1.2, -12.6, -1], [1.2, -9, 0.6], "dark")];
    const rightLeg = [B("thigh", [-0.8, -9, -0.8], [0.8, 0, 0.8], "metal"), J("knee", [0, -9, 0], [156, 0, 0], shin)];
    const leftThigh = [B("thigh", [-0.8, -6.5, -0.8], [0.8, 0, 0.8], "metal"), B("snapped end", [-0.5, -7.3, -0.3], [0.3, -6.5, 0.5], "metal")];
    const pelvis = [
      B("pelvis", [-3, -1, -1.5], [3, 1.5, 1.5], "dark"),
      B("hip plate", [-3.4, -0.6, 1.3], [3.4, 1.2, 1.9], "metal"),
      J("torso", [0, 1.5, 0], [16, 0, -8], torso),
      J("right leg", [2, -0.5, 0], [-66, 0, -4], rightLeg),
      J("left leg", [-2, -0.5, 0], [-75, 0, 18], leftThigh),
    ];
    return J("pelvis", [8, 5.5, 7.5], [0, 12, 0], pelvis);
  };
  // The glass shards' turns and places: random numbers drawn once by the Python generator (random.Random(7)) and kept, so the pod
  // looks as it always has.
  const SHARD_TURNS = [6, -16, 15, -29, -26, 33];
  const FLOOR_SHARDS = [[0.07086092267301591, 19.202062067888583, 1.318733656828792, 54], [-1.1750955142763326, 17.48692536211744, 0.5628698812171571, 23], [10.123039583410486, 13.179770819908375, 1.0089083247737718, 57], [11.873770146098217, 19.204464401294686, 0.5556758435025263, 149], [6.726970442317164, 23.72693371431858, 0.5419244125559807, 34], [4.371404299296878, 14.158933458610532, 0.6060130142705316, 78], [10.32566009428188, 20.343030989753853, 0.5927501411992322, 146], [12.056096316376049, 16.782571741345908, 0.992970019138602, 16], [10.416102448934506, 19.61861032149587, 0.9467730456021426, 136], [7.407030724726862, 16.112692459333104, 1.027005677156875, 116], [5.954811830780459, 15.356905725861818, 0.6617900746248289, 62], [-0.19918976249306342, 15.95286486327469, 0.94560472359973, 87], [14.047796367662787, 15.811284296237146, 1.3821573627433237, 30], [9.26252227424608, 14.397064191901093, 0.807850225543872, 125]];
  const stasisPod = () => {
    const els = [B("base", [-4, 0, 0], [20, 2, 12], "frame", { faces: { south: ["glyph", FULL] } })];
    // Back of the pod: horizontal slabs following the coffin outline.
    for (let y = 2; y < POD_TOP[1]; y += 2) {
      const w = podHalfWidth(y + 1);
      els.push(B(`back ${y}`, [8 - w + 1.5, y, 1], [8 + w - 1.5, y + 2, 2.5], "frame", { faces: { south: "interior" } }));
    }
    els[els.length - 1] = { ...els[els.length - 1], faces: { south: "interior", up: "frame" } };
    // Frame: the coffin outline; the upper right side is broken off and lies on the floor.
    const [[wb, yb], [ws, ys], [wt, yt]] = [POD_BOTTOM, POD_SHOULDER, POD_TOP];
    els.push(
      bar2("frame lower left", [8 - wb, yb], [8 - ws, ys], 2.5, 1, 10, "frame"),
      bar2("frame lower right", [8 + wb, yb], [8 + ws, ys], 2.5, 1, 10, "frame"),
      bar2("frame upper left", [8 - ws, ys], [8 - wt, yt], 2.5, 1, 10, "frame"),
      bar2("frame upper right stub", [8 + ws, ys], [8 + ws - 2, ys + 5], 2.5, 1, 10, "frame"),
      B("frame top", [8 - wt - 1, yt - 1, 1], [8 + wt + 1, yt + 1.5, 10], "frame"),
      B("frame bottom", [8 - wb - 1, yb, 1], [8 + wb + 1, yb + 1.5, 10], "frame"),
      // Fins rising from the shoulders, as on the old tomb doors.
      bar2("fin left", [8 - ws - 1, ys - 2], [8 - ws - 6, ys + 12], 2, 2.5, 5.5, "frame"),
      bar2("fin left spur", [8 - ws - 3, ys + 4], [8 - ws - 7, ys + 6], 1.2, 3, 5, "frame"),
      bar2("fin right", [8 + ws + 1, ys - 2], [8 + ws + 6, ys + 12], 2, 2.5, 5.5, "frame"),
      bar2("fin right spur", [8 + ws + 3, ys + 4], [8 + ws + 7, ys + 6], 1.2, 3, 5, "frame"),
      J("fallen frame piece", [24, 1.2, 9], [0, 35, 0], [B("frame piece", [-1.25, -1.2, -6.5], [1.25, 1.2, 6.5], "frame", { rot: [0, 0, 8] })]),
    );
    // Glass shards still caught in the frame, and more on the floor.
    [[8, -1], [15, -1], [24, -1], [12, 1], [20, 1], [36, -1]].forEach(([y, side], i) => {
      const x = 8 + side * (podHalfWidth(y) - 1.5);
      els.push(B(`shard frame ${i}`, [x - 0.8, y - 1.6, 9.4], [x + 0.8, y + 1.6, 9.6], "glass", { rot: [0, 0, SHARD_TURNS[i]], origin: [x, y, 9.5] }));
    });
    FLOOR_SHARDS.forEach(([x, z, s, a], i) => els.push(B(`shard floor ${i}`, [x - s, 0, z - s * 0.6], [x + s, 0.15, z + s * 0.6], "glass", { rot: [0, a, 0], origin: [x, 0, z] })));
    // The spilled fluid, long since dried to a crust.
    [[3, 12, 13, 18], [6, 18, 15, 22], [1, 15, 5, 20]].forEach(([x0, z0, x1, z1], i) => els.push(B(`spill ${i}`, [x0, 0, z0], [x1, 0.1, z1], "residue")));
    // Canisters: the left one still standing, half drained; the right one knocked over and cracked.
    els.push(
      B("left post", [-7, 0, 5], [-5.5, 12, 6.5], "frame"),
      B("left canister cap", [-8, 18, 4], [-4.5, 19, 7.5], "frame"),
      B("left canister base", [-8, 11.5, 4], [-4.5, 12.5, 7.5], "frame"),
      B("left canister glass", [-7.6, 12.5, 4.4], [-4.9, 18, 7.1], "glass"),
      B("left canister dregs", [-7.4, 12.5, 4.6], [-5.1, 13.4, 6.9], "residue"),
      J("right canister", [23, 1.8, 18], [0, -25, 90], [
        B("right canister glass", [-1.35, -2.8, -1.35], [1.35, 2.8, 1.35], "glass"),
        B("right canister cap", [-1.75, 2.8, -1.75], [1.75, 3.8, 1.75], "frame"),
        B("right canister crack", [-1.4, -1, 1.3], [1.4, -0.4, 1.45], "black")]),
      B("right post", [21.5, 0, 5], [23, 12, 6.5], "frame"),
      B("right post cradle", [21, 11.5, 4], [24.5, 12.5, 7.5], "frame"),
    );
    // Cables from the base curling across the floor.
    for (const [name, pts] of [["cable left", [[-2, 6], [-5, 10], [-6, 15], [-3, 19]]], ["cable right", [[18, 6], [21, 10], [20, 14]]]]) {
      pts.slice(1).forEach(([x1, z1], i) => {
        const [x0, z0] = pts[i], length = Math.hypot(x1 - x0, z1 - z0), cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
        const angle = Math.round((Math.atan2(x1 - x0, z1 - z0) * 180) / Math.PI * 10) / 10;
        els.push(B(`${name} ${i}`, [cx - 0.6, 0, cz - length / 2 - 0.3], [cx + 0.6, 1.2, cz + length / 2 + 0.3], "dark", { rot: [0, angle, 0], origin: [cx, 0.6, cz] }));
      });
    }
    // The automaton, and the pieces torn from it, tilted until both their ends touch the ground and settled on it.
    els.push(automaton());
    els.push(J("torn arm", [14, 0.6, 21], [0, 70, 88], [
      B("torn upper arm", [-0.7, -8, -0.7], [0.7, 0, 0.7], "metal"),
      B("torn forearm a", [-0.9, -15, -0.4], [-0.1, -8, 0.4], "metal"),
      B("torn forearm b", [0.1, -15, -0.4], [0.9, -8, 0.4], "metal"),
      ...[0, 1, 2, 3].map((i) => B(`torn finger ${i}`, [-0.9 + i * 0.6, -18, -0.25], [-0.5 + i * 0.6, -15, 0.25], "metal")),
      B("torn shoulder plate", [-1.6, -1, -2.2], [1.6, 1.8, 2.2], "dark")], { ground: true }));
    els.push(J("snapped shin", [1, 0.7, 19], [0, -40, 94], [
      B("lost shin bone", [-0.7, -8, -0.7], [0.7, 0, 0.7], "metal"),
      B("lost foot", [-1.2, -9, -1], [1.2, -8, 2.8], "dark")], { ground: true }));
    return els;
  };

  // ---- The scarab: a broken mechanical beetle rolled onto its side. Head toward the south: a long domed shell, a round sensor eye
  // under vent slats, curved mandibles and six jointed legs. Broken: the shell cracked open on top with the missing chunk beside it,
  // one leg torn off at the hip, one snapped at the knee, one mandible broken short, the eye dark.
  const SCARAB = { shell: T + "scarab-shell", dark: T + "automaton-dark", inside: T + "residue", lens: T + "glass-grey", black: "game:block/cloth/basic/black" };
  // A leg from a hip on the body's side: thigh out and up, shin down to a point; side 1 east, -1 west.
  const scarabLeg = (name, s, lift, splay, knee, snapped = false) => {
    const shin = snapped ? [B(`${name} snapped shin`, [-0.45, -2.2, -0.45], [0.45, 0, 0.45], "dark")]
      : [B(`${name} shin`, [-0.45, -6.5, -0.45], [0.45, 0, 0.45], "dark"), B(`${name} claw`, [-0.25, -8, -0.25], [0.25, -6.5, 0.25], "shell")];
    const thigh = [B(`${name} hip`, [-0.9, -0.9, -0.9], [0.9, 0.9, 0.9], "shell"),
      B(`${name} thigh`, [Math.min(0, s * 5), -0.55, -0.55], [Math.max(0, s * 5), 0.55, 0.55], "dark"),
      B(`${name} knee`, [s * 5 - 0.7, -0.7, -0.7], [s * 5 + 0.7, 0.7, 0.7], "shell"),
      J(`${name} knee joint`, [s * 5, 0, 0], [0, 0, s * knee], shin)];
    return [[0, splay, s * lift], thigh];
  };
  const scarab = () => {
    const legs = [];
    for (const [side, x] of [[1, 12.6], [-1, 3.4]]) {
      [5, 9, 13].forEach((z, i) => {
        const name = `leg ${side > 0 ? "east" : "west"} ${i}`;
        if (side > 0 && i === 1) { // torn off: only the socket and a few wires are left
          legs.push(B(`${name} socket`, [x - 0.8, 4.2, z - 0.8], [x + 0.8, 5.8, z + 0.8], "black"),
            B(`${name} wire`, [x + 0.3, 2.5, z - 0.1], [x + 0.5, 5, z + 0.1], "black", { rot: [0, 0, 20], origin: [x + 0.4, 5, z] }));
          return;
        }
        const [rot, parts] = scarabLeg(name, side, side > 0 ? 30 - 8 * i : 10 + 6 * i, (i - 1) * -25 * side, -95 - 10 * i, side < 0 && i === 2);
        legs.push(J(name, [x, 5, z], rot, parts));
      });
    }
    const mandibles = [[1, false], [-1, true]].map(([side, broken]) => {
      const x = 8 + side * 2.6;
      const tip = broken ? [] : [B(`mandible ${side} tip`, [-0.3, -0.3, 0], [0.3, 0.3, 3], "shell", { rot: [-40, 0, 0], origin: [0, 0, 0] })];
      return J(`mandible ${side}`, [x, 4.2, 17.5], [20, side * 18, 0], [
        B(`mandible ${side} base`, [-0.5, -0.5, 0], [0.5, 0.5, broken ? 1.6 : 3.6], "shell"),
        broken ? B(`mandible ${side} break`, [-0.35, -0.35, 1.6], [0.35, 0.35, 2], "dark") : J(`mandible ${side} bend`, [0, 0, 3.6], [25, 0, 0], tip)]);
    });
    const body = [
      B("belly", [3.5, 3, 2], [12.5, 5, 15], "dark"),
      B("shell", [3, 5, 2], [13, 8, 15], "shell"),
      B("shell band", [2.8, 5.6, 2.5], [13.2, 6.3, 14.5], "dark"),
      // The top of the shell, cracked open on the east side.
      B("shell top west", [4, 8, 3], [8.5, 9.5, 14], "shell"),
      B("shell top east front", [8.5, 8, 10], [12, 9.5, 14], "shell"),
      B("shell top east back", [8.5, 8, 3], [12, 9.5, 5.5], "shell"),
      B("crack", [8.5, 7.9, 5.5], [12, 8.05, 10], "inside"),
      B("crack edge", [8.4, 8, 5.4], [8.6, 9.2, 10.1], "dark"),
      B("dome", [5.5, 9.5, 4.5], [8.5, 10.5, 12.5], "shell"),
      B("dome east", [8.5, 9.5, 10], [10.5, 10.5, 12.5], "shell"),
      B("tail", [4.5, 3.5, 0.8], [11.5, 7.5, 2], "shell"),
      // Head: a rounded front with the dead sensor eye and vent slats above it.
      B("head", [4.5, 3.5, 15], [11.5, 8, 17.5], "shell"),
      B("snout", [5.5, 4, 17.5], [10.5, 7.5, 18.4], "shell"),
      B("eye rim", [6.6, 4.8, 18.4], [9.4, 7.2, 18.8], "dark"),
      B("eye", [7.1, 5.2, 18.8], [8.9, 6.8, 19], "lens"),
      ...[0, 1, 2].map((i) => B(`vent ${i}`, [5.8, 8.6, 12.6 + i * 0.7], [10.2, 9.6, 12.9 + i * 0.7], "black")),
      ...legs, ...mandibles,
    ];
    return [
      // Rolled onto its west side, nose down; like the two loose pieces, settled so its lowest claw is on the ground.
      J("scarab", [8, -1.6, 8], [6, -18, 24], [J("body", [-8, 0, -8], null, body)], { ground: true }),
      // The chunk of shell that came off, lying beside it.
      J("shell chunk", [15, 0.6, 6], [0, 30, 170], [
        B("chunk", [-1.8, -0.6, -2.2], [1.8, 0.8, 2.2], "shell"),
        B("chunk inside", [-1.6, -0.75, -2], [1.6, -0.6, 2], "inside")], { ground: true }),
      // The torn leg.
      J("torn leg", [2, 0.5, 18], [0, 55, 90], [
        B("torn thigh", [-0.55, -5, -0.55], [0.55, 0, 0.55], "dark"),
        B("torn knee", [-0.7, -5.7, -0.7], [0.7, -4.3, 0.7], "shell"),
        B("torn shin", [-0.45, -11.5, -0.45], [0.45, -5.5, 0.45], "dark")], { ground: true }),
    ];
  };

  return {
    out: "../ReliquiarumMod/assets/nf3treliquiarum/shapes/block",
    gui: { file: "../ReliquiarumMod/assets/nf3treliquiarum/blocktypes/relic.json", key: "*-{name}-*", rotateY: 180, keepScale: true },
    shapes: {
      fallengiant: { textures: ARMOR, elements: fallenGiant() },
      reliquary: { textures: RELIQUARY, elements: reliquary() },
      stasispod: { textures: POD, elements: stasisPod() },
      scarab: { textures: SCARAB, elements: scarab() },
      aquila: { textures: GILT, elements: aquila() },
      eaglebanner: { textures: EAGLE_BANNER, elements: eagleBanner() },
      cogbanner: { textures: COG_BANNER, elements: cogBanner() },
    },
  };
};
