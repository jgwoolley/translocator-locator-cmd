// Shapes of the armored giants, the artificers and the deathless warriors, each in a few poses. Made with (textures first):
//   vsdev texture CreateImages/reliquiarum-grimdark.textures.json5
//   vsdev shape CreateImages/reliquiarum-grimdark.shapes.mjs
// One builder per kind of figure (armoredGiant, artificer, deathless) takes a pose: joint angles in degrees. Conventions: x east,
// y up, z south (the figures face south); a positive x rotation tips an upright part forward (south), a hanging limb swings
// forward with a negative one; a positive z rotation swings a hanging limb east. Skulls are the game's own human skull
// (`vanillaSkull`), copied element for element.
const T = "nf3treliquiarum:block/";

// survival/shapes/item/clutter/skull/humanoid.json, the "skull" element and its "inside skull" child, as in the game (1.22.7).
// Its face is on the west side; `vanillaSkull` turns it to face the way it is told. About 5 units on a side.
const SKULL = {"name":"skull","from":[-2.0,-0.3,-2.5],"to":[3.0,4.7,2.52],"rotationOrigin":[0.0,1.2,0.0],"rotationZ":-4.0,"faces":{"north":{"texture":"#human","uv":[3.0,11.0,8.0,16.0]},"east":{"texture":"#human","uv":[5.5,11.0,10.5,16.0]},"south":{"texture":"#human","uv":[8.0,11.0,13.0,16.0]},"west":{"texture":"#human","uv":[2.5,2.0,7.5,7.0]},"up":{"texture":"#human","uv":[11.0,0.0,16.0,5.0],"rotation":90},"down":{"texture":"#human","uv":[1.0,2.0,6.0,7.0],"rotation":90,"enabled":false}},"children":[{"name":"inside skull","from":[0.1,-0.01,0.01],"to":[4.9,4.99,5.01],"rotationOrigin":[0.1,-0.5,-0.2],"faces":{"north":{"texture":"#human-inside","uv":[3.0,11.0,7.5,16.0]},"east":{"texture":"#human-inside","uv":[5.5,11.0,10.5,16.0]},"south":{"texture":"#human-inside","uv":[8.0,11.0,12.5,16.0]},"west":{"texture":"#human-inside","uv":[2.5,2.0,7.5,7.0]},"up":{"texture":"#human-inside","uv":[10.0,3.5,14.5,8.5]},"down":{"texture":"#human-inside","uv":[2.0,1.0,6.5,6.0],"enabled":false}}}]};
// The skull wears the pale bone of the game's carcass skull (shapes/block/bone/carcass/head.json, same UV layout) rather than the
// clutter skull's brown one; the keys keep the clutter skull's names.
export const SKULL_TEXTURES = { human: "game:block/creature/skullhumanoid", "human-inside": "game:block/creature/insideskull-small" };

