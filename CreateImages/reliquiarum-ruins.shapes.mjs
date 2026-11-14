// Shapes of the relics built with vsdev: the two ruined test-chamber relics and the sun disc. Made with (textures first, the render reads them):
//   vsdev texture CreateImages/reliquiarum-ruins.textures.json5
//   vsdev shape CreateImages/reliquiarum-ruins.shapes.mjs --render
const T = "nf3treliquiarum:block/";

export default ({ box, joint, relief, mirrorRows }) => {
  // ---- Ruined test crate: a framed cube, 14 wide, tilted and sunk into the ground. Built upright in its own
  // coordinates (0..14), then tipped by the joint that holds it.
  const S = 14, CAP = 4, RAIL = 1.5;
  const ends = [0, S - CAP];
  const crate = [box([1, 1, 1], [S - 1, S - 1, S - 1], "panel", { name: "core" })];

  // Corner caps. The top south-east one broke off (it lies beside the crate); the bottom north-west one is cracked loose.
  for (const x of ends) for (const y of ends) for (const z of ends) {
    const gone = x && y && z;
    if (gone) continue;
    const loose = !x && !y && !z;
    crate.push(box([x, y, z], [x + CAP, y + CAP, z + CAP], "enamel", { name: "cap", ...(loose ? { rot: [0, 9, 0] } : {}) }));
  }
  // Where the cap was: the torn mounting, dark.
  crate.push(box([S - 3.4, S - 3.4, S - 3.4], [S - 0.6, S - 0.6, S - 0.6], "hollow", { name: "capsocket" }));

  // Rails along the twelve edges, between the caps. Three are damaged.
  const lo = 0, hi = S - RAIL, a = CAP, b = S - CAP;
  const rail = (from, to, opts = {}) => crate.push(box(from, to, "enamel", { name: "rail", ...opts }));
  for (const y of [lo, hi]) for (const z of [lo, hi]) {
    if (y && z) rail([a, y, z], [a + 3.2, y + RAIL, z + RAIL], { rot: [0, 0, 14], origin: [a, y, z + RAIL] }); // top south: snapped, the stub bent up
    else rail([a, y, z], [b, y + RAIL, z + RAIL]);
  }
  for (const x of [lo, hi]) for (const y of [lo, hi]) {
    if (x && y) continue; // top east: gone with the cap
    rail([x, y, a], [x + RAIL, y + RAIL, b]);
  }
  for (const x of [lo, hi]) for (const z of [lo, hi]) {
    if (x && z) rail([x, a, z], [x + RAIL, b, z + RAIL], { rot: [-10, 0, 16], origin: [x, a, z + RAIL] }); // south-east upright: hangs from its foot
    else rail([x, a, z], [x + RAIL, b, z + RAIL]);
  }

  // Face plates, 6 x 6, half a unit proud. South: cracked. East: fallen off (a dark socket is left).
  const p0 = 4, p1 = 10, out = 0.5;
  const plate = (name, from, to, side, tex) => crate.push(box(from, to, "enamel", { name, faces: { [side]: { texture: tex, uv: "full" } } }));
  plate("platesouth", [p0, p0, S - 1], [p1, p1, S - 1 + out], "south", "emblembroken");
  plate("platenorth", [p0, p0, 1 - out], [p1, p1, 1], "north", "emblem");
  plate("platewest", [1 - out, p0, p0], [1, p1, p1], "west", "emblem");
  plate("platetop", [p0, S - 1, p0], [p1, S - 1 + out, p1], "up", "emblem");
  crate.push(box([S - 1, p0 + 0.5, p0 + 0.5], [S - 0.9, p1 - 0.5, p1 - 0.5], "hollow", { name: "platesocket" }));


  const crateElements = [
    joint([1.5, -0.5, 1], { name: "crate", rot: [5, 0, -8], origin: [8, 0, 8], children: crate }),
    // What fell off: the corner cap, the east plate propped against the crate, a rail end.
    box([14.5, 0, 12.5], [18.5, 4, 16.5], "enamel", { name: "fallencap", ground: true, rot: [0, 28, 0] }),
    box([16.2, 0.3, 3.5], [16.7, 6.3, 9.5], "enamel", { name: "fallenplate", ground: true, rot: [0, -8, -24], origin: [16.2, 0.3, 6.5],
      faces: { east: { texture: "emblembroken", uv: "full" } } }),
    box([5, 0, 16.4], [9.5, 1.5, 17.9], "enamel", { name: "fallenrail", ground: true, rot: [0, -17, 0] }),
  ];

  // ---- Toppled sentinel: a three-legged watcher with a stepped, tapering shell and one dead lens, fallen onto its
  // west side. Built upright around its hip (0, 0, 0; lens to the south), then laid over by the joint that holds it.
  const leg = (name, at, rot, { snapped = false, knee = 20 } = {}) => joint(at, { name, rot, children: [
    box([-0.6, -5, -0.6], [0.6, 0.5, 0.6], "panel", { name: name + "upper" }),
    ...(snapped ? [] : [joint([0, -5, 0], { name: name + "knee", rot: [knee, 0, 0], children: [
      box([-0.45, -5.5, -0.45], [0.45, 0.3, 0.45], "panel", { name: name + "lower" }),
      box([-1.1, -6, -1.1], [1.1, -5.4, 1.1], "enamel", { name: name + "foot" }),
    ] })]),
  ] });

  const sentinel = [
    // Shell: three steps, narrower toward the top, and a cap.
    box([-3.2, 0, -2.6], [3.2, 6, 2.6], "enamel", { name: "shelllower" }),
    box([-2.7, 6, -2.2], [2.7, 11, 2.2], "enamel", { name: "shellmid" }),
    box([-2.1, 11, -1.7], [2.1, 14, 1.7], "enamel", { name: "shellupper" }),
    box([-1.3, 14, -1.1], [1.3, 15, 1.1], "panel", { name: "cap" }),
    box([-0.25, 15, -0.25], [0.25, 18.5, 0.25], "panel", { name: "antenna", rot: [0, 0, -38], origin: [0, 15, 0] }),
    // The dark band the lens sits in, and the lens.
    box([-2.9, 7, -2.4], [2.9, 9.6, 2.4], "panel", { name: "band" }),
    box([-1.5, 6.8, 2.3], [1.5, 9.8, 2.9], "panel", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
    box([-3.4, -0.6, -2.8], [3.4, 0, 2.8], "panel", { name: "hip" }),
    // East flank (it faces the sky now): the side panel has sprung open on its hinge; inside, the dark bay and two bent rods.
    box([3.2, 1, -1.9], [3.3, 5.4, 1.9], "hollow", { name: "bay" }),
    box([3.2, 5.4, -2.1], [3.7, 5.9, 2.1], "panel", { name: "hinge" }),
    box([3.3, 0.6, -2.1], [3.8, 5.6, 2.1], "enamel", { name: "sidepanel", rot: [0, 0, 62], origin: [3.5, 5.6, 0] }),
    box([3.3, 3.6, -1], [5.6, 4.1, -0.5], "panel", { name: "rod", rot: [0, 0, 22], origin: [3.3, 3.85, -0.75] }),
    box([3.3, 2, 0.5], [4.6, 2.5, 1], "panel", { name: "rod", rot: [0, 25, -30], origin: [3.3, 2.25, 0.75] }),
    // West flank lies in the dirt; its panel is gone (see below), so the bay is open there too.
    box([-3.3, 1, -1.9], [-3.2, 5.4, 1.9], "hollow", { name: "bay" }),
    // Legs: the two front ones splay to the sides, the third goes back. The west one snapped as it fell.
    leg("legeast", [2.2, -0.3, 1.6], [-28, 0, 30], { knee: 26 }),
    leg("legwest", [-2.2, -0.3, 1.6], [-28, 0, -30], { snapped: true }),
    leg("legback", [0, -0.3, -1.8], [34, 0, 0], { knee: -30 }),
  ];

  const sentinelElements = [
    joint([10.5, 3.5, 8.5], { name: "sentinel", rot: [0, 14, 84], children: sentinel }),
    // On the ground: the panel torn off the west flank, the snapped half of the leg with its foot.
    box([3, 0, 13], [8, 0.5, 17.2], "enamel", { name: "fallenpanel", ground: true, rot: [0, 24, 0] }),
    joint([14, 0.5, 14], { name: "brokenleg", ground: true, rot: [0, 35, 90], children: [
      box([-0.45, -5.5, -0.45], [0.45, 0, 0.45], "panel", { name: "brokenleglower" }),
      box([-1.1, -6.1, -1.1], [1.1, -5.5, 1.1], "enamel", { name: "brokenlegfoot" }),
    ] }),
  ];

  // ---- Sun disc: a gold disc a block across with a sleeping sun's face, leaning on a block of old masonry, one corner
  // broken off. Built upright (x 0..16, y 0..16, back at z 0), then leaned back by the joint that holds it.
  const chip = (rows) => rows.map((row, i) => (i < 3 ? row.slice(0, 10 + 2 * i).padEnd(16, ".") : row)); // the top east corner is gone
  const outline = chip(mirrorRows(["..######", ".#######", ...Array(12).fill("########"), ".#######", "..######"]));
  const rim = chip(mirrorRows(["..######", ".##.....", "##......", ...Array(10).fill("#......."), "##......", ".##.....", "..######"]));
  const stud = (x, y, size = 1.3) => box([x - size / 2, y - size / 2, 1.5], [x + size / 2, y + size / 2, 2.1], "gold", { name: "stud", rot: [0, 0, 45] });
  const disc = [
    relief(outline, { at: [0, 16, 1.5], cell: 1, depth: 1.5, tex: "recess", name: "plate" }),
    relief(rim, { at: [0, 16, 2.3], cell: 1, depth: 0.8, tex: "gold", name: "rim" }),
    // The boss with the face, its nose standing out.
    box([4, 4, 1.5], [12, 12, 2.6], "gold", { name: "boss", faces: { south: { texture: "face", uv: "full" } } }),
    box([7.5, 7, 2.6], [8.5, 8.2, 3], "gold", { name: "nose" }),
    // Diamond studs between the boss and the rim: three a side, a larger one in each corner that is left.
    ...[5, 8, 11].flatMap((v) => [stud(v, 2.6), stud(v, 13.4), stud(2.6, v), stud(13.4, v)]).filter((_, i) => i !== 9), // not the one by the break
    stud(3, 3, 1.7), stud(13, 3, 1.7), stud(3, 13, 1.7),
  ];
  const sundiscElements = [
    box([2.5, 0, 0.5], [12, 5.5, 6.5], "stone", { name: "masonry", rot: [0, -9, 0] }),
    box([9, 0, 1], [15.5, 3, 5], "stone", { name: "masonry", rot: [0, 14, 4], ground: true }),
    joint([0.5, 0, 12.5], { name: "sundisc", rot: [-48, 0, 0], children: [joint([8, 8, 0], { name: "askew", rot: [0, 0, 7], children: [
      joint([-8, -9.5, 0], { name: "discface", children: disc }),
    ] })] }),
    // The broken corner, face down in the dirt.
    box([13.5, 0, 12], [17, 1.5, 15], "gold", { name: "fragment", rot: [0, 33, 0], ground: true }),
  ];

  // ---- Gilded sun idol: a small gold sun, a round glossy domed face with two black oval eyes inside a raised rim, eight tapering
  // rays round it, the three on top ending in balls. Built around its centre (0, 0, 0; face to the south), then set on a worn stone
  // plinth. The gold is untouched: it did not age with the stone under it. Round parts are stacks of thin discs (a dome, a ball).
  const CELL = 0.3;
  const grid = (w, h, cell, inside) => Array.from({ length: Math.round(h / cell) }, (_, i) => Array.from({ length: Math.round(w / cell) }, (_, j) => {
    const nx = ((j + 0.5) / (w / cell)) * 2 - 1, ny = 1 - ((i + 0.5) / (h / cell)) * 2;
    return inside(nx, ny) ? "#" : ".";
  }).join(""));
  const round = (r, rIn = 0) => grid(2 * r, 2 * r, CELL, (x, y) => Math.hypot(x, y) <= 1 && Math.hypot(x, y) > rIn / r);
  const sunplate = (name, r, front, depth, tex, rIn = 0, at = [0, 0]) => relief(round(r, rIn), { at: [at[0] - r, at[1] + r, front], cell: CELL, depth, tex, name });
  // A ball of radius r at (x, y, z): thin discs, wide in the middle.
  const ball = (r, at) => [-0.8, -0.4, 0, 0.4, 0.8].map((k) => sunplate("ball", r * Math.sqrt(1 - (k / 1.05) ** 2), at[2] + r * k + 0.2, 0.4, "gold", 0, [at[0], at[1]]));
  // A cone-like ray along +y: boxes narrowing in width and thickness.
  const taper = (y0, y1, w0, w1, t0, t1, n) => Array.from({ length: n }, (_, i) => {
    const f = (i + 0.5) / n, w = w0 + (w1 - w0) * f, t = t0 + (t1 - t0) * f;
    return box([-w, y0 + ((y1 - y0) * i) / n, 0.1 - t], [w, y0 + ((y1 - y0) * (i + 1)) / n + 0.05, 0.1 + t], "gold", { name: "ray" });
  });
  const ray = (deg, balled) => joint([0, 0, 0], { name: `ray${deg}`, rot: [0, 0, deg], children: balled
    ? [...taper(4.2, 8.6, 2.7, 0.8, 1.3, 0.7, 6), ...ball(2.1, [0, 10.2, 0.1]).flat()]
    : taper(4.2, 9, 2.8, 0.1, 1.3, 0.15, 9) });
  const eye = (x) => relief(grid(1.6, 3.4, 0.25, (nx, ny) => nx * nx + ny * ny <= 1), { at: [x - 0.8, 1.9, 4.05], cell: 0.25, depth: 0.4, tex: "eye", name: "eye" });
  // The dome: ellipsoid slices (radius 4.4, 1.3 high) over the ring.
  const dome = [0, 0.3, 0.6, 0.9, 1.15].map((h) => sunplate("dome", 4.4 * Math.sqrt(1 - (h / 1.3) ** 2), 2.3 + h, 0.5, "face"));
  const sun = [
    ...[0, 45, -45].map((d) => ray(d, true)),
    ...[90, -90, 135, -135, 180].map((d) => ray(d, false)),
    sunplate("back", 5.5, 1, 2, "gold"),
    sunplate("rim", 5.5, 1.9, 0.9, "gold", 4.5),   // the raised ring round the face
    sunplate("rimin", 4.8, 2.3, 0.4, "gold", 4.4),
    ...dome,
    ...[-1.7, 1.7].flatMap((x) => eye(x)),
    box([-2.7, 2.4, 3.9], [-2, 3.5, 4.1], "glint", { name: "glint", rot: [0, 0, 35] }),
  ];
  const shinesunElements = [
    box([2, 0, 2], [14, 3, 14], "stone", { name: "plinth", rot: [0, 8, 0] }),
    box([4.5, 3, 4.5], [11.5, 4.6, 11.5], "stone", { name: "plinthtop", rot: [0, 8, 0] }),
    box([6, 4.6, 6], [10, 5.6, 10], "gold", { name: "collar" }),
    joint([8, 13.4, 8], { name: "sun", rot: [0, 0, 0], children: sun }),
  ];

  // ---- Gold skull plaque: a rounded gold tile with a beveled rim and round studs, and a crowned skull (sockets with pale eyes, a
  // nose, a toothed mouth) on a pale field. Built upright (x 0..16, y 0..16, back at z 0), then leaned back against masonry.
  // Every part is a shape of the plaque's own units (u east, v up, from the centre), drawn at half a unit per pixel.
  const C2 = 0.5;
  const part = (name, tex, front, depth, pred, box_ = [16, 16], at = [8, 8], c = C2) => relief(
    Array.from({ length: Math.round(box_[1] / c) }, (_, i) => Array.from({ length: Math.round(box_[0] / c) }, (_, j) =>
      pred(-box_[0] / 2 + (j + 0.5) * c, box_[1] / 2 - (i + 0.5) * c) ? "#" : ".").join("")),
    { at: [at[0] - box_[0] / 2, at[1] + box_[1] / 2, front], cell: c, depth, tex, name });
  const squircle = (u, v, r, n = 6) => Math.abs(u) ** n + Math.abs(v) ** n <= r ** n;
  const inDisc = (u, v, cx, cy, r) => Math.hypot(u - cx, v - cy) <= r;
  const cranium = (u, v) => (u / 4.3) ** 2 + ((v - 0.6) / 3.5) ** 2 <= 1;
  const jaw = (u, v) => v <= 0.4 && v >= -4.4 && Math.abs(u) <= 3.5 - Math.max(0, -1.6 - v) * 0.35;
  const crown = (u, v) => (v >= 3.6 && v <= 4.2 && Math.abs(u) <= 4.1) ||
    [[-3.3, 5.2], [-1.65, 6], [0, 6.6], [1.65, 6], [3.3, 5.2]].some(([bx, top]) => Math.abs(u - bx) <= 0.5 && v >= 4.2 && v <= top);
  const face = (u, v) => cranium(u, v) || jaw(u, v) || crown(u, v);
  const sockets = (u, v) => inDisc(Math.abs(u), v, 1.9, 1, 1.15);
  const nose = (u, v) => v <= -0.5 && v >= -1.9 && Math.abs(u) <= (v + 1.9) * 0.45 + 0.1;
  const mouth = (u, v) => v <= -2.4 && v >= -3.9 && Math.abs(u) <= 2.6 && (Math.abs(v + 3.15) < 0.12 || Math.abs((Math.abs(u) % 0.9) - 0.45) < 0.1 || Math.abs(u) > 2.4);
  const studs = [[6.1, 6.1], [-6.1, 6.1], [6.1, -6.1], [-6.1, -6.1], [0, 7.2], [0, -7.2], [7.2, 0], [-7.2, 0]];
  const plaque = [
    part("plate", "field", 1.5, 1.5, (u, v) => squircle(u, v, 7.95)),
    part("rim", "gold", 2.3, 0.8, (u, v) => squircle(u, v, 7.95) && !squircle(u, v, 6.6)),
    part("rimin", "gold", 2, 0.5, (u, v) => squircle(u, v, 6.7) && !squircle(u, v, 6)),
    part("skull", "gold", 3, 1.5, face, [10, 14], [8, 8], 0.25),
    part("hollow", "hollow", 3.15, 0.15, (u, v) => sockets(u, v) || nose(u, v) || mouth(u, v), [10, 14], [8, 8], 0.25),
    part("pupil", "field", 3.25, 0.1, (u, v) => inDisc(Math.abs(u), v, 1.9, 1, 0.6), [10, 14], [8, 8], 0.25),
    ...studs.flatMap(([x, y], i) => [part("stud", "gold", 3.1, 0.8, (u, v) => (i < 4 ? Math.abs(u) + Math.abs(v) <= 1.4 : Math.hypot(u, v) <= 1.1), [4, 4], [8 + x, 8 + y])]),
  ];
  const goldmaskElements = [
    box([3, 0, 2.5], [13, 8, 6.1], "stone", { name: "masonry", rot: [0, -7, 0] }),
    box([10, 0, 8], [15.5, 2.2, 13], "stone", { name: "masonry", rot: [0, 21, 3], ground: true }),
    joint([0, 0.5, 9.2], { name: "plaque", rot: [-20, 0, 0], children: plaque }),
  ];

  // ---- Dead eye core: a boxy white body (a cube with its corners stepped off) with one big eye in a bezel, a U-shaped carrying
  // handle round the front and a vent plate on the east side, slumped over and wrecked: the upper east corner is broken away
  // (the dark inside shows), the handle's east half has snapped and hangs, its east post lies on the ground, the lens is dead.
  // Few, big boxes on purpose. Built upright around its centre (eye to the south), then tipped by the joint that holds it.
  const orb = [
    // Body: a 11 unit cube with the edges stepped off by three overlapping slabs.
    box([-5.5, -5.5, -5.5], [5.5, 5.5, 5.5], "shell", { name: "body" }),
    box([-6.5, -4.5, -4.5], [6.5, 4.5, 4.5], "shell", { name: "slabx" }),
    box([-4.5, -6.5, -4.5], [4.5, 6.5, 4.5], "shell", { name: "slaby" }),
    box([-4.5, -4.5, -6.5], [4.5, 4.5, 6.5], "shell", { name: "slabz" }),
    // The break: the upper east corner is gone, the dark inside shows, one slab edge hangs loose.
    box([3.2, 2.4, -3.4], [6.6, 6.6, 3.6], "hollow", { name: "inside" }),
    box([4.4, 2.8, -2.6], [6.9, 3.4, 2.8], "shell", { name: "flap", rot: [0, 0, 38], origin: [4.4, 2.8, 0] }),
    // The eye: an octagonal bezel (four bars and four corners), a dead lens, a lid plate over the top of it and one under it.
    box([-3.5, 2.6, 6.1], [3.5, 3.8, 7.3], "shell", { name: "bezeltop" }),
    box([-3.5, -3.8, 6.1], [3.5, -2.6, 7.3], "shell", { name: "bezelbottom" }),
    box([-3.8, -3.5, 6.1], [-2.6, 3.5, 7.3], "shell", { name: "bezelwest" }),
    box([2.6, -3.5, 6.1], [3.8, 3.5, 7.3], "shell", { name: "bezeleast" }),
    ...[[-3, 3], [3, 3], [-3, -3], [3, -3]].map(([x, y]) => box([x - 0.9, y - 0.9, 6.1], [x + 0.9, y + 0.9, 7.3], "shell", { name: "bezelcorner", rot: [0, 0, 45] })),
    box([-3, -3, 6.2], [3, 3, 6.7], "panel", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
    box([-2.6, 1.2, 6.7], [2.6, 2.3, 7.1], "shell", { name: "lidtop", rot: [0, 0, -3] }),
    box([-2.6, -2.5, 6.7], [2.6, -1.8, 7.1], "shell", { name: "lidbottom" }),
    // East side: the vent plate with its ribs, sprung loose at the top.
    box([6.5, -3.6, -3.6], [7.1, 3.6, 3.6], "panel", { name: "vent", rot: [0, 0, 0] }),
    ...[-2.4, -0.8, 0.8, 2.4].map((z) => box([7.1, -3.2, z - 0.2], [7.5, 3.2, z + 0.2], "panel", { name: "ventrib" })),
    // West side: the small knob.
    box([-7.6, -1, -1], [-6.5, 1, 1], "panel", { name: "knob" }),
    // The handle: a bar over the front and a post down each side. The east half of the bar snapped and hangs from its middle,
    // the west post is bent at the knee, the east post has gone (see below), a stub of the lower bar is left.
    box([-7.2, 6.4, 5], [1.2, 7.8, 6.4], "panel", { name: "handlebar" }),
    box([1, 6.4, 5], [7.6, 7.8, 6.4], "panel", { name: "handlebarbroken", rot: [0, 0, -58], origin: [1, 7.1, 5.7] }),
    box([-7.8, 0.6, 5], [-6.4, 7.8, 6.4], "panel", { name: "postwestupper" }),
    box([-7.8, -5, 5], [-6.4, 0.8, 6.4], "panel", { name: "postwestlower", rot: [0, 0, -16], origin: [-7.1, 0.8, 5.7] }),
    box([-7.6, -6.4, 5], [-3.8, -5, 6.4], "panel", { name: "lowerbarstub", rot: [0, 0, 0] }),
    box([6.4, -1.2, 5], [7.8, 7.4, 6.4], "panel", { name: "posteaststub", rot: [0, 0, 24], origin: [7.1, 7.4, 5.7] }),
    // Small grey bits on top and below, as on the body.
    box([-1.6, 5.5, -0.8], [1.6, 7.2, 0.8], "panel", { name: "topbit" }),
    box([-1.6, -7.2, -0.8], [1.6, -5.5, 0.8], "panel", { name: "bottombit" }),
  ];
  const eyecoreElements = [
    joint([8, 7, 8], { name: "core", rot: [0, 18, -9], children: orb }),
    // On the ground: the east post with its foot, a chunk of shell, a snapped bit of the bar.
    box([9.5, 0, 14], [16, 1.4, 15.4], "panel", { name: "fallenpost", ground: true, rot: [0, -22, 0] }),
    box([13, 0, 11], [16, 2.2, 14], "shell", { name: "chunk", ground: true, rot: [0, 25, 6] }),
    box([12.5, 0, 3.5], [16, 1.4, 4.9], "panel", { name: "fallenbar", ground: true, rot: [0, -20, 0] }),
  ];

  // ---- Broken rift gun: a white handheld emitter with a barrel, three prongs round a lens, a grip and a rear block, lying on its
  // side: shell chipped, a hole in the top shell with the dark inside, the grip snapped off (it lies nearby), one prong broken
  // short, one bent, the lens dead and a cable hanging. Built upright (barrel to the south, along z) then laid over.
  const gun = [
    box([-2.5, 0, -8], [2.5, 5, 5], "shell", { name: "body" }),
    box([-2, 5, -6], [2, 6.2, 2], "shell", { name: "top" }),
    box([-1.7, 5, 2], [1.7, 6.2, 5], "hollow", { name: "gap" }),       // the top shell is cracked away here
    box([-2, 0.5, -10.5], [2, 4.5, -8], "panel", { name: "rear" }),
    box([-1.2, -3, -3], [1.2, 0, -0.5], "shell", { name: "gripstub", rot: [0, 0, 0] }),
    box([-1.6, 1, 5], [1.6, 4, 9], "shell", { name: "barrel" }),
    box([-2.3, 0.4, 9], [2.3, 4.6, 10.2], "panel", { name: "ring" }),
    box([-1.2, 1.6, 10.2], [1.2, 3.8, 11.4], "panel", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
    // Three prongs round the lens: the top one bent down, the east one broken to a stub, the west one whole.
    box([-2.9, 1.2, 10.2], [-1.7, 3.8, 15.5], "shell", { name: "prongwest", rot: [0, 8, 0], origin: [-2.3, 2.5, 10.2] }),
    box([1.7, 1.2, 10.2], [2.9, 3.8, 12.6], "shell", { name: "prongeast" }),
    box([-0.7, 4.4, 10.2], [0.7, 5.6, 14.6], "shell", { name: "prongtop", rot: [18, 0, 0], origin: [0, 5, 10.2] }),
    // Side: a vent panel with slits, sprung loose; the dark band round the body.
    box([2.5, 0.8, -6], [3.2, 4.2, -0.5], "panel", { name: "vent", rot: [0, 0, 28], origin: [2.5, 4.2, -3] }),
    box([-2.6, 2, -7.5], [2.6, 3, -4], "panel", { name: "band" }),
    // The cable out of the back: two bent lengths.
    box([-0.4, 0.8, -15], [0.4, 1.6, -10.5], "panel", { name: "cable" }),
    box([-0.4, 0.8, -15.2], [0.4, 3.8, -14.4], "panel", { name: "cablebend", rot: [0, 0, 0] }),
  ];
  const gunElements = [
    joint([6, 3, 3.5], { name: "gun", ground: true, rot: [0, 24, 0], children: gun }),
    // The grip, snapped off, lying apart; a chip of shell.
    box([11, 0, 8], [13.4, 3, 14], "shell", { name: "grip", ground: true, rot: [0, -30, 0] }),
    box([12, 0, 3], [14.5, 1.8, 5.5], "shell", { name: "chip", ground: true, rot: [0, 40, 8] }),
  ];

  // ---- Spring-heeled boots: a pair of black leather boots with long spring-loaded steel heels (a piston behind each heel, a coil
  // round it, a heel plate under it). One stands, cracked; the other has fallen on its side with its piston bent.
  const boot = (broken) => [
    box([0, 0, 0], [4.5, 1, 11], "leather", { name: "sole" }),
    box([0.3, 1, 1.5], [4.2, 4, 11], "leather", { name: "foot" }),
    box([0.6, 1, 11], [3.9, 3.2, 12.4], "leather", { name: "toecap" }),
    box([0.3, 4, 1.5], [4.2, 13, 6.5], "leather", { name: "shaft", faces: { up: { texture: "hollow", uv: "full" } } }),
    box([0, 12, 1.2], [4.5, 13.6, 6.8], "leather", { name: "cuff" }),
    box([0, 7, 1.2], [4.5, 8.2, 6.8], "leather", { name: "strap" }),
    box([4.2, 6.6, 3], [4.7, 8.6, 5], "steel", { name: "buckle" }),
    // The heel: a piston behind it, a coil of five rings round it, a plate under it.
    box([1.3, 2, -1.8], [3.2, 12, 0.1], "steel", { name: "piston", ...(broken ? { rot: [0, 0, 0] } : {}) }),
    ...[3.2, 5.2, 7.2, 9.2].map((y) => box([0.7, y, -2.4], [3.8, y + 0.8, 0.7], "steel", { name: "coil" })),
    box([0.6, 0, -4.2], [3.9, 1, -1], "steel", { name: "heelplate" }),
  ];
  const bootsElements = [
    joint([0.5, 0, 3.5], { name: "bootstanding", children: boot(false) }),
    // The fallen boot on its side, its piston snapped halfway and the top half bent away.
    joint([20.5, 0, 3.5], { name: "bootfallen", ground: true, rot: [0, 0, 90], children: boot(true).filter((e) => e.name !== "piston").concat([
      box([1.3, 2, -1.8], [3.2, 6.5, 0.1], "steel", { name: "pistonlower" }),
      box([1.3, 6.5, -1.8], [3.2, 12, 0.1], "steel", { name: "pistonupper", rot: [0, 0, -40], origin: [2.2, 6.5, -0.9] }),
    ]) }),
  ];

  // ---- Brass rune scroll: a long brass scroll case lying on two stone cradles: a plain tube with a hinged front plate, a ribbed
  // cuff and a diamond-lattice cuff at each end with a violet gem, then a bicone, a spiral-bound rod and a spiked finial. The
  // same in both halves (`half` draws one, `side` is -1 west / 1 east). Tubes are two overlapping squares (an octagon). Along x.
  const Y0 = 4.7, ZC = 8, CX = 8;
  const tube = (side, d0, d1, r, tex, name) => {
    const x0 = CX + side * d0, x1 = CX + side * d1, a = 0.72 * r, lo = Math.min(x0, x1), hi = Math.max(x0, x1);
    return [
      box([lo, Y0 - a, ZC - a], [hi, Y0 + a, ZC + a], tex, { name }),
      box([lo, Y0 - a, ZC - a], [hi, Y0 + a, ZC + a], tex, { name, rot: [45, 0, 0], origin: [(lo + hi) / 2, Y0, ZC] }),
    ];
  };
  const half = (side) => {
    const gem = (z) => box([CX + side * 4.2 - 0.5, Y0 - 0.2, ZC + z - 0.2], [CX + side * 4.2 + 0.5, Y0 + 1.1, ZC + z + 0.2], "gem", { name: "gem", rot: [0, 0, 45] });
    return [
      ...tube(side, 0, 3.6, 2.3, "brass", "tube"),
      ...tube(side, 3.5, 5.2, 2.9, "brass", "runeband"),
      gem(2.7), gem(-2.7),
      ...tube(side, 5.2, 7.0, 3.0, "lattice", "latticeband"),
      ...tube(side, 7.0, 7.8, 2.3, "brass", "neck"),
      ...tube(side, 7.8, 8.3, 1.3, "brass", "bicone"),
      ...tube(side, 8.3, 8.8, 1.9, "brass", "bicone"),
      ...tube(side, 8.8, 9.3, 1.3, "brass", "bicone"),
      ...tube(side, 9.3, 11.5, 0.8, "wood", "rod"),
      ...tube(side, 9.8, 10.1, 1.0, "brass", "wire"), ...tube(side, 10.6, 10.9, 1.0, "brass", "wire"),
      ...tube(side, 11.5, 12.3, 1.5, "brass", "finialbase"),
      ...tube(side, 12.3, 13.1, 1.1, "brass", "finialcap"),
      ...tube(side, 13.1, 14, 0.45, "brass", "spike"),
    ];
  };
  const scroll = [
    ...half(-1), ...half(1),
    box([CX - 3.4, Y0 - 1.5, ZC + 2.3], [CX + 3.4, Y0 + 1.2, ZC + 2.6], "plate", { name: "frontplate" }),
    box([CX - 3.4, Y0 + 1.0, ZC + 2.2], [CX + 3.4, Y0 + 1.5, ZC + 2.8], "brass", { name: "hinge" }),
  ];
  const brassscrollElements = [
    box([CX - 7.8, 0, ZC - 3.2], [CX - 5.2, 2.4, ZC + 3.2], "stone", { name: "cradle", rot: [0, 6, 0] }),
    box([CX + 5.2, 0, ZC - 3.2], [CX + 7.8, 2.4, ZC + 3.2], "stone", { name: "cradle", rot: [0, -5, 0] }),
    joint([0, 0, 0], { name: "scrollcase", rot: [0, 10, 0], origin: [8, 0, 8], children: scroll }),
  ];

  // ---- Bronze colossus: a giant bronze automaton caught mid-stride, crouched: broad pauldrons, a crested helmet with a long back
  // sweep, a meander-trimmed chest, a spiked fist on one arm and a launcher on the other, wheel discs on the shoulder, arm and
  // knees. Dormant for ages: verdigris. Big plates, few boxes. Posed by joints around the pelvis; the model is
  // set on the ground (`ground`). Positive x rotation tips something forward (south), a hanging limb swings south with negative.
  const wheel = (x, y, z, s) => box([x, y - 2.4, z - 2.4], [x + s * 1.1, y + 2.4, z + 2.4], "bronze", { name: "wheel", faces: { [s > 0 ? "east" : "west"]: { texture: "wheel", uv: "full" } } });
  const spike = (len, w) => [
    box([-w, -len * 0.5, -w], [w, 0, w], "bronze", { name: "spike" }),
    box([-w * 0.55, -len * 0.8, -w * 0.55], [w * 0.55, -len * 0.5, w * 0.55], "bronze", { name: "spike" }),
    box([-w * 0.25, -len, -w * 0.25], [w * 0.25, -len * 0.8, w * 0.25], "bright", { name: "spike" }),
  ];
  const pauldron = (s) => joint([s * 6, 10, 0], { name: "pauldron", rot: [0, 0, s * 16], children: [
    box([s > 0 ? -0.5 : -6.5, -1.2, -3.9], [s > 0 ? 6.5 : 0.5, 1.6, 3.9], "bronze", { name: "plate" }),
    box([s > 0 ? 0 : -6, 1.6, -2.8], [s > 0 ? 6 : 0, 2.6, 2.8], "bronze", { name: "plateridge", faces: { south: { texture: "key", uv: "full" } } }),
    box([s > 0 ? 5.5 : -6.5, -2.4, -3.7], [s > 0 ? 6.5 : -5.5, 1.2, 3.7], "bright", { name: "edge" }),
  ] });
  const biglegs = (s, hip, knee) => joint([s * 3.4, -1.5, 0], { name: "leg", rot: [hip, 0, s * 3], children: [
    box([-2.5, -8, -2.5], [2.5, 0, 2.5], "bronze", { name: "thigh" }),
    box([-2.6, -4.2, 2.4], [2.6, -3, 2.9], "key", { name: "thighband" }),
    wheel(s > 0 ? 2.5 : -3.6, -8, 0, s),
    joint([0, -8, 0], { name: "knee", rot: [knee, 0, 0], children: [
      box([-2.2, -9, -2.2], [2.2, 0, 2.2], "bronze", { name: "shin" }),
      box([-2.4, -6, 2.1], [2.4, -4.8, 2.6], "key", { name: "shinband" }),
      box([-2.6, -9.6, -2.6], [2.6, -8.4, 2.6], "bright", { name: "ankle" }),
      box([-2.5, -11.2, -2.5], [2.5, -9.6, 4.5], "bronze", { name: "foot" }),
      box([-1.4, -11.2, 4.5], [1.4, -10.4, 6], "bright", { name: "toe" }),
    ] }),
  ] });
  const head = joint([0, 11.5, 0], { name: "head", rot: [-24, 0, 0], children: [
    box([-2.6, 0, -2.6], [2.6, 6, 2.6], "bronze", { name: "helmet" }),
    box([-1.7, 0.6, 2.6], [1.7, 4.6, 3.2], "bright", { name: "face" }),
    box([-1.5, 3, 3.2], [1.5, 3.7, 3.3], "hollow", { name: "eyes" }),
    box([-2.8, 1.5, -1.5], [-2.6, 5, 1.5], "key", { name: "cheekwest", faces: { west: { texture: "key", uv: "full" } } }),
    box([2.6, 1.5, -1.5], [2.8, 5, 1.5], "key", { name: "cheekeast", faces: { east: { texture: "key", uv: "full" } } }),
    // The crest: a long sweep behind, an arch over the top and a hook in front.
    box([-0.5, 5.2, -7.5], [0.5, 6.6, -2], "bright", { name: "crestback", rot: [-22, 0, 0], origin: [0, 6, -2] }),
    box([-0.5, 5.8, -2.6], [0.5, 7.4, 3.2], "bright", { name: "crestarch" }),
    box([-0.5, 4.6, 3], [0.5, 7.4, 4.2], "bright", { name: "cresthook" }),
  ] });
  const forearmSpiked = joint([0, -8, 0], { name: "forearm", rot: [-38, 0, 0], children: [
    box([-2.1, -9, -2.1], [2.1, 0, 2.1], "bronze", { name: "forearm" }),
    box([-2.2, -5.5, 2], [2.2, -4.3, 2.4], "key", { name: "forearmband" }),
    wheel(2.1, -3.5, 0, 1),
    box([-2.5, -10.6, -2.5], [2.5, -8.8, 2.5], "bright", { name: "gauntlet" }),
    joint([0, -10.6, 0], { name: "fistspike", children: spike(6, 1.3) }),
  ] });
  const launcher = joint([0, -8, 0], { name: "forearm", rot: [-55, 0, 0], children: [
    box([-2, -8, -2], [2, 0, 2], "bronze", { name: "forearm" }),
    box([-2.1, -5, 1.9], [2.1, -3.9, 2.3], "key", { name: "forearmband" }),
    wheel(-3.2, -3, 0, -1),
    box([-1, -9, -3], [1, -7.4, 6], "dark", { name: "launcherbed" }),
    box([-3.4, -8.6, 3], [3.4, -7.8, 4], "bright", { name: "launcherbow", rot: [0, 0, 0] }),
    box([-3.4, -9.6, 5], [-2.6, -6.6, 6.6], "bronze", { name: "launcherclawwest" }),
    box([2.6, -9.6, 5], [3.4, -6.6, 6.6], "bronze", { name: "launcherclaweast" }),
  ] });
  const colossus = joint([8, 15, 8], { name: "pelvis", rot: [0, 18, 0], children: [
    box([-4.8, -2, -3.2], [4.8, 2, 3.2], "bronze", { name: "pelvis" }),
    box([-4.9, -1, 3.2], [4.9, 1, 3.6], "key", { name: "belt" }),
    joint([0, 2, 0], { name: "torso", rot: [26, 0, 0], children: [
      box([-4.6, 0, -3.6], [4.6, 5, 3.6], "bronze", { name: "waist" }),
      box([-6, 5, -4.2], [6, 10, 4.2], "bronze", { name: "chest" }),
      box([-3.6, 0.5, 3.6], [3.6, 5, 4.5], "bright", { name: "chestplate" }),
      box([-6.1, 7, 4.2], [6.1, 8.4, 4.6], "key", { name: "chestband" }),
      box([-1.1, 5.6, 4.5], [1.1, 7.6, 5.1], "bright", { name: "boss" }),
      box([-3, 9, -4.6], [3, 11, -3.4], "dark", { name: "backvent" }),
      box([-1.6, 10, -1.6], [1.6, 11.6, 1.6], "dark", { name: "neck" }),
      pauldron(1), pauldron(-1), head,
      joint([7.2, 9, 0], { name: "armeast", rot: [-52, 0, -8], children: [
        box([-1.8, -8, -1.8], [1.8, 0, 1.8], "bronze", { name: "upperarm" }),
        box([-2.2, -8.6, -2.2], [2.2, -7.4, 2.2], "bright", { name: "elbow" }),
        forearmSpiked,
      ] }),
      joint([-7.2, 9, 0], { name: "armwest", rot: [-14, 0, 12], children: [
        box([-1.8, -8, -1.8], [1.8, 0, 1.8], "bronze", { name: "upperarm" }),
        box([-2.2, -8.6, -2.2], [2.2, -7.4, 2.2], "bright", { name: "elbow" }),
        launcher,
      ] }),
    ] }),
    biglegs(1, -64, 78),
    biglegs(-1, 18, 38),
  ] });
  const colossusElements = [
    colossus,
    // On the ground: a spike broken off a fist.
    joint([2, 0.7, 12.5], { name: "brokenspike", ground: true, rot: [0, 40, 90], children: spike(5, 1.1) }),
  ];

  // ---- Dead artificer: a gaunt robed machine-priest sitting slumped, its head bowed a little inside an open cowl: a metal face mask
  // with one dead red lens, an empty socket and a breathing grille. A jointed drill arm on its back has collapsed to the floor;
  // hoses hang slack from mask and back; a dark readout screen on its chest; a lantern and a skull on chains in its lap; a bundle
  // of wires with nozzles at one sleeve, a lamp box at the hip. The robe is faded and stained, close over a thin frame. Built facing
  // south, the back to the north. Boxes only, big and few.
  const sleeve = (s) => joint([s * 4.4, 12.4, 0.2], { name: "arm", rot: [s > 0 ? -34 : -58, 0, s * 10], children: [
    box([-1.5, -7, -1.5], [1.5, 0, 1.5], "robe", { name: "sleeve" }),
    box([-1.9, -9, -1.9], [1.9, -6.5, 1.9], "robe", { name: "cuff" }),
    ...[-0.6, 0, 0.6].map((x) => box([x - 0.22, -11.6, -0.25], [x + 0.22, -9, 0.25], "bone", { name: "finger" })),
    box([-0.9, -9.6, -0.5], [0.9, -9, 0.5], "bone", { name: "hand" }),
  ] });
  const drillArm = joint([2.4, 10.5, -4], { name: "mount", children: [
    box([-2, -2.6, -1.2], [2, 2.6, 1.2], "iron", { name: "mount" }),
    // The arm swung out over the east side and collapsed: upper segment out and down, the forearm lying on the floor, the drill on it.
    joint([0, 2, 0], { name: "shoulderjoint", rot: [0, 0, -78], children: [
      box([-1.2, 0, -1.2], [1.2, 9, 1.2], "iron", { name: "upperarm" }),
      box([-2.1, 3.6, -0.8], [-1.2, 7.6, 0.8], "brass", { name: "armplate" }),
      joint([0, 9, 0], { name: "elbowjoint", rot: [0, 0, -88], children: [
        box([-1.5, -1.5, -1.5], [1.5, 1.5, 1.5], "brass", { name: "elbow" }),
        box([-1, 0, -1], [1, 8.5, 1], "iron", { name: "forearm" }),
        box([-1.6, 8.5, -1.6], [1.6, 10.6, 1.6], "brass", { name: "chuck" }),
        box([-0.9, 10.6, -0.9], [0.9, 12.4, 0.9], "iron", { name: "drill" }),
        box([-0.45, 12.4, -0.45], [0.45, 14.2, 0.45], "iron", { name: "drilltip" }),
      ] }),
    ] }),
  ] });
  // The head: an open cowl (top, sides, back) round a metal face mask that shows from the front.
  const hoodHead = joint([0, 13.4, 0], { name: "head", rot: [16, 0, 0], children: [
    box([-2.7, 4, -2.7], [2.7, 5.1, 1.9], "robe", { name: "cowltop" }),
    box([-2.7, -0.4, -2.7], [-1.9, 4, 0.6], "robe", { name: "cowlwest" }),
    box([1.9, -0.4, -2.7], [2.7, 4, 0.6], "robe", { name: "cowleast" }),
    box([-1.9, -0.4, -2.7], [1.9, 4, -1.7], "robe", { name: "cowlback" }),
    box([-1.9, 0.2, -1.7], [1.9, 4, 1.3], "mask", { name: "mask" }),
    box([-1.5, 2.3, 1.3], [-0.3, 3.5, 1.8], "lens", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
    box([0.4, 2.5, 1.3], [1.3, 3.3, 1.4], "hollow", { name: "socket" }),
    box([-1.1, 0.5, 1.3], [1.1, 1.8, 2], "brass", { name: "grille" }),
    box([-0.4, -3.2, 1.4], [0.4, 0.6, 2.2], "hose", { name: "maskhose", rot: [-10, 0, 0], origin: [0, 0.6, 1.8] }),
  ] });
  const artificer = joint([8, 0, 5], { name: "artificer", rot: [0, 12, 0], children: [
    box([-5.4, 0, -3.5], [5.4, 2, 7], "robe", { name: "robepool" }),
    box([-4.4, 2, -3], [4.4, 4.5, 3.4], "robe", { name: "robelap" }),
    box([-3.4, 4.5, -3], [3.4, 12.6, 2.2], "robe", { name: "robebody", rot: [8, 0, 0], origin: [0, 4.5, 0] }),
    box([-4.6, 11.2, -3], [4.6, 13.6, 2], "robe", { name: "shoulders", rot: [8, 0, 0], origin: [0, 4.5, 0] }),
    box([-3.4, 0, 7], [-1.2, 1.2, 8.4], "iron", { name: "boot" }),
    box([1.2, 0, 7], [3.4, 1.2, 8.4], "iron", { name: "boot" }),
    hoodHead, sleeve(1), sleeve(-1), drillArm,
    // Chest: the readout screen and its cabinet, dark.
    box([-1.6, 7.6, 2.1], [1.6, 10.6, 3.1], "iron", { name: "cabinet", rot: [8, 0, 0], origin: [0, 4.5, 0] }),
    box([-1.1, 8.4, 3.1], [1.1, 10, 3.3], "screen", { name: "screen", rot: [8, 0, 0], origin: [0, 4.5, 0], faces: { south: { texture: "screen", uv: "full" } } }),
    // Chains and what hangs on them, in the lap: the lantern (brass frame, grey glass) and the skull.
    box([-0.25, 6, 3.2], [0.25, 8, 3.6], "brass", { name: "chain" }),
    box([-1, 2, 4], [1, 2.6, 6], "brass", { name: "lanternbase" }),
    box([-0.8, 2.6, 4.2], [0.8, 5.6, 5.8], "glass", { name: "lanternglass" }),
    box([-1, 5.6, 4], [1, 6.2, 6], "brass", { name: "lanterncap" }),
    box([-3.8, 2, 4], [-1.7, 4.2, 6.1], "bone", { name: "skull" }),
    box([-3.5, 3.1, 6.1], [-2.9, 3.7, 6.2], "hollow", { name: "skulleye" }),
    box([-2.6, 3.1, 6.1], [-2, 3.7, 6.2], "hollow", { name: "skulleye" }),
    // The lamp box at the hip, and the bundle of wires with nozzles hanging from the east sleeve.
    box([-6.8, 2, -0.5], [-5, 6, 2], "brass", { name: "lampbox" }),
    box([-6.6, 4.6, 2], [-5.8, 5.4, 2.2], "hollow", { name: "lamp" }),
    ...[0, 1, 2, 3].map((i) => box([5.4 + i * 0.45, 0, 3 + (i % 2) * 0.6], [5.75 + i * 0.45, 3.6, 3.35 + (i % 2) * 0.6], "hose", { name: "wire" })),
    ...[0, 1, 2, 3].map((i) => box([5.3 + i * 0.45, -0.3, 2.9 + (i % 2) * 0.6], [5.85 + i * 0.45, 0.4, 3.45 + (i % 2) * 0.6], "brass", { name: "nozzle" })),
    // Hoses: down the chest and back, slack on the floor.
    box([2.4, 3, 2.3], [3.2, 9, 3.1], "hose", { name: "chesthose", rot: [0, 0, -8], origin: [2.8, 9, 2.7] }),
    box([-3.9, 4, -3.8], [-3.1, 11, -3], "hose", { name: "backhose" }),
    box([-3.5, 0, -8], [-2.6, 0.9, -3.4], "hose", { name: "floorhose" }),
    box([-7, 0, -8.4], [-3, 0.9, -7.5], "hose", { name: "floorhose" }),
    box([3, 0, 7], [3.9, 0.9, 12], "hose", { name: "floorhose" }),
    box([3.9, 0, 11.2], [8.5, 0.9, 12.1], "hose", { name: "floorhose" }),
  ] });
  const artificerElements = [
    artificer,
  ];

  // ---- Dead skull drone: a human skull made into a little flying servant, fallen and dead: one bionic lens (dark) in the east
  // socket, a riveted plate on the cranium (cracked), a hover unit under the jaw (dented), a jointed tool arm (bent), a vane on
  // each side (the west one snapped off and lying beside it), cables out of the back. Built upright around the skull (face to
  // the south), then tipped onto its west side by the joint that holds it.
  const skull = [
    box([-3, 0, -3.4], [3, 5, 2.6], "bone", { name: "cranium" }),
    box([-2.4, -1.6, 0.6], [2.4, 1, 3.2], "bone", { name: "face" }),
    box([-1.9, -3, -0.4], [1.9, -1.6, 2.8], "bone", { name: "jaw", rot: [8, 0, 0], origin: [0, -1.6, -0.4] }),
    box([-1.7, -1.5, 2.8], [1.7, -1.1, 3.3], "hollow", { name: "teethgap" }),
    box([-2, 1.4, 3.1], [-0.6, 2.8, 3.25], "hollow", { name: "socket" }),
    box([-0.35, 0.2, 3.1], [0.35, 1, 3.25], "hollow", { name: "nose" }),
    box([0.3, 1.1, 2.4], [2.3, 3.1, 4.2], "iron", { name: "bioniceye" }),
    box([0.6, 1.4, 4.2], [2, 2.8, 4.4], "lens", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
    box([-1.6, 4.8, -2.2], [1.9, 5.6, 1.4], "iron", { name: "plate" }),
    box([-0.2, 4.9, -2.2], [0.2, 5.7, 1.4], "hollow", { name: "platecrack", rot: [0, 22, 0], origin: [0, 5.3, -0.4] }),
    // The hover unit under the jaw, dented on one side.
    box([-2.2, -5.6, -2.4], [2.2, -3.2, 1.8], "brass", { name: "hover" }),
    box([-2.6, -4.8, -2.8], [2.6, -4, 2.2], "iron", { name: "hoverring" }),
    box([-0.9, -6.4, -1.1], [0.9, -5.6, 0.5], "iron", { name: "hovernozzle" }),
    box([1.6, -5.4, 1.6], [2.4, -3.6, 2.2], "hollow", { name: "dent" }),
    // The tool arm from the east cheek: two segments, bent wrong, a two-finger claw.
    joint([2.6, -1.4, 0.8], { name: "toolarm", rot: [0, 0, 55], children: [
      box([-0.35, -3.6, -0.35], [0.35, 0, 0.35], "iron", { name: "toolupper" }),
      joint([0, -3.6, 0], { name: "toolelbow", rot: [70, 0, -30], children: [
        box([-0.3, -3, -0.3], [0.3, 0, 0.3], "iron", { name: "toolfore" }),
        box([-0.6, -3.9, -0.15], [-0.2, -3, 0.15], "brass", { name: "claw" }),
        box([0.2, -3.9, -0.15], [0.6, -3, 0.15], "brass", { name: "claw" }),
      ] }),
    ] }),
    // The east vane, and the stub where the west one snapped.
    box([3, 1, -2.6], [3.4, 4.6, 1], "iron", { name: "vaneeast", rot: [0, 0, -14], origin: [3, 2.8, 0] }),
    box([-3.4, 2.2, -1.4], [-3, 3.4, -0.2], "iron", { name: "vanestub" }),
    // Cables out of the back of the skull.
    box([-0.4, -1, -6.4], [0.4, -0.2, -3.2], "hose", { name: "cable" }),
    box([0.8, 0.6, -5.4], [1.5, 1.3, -3.2], "hose", { name: "cable" }),
  ];
  const skulldroneElements = [
    joint([8, 4, 8], { name: "drone", ground: true, rot: [0, 28, 72], children: skull }),
    // The snapped west vane and a loose cable end.
    box([2.6, 0, 9.4], [6.2, 0.4, 11.4], "iron", { name: "fallenvane", ground: true, rot: [0, -24, 0] }),
    box([10.5, 0, 3], [11.3, 0.8, 6.6], "hose", { name: "loosecable", ground: true, rot: [0, 30, 0] }),
  ];

  // ---- Withered throne: a vast gilt throne on a dais with a row of dials, and on it a withered king long dead: a bare skull with
  // one red lens and wisps of white hair, ribs showing through a rotted robe, arms laid along the armrests with bony hands over
  // the ends, knees apart. A hose bundle plugs his skull into the backrest; more hoses run round the throne (top edge, sides, loops) with red capsules; a skull sits on the west
  // armrest. Built facing south, the back to the north.
  // The life-support cables: each runs from a brass port on the backrest, sagging, into the back of his skull. A cable is a few
  // straight bars along a curve in the plane of the backrest (from the front they read as curves); some carry a red capsule.
  const seg2d = (name, p, q, t, z0, z1, tex) => {
    const cx = (p[0] + q[0]) / 2, cy = (p[1] + q[1]) / 2, len = Math.hypot(q[0] - p[0], q[1] - p[1]) + t * 0.6;
    const deg = -(Math.atan2(q[0] - p[0], q[1] - p[1]) * 180) / Math.PI;
    return box([cx - t / 2, cy - len / 2, z0], [cx + t / 2, cy + len / 2, z1], tex, { name, rot: [0, 0, Math.round(deg * 10) / 10], origin: [cx, cy, (z0 + z1) / 2] });
  };
  const lifeCable = (deg, r, capsule) => {
    const a = (deg * Math.PI) / 180;
    const port = [8 + r * Math.sin(a), 17.8 + r * Math.cos(a)];
    const end = [8 + 1.6 * Math.sin(a), 16.6 + 1.8 * Math.cos(a)];
    const sag = Math.abs(deg) < 10 ? [1.8, -0.8] : [0, -3.2];
    const ctrl = [(port[0] + end[0]) / 2 + sag[0], (port[1] + end[1]) / 2 + sag[1]];
    const at = (t) => [0, 1].map((k) => (1 - t) ** 2 * port[k] + 2 * (1 - t) * t * ctrl[k] + t * t * end[k]);
    const pts = [0, 0.3, 0.6, 1].map(at);
    const mid = at(0.45);
    return [
      box([port[0] - 0.7, port[1] - 0.7, 0], [port[0] + 0.7, port[1] + 0.7, 0.9], "giltdark", { name: "port" }),
      ...pts.slice(1).map((q, i) => seg2d("cable", pts[i], q, 1.2, 0.3, 1.5, "hose")),
      ...(capsule ? [box([mid[0] - 0.8, mid[1] - 1.1, 0.2], [mid[0] + 0.8, mid[1] + 1.1, 1.6], "capsule", { name: "capsule" })] : []),
    ];
  };
  const hair = (deg, len) => joint([8, 17.6, 3.4], { name: "hair", rot: [-10, 0, deg], children: [
    box([-0.15, 0, -0.15], [0.15, len, 0.15], "hair", { name: "hair" }),
  ] });
  // A forearm lying along an armrest (centred on x = cx), the fingers over its front end.
  const forearm = (cx) => [
    box([cx - 0.5, 13, 4.4], [cx + 0.5, 14, 9.4], "bone", { name: "forearm" }),
    ...[-0.6, 0, 0.6].map((dx) => box([cx + dx - 0.18, 11.4, 9.5], [cx + dx + 0.18, 14, 9.9], "bone", { name: "finger" })),
  ];
  // An upper arm from the shoulder (sx) straight to the elbow at the back of the armrest (ex, 4.9).
  const upperArm = (sx, ex) => {
    const dx = ex - sx, dz = 4.9 - 3, len = Math.hypot(dx, dz), deg = (Math.atan2(dz, Math.abs(dx)) * 180) / Math.PI;
    return joint([sx, 13.5, 3], { name: "upperarm", rot: [0, dx < 0 ? deg : -deg, 0], children: [
      box(dx < 0 ? [-len - 0.3, -0.5, -0.5] : [0, -0.5, -0.5], dx < 0 ? [0, 0.5, 0.5] : [len + 0.3, 0.5, 0.5], "bone", { name: "upperarm" }),
    ] });
  };
  const throneElements = [
    // Dais with a row of dials along its front.
    box([-1.5, 0, -3.5], [17.5, 2, 12], "giltdark", { name: "dais" }),
    ...[0.5, 3.5, 6.5, 9.5, 12.5, 15.5].map((x) => box([x - 0.9, 0.4, 12], [x + 0.9, 1.6, 12.3], "giltworn", { name: "dial" })),
    ...[0.5, 3.5, 6.5, 9.5, 12.5, 15.5].map((x) => box([x - 0.45, 0.7, 12.3], [x + 0.45, 1.3, 12.4], "hollow", { name: "dialface" })),
    // The throne: seat, a tall back with a stepped crest, two great armrest blocks hung with rotten cloth.
    box([3, 2, 0], [13, 7, 8.4], "gilt", { name: "seat" }),
    box([0, 2, -3], [16, 25, 0], "gilt", { name: "back" }),
    box([3, 25, -3], [13, 28, -0.5], "gilt", { name: "crest" }),
    box([5.5, 28, -2.6], [10.5, 30.5, -1], "giltworn", { name: "crestcap" }),
    box([1.5, 7, -0.3], [14.5, 24, 0.1], "giltdark", { name: "backpanel" }),
    box([0, 2, 0], [3, 13, 9.5], "gilt", { name: "armwest" }),
    box([13, 2, 0], [16, 13, 9.5], "gilt", { name: "armeast" }),
    box([0.3, 4, 9.5], [2.7, 12, 9.9], "cloth", { name: "drapewest" }),
    box([13.3, 4, 9.5], [15.7, 12, 9.9], "cloth", { name: "drapeeast" }),
    // The fan of cables behind his head, some with red capsules on them.
    // The hoses. A short bundle plugs the back of his skull into the backrest; the rest run round the throne: along the top
    // edge, down both sides of the backrest to the dais, and in loose loops sagging out beside it, red capsules on them.
    box([7, 16.2, 0], [9, 18, 1.4], "hose", { name: "headbundle" }),
    box([6.4, 17.6, 0], [7.2, 19.2, 1.2], "hose", { name: "headhose", rot: [0, 0, 30], origin: [6.8, 17.6, 0.6] }),
    box([8.8, 17.6, 0], [9.6, 19.2, 1.2], "hose", { name: "headhose", rot: [0, 0, -30], origin: [9.2, 17.6, 0.6] }),
    box([0, 24, 0], [16, 25.2, 1.2], "hose", { name: "tophose" }),
    ...[0.8, 15.2].map((x) => box([x - 0.7, 23.9, 1.2], [x + 0.7, 25.3, 1.5], "giltdark", { name: "clamp" })),
    // Hoses into him: two drop from the top hose into the crown of his skull, and one from each side bundle sags across into his
    // shoulder, a capsule on it.
    seg2d("crownhose", [6.4, 24.4], [6.6, 21.4], 0.9, 1.3, 2.3, "hose"), seg2d("crownhose", [6.6, 21.4], [7.3, 18.6], 0.9, 1.3, 2.3, "hose"),
    seg2d("crownhose", [9.6, 24.4], [9.4, 21.4], 0.9, 1.3, 2.3, "hose"), seg2d("crownhose", [9.4, 21.4], [8.7, 18.6], 0.9, 1.3, 2.3, "hose"),
    ...[-1, 1].flatMap((side) => {
      const xo = (d) => 8 + side * d;
      return [
        box([Math.min(xo(8), xo(7)), 20.4, 0], [Math.max(xo(8), xo(7)), 21.6, 2.8], "hose", { name: "shoulderstub" }),
        seg2d("shoulderhose", [xo(7.4), 21], [xo(6.2), 17], 1.1, 2, 3.2, "hose"),
        seg2d("shoulderhose", [xo(6.2), 17], [xo(4.6), 15.2], 1.1, 2, 3.2, "hose"),
        seg2d("shoulderhose", [xo(4.6), 15.2], [xo(2.6), 14.2], 1.1, 2, 3.2, "hose"),
        box([Math.min(xo(5.2), xo(7)), 15.6, 1.8], [Math.max(xo(5.2), xo(7)), 17.6, 3.4], "capsule", { name: "capsule" }),
      ];
    }),
    ...[-1, 1].flatMap((side) => {
      const x = side < 0 ? 0 : 16, out = side < 0 ? -1 : 1;
      const xo = (d) => x + out * d;
      return [
        // Two straight drops down the side of the backrest to the dais.
        box([Math.min(xo(0), xo(1.2)), 2, -2.6], [Math.max(xo(0), xo(1.2)), 25, -1.4], "hose", { name: "sidehose" }),
        box([Math.min(xo(0), xo(1.1)), 2, -1.2], [Math.max(xo(0), xo(1.1)), 24, 0], "hose", { name: "sidehose" }),
        box([Math.min(xo(-0.2), xo(1.5)), 15, -2.9], [Math.max(xo(-0.2), xo(1.5)), 17.6, -1.1], "capsule", { name: "capsule" }),
        // A loose loop from the top corner, sagging out and back in at the armrest.
        seg2d("loophose", [xo(0.4), 24.6], [xo(2.6), 20], 1.1, -0.4, 0.7, "hose"),
        seg2d("loophose", [xo(2.6), 20], [xo(2.8), 15.6], 1.1, -0.4, 0.7, "hose"),
        seg2d("loophose", [xo(2.8), 15.6], [xo(1.4), 13.2], 1.1, -0.4, 0.7, "hose"),
        box([Math.min(xo(2), xo(3.6)), 18.6, -0.6], [Math.max(xo(2), xo(3.6)), 21, 0.9], "capsule", { name: "capsule" }),
        // Where the drops meet the dais they coil out along it.
        box([Math.min(xo(-0.5), xo(1.6)), 2, -3.4], [Math.max(xo(-0.5), xo(1.6)), 3.1, 3], "hose", { name: "coil" }),
      ];
    }),
    // The king: robe over a thin body, ribs showing, the skull with its lens and hair.
    box([5.6, 7, 1.4], [10.4, 13.4, 4.6], "cloth", { name: "torso" }),
    box([6.6, 9.6, 4.6], [9.4, 12.8, 4.9], "bone", { name: "ribs" }),
    ...[10.2, 11.2, 12.2].map((y) => box([6.8, y, 4.9], [9.2, y + 0.35, 5], "hollow", { name: "ribgap" })),
    box([5, 12.8, 1.4], [11, 14.2, 4.4], "cloth", { name: "shoulders" }),
    box([7.4, 14, 2.4], [8.6, 15, 3.6], "bone", { name: "neck" }),
    joint([8, 14.8, 3], { name: "head", rot: [10, 0, 6], children: [
      box([-1.9, 0, -1.9], [1.9, 3.9, 1.9], "bone", { name: "skull" }),
      box([-1.4, -0.9, -0.6], [1.4, 0, 1.7], "bone", { name: "jaw" }),
      box([-1.4, 1.6, 1.9], [-0.4, 2.6, 2.05], "hollow", { name: "socket" }),
      // The east eye: one steel block capping that corner of the skull (front, side and top), the lens set in its front, a hose from
      // it back into the throne and one to the neck.
      box([0.05, 1.2, -0.6], [2.05, 4.05, 2.05], "steel", { name: "eyecap" }),
      box([0.3, 1.5, 2.05], [1.6, 2.7, 2.2], "lens", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
      box([2.05, 1.7, -3.4], [2.7, 2.4, 0.4], "hose", { name: "eyehose" }),
      box([1.6, -1.6, 0.6], [2.1, 1, 1.1], "hose", { name: "eyehoseneck" }),
      box([-0.3, 0.8, 1.9], [0.3, 1.3, 2.05], "hollow", { name: "nose" }),
      box([-1.1, -0.2, 1.7], [1.1, 0.2, 1.8], "hollow", { name: "teeth" }),
    ] }),
    ...[-80, -58, -36, 36, 58, 80].map((d, i) => hair(d, 2 + (i % 2))),
    // Arms: upper arms out to the armrests, forearms along them, hands over the ends.
    upperArm(5.2, 1.5), upperArm(10.8, 14.5),
    ...forearm(1.5), ...forearm(14.5),
    // Lap and legs: the robe over his knees, shins down to the seat's edge, feet on the dais.
    box([4.6, 7, 2], [11.4, 8.8, 9], "cloth", { name: "lap" }),
    box([4.9, 2, 7.6], [6.9, 7, 9.4], "cloth", { name: "shinwest" }),
    box([9.1, 2, 7.6], [11.1, 7, 9.4], "cloth", { name: "shineast" }),
    box([4.8, 2, 8.8], [7, 2.9, 11], "bone", { name: "footwest" }),
    box([9, 2, 8.8], [11.2, 2.9, 11], "bone", { name: "footeast" }),
    // The skull on the west armrest.
    box([0.1, 13, 0.6], [2.9, 15.6, 3.4], "bone", { name: "armskull" }),
    box([0.6, 14.2, 3.4], [1.3, 14.9, 3.5], "hollow", { name: "armskulleye" }),
    box([1.7, 14.2, 3.4], [2.4, 14.9, 3.5], "hollow", { name: "armskulleye" }),
  ];

  // ---- Hunched gilt statue: an orange-gold statue on a square plinth of a hunched figure: its trunk bent forward into a great
  // round hump with small spikes along the spine and dark segment bands; the head with its leaf-shaped mask hangs low at the front
  // under the hump; a small hand holds a staff with a fletched head; brown rags hang from under the hump and over the post.
  // Built facing south.
  const backSpike = (x, y, z) => [
    box([x - 0.6, y, z - 0.6], [x + 0.6, y + 0.8, z + 0.6], "dark", { name: "spike" }),
    box([x - 0.25, y + 0.8, z - 0.25], [x + 0.25, y + 1.5, z + 0.25], "dark", { name: "spiketip" }),
  ];
  const statueRag = (x, z, top, len, w = 1.2, side = "z") => box(side === "z" ? [x - w / 2, top - len, z] : [x, top - len, z - w / 2],
    side === "z" ? [x + w / 2, top, z + 0.3] : [x + 0.3, top, z + w / 2], "rag", { name: "rag" });
  const leafMask = mirrorRows(["..####", ".#####", "######", "######", "######", "######", "######", ".#####", ".#####", "..####", "..####", "...###", "....##"]);
  const gildedStatue = [
    // Plinth, step and the square post.
    box([2.5, 0, 2.5], [13.5, 1.2, 13.5], "ochre", { name: "plinth" }),
    box([3.5, 1.2, 3.5], [12.5, 2.2, 12.5], "dark", { name: "plinthstep" }),
    box([5.2, 2.2, 5.2], [10.8, 5.4, 10.8], "ochre", { name: "post" }),
    // Waist, and the trunk bending forward from it: a long hump built along the spine (local y up the spine, z toward the
    // front), its back bulging out (-z) into the highest point, spikes along the spine, dark segment bands across it. The head
    // hangs from the front of the trunk's top, below the hump, lifted to look forward; the leaf mask covers it.
    box([6.2, 5.4, 6.2], [9.8, 10.4, 9.8], "ochre", { name: "waist" }),
    joint([8, 9.6, 8.4], { name: "trunk", rot: [38, 0, 0], children: [
      box([-4.4, 0, -3], [4.4, 6, 3], "ochre", { name: "belly" }),
      box([-5.4, 3, -8], [5.4, 12, 2], "ochre", { name: "hump" }),
      box([-4.4, 4, -9.6], [4.4, 13.4, 1], "ochre", { name: "humpback" }),
      box([-6.4, 5, -7], [6.4, 10.5, 1], "ochre", { name: "humpside" }),
      box([-3.4, 6, -10.6], [3.4, 12.4, -2], "ochre", { name: "humpcrest" }),
      box([-3.6, 12.8, -8], [3.6, 14.4, -1], "ochre", { name: "humptop" }),
      box([-4, 11, -4], [4, 13.5, 1.5], "ochre", { name: "shoulders" }),
      ...[6, 9.4].map((y) => box([-6.5, y, -10.7], [6.5, y + 0.7, 2.1], "dark", { name: "band" })),
      ...[5.2, 7.6, 10].flatMap((y) => [box([-0.6, y - 0.6, -11.4], [0.6, y + 0.6, -10.6], "dark", { name: "spike" }),
        box([-0.25, y - 0.25, -12.1], [0.25, y + 0.25, -11.4], "dark", { name: "spiketip" })]),
      ...[-1, 1].flatMap((sd) => [6.4, 9].map((y) => box([sd * 3 - 0.6, y - 0.6, -11.4], [sd * 3 + 0.6, y + 0.6, -10.6], "dark", { name: "spike" }))),
      ...[-2, 2].map((x) => box([x - 0.6, 13, -6], [x + 0.6, 14.2, -4.8], "dark", { name: "spike" })),
      // The head, hanging from the front under the hump, lifted to look ahead; the leaf mask over its face.
      joint([0, 9.6, 2.2], { name: "head", rot: [-50, 0, 6], children: [
        box([-2, -1.5, -1.5], [2, 2.5, 2], "ochre", { name: "headcore" }),
        joint([0, 3, 1.6], { name: "mask", rot: [6, 0, 0], children: [
          relief(leafMask, { at: [-4.2, 0, 1], cell: 0.7, depth: 1.2, tex: "ochre", name: "maskplate" }),
          box([-3, -3.4, 1], [-0.8, -2.8, 1.2], "hollow", { name: "eye", rot: [0, 0, -14] }),
          box([0.8, -3.4, 1], [3, -2.8, 1.2], "hollow", { name: "eye", rot: [0, 0, 14] }),
          box([-0.25, -8.6, 1], [0.25, -0.8, 1.3], "dark", { name: "ridge" }),
        ] }),
      ] }),
    ] }),
    // The arm out of the east side to the staff, and the staff leaning from the plinth's corner, a fletched head on top.
    box([11.8, 13.4, 12.2], [15.2, 14.8, 13.8], "ochre", { name: "arm" }),
    box([14.2, 12.8, 12.6], [16.6, 15.6, 15], "ochre", { name: "hand" }),
    joint([12.4, 2.2, 13], { name: "staff", rot: [4, 0, -14], children: [
      box([-0.4, 0, -0.4], [0.4, 25, 0.4], "ochre", { name: "staffpole" }),
      box([-0.9, 22, -0.4], [0.9, 23, 0.4], "dark", { name: "staffbind" }),
      box([-0.5, 25, -0.5], [0.5, 27.4, 0.5], "ochre", { name: "staffhead" }),
      ...[-1, 1].map((sd) => box([sd > 0 ? 0.4 : -2.2, 25.4, -0.3], [sd > 0 ? 2.2 : -0.4, 27.8, 0.3], "ochre", { name: "fletch", rot: [0, 0, sd * -28], origin: [0, 25.4, 0] })),
    ] }),
    // Rags: a fringe hanging from under the hump's sides and back, and more draped over the post.
    ...[1.8, 3.6, 12.4, 14.2].map((x, i) => statueRag(x, 4.4, 17, [5, 7, 6, 4][i], 1.2, "x")),
    ...[3, 5.5, 8, 10.5, 13].map((x, i) => statueRag(x, 1.2, 18.4, [6, 4, 7, 5, 6][i])),
    ...[6, 8.5, 10.8].map((x, i) => statueRag(x, 10.8, 5.4, [2.4, 3, 2][i])),
    ...[5.5, 9].map((z, i) => statueRag(4.9, z, 5.4, [2.6, 2][i], 1.4, "x")),
  ];

  // ---- Ember crystal: a cluster of red crystals round a faceted core with a flame-drop emblem cut into its front. The crystals
  // are made the way the game makes its own (shapes/block/stone/mineral/large1.json, cluster1.json): each one a single plain box
  // with a flat top and no bottom face, rooted at its base and tilted on all three axes; many of them, of different sizes and
  // leans, and the texture does the rest. Built facing south.
  const crystal = (at, rot, len, w) => box([at[0] - w, at[1], at[2] - w], [at[0] + w, at[1] + len, at[2] + w], "crystal",
    { name: "crystal", rot, origin: at, faces: { down: false } });
  const flame = ["....##....", "...####...", "..##..##..", ".##.##.##.", "##.####.##", "##.####.##", "##..##..##", ".##....##.", "..######..", "...####..."];
  const emberCrystal = [
    // The core: two crossed boxes (a plus seen from above, its corners cut), a cap, the emblem on the front.
    box([4, 0, 3], [12, 7, 13], "crystal", { name: "core" }),
    box([3, 0, 4], [13, 7, 12], "crystal", { name: "core" }),
    box([4.6, 7, 4.6], [11.4, 8.4, 11.4], "dark", { name: "corecap" }),
    relief(flame, { at: [5, 6.4, 13.4], cell: 0.6, depth: 0.5, tex: "dark", name: "emblem" }),
    // The crystals: a great one on top, two long ones out to the sides, shorter ones low at the front and the back, small ones
    // between them.
    crystal([8, 6.5, 7.5], [10, 4, -8], 13, 2.6),
    crystal([5, 5, 7], [6, 20, 48], 10, 1.8),
    crystal([11, 5, 8], [-4, -15, -60], 10, 1.8),
    crystal([4.6, 3, 10.6], [52, 30, 58], 7, 1.4),
    crystal([11.4, 2.6, 11], [58, -25, -55], 5, 1.1),
    crystal([9.4, 5, 4], [-40, 15, -22], 8, 1.6),
    crystal([6, 5, 4], [-34, -20, 30], 6, 1.2),
    crystal([7, 6.5, 9], [24, 40, 14], 6, 1.1),
    crystal([10, 6.5, 6], [-12, 30, -24], 7, 1.2),
    crystal([3.5, 1.5, 6], [10, 40, 70], 4, 1),
    crystal([12.8, 1.5, 5], [-20, 10, -72], 4.5, 1),
  ];

  // ---- Painted wing: a pale shell with a long rounded nose and two talons, painted with red glyphs, a fan of rainbow feathers out of its east side and its lower
  // edge, a sun gem in a brown sheath at the corner where the fans meet. Built upright (face to the south, x east, y up), then
  // laid back on the ground. A feather is a few flat boxes along its axis, widest a third of the way out, tipped to a point.
  const feather = (at, deg, len, w, tex) => joint(at, { name: "feather", rot: [0, 0, deg], children:
    [[0, 0.2, 0.55], [0.2, 0.55, 1], [0.55, 0.8, 0.65], [0.8, 1, 0.3]].map(([f0, f1, k]) =>
      box([-w * k, f0 * len, -0.35], [w * k, f1 * len + 0.05, 0.35], tex, { name: "vane" })) });
  // A talon: a base and a tip bent by `bend` (its sign sets the way it hooks).
  const wingHorn = (at, deg, bend) => joint(at, { name: "talon", rot: [0, 0, deg], children: [
    box([-0.9, 0, -0.9], [0.9, 2.2, 0.9], "horn", { name: "talonbase" }),
    joint([0, 2.1, 0], { name: "talonbend", rot: [0, 0, bend], children: [
      box([-0.6, 0, -0.6], [0.6, 2, 0.6], "horn", { name: "talontip" }),
      box([-0.3, 1.9, -0.3], [0.3, 3, 0.3], "horn", { name: "talontip" }),
    ] }),
  ] });
  const fcol = ["green", "orange", "pink", "violet", "blue"];
  const wing = [
    // The shell: a broad plate with a hump on top and a long rounded nose out of its west side (away from the feathers), the
    // glyphs on its face. One talon on the hump curls toward the nose; the other hooks down from under the nose's tip.
    box([0, 0, -1.2], [12, 6, 1.2], "shell", { name: "shell", faces: { south: { texture: "shell", uv: "full" } } }),
    box([3, 6, -1], [9.6, 7.8, 1], "shell", { name: "hump" }),
    box([-5, 2.6, -1.1], [0.2, 7, 1.1], "shell", { name: "nose" }),
    box([-8.4, 3.2, -0.9], [-4.8, 6.8, 0.9], "shell", { name: "nosemid" }),
    box([-10, 3.8, -0.7], [-8.2, 6.4, 0.7], "shell", { name: "nosetip" }),
    box([-4.6, 3.6, 1.1], [-0.4, 4.2, 1.2], "glyph", { name: "noseglyph" }),
    wingHorn([6.4, 7.6, 0], 18, 48),
    wingHorn([-8.8, 4, 0], 160, -44),
    // The fans: five feathers out of the east side, sweeping from east to south-east; five down from the lower edge.
    ...[-60, -80, -100, -118, -136].map((d, i) => feather([11.6, 5.8 - i * 1.3, i % 2 ? 0.25 : -0.25], d, 9 - i * 0.6, 1.3, fcol[i])),
    ...[172, 182, 192, 202, 212].map((d, i) => feather([1.2 + i * 2, 0.4, i % 2 ? 0.3 : -0.3], d, 10 - i * 0.5, 1.2, fcol[(i + 2) % 5])),
    // The sun gem in its sheath at the corner.
    joint([11, 0.6, 0.6], { name: "gem", rot: [0, 0, 145], children: [
      box([-1.1, 0, -0.8], [1.1, 5.5, 0.8], "sheath", { name: "sheath" }),
      box([-0.6, 5.5, -0.5], [0.6, 7, 0.5], "sheath", { name: "sheathtip" }),
      box([-1.6, -0.6, -0.4], [1.6, 1.4, 1.2], "gem", { name: "sungem" }),
    ] }),
  ];
  const paintedWingElements = [
    joint([2.5, 2, 4], { name: "wing", rot: [-72, 0, 0], children: wing }),
  ];

  // ---- Suited effigy: a stout figure of painted plaster in a maroon check suit, a tall blocky blue head with a scowl, pale blue
  // gloves, a striped tie and a lapel badge with three rising bars. Left standing for ages: the paint chipped to plaster, the cloth
  // moth-eaten, the east forearm snapped off at the elbow (it lies at its feet), the top east corner of the head broken off (beside
  // the forearm). Built facing south around x = 8, z = 8, then leaned a little by the joint that holds it.
  const effSleeve = (s, broken) => joint([8 + s * 7.1, 18.4, 8], { name: "arm", rot: [broken ? -6 : 4, 0, s * 6], children: [
    box([-1.7, -6, -1.7], [1.7, 0.6, 1.7], "suit", { name: "upperarm" }),
    ...(broken ? [box([-1.3, -6.3, -1.3], [1.3, -6, 1.3], "hollow", { name: "stump" })] : [
      joint([0, -6, 0], { name: "elbow", rot: [-14, 0, 0], children: [
        box([-1.6, -4.4, -1.6], [1.6, 0.2, 1.6], "suit", { name: "forearm" }),
        box([-1.3, -5, -1.3], [1.3, -4.4, 1.3], "shirt", { name: "cuff" }),
        box([-1.4, -7.6, -1.3], [1.4, -5, 1.3], "glove", { name: "hand" }),
        box([-1.4, -8.6, -1.1], [1.4, -7.6, 0.9], "glove", { name: "fingers", rot: [-20, 0, 0], origin: [0, -7.6, 0] }),
        box([s > 0 ? -1.8 : 1.4, -7.2, 0.2], [s > 0 ? -1.4 : 1.8, -5.8, 1.1], "glove", { name: "thumb" }),
      ] }),
    ]),
  ] });
  const effigy = [
    box([3.4, 0, 5.4], [7.3, 1.6, 11.6], "shoe", { name: "shoewest" }),
    box([8.7, 0, 5.4], [12.6, 1.6, 11.6], "shoe", { name: "shoeeast" }),
    box([3.9, 1.6, 6], [7.2, 7.4, 10], "suit", { name: "legwest" }),
    box([8.8, 1.6, 6], [12.1, 7.4, 10], "suit", { name: "legeast" }),
    box([2.3, 7, 4.2], [13.7, 14.2, 11.8], "suit", { name: "belly" }),
    box([2.6, 14.2, 4.6], [13.4, 18.6, 11.2], "suit", { name: "chest" }),
    box([2, 17.6, 4.8], [14, 19.6, 11], "suit", { name: "shoulders" }),
    // Front: the shirt's V, tie, lapels, two rows of buttons, the badge.
    box([6.9, 15.2, 11.2], [9.1, 19.6, 11.4], "shirt", { name: "shirt" }),
    box([7.5, 12.4, 11.4], [8.5, 19, 11.7], "tie", { name: "tie" }),
    box([7.6, 18.6, 11.4], [8.4, 19.4, 11.8], "tie", { name: "knot" }),
    box([5.6, 14.8, 11.2], [6.9, 19.6, 11.5], "suit", { name: "lapelwest", rot: [0, 0, -12] }),
    box([9.1, 14.8, 11.2], [10.4, 19.6, 11.5], "suit", { name: "lapeleast", rot: [0, 0, 12] }),
    ...[8.6, 10.8, 13].flatMap((y) => [6.2, 9.8].map((x) => box([x - 0.35, y - 0.35, 11.8], [x + 0.35, y + 0.35, 12.1], "shoe", { name: "button" }))),
    box([10.6, 15.6, 11.4], [12.6, 17.6, 11.8], "badge", { name: "badge", faces: { south: { texture: "badge", uv: "full" } } }),
    // The head: tall, flat-faced, a little wider at the brow; the top east corner gone (dark break).
    box([4.6, 19.6, 5.2], [11.4, 27, 10.8], "blue", { name: "head", faces: { south: { texture: "face", uv: [0, 0, 13, 15] } } }),
    box([4.4, 25.4, 5], [11.6, 27, 11], "blue", { name: "brow", faces: { south: { texture: "face", uv: [0, 0, 13, 2] } } }),
    box([4.4, 27, 5], [9.2, 28.4, 11], "blue", { name: "crown" }),
    box([9.2, 27, 5.4], [11.4, 27.2, 10.8], "hollow", { name: "break" }),
    effSleeve(-1, false), effSleeve(1, true),
  ];
  const effigyElements = [
    joint([0, 0, 0], { name: "effigy", rot: [-3, 0, 4], children: effigy }),
    // What broke off: the east forearm with its hand, lying in front; the corner of the head.
    joint([13, 0, 14], { name: "fallenforearm", rot: [0, -35, 0], children: [
      box([0, 0, 0], [4.6, 3.2, 3.2], "suit", { name: "forearm" }),
      box([4.6, 0.3, 0.3], [5.2, 2.9, 2.9], "shirt", { name: "cuff" }),
      box([5.2, 0, 0.3], [7.8, 2.8, 2.9], "glove", { name: "hand" }),
      box([7.8, 0, 0.5], [8.8, 2.6, 2.6], "glove", { name: "fingers" }),
      box([-0.1, 0.4, 0.4], [0, 2.8, 2.8], "hollow", { name: "break" }),
    ] }),
    box([3, 0, 13.5], [5.4, 1.6, 15.6], "blue", { name: "fallencorner", ground: true, rot: [0, 22, 8] }),
  ];

  // ---- Broken target bots: three little practice bots, one relic each, left on their posts, shot full of arrows and rotting. Each is
  // a short square body of painted bands, a stepped square head with two square goggle eyes and ear knobs, small arms, two spike legs,
  // mounted on a hooked steel post behind it. Red: leaning off its post, a leg snapped and lying in front. Green: upright, a red X
  // scratched on its chest, an arrow in it, a spent arrow on the floor. Tan: its head knocked back open on the hinge, two arrows in it. Bots are built
  // around their own feet (0, 0, 0; eyes to the south) and set by the joint that holds them. Square boxes, stepped in for the head.
  const oct = (r, y0, y1, tex, name, at = [0, 0]) => [box([at[0] - r * 0.9, y0, at[1] - r * 0.9], [at[0] + r * 0.9, y1, at[1] + r * 0.9], tex, { name })];
  const botLeg = (s) => joint([s * 1.7, 6.6, 0], { name: "leg", rot: [0, 0, s * 24], children:
    [1.3, 1, 0.7, 0.4].map((w, i) => box([-w / 2, -1.8 * (i + 1), -w / 2], [w / 2, -1.8 * i + 0.1, w / 2], "metal", { name: "spike" })) });
  const arrow = (at, rot) => joint(at, { name: "arrow", rot, children: [
    box([-0.2, -1, -0.2], [0.2, 9, 0.2], "shaft", { name: "shaft" }),
    box([-0.05, 7, -0.8], [0.05, 9, 0.8], "fletch", { name: "fletch" }),
    box([-0.8, 7, -0.05], [0.8, 9, 0.05], "fletch", { name: "fletch" }),
  ] });
  const botHead = (paint) => [
    ...oct(2.9, 0, 2.6, paint, "dome"), ...oct(2.6, 2.6, 3.8, paint, "dome"), ...oct(2, 3.8, 4.7, paint, "dome"), ...oct(1.1, 4.7, 5.2, paint, "dome"),
    ...[-1.25, 1.25].map((x) => box([x - 1.15, 1, 1.9], [x + 1.15, 3.3, 3.2], "metal", { name: "eye", faces: { south: { texture: "eye", uv: "full" } } })),
    ...[-1, 1].map((s) => box([s > 0 ? 2.6 : -3.4, 0.6, -0.8], [s > 0 ? 3.4 : -2.6, 2.4, 0.9], "metal", { name: "ear" })),
  ];
  const bot = (paint, { lean = [0, 0, 0], head = [0, 0, 0], legs = [-1, 1], chest = paint, extra = [] } = {}) => joint([0, 0, 0], { name: "bot", rot: lean, children: [
    ...legs.map(botLeg),
    ...oct(3.1, 6, 8.6, paint, "band"),
    ...oct(2.9, 8.6, 8.9, "metal", "seam"),
    ...oct(3.1, 8.9, 12.4, paint, "band"),
    box([-1.4, 9.6, 2.6], [1.4, 11.8, 2.67], chest, { name: "chest", faces: { south: { texture: chest, uv: "full" } } }),
    ...oct(2.9, 12.4, 12.7, "metal", "seam"),
    ...oct(3.1, 12.7, 15.6, paint, "band"),
    ...[-1, 1].map((s) => box([s > 0 ? 2.7 : -4.1, 10.4, -0.7], [s > 0 ? 4.1 : -2.7, 11.8, 0.7], "metal", { name: "arm" })),
    box([-0.7, 10.6, -4.6], [0.7, 12, -2.4], "metal", { name: "bracket" }),
    // The head hinges at the back of the top band.
    joint([0, 15.6, -2.6], { name: "head", rot: head, children: [joint([0, 0, 2.6], { name: "headbody", children: botHead(paint) })] }),
    ...extra,
  ] });
  const post = (x, z) => [
    box([x - 0.6, 0, z - 0.6], [x + 0.6, 26, z + 0.6], "metal", { name: "post" }),
    box([x - 0.6, 24.8, z - 0.6], [x + 0.6, 26, z + 2.6], "metal", { name: "hook" }),
    box([x - 0.5, 23.6, z + 1.6], [x + 0.5, 24.8, z + 2.6], "metal", { name: "hooktip" }),
    box([x - 1.4, 0, z - 1.4], [x + 1.4, 0.6, z + 1.4], "metal", { name: "postfoot" }),
  ];
  // Each bot is its own relic: the bot on its post (the post 4.4 behind its feet), and what fell from it.
  const botRed = [
    ...post(8, 5),
    joint([8, 0, 9.4], { name: "redbot", children: [bot("red", { lean: [6, 10, 8], head: [10, 0, 6], legs: [-1],
      extra: [arrow([-2.4, 13, 1.6], [40, 0, 40])] })] }),
    // Its snapped leg, in front of it.
    joint([11, 0.65, 15], { name: "fallenleg", rot: [0, 30, 90], children:
      [1.3, 1, 0.7, 0.4].map((w, i) => box([-w / 2, -1.8 * (i + 1), -w / 2], [w / 2, -1.8 * i + 0.1, w / 2], "metal", { name: "spike" })) }),
  ];
  const botGreen = [
    ...post(8, 5),
    joint([8, 0, 9.4], { name: "greenbot", children: [bot("green", { lean: [0, -6, 0], chest: "greenx",
      extra: [arrow([1.6, 10.4, 2.4], [45, 0, -25])] })] }),
    // A spent arrow on the floor.
    joint([12, 0.3, 15], { name: "floorarrow", rot: [0, -60, 90], children: [arrow([0, 0, 0], [0, 0, 0])] }),
  ];
  const botTan = [
    ...post(8, 5),
    joint([8, 0, 9.4], { name: "tanbot", children: [bot("tan", { lean: [0, -14, 3], head: [-38, 0, 0],
      extra: [...oct(2.8, 15.6, 15.65, "hollow", "open"), arrow([1.2, 11, 2.6], [50, 0, -15]), arrow([2.8, 8, 1], [25, 0, -55])] })] }),
  ];

  // ---- Ruined animatronics: four stage figures of a long-closed party hall, the mod's own cast (a boar drummer, a hare, a moth
  // usher, a goat ringmaster). Square-boxed fur suits over a metal frame: matted and grimy, torn at the seams with stuffing and wires
  // showing, here and there a limb stripped to the frame, one eye lamp still faintly lit. One builder (`anim`) makes the body from
  // a pose (joint angles) and a head; positive x tips something forward (south), a hanging limb swings south with negative.
  const jointBall = (r, tex, name = "joint") => box([-r, -r, -r], [r, r, r], tex, { name });
  // A limb: upper and lower segment (fur, or bare metal "frame" with a torn fur cuff at the root), then a hand or a foot.
  const animLimb = (name, at, rot, bend, { len, w, bare = "", end = "hand", endTex = "fur", grip = [], tex = "fur" }) => {
    const seg = (lo, part) => (bare === "all" || bare === part)
      ? [box([-0.5, -len, -0.5], [0.5, 0.2, 0.5], "metal", { name: name + part + "frame" }), ...(part === "upper" ? [box([-w - 0.1, -1.4, -w - 0.1], [w + 0.1, 0.6, w + 0.1], tex, { name: name + "cuff" }), box([-w + 0.3, -1.45, -w + 0.3], [w - 0.3, -1.4, w - 0.3], "hollow", { name: name + "torn" })] : [])]
      : [box([-w * lo, -len, -w * lo], [w * lo, 0.6, w * lo], tex, { name: name + part })];
    const bareLow = bare === "all" || bare === "lower";
    const endPart = end === "hand"
      ? (bareLow ? [0, 1, 2].map((i) => box([-0.9 + i * 0.75, -len - 2.4, -0.25], [-0.6 + i * 0.75, -len, 0.25], "metal", { name: name + "claw" }))
        : [box([-w, -len - 2.4, -w * 0.8], [w, -len, w * 0.8], endTex, { name: name + "hand" })])
      : [box([-w * 1.15, -len - 1.3, -w * 1.15], [w * 1.15, -len + 0.2, w * 1.15 + 2.2], endTex, { name: name + "foot" })];
    return joint(at, { name, rot, children: [
      ...seg(1, "upper"), ...(bare ? [jointBall(0.9, "metal")] : []),
      joint([0, -len, 0], { name: name + "bend", rot: bend, children: [...seg(0.92, "lower"), ...endPart,
        ...(grip.length ? [joint([0, -len - 1.4, 0], { name: name + "grip", children: grip })] : [])] }),
    ] });
  };
  const anim = ({ tw, th, td, limb, lw, torsoTex = "fur", armTex = "fur", legTex = "fur", pose, bare = {}, head, extra = [], grip = {}, foot = "fur" }) => [
    box([-tw * 0.85, -1.6, -td], [tw * 0.85, 1.6, td], torsoTex, { name: "pelvis" }),
    ...[-1, 1].map((s) => { const k = s < 0 ? "W" : "E"; return animLimb("leg" + k, [s * tw * 0.48, -1, 0], pose["leg" + k], pose["knee" + k],
      { len: limb, w: lw * 1.12, bare: bare["leg" + k], end: "foot", endTex: foot, tex: legTex }); }),
    joint([0, 1.4, 0], { name: "torso", rot: pose.torso, children: [
      box([-tw, 0, -td], [tw, th, td], torsoTex, { name: "torso" }),
      box([-tw * 0.62, 1, td], [tw * 0.62, th * 0.72, td + 0.3], torsoTex === "fur" ? "belly" : torsoTex, { name: "belly" }),
      joint([0, th, 0], { name: "neck", rot: pose.head, children: [box([-1, -0.4, -1], [1, 0.8, 1], "metal", { name: "neck" }), ...head] }),
      ...[-1, 1].map((s) => { const k = s < 0 ? "W" : "E"; return animLimb("arm" + k, [s * (tw + lw), th - 1.3, 0], pose["arm" + k], pose["elbow" + k],
        { len: limb * 0.9, w: lw, bare: bare["arm" + k], grip: grip[k] ?? [], endTex: torsoTex === "fur" ? "fur" : "hand", tex: armTex }); }),
      ...extra,
    ] }),
  ];
  const eyes = (y, z, xs = [1.95], { w = 1.6, h = 1.5, lit = "E" } = {}) => [-1, 1].map((s) => box([s * xs[0] - w / 2, y, z], [s * xs[0] + w / 2, y + h, z + 0.25], "fur",
    { name: "eye", faces: { south: { texture: (s > 0) === (lit === "E") ? "eyelit" : "eyedead", uv: "full" } } }));
  // A seam split open on the torso: dark inside, a tuft of stuffing, two wires.
  const split = (x, y, z, h = 4) => [
    box([x - 0.7, y, z], [x + 0.7, y + h, z + 0.1], "hollow", { name: "split" }),
    box([x - 0.5, y + h * 0.3, z], [x + 0.6, y + h * 0.6, z + 0.9], "stuff", { name: "stuffing" }),
    box([x - 0.2, y - 2.2, z + 0.1], [x + 0.1, y + h * 0.5, z + 0.4], "wire", { name: "wire", rot: [0, 0, 12] }),
  ];

  // Boar drummer: sits slumped on the floor beside its stove-in drum, jaw hanging open, the east arm stripped to the frame with a stick still in its claw.
  const boarHead = [
    box([-3.6, 0.4, -3.2], [3.6, 6.8, 3.2], "fur", { name: "head" }),
    box([-2.1, 0.9, 3.2], [2.1, 3.9, 5.6], "belly", { name: "snout" }),
    ...[-0.85, 0.85].map((x) => box([x - 0.45, 2, 5.6], [x + 0.45, 2.9, 5.7], "hollow", { name: "nostril" })),
    ...[-1, 1].map((s) => joint([s * 1.9, 1.2, 5], { name: "tusk", rot: [0, 0, -s * 15], children: [box([-0.35, 0, -0.35], [0.35, 2.4, 0.35], "bone", { name: "tusk" })] })),
    ...eyes(4, 3.2),
    ...[-1, 1].map((s) => box([s * 1.95 - 1.1, 5.6, 3.2], [s * 1.95 + 1.1, 6.2, 3.7], "fur", { name: "brow", rot: [0, 0, s * 12] })),
    joint([3, 6.4, -0.5], { name: "earE", rot: [0, 0, -35], children: [box([-0.9, 0, -0.4], [0.9, 2.6, 0.4], "fur", { name: "ear" })] }),
    joint([-3, 6.4, -0.5], { name: "earW", rot: [10, 0, 110], children: [box([-0.9, 0, -0.4], [0.9, 2.6, 0.4], "fur", { name: "ear" })] }),
    box([-1.9, 0.5, 0.5], [1.9, 0.9, 5.4], "hollow", { name: "mouth" }),
    box([-1.9, 0.5, 5], [1.9, 0.95, 5.5], "bone", { name: "teethupper" }),
    joint([0, 0.6, -2.4], { name: "jaw", rot: [38, 0, 0], children: [
      box([-2.6, -1.3, 0], [2.6, 0, 7.6], "belly", { name: "jaw" }),
      box([-2.2, 0, 6.8], [2.2, 0.6, 7.4], "bone", { name: "teethlower" }),
    ] }),
  ];
  const stick = (name) => box([-0.3, -0.3, 0], [0.3, 0.3, 7], "wood", { name });
  const boar = anim({ tw: 4.6, th: 9, td: 3, limb: 5, lw: 1.7, head: boarHead,
    pose: { torso: [-16, 0, 6], head: [26, 0, 16], legW: [-84, -8, 0], kneeW: [40, 0, 0], legE: [-80, 10, 0], kneeE: [20, 0, 0],
      armW: [-8, 0, -22], elbowW: [-24, 0, 0], armE: [-30, 0, 16], elbowE: [-40, 0, 0] },
    bare: { armE: "all" }, grip: { E: [joint([0, 0, 0], { name: "stick", rot: [70, 0, 0], children: [stick("stick")] })] },
    extra: split(2.6, 3, 3) });
  const drum = joint([18, 0, 9], { name: "drum", rot: [0, -30, -80], children: [
    box([-4.5, 0, -4.5], [4.5, 6, 4.5], "drum", { name: "shell", faces: { up: "skin", down: "skin" } }),
    box([-4.8, 5.6, -4.8], [4.8, 6.4, 4.8], "brass", { name: "rim", faces: { up: false } }),
    box([-4.8, -0.4, -4.8], [4.8, 0.4, 4.8], "brass", { name: "rim", faces: { down: false } }),
    box([-2.6, 6.35, -1.8], [1.4, 6.45, 2.4], "hollow", { name: "hole" }),
  ] });
  const boarElements = [
    joint([8, 7, 6], { name: "boar", rot: [0, 12, 0], children: boar }),
    drum,
    joint([3, 0.3, 14], { name: "floorstick", rot: [0, 50, 0], children: [stick("floorstick")] }),
  ];

  // Hare: stands askew in a red sash, head lolling, the west ear torn off (it lies at its feet with its wires), the west forearm
  // stripped to the frame, a dropped tambourine.
  const hareEar = (torn) => [
    ...(torn ? [box([-0.9, 0, -0.45], [0.9, 1.8, 0.45], "fur", { name: "earstump" }), box([-0.7, 1.8, -0.3], [0.7, 1.85, 0.3], "hollow", { name: "eartorn" }),
      box([-0.15, 1.8, -0.1], [0.15, 3.2, 0.2], "wire", { name: "earwire", rot: [0, 0, 20] })]
      : [box([-0.9, 0, -0.45], [0.9, 9, 0.45], "fur", { name: "ear" }), box([-0.5, 1, 0.45], [0.5, 8, 0.6], "belly", { name: "earinner" })]),
  ];
  const hareHead = [
    box([-3, 0.4, -2.8], [3, 6.6, 2.8], "fur", { name: "head" }),
    box([-1.8, 0.6, 2.8], [1.8, 3, 4.2], "belly", { name: "muzzle" }),
    box([-0.6, 2.5, 4.2], [0.6, 3.1, 4.45], "red", { name: "nose" }),
    box([-0.8, 0.2, 3.9], [0.8, 1, 4.25], "bone", { name: "teeth" }),
    ...eyes(3.6, 2.8, [1.6], { w: 1.5, h: 1.7, lit: "W" }),
    joint([1.6, 6.6, -0.4], { name: "earE", rot: [-10, 0, -14], children: [...hareEar(false), joint([0, 6, 0], { name: "earflop", rot: [-70, 0, 0], children: [] })] }),
    joint([-1.6, 6.6, -0.4], { name: "earW", rot: [-6, 0, 10], children: hareEar(true) }),
  ];
  const hare = anim({ tw: 3.8, th: 8.6, td: 2.6, limb: 5.4, lw: 1.5, head: hareHead,
    pose: { torso: [6, 0, -6], head: [14, 0, -20], legW: [0, 0, -4], kneeW: [0, 0, 0], legE: [-6, 0, 6], kneeE: [12, 0, 0],
      armW: [6, 0, -10], elbowW: [-10, 0, 0], armE: [-10, 0, 14], elbowE: [-30, 0, 0] },
    bare: { armW: "lower" },
    extra: [box([-4.1, 3.4, -2.9], [4.1, 4.8, 2.9], "red", { name: "sash", rot: [0, 0, 34] }), ...split(-1.6, 1.4, 2.6, 3)] });
  const tambourine = joint([13, 0, 13], { name: "tambourine", rot: [0, 20, 0], children: [
    box([-2.8, 0, -2.8], [2.8, 0.15, 2.8], "skin", { name: "skin" }),
    ...[[-2.8, -2.8, 2.8, -2.3], [-2.8, 2.3, 2.8, 2.8], [-2.8, -2.3, -2.3, 2.3], [2.3, -2.3, 2.8, 2.3]].map(([x0, z0, x1, z1]) => box([x0, 0, z0], [x1, 1.2, z1], "wood", { name: "hoop" })),
    ...[[-2.95, 0], [2.95, 0], [0, -2.95], [0, 2.95]].map(([x, z]) => box([x - 0.5, 0.3, z - 0.5], [x + 0.5, 0.9, z + 0.5], "brass", { name: "jingle" })),
  ] });
  const hareElements = [
    joint([8, 12.6, 8], { name: "hare", rot: [0, -8, 4], children: hare }),
    joint([3, 0.45, 14], { name: "fallenear", rot: [90, 70, 0], children: hareEar(false) }),
    tambourine,
  ];

  // Moth usher: kneels on one knee holding out a dead lantern, feathery antennae (one bent), big compound eyes (one lamp lit), a pale
  // ruff, one wing torn off and lying behind it.
  const antenna = (bent) => [
    box([-0.2, 0, -0.2], [0.2, 6, 0.2], "dark", { name: "antenna" }),
    ...[1.2, 2.2, 3.2, 4.2, 5.2].map((y, i) => box([-1.4 + i * 0.2, y, -0.1], [1.4 - i * 0.2, y + 0.4, 0.1], "fur", { name: "barb", rot: [0, 0, bent ? 25 : 0] })),
  ];
  const mothHead = [
    box([-4.2, -0.6, -3.2], [4.2, 1.6, 3.2], "ruff", { name: "ruff" }),
    box([-2.8, 0.6, -2.4], [2.8, 5.6, 2.4], "fur", { name: "head" }),
    ...[-1, 1].map((s) => box([s > 0 ? 1.4 : -3.6, 1.8, 0.2], [s > 0 ? 3.6 : -1.4, 5, 3], "fur", { name: "eye",
      faces: { south: { texture: s > 0 ? "eyelit" : "eyedead", uv: "full" }, [s > 0 ? "east" : "west"]: { texture: "eyedead", uv: "full" } } })),
    box([-0.4, 0.4, 2.4], [0.4, 1.8, 3.4], "bone", { name: "proboscis" }),
    joint([1.2, 5.6, 0.6], { name: "antennaE", rot: [-20, 0, -25], children: antenna(false) }),
    joint([-1.2, 5.6, 0.6], { name: "antennaW", rot: [-20, 0, 25], children: [box([-0.2, 0, -0.2], [0.2, 3, 0.2], "dark", { name: "antenna" }),
      joint([0, 3, 0], { name: "antennabent", rot: [70, 0, 30], children: antenna(true) })] }),
  ];
  const lantern = [
    box([-0.15, -1.5, -0.15], [0.15, 0, 0.15], "metal", { name: "bail" }),
    box([-1.3, -1.9, -1.3], [1.3, -1.5, 1.3], "brass", { name: "lanterntop" }),
    box([-1, -4.5, -1], [1, -1.9, 1], "glass", { name: "lanternglass" }),
    box([-1.3, -4.9, -1.3], [1.3, -4.5, 1.3], "brass", { name: "lanternbase" }),
  ];
  const moth = anim({ tw: 4, th: 8.4, td: 2.8, limb: 5.2, lw: 1.5, head: mothHead,
    pose: { torso: [8, 0, 0], head: [16, 0, 8], legW: [0, 0, -3], kneeW: [-90, 0, 0], legE: [-88, 0, 4], kneeE: [88, 0, 0],
      armW: [4, 0, -8], elbowW: [-12, 0, 0], armE: [-62, 0, 6], elbowE: [-30, 0, 0] },
    grip: { E: [joint([0, 0, 0], { name: "lanternhang", rot: [90, 0, 0], children: lantern })] },
    extra: [
      joint([-1.6, 7.6, -2.8], { name: "wingW", rot: [-12, -30, 0], children: [box([-7, -8, -0.2], [0, 2, 0.2], "fur", { name: "wing" })] }),
      joint([1.6, 7.6, -2.8], { name: "wingE", rot: [-12, 30, 0], children: [box([0, -1.6, -0.2], [2.6, 2, 0.2], "fur", { name: "wingstump" }), box([2.6, -1.6, -0.2], [2.7, 2, 0.2], "hollow", { name: "wingtorn" })] }),
      ...split(1.4, 1.6, 2.8, 3),
    ] });
  const mothElements = [
    joint([8, 7, 7], { name: "moth", rot: [0, 10, 0], children: moth }),
    joint([14, 0.2, 2], { name: "fallenwing", rot: [-90, 20, 0], children: [box([0, -8, -0.2], [7, 2, 0.2], "fur", { name: "wing" })] }),
  ];

  // Goat ringmaster: lanky, in a violet tailcoat, leans on a cane, its head knocked over to one side, one horn snapped, the west
  // forearm stripped to the frame; its cracked top hat has fallen at its feet.
  const horn = (s, snapped) => joint([s * 1.6, 6.8, -0.6], { name: "horn", rot: [-40, 0, -s * 22], children: [
    box([-0.7, 0, -0.7], [0.7, 2.2, 0.7], "bone", { name: "horn" }),
    ...(snapped ? [box([-0.6, 2.2, -0.6], [0.6, 2.3, 0.6], "hollow", { name: "hornbreak" })] : [joint([0, 2.2, 0], { name: "horncurl", rot: [-60, 0, 0], children: [
      box([-0.55, 0, -0.55], [0.55, 2, 0.55], "bone", { name: "horn" }),
      joint([0, 2, 0], { name: "horntip", rot: [-60, 0, 0], children: [box([-0.4, 0, -0.4], [0.4, 1.8, 0.4], "bone", { name: "horn" })] }),
    ] })]),
  ] });
  const goatHead = [
    box([-2.4, 0.4, -2.4], [2.4, 7, 2.4], "fur", { name: "head" }),
    box([-1.7, 0.4, 2.4], [1.7, 3.4, 5.2], "fur", { name: "muzzle" }),
    box([-1, 2.6, 5.2], [1, 3.2, 5.35], "hollow", { name: "nose" }),
    box([-0.9, -2.6, 3.6], [0.9, 0.4, 4.8], "ruff", { name: "beard" }),
    ...eyes(4.4, 2.4, [1.3], { w: 1.4, h: 1.3 }),
    horn(1, false), horn(-1, true),
    ...[-1, 1].map((s) => box([s > 0 ? 2.4 : -4.4, 4.2, -0.6], [s > 0 ? 4.4 : -2.4, 5, 0.6], "fur", { name: "ear", rot: [0, 0, -s * 20] })),
  ];
  const goat = anim({ tw: 3.4, th: 10, td: 2.4, limb: 7.2, lw: 1.2, torsoTex: "coat", armTex: "coat", legTex: "trousers", foot: "black", head: goatHead,
    pose: { torso: [8, 0, 8], head: [12, 0, 30], legW: [0, 0, -4], kneeW: [0, 0, 0], legE: [-4, 0, 6], kneeE: [6, 0, 0],
      armW: [4, 0, -6], elbowW: [-8, 0, 0], armE: [-34, 0, 10], elbowE: [-26, 0, 0] },
    bare: { armW: "lower" },
    extra: [
      box([-3.4, -7, -2.75], [3.4, 0.5, -2.1], "coat", { name: "coattails", rot: [8, 0, 0], origin: [0, 0.5, -2.4] }),
      ...[2, 4, 6].map((y) => box([0.9, y, 2.4], [1.6, y + 0.7, 2.7], "brass", { name: "button" })),
      box([-2.4, 7.4, 2.4], [-0.2, 10, 2.6], "red", { name: "lapel" }),
      ...split(-1.5, 2.4, 2.4, 3),
    ] });
  const hat = joint([3, 0, 13], { name: "hat", rot: [0, -30, 0], children: [joint([0, 3.6, 0], { name: "hattip", rot: [0, 0, 80], children: [
    box([-3.6, -0.25, -3.6], [3.6, 0.25, 3.6], "black", { name: "brim" }),
    box([-2.4, 0.25, -2.4], [2.4, 5.4, 2.4], "black", { name: "crown" }),
    box([-2.5, 0.25, -2.5], [2.5, 1.2, 2.5], "red", { name: "band" }),
    box([-0.4, 2.2, 2.4], [0.4, 5.4, 2.5], "hollow", { name: "crack" }),
  ] })] });
  const goatElements = [
    joint([8, 16.4, 7], { name: "goat", rot: [0, -12, 0], children: goat }),
    box([14.2, 0, 10.6], [14.9, 16.5, 11.3], "black", { name: "cane", rot: [-6, 0, 10], origin: [14.55, 0, 10.95] }),
    box([13.9, 16, 10.3], [15.2, 17.2, 11.6], "brass", { name: "canetop", rot: [-6, 0, 10], origin: [14.55, 0, 10.95] }),
    hat,
  ];

  const animTex = { metal: T + "bot-metal", hollow: T + "hollow-dark", stuff: T + "shroud-pale", wire: T + "hose-grey", bone: T + "bone-old",
    eyelit: T + "eye-lamp", eyedead: T + "eye-dead", wood: T + "scroll-wood", skin: T + "parchment-worn", brass: T + "brass-aged",
    red: T + "robe-red", black: T + "cloth-black", glass: T + "glass-grey", drum: T + "robe-red" };

  const textures = { enamel: T + "enamel-ruined", panel: T + "panel-ruined", hollow: T + "hollow-dark" };
  return {
    out: "../ReliquiarumMod/assets/nf3treliquiarum/shapes/block",
    prune: true, // shapes spreading a shared texture map keep only the keys they use
    // Each shape's inventory icon is kept in relic.json by vsdev shape (its origin follows the model; a scale tuned by eye stays).
    gui: { file: "../ReliquiarumMod/assets/nf3treliquiarum/blocktypes/relic.json", key: "*-{name}-*", rotateY: 180, keepScale: true },
    shapes: {
      crate: { textures: { ...textures, emblem: T + "crate-emblem", emblembroken: T + "crate-emblem-broken" }, elements: crateElements },
      sentinel: { textures: { ...textures, lens: T + "lens-dead" }, elements: sentinelElements },
      shinesun: { textures: { gold: T + "idol-gold", face: T + "idol-face", eye: T + "idol-eye", glint: T + "idol-glint", stone: T + "rubble-stone" }, elements: shinesunElements },
      goldmask: { textures: { gold: T + "idol-gold", field: T + "idol-field", hollow: T + "hollow-dark", stone: T + "rubble-stone" }, elements: goldmaskElements },
      eyecore: { textures: { ...textures, lens: T + "orb-lens", shell: T + "enamel-ruined" }, elements: eyecoreElements },
      riftgun: { textures: { ...textures, shell: T + "enamel-ruined", lens: T + "lens-dead" }, elements: gunElements },
      springboots: { textures: { leather: T + "boot-leather", steel: T + "steel-dull", hollow: T + "hollow-dark" }, elements: bootsElements },
      brassscroll: { textures: { brass: T + "brass-aged", lattice: T + "brass-lattice", wood: T + "scroll-wood", gem: T + "gem-violet", plate: T + "brass-aged", stone: T + "rubble-stone" }, elements: brassscrollElements },
      colossus: { ground: true, textures: { bronze: T + "colossus-bronze", key: T + "colossus-key", wheel: T + "colossus-wheel", bright: T + "bronze-bright", dark: T + "bronze-dark", hollow: T + "hollow-dark" }, elements: colossusElements },
      artificer: { ground: true, textures: { lens: T + "lens-dead", mask: T + "steel-dull", robe: T + "robe-red", iron: T + "iron-worn", brass: T + "bronze-dark", hose: T + "hose-grey", bone: T + "bone-old", screen: T + "screen-dead", glass: T + "glass-grey", hollow: T + "hollow-dark" }, elements: artificerElements },
      skulldrone: { textures: { bone: T + "bone-old", iron: T + "iron-worn", brass: T + "bronze-dark", lens: T + "lens-dead", hose: T + "hose-grey", hollow: T + "hollow-dark" }, elements: skulldroneElements },
      witheredthrone: { textures: { steel: T + "steel-dull", gilt: T + "gilt-worn", giltworn: T + "sundisc-gold", giltdark: T + "gilt-dark", cloth: T + "shroud-pale", bone: T + "bone-old", hair: T + "parchment-worn", hose: T + "hose-grey", capsule: T + "robe-red", lens: T + "lens-dead", hollow: T + "hollow-dark" }, elements: throneElements },
      giltstatue: { textures: { ochre: T + "statue-ochre", dark: T + "statue-ochre-dark", rag: T + "rag-brown", hollow: T + "hollow-dark" }, elements: gildedStatue },
      embercrystal: { ground: true, textures: { crystal: T + "crystal-red", dark: T + "crystal-red-dark" }, elements: emberCrystal },
      paintedwing: { ground: true, textures: { glyph: T + "crystal-red", shell: T + "wing-shell", horn: T + "bone-old", sheath: T + "scroll-wood", gem: T + "gem-sun", green: T + "feather-green", orange: T + "feather-orange", pink: T + "feather-pink", violet: T + "feather-violet", blue: T + "feather-blue" }, elements: paintedWingElements },
      suitedeffigy: { ground: true, textures: { suit: T + "suit-plaid", shirt: T + "shroud-pale", tie: T + "tie-stripe", shoe: T + "boot-leather", badge: T + "badge-chart", blue: T + "effigy-blue", face: T + "effigy-face", glove: T + "glove-pale", hollow: T + "hollow-dark" }, elements: effigyElements },
      targetbotred: { ground: true, textures: { red: T + "bot-red", green: T + "bot-green", greenx: T + "bot-green-x", tan: T + "bot-tan", metal: T + "bot-metal", eye: T + "bot-eye", shaft: T + "scroll-wood", fletch: T + "rag-brown", hollow: T + "hollow-dark" }, elements: botRed },
      targetbotgreen: { ground: true, textures: { red: T + "bot-red", green: T + "bot-green", greenx: T + "bot-green-x", tan: T + "bot-tan", metal: T + "bot-metal", eye: T + "bot-eye", shaft: T + "scroll-wood", fletch: T + "rag-brown", hollow: T + "hollow-dark" }, elements: botGreen },
      targetbottan: { ground: true, textures: { red: T + "bot-red", green: T + "bot-green", greenx: T + "bot-green-x", tan: T + "bot-tan", metal: T + "bot-metal", eye: T + "bot-eye", shaft: T + "scroll-wood", fletch: T + "rag-brown", hollow: T + "hollow-dark" }, elements: botTan },
      animboar: { ground: true, textures: { ...animTex, fur: T + "fur-boar", belly: T + "fur-hare" }, elements: boarElements },
      animhare: { ground: true, textures: { ...animTex, fur: T + "fur-hare", belly: T + "shroud-pale" }, elements: hareElements },
      animmoth: { ground: true, textures: { ...animTex, fur: T + "fur-moth", belly: T + "fur-hare", ruff: T + "fur-hare", dark: T + "boot-leather" }, elements: mothElements },
      animgoat: { ground: true, textures: { ...animTex, fur: T + "fur-goat", coat: T + "coat-violet", trousers: T + "suit-plaid", hand: T + "shroud-pale", ruff: T + "fur-hare" }, elements: goatElements },
      sundisc: { textures: { gold: T + "sundisc-gold", recess: T + "sundisc-recess", face: T + "sundisc-face", stone: T + "rubble-stone" }, elements: sundiscElements },
    },
  };
};