export default ({ box, joint, gameShape, statue, animationPose, prism }) => {
  // A game skull with its base centre at `at`, facing south turned by `turn` degrees about y (and tipped by `tilt` [x, z]).
  const vanillaSkull = (at, { turn = 0, tilt = [0, 0], name = "skull" } = {}) =>
    joint(at, { name, rot: [tilt[0], 90 + turn, tilt[1]], children: [joint([-0.5, 0, 0], { name: name + "centre", children: [structuredClone(SKULL)] })] });
  const lo = (a, b) => Math.min(a, b), hi = (a, b) => Math.max(a, b);

  // ---- Armored giant: power armor over a body twice a man's size. Pelvis at the joint; legs hang from it, the torso rises from
  // it. Pose: { torso, head, armE, armW: { sh, el, hold }, legE, legW: { hip, knee, foot } } (rotations), `seal` puts a purity seal
  // (wax and parchment) on the west pauldron. `hold` is a list of elements in the hand's frame (the hand at 0, -7, 0 of the
  // forearm, which points along -y).
  const armoredGiant = (pose) => {
    const arm = (s, a) => joint([s * 6.4, 8, 0], { name: "arm", rot: a.sh, children: [
      box([-1.6, -5.4, -1.6], [1.6, 0, 1.6], "armor", { name: "upperarm" }),
      box([-1.9, -6.2, -1.9], [1.9, -5, 1.9], "joint", { name: "elbow" }),
      joint([0, -5.6, 0], { name: "forearmjoint", rot: a.el, children: [
        box([-1.7, -5.2, -1.7], [1.7, 0, 1.7], "armor", { name: "forearm" }),
        box([-1.9, -7.8, -1.9], [1.9, -5.2, 1.9], "armor", { name: "gauntlet" }),
        ...(a.hold ?? []),
      ] }),
    ] });
    const pauldron = (s) => joint([s * 6.4, 8.6, 0], { name: "pauldron", rot: [0, 0, s * 10], children: [
      box([-3, -2, -3.4], [3, 2.4, 3.4], "armor", { name: "pauldron" }),
      box([-2.6, 2.4, -3], [2.6, 3.2, 3], "armor", { name: "pauldrontop" }),
      box([-3.2, -2.6, -3.6], [3.2, -1.8, 3.6], "trim", { name: "pauldronrim" }),
      ...(s < 0 && pose.seal ? [
        box([-1.1, -0.6, 3.4], [1.1, 1.6, 3.8], "wax", { name: "sealwax" }),
        ...[-0.7, 0.1].map((x, i) => box([x, -4.2 - i, 3.45], [x + 0.6, -0.2, 3.55], "parchment", { name: "sealstrip" })),
      ] : []),
    ] });
    const leg = (s, l) => joint([s * 2.2, -0.6, 0], { name: "leg", rot: l.hip, children: [
      box([-1.8, -7, -1.9], [1.8, 0, 1.9], "armor", { name: "thigh" }),
      box([-2, -3, 1.8], [2, 0.4, 2.4], "armor", { name: "tasset" }),
      joint([0, -7, 0], { name: "knee", rot: l.knee, children: [
        // The knee itself, round the pivot: fills the corner a bent knee leaves between thigh and shin, and holds the kneepad.
        box([-1.7, -1.9, -2], [1.7, 1.9, 2], "joint", { name: "kneejoint" }),
        box([-1.9, -1.2, 1.2], [1.9, 1.6, 2.7], "trim", { name: "kneepad" }),
        box([-1.8, -7, -1.9], [1.8, 0, 1.9], "armor", { name: "shin" }),
        joint([0, -7, 0], { name: "ankle", rot: l.foot ?? [0, 0, 0], children: [
          box([-2, -2.2, -2.2], [2, 0, 3.8], "armor", { name: "boot" }),
        ] }),
      ] }),
    ] });
    const head = joint([0, 9.6, 0.4], { name: "head", rot: pose.head, children: [
      box([-2.1, 0, -2.2], [2.1, 4.4, 2.3], "armor", { name: "helmet" }),
      box([-1, 0.3, 2.3], [1, 1.4, 2.7], "joint", { name: "grille" }),
      box([-1.7, 2.2, 2.3], [-0.4, 3, 2.45], "lens", { name: "lens" }),
      box([0.4, 2.2, 2.3], [1.7, 3, 2.45], "lens", { name: "lens" }),
    ] });
    const torso = joint([0, 1.2, 0], { name: "torso", rot: pose.torso, children: [
      box([-3.6, 0, -2.6], [3.6, 3, 2.6], "joint", { name: "abdomen" }),
      box([-4.6, 3, -3.2], [4.6, 9, 3.2], "armor", { name: "chest" }),
      box([-3, 5, 3.2], [3, 7.4, 3.7], "trim", { name: "chestemblem" }),
      box([-2.8, 8.6, -2.4], [2.8, 9.8, 2.4], "armor", { name: "collar" }),
      box([-4, 3.4, -6.6], [4, 10, -3.2], "armor", { name: "backpack" }),
      ...[-2.6, 2.6].map((x) => box([x - 0.8, 10, -6.2], [x + 0.8, 11.8, -4.6], "joint", { name: "exhaust" })),
      head, pauldron(1), pauldron(-1), arm(1, pose.armE), arm(-1, pose.armW),
    ] });
    return [
      box([-4, -1, -2.8], [4, 1.4, 2.8], "armor", { name: "pelvis" }),
      box([-0.9, -0.6, 2.8], [0.9, 1, 3.3], "trim", { name: "buckle" }),
      torso, leg(1, pose.legE), leg(-1, pose.legW),
    ];
  };
  // Held in an armored hand: a heavy gun along the forearm (barrel forward), and a sword hilt (blade down along -y).
  const gun = [
    box([-1.2, -12.5, -1.6], [1.2, -5.6, 1.6], "gunmetal", { name: "gunbody" }),
    box([-0.6, -16, -0.2], [0.6, -12.5, 1], "gunmetal", { name: "barrel" }),
    box([-0.9, -10.6, -4.4], [0.9, -8.2, -1.6], "gunmetal", { name: "magazine" }),
    box([-1.3, -7.6, 1.6], [1.3, -5.4, 2.2], "trim", { name: "gunplate" }),
  ];

  // ---- Artificer: a robed machine-priest. The robe is a stack of boxes narrowing upward; under the cowl a steel mask with one
  // dead lens. Mechanical arms (mechadendrites) come out of its back. Pose: { body, head, armE, armW: { sh, el, hold }, dendrites:
  // [{ at, rot, bend }] }. `kneel` makes the robe pool round its knees instead of hanging to the floor.
  const artificer = (pose) => {
    const sleeve = (s, a) => joint([s * 4.4, 12.2, 0], { name: "arm", rot: a.sh, children: [
      box([-1.4, -5.6, -1.4], [1.4, 0, 1.4], "robe", { name: "sleeve" }),
      joint([0, -5.4, 0], { name: "elbow", rot: a.el, children: [
        box([-1.5, -5, -1.5], [1.5, 0, 1.5], "robe", { name: "sleeve" }),
        box([-1.8, -6.6, -1.8], [1.8, -4.6, 1.8], "robe", { name: "cuff" }),
        box([-0.9, -8, -0.6], [0.9, -6.6, 0.6], "steel", { name: "hand" }),
        ...(a.hold ?? []),
      ] }),
    ] });
    const dendrite = (d) => joint(d.at, { name: "dendrite", rot: d.rot, children: [
      box([-0.6, 0, -0.6], [0.6, 6, 0.6], "iron", { name: "dendriteupper" }),
      box([-0.9, 5.4, -0.9], [0.9, 6.6, 0.9], "brass", { name: "dendritejoint" }),
      joint([0, 6, 0], { name: "dendritebend", rot: d.bend, children: [
        box([-0.5, 0, -0.5], [0.5, 5.5, 0.5], "iron", { name: "dendritelower" }),
        box([-0.9, 5.5, -0.3], [-0.4, 7, 0.3], "brass", { name: "claw" }),
        box([0.4, 5.5, -0.3], [0.9, 7, 0.3], "brass", { name: "claw" }),
      ] }),
    ] });
    const head = joint([0, 13.6, 0.2], { name: "head", rot: pose.head, children: [
      box([-2.6, 4, -2.6], [2.6, 5, 2], "robe", { name: "cowltop" }),
      box([-2.6, -0.4, -2.6], [-1.9, 4, 0.8], "robe", { name: "cowlwest" }),
      box([1.9, -0.4, -2.6], [2.6, 4, 0.8], "robe", { name: "cowleast" }),
      box([-1.9, -0.4, -2.6], [1.9, 4, -1.6], "robe", { name: "cowlback" }),
      box([-1.9, 0.2, -1.6], [1.9, 4, 1.3], "steel", { name: "mask" }),
      box([-1.5, 2.3, 1.3], [-0.3, 3.5, 1.6], "lens", { name: "lens", faces: { south: { texture: "lens", uv: "full" } } }),
      box([0.4, 2.4, 1.3], [1.4, 3.2, 1.4], "hollow", { name: "socket" }),
      box([-1.1, 0.5, 1.3], [1.1, 1.8, 1.9], "brass", { name: "grille" }),
    ] });
    const body = [
      box([-3.4, 0, -2.8], [3.4, 8.4, 2.2], "robe", { name: "robebody" }),
      box([-4.6, 11, -3], [4.6, 13.4, 2], "robe", { name: "shoulders" }),
      box([-3.6, 8.4, -2.9], [3.6, 11, 2.1], "robe", { name: "chest" }),
      box([-1.4, 8, 2.1], [1.4, 10.6, 2.6], "iron", { name: "chestplate" }),
      box([-3.6, 3.6, -3], [3.6, 4.6, 2.4], "brass", { name: "cord" }),
      box([-2.4, 6, -5.4], [2.4, 12, -2.8], "iron", { name: "backunit" }),
      head, sleeve(1, pose.armE), sleeve(-1, pose.armW), ...pose.dendrites.map(dendrite),
    ];
    // The robe below the waist: three tiers flaring to the hem (standing) or one pool round the knees (kneeling).
    const skirt = pose.kneel
      ? [box([-5, -6.5, -4], [5, -3.4, 6.5], "robe", { name: "robepool" }), box([-4.2, -3.4, -3.4], [4.2, 0, 4], "robe", { name: "robelap" })]
      : [box([-3.8, -4.6, -3], [3.8, 0, 2.6], "robe", { name: "robeupper" }),
         box([-4.3, -9.2, -3.4], [4.3, -4.6, 3], "robe", { name: "robemiddle" }),
         box([-4.9, -13.6, -3.9], [4.9, -9.2, 3.6], "robe", { name: "robehem" }),
         box([-3.2, -13.6, 3.4], [-1, -12.6, 5], "iron", { name: "boot" }), box([1, -13.6, 3.4], [3.2, -12.6, 5], "iron", { name: "boot" })];
    return [...skirt, joint([0, 0, 0], { name: "upperbody", rot: pose.body, children: body })];
  };
  // The artificer's staff: a pole with a cog on top and a game skull under the cog, held upright in the hand (pole along y).
  const staff = (len = 30) => {
    const top = len - 9;
    return [
      box([-0.45, -9, -0.45], [0.45, top, 0.45], "iron", { name: "staffpole" }),
      box([-2.6, top, -0.5], [2.6, top + 5.2, 0.5], "brass", { name: "cog" }),
      box([-2.6, top, -0.5], [2.6, top + 5.2, 0.5], "brass", { name: "cog", rot: [0, 0, 45], origin: [0, top + 2.6, 0] }),
      box([-1, top + 1.6, -0.6], [1, top + 3.6, 0.6], "hollow", { name: "coghub" }),
      vanillaSkull([0, top - 5.4, 0], { name: "staffskull" }),
    ];
  };

  // ---- Deathless warrior: a metal skeleton. Spine and ribcage, a skull with a ridged crown and two dim eyes, shoulder plates,
  // thin limbs with knob joints. Pose like the giant's. `noLegs` leaves the spine trailing from the ribcage (the crawler).
  const deathless = (pose) => {
    const limb = (name, len, w, kids = []) => [
      box([-w, -len, -w], [w, 0, w], "metal", { name }),
      box([-w - 0.35, -0.6, -w - 0.35], [w + 0.35, 0.6, w + 0.35], "dark", { name: name + "knob" }),
      ...kids,
    ];
    const arm = (s, a) => joint([s * 4.4, 9.6, 0], { name: "arm", rot: a.sh, children: limb("upperarm", 6, 0.55, [
      joint([0, -6, 0], { name: "elbow", rot: a.el, children: limb("forearm", 5.6, 0.5, [
        ...[-0.6, 0, 0.6].map((x) => box([x - 0.18, -8, -0.2], [x + 0.18, -5.6, 0.2], "metal", { name: "finger" })),
        ...(a.hold ?? []),
      ]) }),
    ]) });
    const leg = (s, l) => joint([s * 1.8, -0.4, 0], { name: "leg", rot: l.hip, children: limb("thigh", 7, 0.6, [
      joint([0, -7, 0], { name: "knee", rot: l.knee, children: limb("shin", 7, 0.55, [
        box([-1.2, -7.6, -1], [1.2, -6.8, 2.6], "metal", { name: "foot" }),
      ]) }),
    ]) });
    const head = joint([0, 10.8, 0.3], { name: "head", rot: pose.head, children: [
      box([-1.8, 0, -1.9], [1.8, 3.6, 1.9], "metal", { name: "skull" }),
      box([-1.4, -1.2, -0.6], [1.4, 0, 1.8], "metal", { name: "jaw" }),
      box([-2.1, 3.2, -2.3], [2.1, 4, 1.4], "metal", { name: "crown" }),
      box([-0.4, 3.6, -2.6], [0.4, 4.8, 1.2], "metal", { name: "crest" }),
      box([-2.2, 0.4, -1.4], [-1.8, 3.4, 1.2], "dark", { name: "cheek" }), box([1.8, 0.4, -1.4], [2.2, 3.4, 1.2], "dark", { name: "cheek" }),
      box([-1.4, 1.7, 1.9], [-0.4, 2.6, 2], "glow", { name: "eye" }), box([0.4, 1.7, 1.9], [1.4, 2.6, 2], "glow", { name: "eye" }),
      box([-1.1, -0.9, 1.8], [1.1, -0.5, 1.9], "dark", { name: "teeth" }),
    ] });
    const torso = joint([0, 0.6, 0], { name: "torso", rot: pose.torso, children: [
      box([-0.6, 0, -0.9], [0.6, 10, 0.3], "dark", { name: "spine" }),
      ...[4.2, 5.6, 7, 8.4].map((y) => box([-3.2, y, -1.2], [3.2, y + 0.7, 1.6], "metal", { name: "rib" })),
      box([-1.2, 4.6, 0.2], [1.2, 7.4, 1.4], "glow", { name: "core" }),
      box([-4.6, 9.2, -1.8], [4.6, 10.4, 1.8], "metal", { name: "collarbone" }),
      ...[-1, 1].map((s) => box([lo(s * 3.6, s * 6.2), 9.4, -2.2], [hi(s * 3.6, s * 6.2), 11.4, 2.2], "metal", { name: "shoulderplate", rot: [0, 0, s * -14], origin: [s * 4.4, 10.4, 0] })),
      head, arm(1, pose.armE), arm(-1, pose.armW),
    ] });
    return [
      box([-2.8, -1, -1.4], [2.8, 1, 1.4], "metal", { name: "pelvis" }),
      box([-2.2, -2, 0.8], [2.2, -0.6, 1.5], "dark", { name: "loincloth" }),
      torso,
      ...(pose.noLegs ? [box([-0.5, -6, -0.8], [0.5, -1, 0.2], "dark", { name: "spinetail", rot: [20, 0, 8], origin: [0, -1, 0] })]
        : [leg(1, pose.legE), leg(-1, pose.legW)]),
    ];
  };
  // The deathless rifle: a long frame with a blade under the barrel and a green tube along it (along the forearm, forward).
  const rifle = [
    box([-0.7, -13, -1], [0.7, -4.6, 1.2], "dark", { name: "riflebody" }),
    box([-0.4, -18, -0.2], [0.4, -13, 0.6], "metal", { name: "riflebarrel" }),
    box([-0.25, -18.5, -2.2], [0.25, -12.4, -1], "metal", { name: "rifleblade" }),
    box([-0.5, -12.6, 1.2], [0.5, -6.4, 2], "glow", { name: "rifletube" }),
  ];

  // ---- The poses -----------------------------------------------------------------------------------------------------------
  const giantTextures = (armor, trim = "armor-trim-brass") => ({ armor: T + armor, trim: T + trim, joint: T + "joint-worn", lens: T + "lens-dead",
    wax: T + "wax-worn", parchment: T + "parchment-worn", gunmetal: T + "automaton-dark", steel: T + "steel-dull",
    stone: T + "rubble-stone", ...SKULL_TEXTURES });

  // Kneeling vigil: on the east knee, head bowed, both hands on the pommel of a great sword planted point down before him.
  const sword = [
    box([7.2, 0, 13.2], [8.8, 9, 13.8], "steel", { name: "blade" }),
    box([5.4, 9, 12.9], [10.6, 10.2, 14.1], "trim", { name: "crossguard" }),
    box([7.4, 10.2, 13.1], [8.6, 13.4, 13.9], "joint", { name: "grip" }),
    box([7, 13.4, 12.9], [9, 14.6, 14.1], "trim", { name: "pommel" }),
  ];
  const giantKneeling = [
    joint([8, 9.8, 6], { name: "giant", children: armoredGiant({
      seal: true,
      torso: [6, 0, 0], head: [10, 0, 0],
      armE: { sh: [-22, 0, -16], el: [-52, 0, 0] }, armW: { sh: [-22, 0, 16], el: [-52, 0, 0] },
      legE: { hip: [0, 0, 0], knee: [92, 0, 0], foot: [-10, 0, 0] }, legW: { hip: [-86, 0, -4], knee: [88, 0, 0] },
    }) }),
    ...sword,
    vanillaSkull([2.5, 0, 14], { turn: 30, name: "groundskull" }),
  ];
  // On guard: standing, feet apart, the gun held across the chest, head up. Dead on his feet, the paint long faded.
  const giantGuard = [
    joint([8, 18, 7], { name: "giant", children: armoredGiant({
      torso: [4, 0, 0], head: [-4, 0, 0],
      armE: { sh: [-28, 0, 14], el: [-78, 0, 18], hold: gun }, armW: { sh: [-46, 0, -6], el: [-58, 0, -24] },
      legE: { hip: [0, 0, 6], knee: [0, 0, 0] }, legW: { hip: [-6, 0, -6], knee: [6, 0, 0] },
    }) }),
    vanillaSkull([13.5, 0, 13], { turn: -40, name: "groundskull" }),
  ];
  // Fallen face down, one arm reaching forward, the gun lying out of reach.
  const giantProne = [
    joint([8, 4.4, 4], { name: "giant", ground: true, rot: [86, 0, 6], children: armoredGiant({
      seal: true,
      torso: [0, 0, 0], head: [-30, 0, 20],
      armE: { sh: [-160, 0, 10], el: [-10, 0, 0] }, armW: { sh: [10, 0, 20], el: [-20, 0, 0] },
      legE: { hip: [0, 0, 4], knee: [-12, 0, 0] }, legW: { hip: [-8, 0, -6], knee: [-4, 0, 0] },
    }) }),
    joint([10, 1.6, 22], { name: "droppedgun", ground: true, rot: [0, 30, 90], children: gun }),
    vanillaSkull([-4, 0, 20], { turn: 70, name: "groundskull" }),
  ];

  const artificerTextures = { robe: T + "robe-red", iron: T + "iron-worn", brass: T + "bronze-dark", steel: T + "steel-dull",
    lens: T + "lens-dead", hollow: T + "hollow-dark", hose: T + "hose-grey", ...SKULL_TEXTURES };
  // Dead on its feet, leaning on its staff: body bowed over the staff in its east hand, a mechadendrite hanging slack.
  const artificerLeaning = [
    joint([8, 13.6, 7], { name: "artificer", children: artificer({
      body: [14, 0, 4], head: [26, 0, 8],
      armE: { sh: [-36, 0, -4], el: [-40, 0, 0] },
      armW: { sh: [-10, 0, 8], el: [-24, 0, 0] },
      dendrites: [{ at: [1.8, 10.6, -5.2], rot: [-150, 0, 30], bend: [-25, 0, 0] }, { at: [-1.8, 10.6, -5.2], rot: [-160, 0, -40], bend: [-15, 0, 0] }],
    }) }),
    // The staff, planted on the ground before it, its east hand on the pole below the skull.
    joint([12.6, 0, 14], { name: "staff", rot: [-8, 0, 2], children: [joint([0, 9, 0], { name: "staffbase", children: staff(30) })] }),
  ];
  // Kneeling before a skull set on a stone, hands raised to it, the mechadendrites drooping onto the ground behind.
  const artificerKneeling = [
    joint([8, 6.6, 5.5], { name: "artificer", children: artificer({
      kneel: true, body: [18, 0, 0], head: [30, 0, 0],
      armE: { sh: [-70, 0, -10], el: [-30, 0, 0] }, armW: { sh: [-70, 0, 10], el: [-30, 0, 0] },
      dendrites: [{ at: [1.8, 10.6, -5.2], rot: [-130, 0, 60], bend: [-45, 0, 0] }, { at: [-1.8, 10.6, -5.2], rot: [-130, 0, -60], bend: [-45, 0, 0] }],
    }) }),
    box([5.5, 0, 14.5], [10.5, 3.4, 18], "stone", { name: "altarstone" }),
    vanillaSkull([8, 3.4, 16.2], { name: "altarskull" }),
  ];

  const deathlessTextures = { metal: T + "deathless-metal", dark: T + "deathless-dark", glow: T + "deathless-glow" };
  // Dormant: standing, slightly bowed, the rifle held diagonally across the body.
  const deathlessStanding = [
    joint([8, 15.2, 7.5], { name: "deathless", children: deathless({
      torso: [10, 0, 0], head: [14, 0, 0],
      armE: { sh: [-24, 0, 6], el: [-66, 0, -40], hold: rifle }, armW: { sh: [-34, 0, -6], el: [-70, 0, 40] },
      legE: { hip: [-6, 0, 4], knee: [10, 0, 0] }, legW: { hip: [4, 0, -4], knee: [6, 0, 0] },
    }) }),
  ];
  // Crawling: legs gone, the torso dragging itself forward on its arms, head raised; the rifle left behind.
  const deathlessCrawler = [
    joint([8, 4.2, 4], { name: "deathless", ground: true, rot: [78, 0, 0], children: deathless({
      noLegs: true, torso: [0, 0, 0], head: [-50, 0, 0],
      armE: { sh: [-150, 0, 18], el: [60, 0, 0] }, armW: { sh: [-120, 0, -16], el: [80, 0, 0] },
    }) }),
    joint([3, 1, -2], { name: "droppedrifle", ground: true, rot: [0, -20, 90], children: rifle }),
    // The legs it left behind: a thigh and shin still joined, lying in the dirt.
    joint([15, 1, 1], { name: "lostleg", ground: true, rot: [0, 30, 90], children: [
      box([-0.6, -7, -0.6], [0.6, 0, 0.6], "metal", { name: "lostthigh" }),
      box([-0.55, -14, -0.55], [0.55, -7, 0.55], "metal", { name: "lostshin" }),
      box([-0.95, -7.6, -0.95], [0.95, -6.4, 0.95], "dark", { name: "lostknee" }),
    ] }),
  ];
  // Collapsed: fallen to its knees, slumped forward, one hand on the ground, the rifle across its thighs.
  const deathlessKneeling = [
    joint([8, 8.4, 4.5], { name: "deathless", children: deathless({
      torso: [34, 0, 6], head: [20, 0, -12],
      armE: { sh: [-20, 0, 6], el: [-30, 0, 0] }, armW: { sh: [-62, 0, -10], el: [10, 0, 0] },
      legE: { hip: [-12, 0, 6], knee: [96, 0, 0] }, legW: { hip: [-22, 0, -6], knee: [104, 0, 0] },
    }) }),
    joint([3.5, 1.2, 13], { name: "droppedrifle", ground: true, rot: [0, 70, 90], children: rifle }),
  ];

  // The last clash: an armored giant and a deathless warrior who killed each other and never fell. The giant, down on his west
  // knee (he faces north, the deathless south), has run his sword through the deathless's glowing core, the point out of its
  // back; the deathless, braced over him, has driven its rifle blade into his chest. His gun lies where he dropped it.
  const giantSword = [
    box([-0.6, -9.2, -0.6], [0.6, -5.8, 0.6], "joint", { name: "grip" }),
    box([-0.9, -10.4, -2.8], [0.9, -9.2, 2.8], "trim", { name: "crossguard" }),
    box([-0.3, -22, -1.1], [0.3, -10.4, 1.1], "steel", { name: "blade" }),
    box([-0.25, -23.2, -0.6], [0.25, -22, 0.6], "steel", { name: "bladetip" }),
  ];
  const lastClash = [
    joint([8, 9.8, 24], { name: "giant", rot: [0, 180, 0], children: armoredGiant({
      seal: true,
      torso: [16, 0, 0], head: [-14, 0, 0],
      armE: { sh: [-101, 0, -14], el: [-8, 0, 0], hold: giantSword },
      armW: { sh: [-75, 0, 22], el: [-40, 0, 0] },
      legE: { hip: [-78, 0, 4], knee: [80, 0, 0] }, legW: { hip: [0, 0, 0], knee: [92, 0, 0], foot: [-10, 0, 0] },
    }) }),
    joint([8, 13.3, -4], { name: "deathless", children: deathless({
      torso: [22, 0, 0], head: [-6, 0, 0],
      armE: { sh: [-62, 0, -10], el: [-43, 0, 0], hold: rifle }, armW: { sh: [-58, 0, -10], el: [-34, 0, 30] },
      legE: { hip: [-18, 0, 6], knee: [24, 0, 0] }, legW: { hip: [22, 0, -4], knee: [8, 0, 0] },
    }) }),
    joint([13, -0.33, 21], { name: "droppedgun", rot: [0, -60, 90], children: gun }),
  ];

  // ---- Furniture and curios for building a ruin. They use the game's own textures wherever one fits (stone, brick, wood, iron,
  // brass, cloth, candle wax, paper); only what the game has nothing for keeps the mod's own (the dead screen, wax seals, parchment).
  const G = "game:block/";
  const ruinTextures = {
    stone: G + "stone/polishedrock-old/full/basalt1", brick: G + "stone/agedbrick/basalt1", skullcobble: G + "stone/cobbleskull/basalt",
    marble: G + "stone/rock/whitemarble1", rock: G + "stone/rock/basalt1", wood: G + "wood/planks/rottenebony1",
    iron: G + "metal/riveted/iron1", rust: G + "metal/corroded/rusty1", brass: G + "metal/tarnished/brass", chain: G + "metal/tarnished/chain-rust",
    red: G + "cloth/basic/red", black: G + "cloth/basic/black", wax: G + "candle", paper: G + "cloth/paper/blank-small",
    screen: T + "screen-dead", seal: T + "wax-worn", parchment: T + "parchment-worn", hollow: T + "hollow-dark", ...SKULL_TEXTURES,
  };
  // A candle of height h at (x, z) standing on y, with a drip down its side and a pool of wax at its foot. Its faces map the
  // game's candle texture as the game's candle does (shapes/block/wax/candle.json): the sides from the wax strip along the
  // bottom of the texture, the top from its corner.
  const waxFaces = { north: { texture: "wax", uv: [0, 12, 2, 16] }, east: { texture: "wax", uv: [2, 12, 4, 16] }, south: { texture: "wax", uv: [4, 12, 6, 16] },
    west: { texture: "wax", uv: [6, 12, 8, 16] }, up: { texture: "wax", uv: [14, 0, 16, 2] }, down: { texture: "wax", uv: [14, 0, 16, 2] } };
  const candle = (x, y, z, h, w = 0.6) => [
    box([x - w, y, z - w], [x + w, y + h, z + w], "wax", { name: "candle", faces: waxFaces }),
    box([x - 0.15, y + h, z - 0.15], [x + 0.15, y + h + 0.6, z + 0.15], "black", { name: "wick" }),
    box([x + w, y + h * 0.4, z - 0.25], [x + w + 0.25, y + h - 0.2, z + 0.25], "wax", { name: "drip", faces: waxFaces }),
    box([x - w - 0.5, y, z - w - 0.5], [x + w + 0.5, y + 0.25, z + w + 0.5], "wax", { name: "waxpool", faces: waxFaces }),
  ];
  // A cog: two crossed squares and a dark hub, facing south, centred at (x, y, z), r across.
  const cog = (x, y, z, r, d = 0.5) => [
    box([x - r, y - r, z - d], [x + r, y + r, z], "brass", { name: "cog" }),
    box([x - r, y - r, z - d], [x + r, y + r, z], "brass", { name: "cog", rot: [0, 0, 45], origin: [x, y, z] }),
    box([x - r * 0.4, y - r * 0.4, z], [x + r * 0.4, y + r * 0.4, z + 0.15], "hollow", { name: "coghub" }),
  ];
  // A purity seal: a wax disc and parchment strips hanging from it, on a south-facing surface at z.
  const seal = (x, y, z) => [
    box([x - 0.9, y - 0.9, z], [x + 0.9, y + 0.9, z + 0.3], "seal", { name: "sealwax" }),
    box([x - 0.7, y - 4, z + 0.05], [x - 0.15, y - 0.5, z + 0.12], "parchment", { name: "sealstrip" }),
    box([x + 0.1, y - 3.2, z + 0.05], [x + 0.65, y - 0.5, z + 0.12], "parchment", { name: "sealstrip" }),
  ];
  // A book: covers of `tex`, the page block between them, lying on y.
  const book = (x, y, z, w, d, h, tex, turn = 0) => joint([x, y, z], { name: "book", rot: [0, turn, 0], children: [
    box([-w / 2, 0, -d / 2], [w / 2, h, d / 2], tex, { name: "cover" }),
    box([-w / 2 + 0.2, 0.15, -d / 2 + 0.3], [w / 2 + 0.05, h - 0.15, d / 2 + 0.05], "paper", { name: "pages" }),
  ] });

  // Lectern: a stone pedestal, a slanted reading desk of dark wood, a great book open on it, a skull and a candle beside it.
  const lectern = [
    box([5, 0, 5], [11, 1.2, 11], "stone", { name: "base" }),
    box([6.4, 1.2, 6.4], [9.6, 12, 9.6], "stone", { name: "column" }),
    box([5.6, 11, 5.6], [10.4, 12.6, 10.4], "stone", { name: "capital" }),
    joint([8, 12.6, 8], { name: "desk", rot: [24, 0, 0], children: [
      box([-5, 0, -4], [5, 1, 4], "wood", { name: "desk" }),
      box([-5, 1, 3.4], [5, 1.8, 4], "wood", { name: "booklip" }),
      box([-4.4, 1, -3.4], [-0.1, 1.6, 3.4], "paper", { name: "pageleft" }),
      box([0.1, 1, -3.4], [4.4, 1.6, 3.4], "paper", { name: "pageright" }),
      box([-4.6, 1, -3.6], [4.6, 1.15, 3.6], "red", { name: "cover" }),
      box([-0.1, 1, -3.4], [0.1, 1.9, 3.4], "red", { name: "spine" }),
      box([2.2, 1.6, -3], [2.5, 1.7, 2.8], "red", { name: "ribbon" }),
    ] }),
    vanillaSkull([3, 0, 12.5], { turn: 25, name: "floorskull" }),
    ...candle(12.6, 0, 12.2, 4.4),
  ];

  // Candelabrum: a tall iron stand on three feet with five candles at different heights, wax run down onto the floor.
  const candelabrum = [
    ...[0, 120, 240].map((d) => joint([8, 0, 8], { name: "foot", rot: [0, d, 0], children: [box([-0.5, 0, 0], [0.5, 1, 4.5], "iron", { name: "foot" })] })),
    box([7.4, 0, 7.4], [8.6, 16, 8.6], "iron", { name: "stem" }),
    box([2.6, 16, 7.5], [13.4, 17, 8.5], "iron", { name: "arms" }),
    box([7.5, 16, 4.6], [8.5, 17, 11.4], "iron", { name: "arms" }),
    ...[[3.2, 8, 17, 5], [12.8, 8, 17, 3.4], [8, 5.2, 17, 4.2], [8, 10.8, 17, 2.4], [8, 8, 17, 6.2]].flatMap(([x, z, y, h]) =>
      [box([x - 1, y, z - 1], [x + 1, y + 0.6, z + 1], "brass", { name: "cup" }), ...candle(x, y + 0.6, z, h, 0.55)]),
    box([5, 0, 9], [9.5, 0.3, 13], "wax", { name: "floorwax", faces: waxFaces }),
  ];

  // Machine altar: a block of old brick under a red runner, a brass cog on its front, a skull on top between two candles.
  const machineAltar = [
    box([1, 0, 4], [15, 9, 12], "brick", { name: "altar" }),
    box([0.4, 9, 3.4], [15.6, 10.4, 12.6], "stone", { name: "altartop" }),
    box([5, 10.4, 3.2], [11, 10.6, 12.8], "red", { name: "runner" }),
    box([5, 3, 12.6], [11, 10.6, 12.8], "red", { name: "runnerfront" }),
    box([5.4, 2.4, 12.6], [6.6, 3, 12.8], "red", { name: "runnerfringe" }), box([7.4, 2.2, 12.6], [8.6, 3, 12.8], "red", { name: "runnerfringe" }), box([9.4, 2.5, 12.6], [10.6, 3, 12.8], "red", { name: "runnerfringe" }),
    ...cog(8, 6.6, 13.4, 2.2),
    vanillaSkull([8, 10.6, 7.5], { name: "altarskull" }),
    ...candle(3, 10.4, 7, 5), ...candle(13, 10.4, 8, 3.6), ...candle(4.4, 10.4, 10.2, 2.2),
  ];

  // Dead cogitator: an iron console with a slanted face, a dark screen, rows of brass keys, a cog on its side, cables to the floor.
  const cogitator = [
    box([2, 0, 4], [14, 10, 11], "iron", { name: "cabinet" }),
    box([2.6, 10, 4.6], [13.4, 15, 8], "iron", { name: "screenhousing" }),
    box([4, 10.8, 8], [12, 14.2, 8.3], "screen", { name: "screen", faces: { south: { texture: "screen", uv: "full" } } }),
    joint([8, 10, 11], { name: "keyboard", rot: [-30, 0, 0], children: [
      box([-6, -0.6, -3], [6, 0, 0], "rust", { name: "keyplate" }),
      ...[0, 1, 2].flatMap((r) => [-4.5, -2.5, -0.5, 1.5, 3.5].map((x) => box([x - 0.7, 0, -2.6 + r], [x + 0.7, 0.5, -1.9 + r], "brass", { name: "key" }))),
    ] }),
    ...cog(14.6, 6, 7.5, 2, 0.5).map((e) => ({ ...e, rot: [0, 90, (e.rot ?? [0, 0, 0])[2]], origin: [14.6, 6, 7.5] })),
    box([3, 0, 2], [4, 1, 4], "black", { name: "cable" }), box([-2, 0, 2], [3, 1, 3], "black", { name: "cable" }),
    box([11, 0, 1.4], [12, 1, 4], "black", { name: "cable" }), box([12, 0, 1.4], [17, 1, 2.4], "black", { name: "cable" }),
    ...seal(5, 9, 11),
  ];

  // Broken pew: a high-backed bench of dark wood, its east legs given way so it leans, the back cracked and a plank fallen.
  const brokenPew = [
    joint([0, 0, 4], { name: "pew", rot: [0, 0, 7], origin: [0, 0, 4], children: [
      box([0.5, 4.4, 0], [15.5, 5.4, 5], "wood", { name: "seat" }),
      box([0.5, 5.4, 0], [15.5, 12.5, 1], "wood", { name: "back" }),
      box([0, 0, -0.2], [1.2, 13, 5.2], "wood", { name: "endwest" }),
      box([14.8, 1.6, -0.2], [16, 13, 5.2], "wood", { name: "endeast" }),
      box([0.5, 12.5, -0.2], [15.5, 13.4, 1.2], "wood", { name: "backrail" }),
      box([8.6, 5.4, -0.1], [9.1, 12.5, 1.1], "hollow", { name: "crack" }),
    ] }),
    box([5, 0, 10], [14, 0.9, 11.4], "wood", { name: "fallenplank", rot: [0, -16, 0] }),
    box([13, 0, 2], [14.6, 1.6, 5.6], "wood", { name: "brokenleg", rot: [0, 30, 90], origin: [13.8, 0.8, 3.8] }),
  ];

  // Skull niche: a section of brick wall with an arched opening, its back of skull-set cobble, four skulls stacked two by two
  // inside, another fallen to the floor.
  const skullNiche = [
    box([0, 0, 0], [3, 16, 4], "brick", { name: "wall" }), box([13, 0, 0], [16, 16, 4], "brick", { name: "wall" }),
    box([3, 0, 0], [13, 2, 4], "brick", { name: "wall" }), box([3, 12.6, 0], [13, 16, 4], "brick", { name: "wall" }),
    box([3, 11.4, 0], [4.4, 12.6, 4], "brick", { name: "archstep" }), box([11.6, 11.4, 0], [13, 12.6, 4], "brick", { name: "archstep" }),
    box([3, 2, 0], [13, 12.6, 0.8], "skullcobble", { name: "nicheback" }),
    box([0, 16, -0.4], [16, 17.4, 4.6], "stone", { name: "coping" }),
    box([2.4, 1.4, 3.8], [13.6, 2, 5.4], "stone", { name: "sill" }),
    vanillaSkull([5.6, 2, 3], { turn: 6, name: "nicheskull" }), vanillaSkull([10.6, 2, 3], { turn: -8, name: "nicheskull" }),
    vanillaSkull([5.6, 7, 3], { turn: -4, name: "nicheskull" }), vanillaSkull([10.4, 7, 3], { turn: 10, name: "nicheskull" }),
    vanillaSkull([8, 0, 11], { turn: 35, tilt: [0, 18], name: "floorskull" }),
  ];

  // Sealed munitions crate: a riveted iron crate, lid knocked ajar, brass shells inside, a purity seal on the front.
  const munitionsCrate = [
    box([2, 0, 3], [14, 7, 12], "iron", { name: "crate" }),
    box([2.2, 0, 11.9], [13.8, 1, 12.2], "rust", { name: "kick" }),
    box([6.4, 7, 2.4], [18.6, 8.2, 11.6], "iron", { name: "lid", rot: [0, 14, 0], origin: [12.5, 7.6, 7] }),
    ...[0, 1, 2].flatMap((i) => [0, 1].map((j) => box([3 + i * 2.2, 3, 4.4 + j * 3.6], [4.4 + i * 2.2, 7.8, 6 + j * 3.6], "brass", { name: "shell" }))),
    ...seal(10.5, 5.6, 12),
    box([3.6, 4.4, 12], [8.6, 5.6, 12.1], "black", { name: "stencil" }),
  ];

  // Cold brazier: an iron bowl on three legs, full of ash and a few charred lumps, long gone cold.
  const brazier = [
    ...[0, 120, 240].map((d) => joint([8, 0, 8], { name: "leg", rot: [0, d, 0], children: [box([-0.5, 0, 3.4], [0.5, 8, 4.4], "iron", { name: "leg", rot: [-14, 0, 0], origin: [0, 0, 3.9] })] })),
    // The bowl: two crossed boxes (eight-sided), narrower at the foot; ash heaped in it.
    box([4.4, 7.4, 4.4], [11.6, 8.6, 11.6], "rust", { name: "bowlfoot" }),
    box([4.4, 7.4, 4.4], [11.6, 8.6, 11.6], "rust", { name: "bowlfoot", rot: [0, 45, 0], origin: [8, 8, 8] }),
    box([3, 8.6, 3], [13, 10.6, 13], "rust", { name: "bowl" }),
    box([3, 8.6, 3], [13, 10.6, 13], "rust", { name: "bowl", rot: [0, 45, 0], origin: [8, 9.6, 8] }),
    box([3.8, 10.6, 3.8], [12.2, 11, 12.2], "rock", { name: "ash" }),
    box([3.8, 10.6, 3.8], [12.2, 11, 12.2], "rock", { name: "ash", rot: [0, 45, 0], origin: [8, 10.8, 8] }),
    box([5, 11, 6], [7.4, 11.9, 8], "black", { name: "charred" }), box([8.6, 11, 8.4], [10.4, 11.7, 10.6], "black", { name: "charred" }),
  ];

  // Fallen saint's head: the hooded marble head of a great statue, broken off at the neck and lying on its side, a hand beside it.
  const saintHead = [
    joint([8, 5.6, 8], { name: "head", rot: [-70, 24, 8], children: [
      box([-4, -5, -4.2], [4, 5, 4], "marble", { name: "head" }),
      box([-5, -3, -5], [5, 7, 3.4], "marble", { name: "hood" }),
      box([-4.6, 6, -4.6], [4.6, 7.4, 2.4], "marble", { name: "hoodtop" }),
      box([-1, -1, 4], [1, 2, 5.2], "marble", { name: "nose" }),
      box([-3, 1.8, 3.9], [-1, 2.8, 4.1], "hollow", { name: "eye" }), box([1, 1.8, 3.9], [3, 2.8, 4.1], "hollow", { name: "eye" }),
      box([-2, -3.4, 3.9], [2, -2.8, 4.1], "hollow", { name: "mouth" }),
      box([-2.6, -8, -2.6], [2.6, -5, 2.6], "marble", { name: "neckstump" }),
      box([-1.8, -8.4, -1.8], [1.8, -7.6, 1.8], "rock", { name: "break" }),
    ] }),
    joint([2, 1, 14], { name: "hand", rot: [0, -40, 90], children: [
      box([-1.6, -1, -0.8], [1.6, 2, 0.8], "marble", { name: "palm" }),
      ...[-1.2, -0.4, 0.4, 1.2].map((x) => box([x - 0.35, 2, -0.4], [x + 0.35, 4.4, 0.4], "marble", { name: "finger" })),
    ] }),
  ];

  // Broken pillar: a stepped plinth and the stump of a column, a band of skulls carved round it, its top broken off and lying beside.
  const brokenPillar = [
    box([2, 0, 2], [14, 1.6, 14], "stone", { name: "plinth" }),
    box([3, 1.6, 3], [13, 3, 13], "stone", { name: "plinthstep" }),
    box([4.5, 3, 4.5], [11.5, 14, 11.5], "marble", { name: "column" }),
    box([4.3, 7, 4.3], [11.7, 9.6, 11.7], "skullcobble", { name: "skullband" }),
    box([5.4, 14, 5.4], [10.6, 16, 9.6], "marble", { name: "jaggedtop" }),
    box([6.6, 16, 6], [9.4, 17.2, 8.4], "marble", { name: "jaggedtop" }),
    box([13, 0, 9], [20, 7, 16], "marble", { name: "fallentop", rot: [0, 24, 90], origin: [16.5, 3.5, 12.5] }),
    box([-2, 0, 13], [1.4, 1.8, 16], "rock", { name: "rubble", rot: [0, 30, 0] }),
  ];

  // Scattered data-slates: iron tablets with dead screens, a rolled scroll, a loose purity seal and a toppled candle on the floor.
  const slate = (x, z, turn) => joint([x, 0, z], { name: "slate", rot: [0, turn, 0], children: [
    box([-2, 0, -2.8], [2, 0.6, 2.8], "iron", { name: "slate" }),
    box([-1.5, 0.6, -2.2], [1.5, 0.7, 1.6], "screen", { name: "slatescreen", faces: { up: { texture: "screen", uv: "full" } } }),
  ] });
  const dataSlates = [
    slate(4, 5, 20), slate(10.5, 7, -35), slate(7, 12, 70),
    joint([12, 0.6, 13], { name: "scroll", rot: [0, 25, 0], children: [
      box([-3.4, -0.6, -0.6], [3.4, 0.6, 0.6], "paper", { name: "scroll" }),
      box([-4, -0.8, -0.8], [-3.4, 0.8, 0.8], "wood", { name: "rod" }), box([3.4, -0.8, -0.8], [4, 0.8, 0.8], "wood", { name: "rod" }),
    ] }),
    box([1.6, 0, 11.4], [3.4, 0.3, 13.2], "seal", { name: "sealwax" }),
    box([1.8, 0, 13.2], [2.4, 0.1, 15.4], "parchment", { name: "sealstrip" }), box([2.6, 0, 13.2], [3.2, 0.1, 14.8], "parchment", { name: "sealstrip" }),
    joint([13.4, 0.6, 3], { name: "toppledcandle", rot: [0, -30, 90], children: candle(0, 0, 0, 4, 0.6).slice(0, 2) }),
    book(4, 0, 0.6 + 1.5, 4.4, 3, 1.2, "red", 12),
  ];

  // ---- Angel statues, made from the game's player model the way the Player Statues mod makes its statues: the seraph's elements
  // are posed by baking a keyframe of one of its own animations into their rotations and positions (StatuePose.Apply), every face
  // points at one marble tile sized by the face (StatuePose.UseSingleTile), the whole model is scaled by k and turned to face south
  // (the seraph faces west); vsdev's gameShape and statue do this. Wings are added to the upper torso so they follow the pose; the
  // tools hidden in its hand anchors and the attachment points are dropped. Small: the "cry" animation with its hands at its face, still standing: a weeping angel. Large: hands
  // held together before it ("holdbothhands") on the pommel of a gilt sword planted point down.
  const seraph = gameShape("entity/humanoid/seraph-faceless");
  // Standing weeping: the hands and head of "cry" once they reach the face, the legs as they were before it sinks to its knees.
  const standingCry = () => {
    const pose = animationPose(seraph, "cry", 3), start = animationPose(seraph, "cry", 0);
    for (const leg of ["UpperFootL", "LowerFootL", "UpperFootR", "LowerFootR", "LowerTorso"]) pose[leg] = start[leg];
    return pose;
  };
  // The wings, in their own frame (x out to the statue's left, y up, z its front), drawn at scale w; turned into the seraph's
  // frame (it faces -x, its left is +z) and set on its upper back.
  const wings = (w) => {
    const v = (a) => a.map((x) => x * w);
    const B = (f, t, tex, o = {}) => box(v(f), v(t), tex, { ...o, ...(o.origin ? { origin: v(o.origin) } : {}) });
    const J = (at, o) => joint(v(at), o);
    // Each wing grows from a root block set into the shoulder blade; its leading edge starts at the root and rises outward.
    const wing = (sd) => J([sd * 0.9, 0, 0.3], { name: "wing", rot: [0, sd * 30, 0], children: [
      B([lo(-0.6, sd * 1.8), -1.2, -0.9], [hi(-0.6, sd * 1.8), 1.8, 0.9], "marble", { name: "wingroot" }),
      B([lo(0, sd * 8.6), 0.4, -0.5], [hi(0, sd * 8.6), 1.6, 0.5], "marble", { name: "wingedge", rot: [0, 0, sd * 24], origin: [0, 1, 0] }),
      ...[0, 1, 2, 3, 4, 5].map((i) => {
        const x = sd * (1 + i * 1.4), top = 0.8 + i * 0.62 * 1.4, len = 6 + i * 0.9, z0 = i % 2 ? 0.05 : -0.35;
        return J([x, top, 0], { name: "flightfeather", rot: [0, 0, sd * (4 + i * 3)], children: [
          B([-0.75, -len * 0.72, z0], [0.75, 0, z0 + 0.4], "marble", { name: "vane" }),
          B([-0.45, -len * 0.9, z0], [0.45, -len * 0.72, z0 + 0.4], "marble", { name: "vanetip" }),
          B([-0.2, -len, z0], [0.2, -len * 0.9, z0 + 0.4], "marble", { name: "vanetip" }),
        ] });
      }),
      ...[0, 1, 2, 3, 4].map((i) => {
        const x = sd * (1.6 + i * 1.5), top = 1.2 + i * 0.62 * 1.5;
        return B([x - 0.9, top - 3.2, 0.4], [x + 0.9, top, 0.8], "marble", { name: "covert" });
      }),
    ] });
    return joint([5.5 * 0, 0, 0], { name: "wings", rot: [0, -90, 0], children: [wing(1), wing(-1)] });
  };
  // A statue: plinth, the posed seraph at scale k standing on it facing south, wings on its back (attached to the upper torso,
  // in the seraph's own units: vsdev's statue scales them with it).
  const seraphStatue = (pose, k, extraTop = []) => {
    const torso = seraph.elements[0].children.find((c) => c.name === "LowerTorso").children.find((c) => c.name === "UpperTorso");
    const att = torso.children.find((c) => c.name === "UpperBackAttachment").from;
    const mount = [att[0] - 0.6, att[1] + 2.2, att[2]]; // the shoulder blades: the back surface, above the back attachment point
    const root = statue(seraph, { pose, texture: "marble", scale: k, drop: /^(Knife|saw|Shovel)|Attachment$/,
      attach: { UpperTorso: [joint(mount, { name: "wingmount", children: [wings(1.5)] })] } });
    const ph = 2.5 * 1.5 * k, pw = 5.2 * 1.5 * k;
    return [
      box([8 - pw, 0, 8 - pw], [8 + pw, ph * 0.64, 8 + pw], "stone", { name: "plinth" }),
      box([8 - pw * 0.84, ph * 0.64, 8 - pw * 0.84], [8 + pw * 0.84, ph, 8 + pw * 0.84], "stone", { name: "plinthstep" }),
      joint([8, ph, 8], { name: "statue", rot: [0, 90, 0], children: [joint([-8.5 * k, 0, -8 * k], { name: "statuecentre", children: root })] }),
      ...extraTop,
    ];
  };
  const angelSmall = seraphStatue(standingCry(), 0.6);
  const angelLarge = seraphStatue(animationPose(seraph, "holdbothhands", 0), 1.45, [
    box([7, 5.4, 14], [9, 27, 15.4], "brass", { name: "blade" }),
    box([3.6, 27, 13.6], [12.4, 28.6, 15.8], "brass", { name: "crossguard" }),
    box([7.3, 28.6, 14.2], [8.7, 31.6, 15.2], "brass", { name: "grip" }),
    box([6.6, 31.6, 13.8], [9.4, 33.4, 15.6], "brass", { name: "pommel" }),
  ]);

  // ---- Domed war casing: a long-dead war machine shaped like a pepper pot. An eight-sided skirt tapering upward, studded with
  // blue hemispheres; a shoulder band of vertical slats over a dark core with a gun arm; a neck of three rings over a dark core; a
  // stepped dome with two lamps and an eyestalk. Dead: the eyestalk drooping, the plunger arm snapped off and lying at its foot,
  // one hemisphere gone. All boxes. An eight-sided section is eight flat panels round a core (a box and its
  // 45 degree copy would make an eight-pointed star); the skirt's panels lean inward, the hemispheres sit on them. Textures are
  // the game's own (stainless and plain steel sheet, lead sheet, blue leather, yellow glass, black cloth).
  const warTextures = { body: G + "metal/sheet/stainlesssteel1", steel: G + "metal/sheet/steel1", fender: G + "metal/sheet/lead1",
    stud: G + "leather/blue", lamp: G + "glass/yellow", black: G + "cloth/basic/black", hollow: T + "hollow-dark" };
  // An eight-sided section of apothem h from y0 to y1 (vsdev's prism), its panels `t` thick, leaning in by `lean` degrees; `kids(a)`
  // adds elements to the panel facing a (in the panel's frame: x across, y up its face, z out of it).
  const oct = (name, h, y0, y1, tex, { t = 1, lean = 0, core = true, kids = () => [] } = {}) =>
    prism({ name, apothem: h, y: [y0, y1], thickness: t, lean, core, tex, panel: (i, a) => kids(a) });
  const skirtLean = (Math.atan((6.4 - 4.6) / 11.2) * 180) / Math.PI;
  const warCasing = [
    ...oct("fender", 7, 0, 1.6, "fender"),
    // The skirt: one ring of panels leaning in; on each, a column of four hemispheres (the third on the north-east face is gone).
    ...oct("skirt", 6.4, 1.6, 12.8, "body", { t: 1.2, lean: skirtLean, core: false, kids: (a) => [1.6, 4.4, 7.2, 9.9].filter((y, r) => !(a === 135 && r === 2)).map((y) =>
      box([-0.8, y - 0.8, -0.2], [0.8, y + 0.8, 0.9], "stud", { name: "stud" })) }),
    // Shoulders: a collar, a dark core, sixteen slats round it.
    ...oct("collar", 5, 12.6, 13.4, "steel"),
    ...oct("shoulder", 4.2, 13.4, 17.6, "hollow"),
    ...Array.from({ length: 16 }, (_, i) => box([7.45, 13.6, 12.2], [8.55, 17.4, 12.7], "body", { name: "slat", ...(i ? { rot: [0, i * 22.5, 0], origin: [8, 13.6, 8] } : {}) })),
    // The gun arm with a wire cage round it; the plunger arm's snapped stub.
    box([9.6, 14.4, 12], [10.4, 15.2, 18.4], "steel", { name: "gun" }),
    ...[13.4, 14.8, 16.2].map((z) => box([9.1, 13.9, z], [10.9, 15.7, z + 0.3], "steel", { name: "guncage" })),
    box([9.1, 15.4, 13.4], [9.4, 15.7, 16.5], "steel", { name: "guncagebar" }), box([10.6, 13.9, 13.4], [10.9, 14.2, 16.5], "steel", { name: "guncagebar" }),
    box([5.6, 14.4, 12], [6.4, 15.2, 13.8], "steel", { name: "plungerstub" }),
    // The neck: a dark core, three rings, four posts.
    box([4.6, 17.6, 4.6], [11.4, 23, 11.4], "hollow", { name: "neckcore" }),
    ...[18.4, 20, 21.6].flatMap((y) => oct("neckring", 4.3, y, y + 0.6, "body", { t: 0.9, core: false })),
    ...[0, 90, 180, 270].map((a) => box([7.7, 17.6, 11.3], [8.3, 23, 11.7], "steel", { name: "neckpost", ...(a ? { rot: [0, a, 0], origin: [8, 17.6, 8] } : {}) })),
    // The dome, stepped in toward the top, two lamps, and the eyestalk drooping toward the ground.
    ...oct("dome", 4.2, 23, 24.6, "body"), ...oct("dome", 3.6, 24.6, 25.8, "body"), ...oct("dome", 2.7, 25.8, 26.8, "body"),
    box([6.6, 26.8, 6.6], [9.4, 27.4, 9.4], "body", { name: "domecap" }),
    box([4.4, 25.4, 7.4], [5.4, 27.2, 8.4], "lamp", { name: "lamp" }), box([10.6, 25.4, 7.4], [11.6, 27.2, 8.4], "lamp", { name: "lamp" }),
    joint([8, 25.2, 11.4], { name: "eyestalk", rot: [26, 0, 0], children: [
      box([-0.3, -0.3, 0], [0.3, 0.3, 7], "steel", { name: "stalk" }),
      ...[2.6, 3.4, 4.2].map((z) => box([-1, -1, z], [1, 1, z + 0.35], "stud", { name: "stalkdisc" })),
      box([-0.9, -0.9, 7], [0.9, 0.9, 8.6], "black", { name: "eye" }),
    ] }),
    // The plunger arm, snapped off and lying at its foot.
    joint([3, 0.4, 14], { name: "fallenplunger", rot: [0, -30, 0], children: [
      box([-0.4, 0, -0.4], [0.4, 0.8, 6], "steel", { name: "plungerrod" }),
      box([-1.3, -0.4, 6], [1.3, 1.9, 7.4], "black", { name: "plungercup" }),
    ] }),
  ];

  // ---- Broken war casing: the same machine toppled onto its side, the dome broken off and lying beside it (its dark inside
  // showing), the eyestalk snapped off in the dirt, hemispheres knocked loose, the plunger arm lost. Built from the standing
  // casing's own parts, so the two always match.
  const casingBody = warCasing.filter((e) => !/^(dome|lamp|eyestalk|fallenplunger)/.test(e.name));
  const casingDome = warCasing.filter((e) => /^(dome|lamp)/.test(e.name));
  const casingStalk = warCasing.find((e) => e.name === "eyestalk");
  const brokenCasing = [
    joint([11, 7, 8.5], { name: "toppled", ground: true, rot: [0, 24, 86], children: [joint([-8, 0, -8], { name: "toppledcentre", children: casingBody })] }),
    joint([-6, 2.4, 3], { name: "fallendome", ground: true, rot: [62, -30, 18], children: [joint([-8, -23, -8], { name: "domecentre", children: [
      ...casingDome, box([4.4, 22.6, 4.4], [11.6, 23.2, 11.6], "hollow", { name: "domebreak" }),
    ] })] }),
    { ...casingStalk, joint: [1, 1, 15], ground: true, rot: [0, 50, 4] },
    ...[[13.5, 15, 20], [16.5, 11, -35], [5, 17.5, 60]].map(([x, z, a]) => box([x - 0.8, 0, z - 0.8], [x + 0.8, 1.1, z + 0.8], "stud", { name: "loosestud", rot: [0, a, 0] })),
  ];

  return {
    out: "../ReliquiarumMod/assets/nf3treliquiarum/shapes/block",
    prune: true, // the shapes share one texture map; each file keeps only the keys it uses
    // Each shape's inventory icon is kept in relic.json by vsdev shape (its origin follows the model; a scale tuned by eye stays).
    gui: { file: "../ReliquiarumMod/assets/nf3treliquiarum/blocktypes/relic.json", key: "*-{name}-*", rotateY: 180, keepScale: true },
    shapes: {
      giantkneeling: { ground: true, textures: giantTextures("armor-blue-worn"), elements: giantKneeling },
      giantguard: { ground: true, textures: giantTextures("armor-black-worn"), elements: giantGuard },
      giantprone: { textures: giantTextures("armor-worn", "armor-trim-worn"), elements: giantProne },
      artificerleaning: { ground: true, textures: artificerTextures, elements: artificerLeaning },
      artificerkneeling: { ground: true, textures: { ...artificerTextures, stone: T + "rubble-stone" }, elements: artificerKneeling },
      deathlessstanding: { ground: true, textures: deathlessTextures, elements: deathlessStanding },
      deathlesscrawler: { textures: deathlessTextures, elements: deathlessCrawler },
      lectern: { ground: true, textures: ruinTextures, elements: lectern },
      candelabrum: { ground: true, textures: ruinTextures, elements: candelabrum },
      machinealtar: { ground: true, textures: ruinTextures, elements: machineAltar },
      cogitator: { ground: true, textures: ruinTextures, elements: cogitator },
      brokenpew: { ground: true, textures: ruinTextures, elements: brokenPew },
      skullniche: { ground: true, textures: ruinTextures, elements: skullNiche },
      munitionscrate: { ground: true, textures: ruinTextures, elements: munitionsCrate },
      brazier: { ground: true, textures: ruinTextures, elements: brazier },
      sainthead: { ground: true, textures: ruinTextures, elements: saintHead },
      brokenpillar: { ground: true, textures: ruinTextures, elements: brokenPillar },
      dataslates: { ground: true, textures: ruinTextures, elements: dataSlates },
      angelsmall: { ground: true, textures: ruinTextures, elements: angelSmall },
      angellarge: { ground: true, textures: ruinTextures, elements: angelLarge },
      warcasing: { textures: warTextures, elements: warCasing },
      warcasingbroken: { textures: warTextures, elements: brokenCasing },
      deathlesskneeling: { ground: true, textures: deathlessTextures, elements: deathlessKneeling },
      lastclash: { ground: true, textures: { ...giantTextures("armor-blue-worn"), ...deathlessTextures }, elements: lastClash },
    },
  };
};
