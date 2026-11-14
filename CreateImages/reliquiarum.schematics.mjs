// The Reliquiarum's worldgen schematics, and a preview of each. Made with:
//   node CreateImages/reliquiarum.schematics.mjs
// Writes ReliquiarumMod/assets/nf3treliquiarum/worldgen/schematics/surface/<name>.json (placed by worldgen/structures.json) and
// .vsdev/reliquiarum-previews.json, a vsdev manifest that draws each schematic with the relics' real models and the blocks as
// textured cubes (a plain `vsdev render` of a schematic draws one colored cell per block):
//   vsdev screenshots --manifest .vsdev/reliquiarum-previews.json     # -> .vsdev/renders/reliquiarum/<name>.png
//
// Layers: in the relic scenes y 0 is soil under the surface, y 1 the surface itself, y 2 up what stands on it. The structures use
// "surface" placement, which starts one block above the terrain, so an offsetY of -(ground layer + 1) puts the ground layer at
// the terrain's own surface (-2 for these, see SCHEMATICS for the shrines). "soil-low-normal" is swapped for the local soil and grass ("replacewithblocklayers"), granite
// for the local rock (the game's "standard" rock remap group); basalt is kept as it is. "meta-filler" is placed as air: it clears
// the terrain and plants out of a ruin's inside. Worldgen turns a whole schematic one of four ways at random.
//
// A relic's variant is the side a player placing it looked to, so the model faces the other way (as the game's chests):
// `relic(x, y, z, type, "south")` puts a relic facing south, i.e. the variant "north" (the model as built).
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, "..");
const schematicDir = join(root, "ReliquiarumMod/assets/nf3treliquiarum/worldgen/schematics/surface");
const previewFile = join(root, ".vsdev/reliquiarum-previews.json");

const OPPOSITE = { north: "south", south: "north", east: "west", west: "east" };
const ROTATE_Y = { north: 0, west: 90, south: 180, east: 270 }; // the variants' turns in blocktypes/relic.json

// Textures for the preview's cubes (a block not listed is left out of the preview: soil and filler, small loose stones).
const PREVIEW_TEXTURE = {
  "game:stonebricks-basalt": "game:block/stone/brick/basalt1",
  "game:agedstonebricks-basalt": "game:block/stone/agedbrick/basalt1",
  "game:crackedstonebricks-basalt": "game:block/stone/crackedbrick/basalt1",
  "game:polishedrockold-full-basalt": "game:block/stone/polishedrock-old/full/basalt1",
  "game:polishedrockold-cobbled-basalt": "game:block/stone/polishedrock-old/full/basalt2",
  "game:cobblestone-basalt": "game:block/stone/cobblestone/basalt1",
  "game:gravel-basalt": "game:block/stone/gravel/basalt",
  "game:gravel-granite": "game:block/stone/gravel/granite",
  "game:crackedrock-granite": "game:block/stone/rock-cracked/granite1",
  "game:rockpolished-whitemarble": "game:block/stone/polishedrock/whitemarble",
  "game:rockpolished-redmarble": "game:block/stone/polishedrock/redmarble",
  "game:rockpolished-greenmarble": "game:block/stone/polishedrock/greenmarble",
  "game:quartz-plain": "game:block/stone/quartz/full/plain1",
  "game:quartz-ornate": "game:block/stone/quartz/full/ornate-side",
  "game:quartzpillar-ud": "game:block/stone/quartz/pillar/side",
  "game:soil-medium-none": "game:block/soil/fertmedium",
  "game:soil-medium-verysparse": "game:block/soil/fertmedium",
  "game:soil-medium-sparse": "game:block/soil/fertmedium",
};

class Schematic {
  constructor(size) {
    this.size = size;
    this.blocks = new Map(); // "x,y,z" -> code, in the order first set
    this.kept = {}; // what an imported schematic carries besides its blocks (block entities, item codes...), written back as is
    this.lazyEntities = new Map(); // index -> (idOf block code) => encoded block entity, for entities that name block ids
  }
  /** An existing schematic (one made in game), its blocks and everything else kept. */
  static fromFile(file) {
    const d = JSON.parse(readFileSync(file, "utf8"));
    const s = new Schematic({ x: d.SizeX, y: d.SizeY, z: d.SizeZ });
    d.Indices.forEach((index, i) => s.set(index & 1023, index >> 20, (index >> 10) & 1023, d.BlockCodes[String(d.BlockIds[i])]));
    for (const key of ["GameVersion", "ItemCodes", "DecorIndices", "DecorIds", "BlockEntities", "Entities", "ReplaceMode", "EntranceRotation"])
      if (d[key] !== undefined) s.kept[key] = d[key];
    if ((d.DecorIds ?? []).length) throw new Error(`${file}: decor is not supported here (its codes would need remapping)`);
    return s;
  }
  /** Meta-filler in every empty cell of each layer from y0 to y1, inside the box around that layer's blocks: clears the dirt and
   * rock worldgen would otherwise leave in a buried room (a schematic stores no air). */
  fillRooms(y0, y1) {
    for (let y = y0; y <= y1; y++) {
      const cells = [...this.blocks.keys()].map((k) => k.split(",").map(Number)).filter(([, cy]) => cy === y);
      if (!cells.length) continue;
      const xs = cells.map(([x]) => x), zs = cells.map(([, , z]) => z);
      this.clear(Math.min(...xs), y, Math.min(...zs), Math.max(...xs), y, Math.max(...zs));
    }
  }
  set(x, y, z, code) {
    const { x: sx, y: sy, z: sz } = this.size;
    if (x < 0 || y < 0 || z < 0 || x >= sx || y >= sy || z >= sz) throw new Error(`${x},${y},${z} is outside the schematic`);
    const key = `${x},${y},${z}`;
    if (code === null) this.blocks.delete(key);
    else this.blocks.set(key, code);
  }
  get(x, y, z) {
    if (y < 0) return undefined;
    return this.blocks.get(`${x},${y},${z}`);
  }
  relic(x, y, z, type, facing) {
    this.set(x, y, z, `nf3treliquiarum:relic-${type}-${OPPOSITE[facing]}`);
  }
  /** Fills every empty cell of a box with meta-filler (air when placed). */
  clear(x0, y0, z0, x1, y1, z1) {
    for (let y = y0; y <= y1; y++) for (let z = z0; z <= z1; z++) for (let x = x0; x <= x1; x++)
      if (!this.get(x, y, z)) this.set(x, y, z, "game:meta-filler");
  }
  toJson() {
    const ids = new Map();
    const Indices = [], BlockIds = [];
    for (const [key, code] of this.blocks) {
      const [x, y, z] = key.split(",").map(Number);
      if (!ids.has(code)) ids.set(code, ids.size + 1);
      Indices.push((y << 20) | (z << 10) | x);
      BlockIds.push(ids.get(code));
    }
    const idOf = (code) => { if (!ids.has(code)) ids.set(code, ids.size + 1); return ids.get(code); };
    const lazy = Object.fromEntries([...this.lazyEntities].map(([index, make]) => [index, make(idOf)]));
    return {
      GameVersion: "1.22.0",
      SizeX: this.size.x, SizeY: this.size.y, SizeZ: this.size.z,
      BlockCodes: Object.fromEntries([...ids].map(([code, id]) => [String(id), code])),
      ItemCodes: {},
      Indices, BlockIds,
      DecorIndices: [], DecorIds: [], BlockEntities: {}, Entities: [],
      ReplaceMode: 2, // ReplaceAllNoAir, as the game's own surface ruins
      EntranceRotation: -1,
      ...this.kept,
      ...(this.lazyEntities.size ? { BlockEntities: { ...(this.kept.BlockEntities ?? {}), ...lazy } } : {}),
    };
  }
  /** vsdev scene parts: the ground layer just under the grass plane, what is above it over it; layers under the ground are
   * left out unless `below` (a cutaway: layers up to `below` only, no grass). Water, chests and lanterns are not drawn. */
  previewParts(ground, below) {
    const parts = [];
    for (const [key, code] of this.blocks) {
      const [x, y, z] = key.split(",").map(Number);
      if (below === undefined ? y < ground : y > below) continue;
      const at = [x, y - ground - 1 + 0.01, z];
      const relic = /^nf3treliquiarum:relic-(.+)-(north|east|south|west)$/.exec(code);
      if (relic) {
        parts.push({ model: `../ReliquiarumMod/assets/nf3treliquiarum/shapes/block/${relic[1]}.json`, at, rotationY: ROTATE_Y[relic[2]] });
      } else if (PREVIEW_TEXTURE[code]) {
        parts.push({ model: "game:shapes/block/basic/cube.json", at, material: PREVIEW_TEXTURE[code] });
      }
    }
    return parts;
  }
}

// ---- Loot. A container's contents are a block entity: vsapi's TreeAttribute bytes (tag, key, value; LE numbers, 7-bit-length
// strings; 0 ends a tree), Ascii85-encoded, keyed by the block's index. Only the types a chest needs are written. The game sets the
// position itself on placement. Each slot holds one of the game's stackrandomizer items (as its own ruins do): it turns into a
// random stack from that pool (survival itemtypes/meta/stackrandomizer.json) when the chest is first opened.
const attr = {
  int: (v) => ({ id: 1, write: (w) => w.i32(v) }),
  float: (v) => ({ id: 4, write: (w) => w.f32(v) }),
  string: (v) => ({ id: 5, write: (w) => w.str(v) }),
  tree: (entries) => ({ id: 6, write: (w) => w.tree(entries) }),
  item: (id, size = 1) => ({ id: 7, write: (w) => { w.u8(0); w.i32(1); w.i32(id); w.i32(size); w.tree([]); } }), // not null, item class
  bytes: (a) => ({ id: 8, write: (w) => { w.u8(a.length & 255); w.u8(a.length >> 8); a.forEach((v) => w.u8(v)); } }),
  ints: (a) => ({ id: 11, write: (w) => { w.i32(a.length); a.forEach((v) => w.i32(v)); } }),
};
class TreeWriter {
  bytes = [];
  u8(v) { this.bytes.push(v & 255); }
  i32(v) { const b = Buffer.alloc(4); b.writeInt32LE(v); this.bytes.push(...b); }
  f32(v) { const b = Buffer.alloc(4); b.writeFloatLE(v); this.bytes.push(...b); }
  str(v) {
    const b = Buffer.from(v, "utf8");
    for (let n = b.length; ; n >>= 7) { if (n < 128) { this.u8(n); break; } this.u8((n & 127) | 128); }
    this.bytes.push(...b);
  }
  tree(entries) { for (const [key, a] of entries) { this.u8(a.id); this.str(key); a.write(this); } this.u8(0); }
}
const ascii85 = (bytes) => {
  let out = "";
  for (let i = 0; i < bytes.length; i += 4) {
    const n = Math.min(4, bytes.length - i);
    const v = ((bytes[i] << 24) | ((bytes[i + 1] ?? 0) << 16) | ((bytes[i + 2] ?? 0) << 8) | (bytes[i + 3] ?? 0)) >>> 0;
    if (v === 0 && n === 4) { out += "z"; continue; }
    const digits = [];
    for (let k = 0, x = v; k < 5; k++, x = Math.floor(x / 85)) digits.unshift(String.fromCharCode(33 + (x % 85)));
    out += digits.slice(0, n + 1).join("");
  }
  return out;
};
const encodeTree = (entries) => { const w = new TreeWriter(); w.tree(entries); return ascii85(w.bytes); };
/** A collapsed chest (8 slots) at x, y, z, its lid turned `meshAngle` radians (0: front to the north, PI: to the south, PI/2:
 * to the west), holding one stackrandomizer of each pool in `loot` (e.g. "ingot"), spread over its slots. */
const lootChest = (s, x, y, z, meshAngle, loot) => {
  s.set(x, y, z, "game:chest-east");
  const itemCodes = (s.kept.ItemCodes ??= {});
  const idOf = (code) => {
    const found = Object.entries(itemCodes).find(([, c]) => c === code);
    if (found) return Number(found[0]);
    const id = Object.keys(itemCodes).length + 1;
    itemCodes[String(id)] = code;
    return id;
  };
  const slotsUsed = [0, 3, 5, 6, 1, 7, 2, 4];
  const slots = loot.map((pool, i) => [String(slotsUsed[i]), attr.item(idOf(`game:stackrandomizer-${pool}`))]);
  (s.kept.BlockEntities ??= {})[String((y << 20) | (z << 10) | x)] = encodeTree([
    ["posx", attr.int(0)], ["posy", attr.int(0)], ["posz", attr.int(0)],
    ["blockCode", attr.string("chest-east")],
    ["inventory", attr.tree([["qslots", attr.int(8)], ["slots", attr.tree(slots)]])],
    ["forBlockCode", attr.string("chest-east")], ["type", attr.string("collapsed1")], ["meshAngle", attr.float(meshAngle)],
  ]);
};

// ---- Marble pillars: chiseled blocks of polished white marble, after the two-block pillar built in game
// (CreateImages/reliquiarum-shrines/marble_pillar.json): a base (a full-width 2 px slab under a 12 x 12 shaft) and a capital (the
// shaft under a slab). The shaft alone makes the middle of a taller column, and base, shaft and capital in one block a column one
// block tall. The block-entity fields are written as the game writes them (checked: this encoder rebuilds the in-game base
// byte for byte). emitSideAo / sideAlmostSolid: the faces a piece covers whole (bit 4 up, bit 5 down); sideSolid: faces solid
// at their centre (up and down for every piece).
const cuboid = (x1, y1, z1, x2, y2, z2) => (x1 | (y1 << 4) | (z1 << 8) | ((x2 - 1) << 12) | ((y2 - 1) << 16) | ((z2 - 1) << 20)) >>> 0;
const PILLAR = {
  base: { cuboids: [cuboid(0, 0, 0, 16, 2, 16), cuboid(2, 2, 2, 14, 16, 14)], faces: 32 },
  capital: { cuboids: [cuboid(0, 14, 0, 16, 16, 16), cuboid(2, 0, 2, 14, 14, 14)], faces: 16 },
  shaft: { cuboids: [cuboid(2, 0, 2, 14, 16, 14)], faces: 0 },
  single: { cuboids: [cuboid(0, 0, 0, 16, 2, 16), cuboid(2, 2, 2, 14, 14, 14), cuboid(0, 14, 0, 16, 16, 16)], faces: 48 },
};
// The game's three marbles; each shrine is written once in each (worldgen picks one of a shrine's at random).
const MARBLES = ["white", "red", "green"];
const polished = (marble) => `game:rockpolished-${marble}marble`;
const pillarPiece = (s, x, y, z, piece, marble) => {
  s.set(x, y, z, "game:chiseledblock");
  const { cuboids, faces } = PILLAR[piece];
  // Its material is a block id of the schematic's own BlockCodes, known only when the file is written.
  s.lazyEntities.set(String((y << 20) | (z << 10) | x), (idOf) => encodeTree([
    ["posx", attr.int(0)], ["posy", attr.int(0)], ["posz", attr.int(0)], ["blockCode", attr.string("chiseledblock")],
    ["materials", attr.ints([idOf(polished(marble))])], ["decorRot", attr.int(0)], ["rotation", attr.int(360 << 10)], ["cuboids", attr.ints(cuboids)],
    ["emitSideAo", attr.bytes([faces])], ["sideSolid", attr.bytes([48])], ["sideAlmostSolid", attr.bytes([faces])],
    ["blockName", attr.string("")], ["availMaterialQuantities", attr.ints([4096])],
  ]));
};
/** Quartz and white marble to one marble: every upright run of quartz pillar blocks becomes a marble pillar (base, shaft...,
 * capital; one block: the single piece), every other quartz block (ornate caps, beams and dome, plain walls) and the shrine's own
 * polished white marble polished `marble`. */
const marbleize = (s, marble) => {
  const PILLAR_CODE = "game:quartzpillar-ud";
  for (const [key, code] of [...s.blocks]) {
    if (code === PILLAR_CODE || !(/^game:quartz/.test(code) || code === polished("white"))) continue;
    const [x, y, z] = key.split(",").map(Number);
    s.set(x, y, z, polished(marble));
  }
  for (let x = 0; x < s.size.x; x++) for (let z = 0; z < s.size.z; z++) {
    for (let y = 0; y < s.size.y; y++) {
      if (s.get(x, y, z) !== PILLAR_CODE || s.get(x, y - 1, z) === PILLAR_CODE) continue;
      let top = y;
      while (s.get(x, top + 1, z) === PILLAR_CODE) top++;
      if (top === y) pillarPiece(s, x, y, z, "single", marble);
      else for (let py = y; py <= top; py++) pillarPiece(s, x, py, z, py === y ? "base" : py === top ? "capital" : "shaft", marble);
    }
  }
  if ([...s.blocks.values()].some((c) => /^game:quartz/.test(c))) throw new Error("quartz left after marbleize");
};

// A small seeded random (the same layout on every run).
const random = (seed) => () => ((seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648);

// ---- The last clash (not in worldgen for now: left out of SCHEMATICS and structures.json, kept here to bring back): the relic on a patch of churned ground (gravel, cracked rock, loose stones). The relic faces south as built;
// its figures reach about one block west, east and north of it and two south, so nothing solid is put there.
const lastClash = () => {
  const s = new Schematic({ x: 7, y: 3, z: 8 });
  const surface = [ // s soil, g gravel (churned earth), c cracked rock; north at the top, x west to east. A small, ragged scuff
    "sssssss",       // round the fighters, not a square patch: the rest is soil, i.e. the local ground and grass.
    "ssgssss",
    "sggcsss",
    "ssgggss",
    "sssgsss",
    "ssssgss",
    "sssssss",
    "sssssss",
  ];
  const above = [ // R the relic, o loose stones
    ".o.....",
    ".......",
    "...R...",
    ".......",
    ".....o.",
    "o......",
    ".......",
    ".......",
  ];
  const surfaceCode = { s: "game:soil-low-normal", g: "game:gravel-granite", c: "game:crackedrock-granite" };
  for (let z = 0; z < 8; z++) for (let x = 0; x < 7; x++) s.set(x, 0, z, "game:soil-low-normal");
  for (let z = 0; z < 8; z++) for (let x = 0; x < 7; x++) s.set(x, 1, z, surfaceCode[surface[z][x]]);
  for (let z = 0; z < 8; z++) for (let x = 0; x < 7; x++) {
    if (above[z][x] === "R") s.relic(x, 2, z, "lastclash", "south");
    if (above[z][x] === "o") s.set(x, 2, z, "game:loosestones-granite-free");
  }
  return s;
};

// ---- The imperial reliquary: a roofless basalt chapel, its south front fallen. At the north end the reliquary between two
// candelabra, an eagle banner either side and a gilded eagle on a pedestal in each corner; a lectern before it facing the pews,
// two rows of broken pews either side of the aisle, a skull niche in each side wall, data-slates on the floor, a munitions crate
// by the door. The walls stand four blocks at the back and fall away toward the front; grass comes up through broken floor.
// Outside the fallen front, the fallen giant sits slumped against what is left of the wall beside the door, as if he died
// guarding it.
const imperialReliquary = () => {
  const W = 9, H = 6, D = 13; // x 0..8 (walls at 0 and 8), z 0..12 (back wall at 0, the fallen front at 9, open ground 10..12)
  const s = new Schematic({ x: W, y: H, z: D });
  const rand = random(7);
  const brick = () => {
    const r = rand();
    return r < 0.5 ? "game:stonebricks-basalt" : r < 0.8 ? "game:agedstonebricks-basalt" : "game:crackedstonebricks-basalt";
  };
  const isWall = (x, z) => (x === 0 || x === W - 1) && z <= 9 || z === 0 || z === 9 && (x <= 2 || x >= 6);

  // Foundations and floor.
  for (let z = 0; z < D; z++) for (let x = 0; x < W; x++) {
    s.set(x, 0, z, isWall(x, z) ? "game:cobblestone-basalt" : "game:soil-low-normal");
    let top = "game:soil-low-normal";
    if (isWall(x, z)) top = brick();
    else if (z >= 1 && z <= 8) top = x === 4 ? "game:polishedrockold-cobbled-basalt" : "game:polishedrockold-full-basalt";
    else if (z === 9) top = "game:polishedrockold-cobbled-basalt"; // the threshold
    s.set(x, 1, z, top);
  }
  // Broken floor: gravel, and soil where grass has come through.
  for (const [x, z] of [[2, 8], [1, 6], [7, 4]]) s.set(x, 1, z, "game:gravel-basalt");
  for (const [x, z] of [[6, 7], [7, 8], [5, 9], [1, 9]]) s.set(x, 1, z, "game:soil-low-normal");

  // Walls: how many blocks each column stands above the floor.
  const north = [3, 4, 4, 4, 4, 4, 4, 3, 2];          // x 0..8
  const west = [4, 4, 3, 3, 2, 2, 1, 0, 1];           // z 1..9
  const east = [3, 4, 3, 2, 2, 1, 1, 0, 0];           // z 1..9
  const front = { 1: 1, 2: 0, 6: 2, 7: 2 };           // the fallen south wall, x -> height (posts at 0 and 8 are the side walls)
  const raise = (x, z, h) => { for (let y = 2; y < 2 + h; y++) s.set(x, y, z, brick()); };
  north.forEach((h, x) => raise(x, 0, h));
  west.forEach((h, i) => raise(0, i + 1, h));
  east.forEach((h, i) => raise(W - 1, i + 1, h));
  for (const [x, h] of Object.entries(front)) raise(Number(x), 9, h);

  // Skull niches set into the side walls, opening inward.
  s.relic(0, 2, 3, "skullniche", "east");
  s.relic(W - 1, 2, 3, "skullniche", "west");

  // The altar end.
  s.set(1, 2, 1, "game:polishedrockold-full-basalt"); s.relic(1, 3, 1, "aquila", "south");
  s.relic(2, 2, 1, "eaglebanner", "south");
  s.relic(3, 2, 1, "candelabrum", "south");
  s.relic(4, 2, 1, "reliquary", "south");
  s.relic(5, 2, 1, "candelabrum", "south");
  s.relic(6, 2, 1, "eaglebanner", "south");
  s.set(7, 2, 1, "game:polishedrockold-full-basalt"); s.relic(7, 3, 1, "aquila", "south");

  // The lectern, facing the pews; the pews facing the altar; clutter.
  s.relic(4, 2, 3, "lectern", "north");
  s.relic(3, 2, 4, "dataslates", "south");
  for (const x of [2, 3, 5, 6]) s.relic(x, 2, 5, "brokenpew", "north");
  for (const x of [2, 6]) s.relic(x, 2, 7, "brokenpew", "north");
  s.relic(7, 2, 8, "munitionscrate", "west");

  // Rubble from the walls.
  s.set(5, 2, 7, "game:cobblestone-basalt");
  s.set(1, 2, 8, "game:cobblestone-basalt");
  for (const [x, z] of [[7, 6], [2, 8], [3, 9], [2, 10], [4, 11]]) s.set(x, 2, z, "game:loosestones-basalt-free");
  s.set(1, 2, 10, "game:agedstonebricks-basalt");

  // The fallen giant, his back to the wall stub at x 6-7 of the front, legs out to the south: he reaches about a block west and
  // east of his block and two south, so that ground is cleared of terrain and plants (and nothing else is put there).
  s.relic(7, 2, 10, "fallengiant", "south");
  s.clear(6, 2, 10, 8, 4, 12);

  // Clear the inside (and the doorway) of terrain and plants.
  s.clear(1, 2, 1, W - 2, H - 1, 9);
  return s;
};

// ---- Three shrines built in game (white marble and quartz; copies of the vintage-story-wiki schematic viewer's samples, in
// CreateImages/reliquiarum-shrines/), kept as built: only meta-filler is added, so their buried rooms are hollow when placed and
// terrain and plants are cleared from round what stands above ground.
const shrine = (name) => Schematic.fromFile(join(here, "reliquiarum-shrines", `${name}.json`));
// A stepped marble platform (its base level with the ground): four quartz pillars, a fountain with a paper lantern, four
// collapsed chests.
const smallShrine = (marble) => {
  const s = shrine("small_shrine");
  marbleize(s, marble);
  // Its four collapsed chests (empty as built), turned as built: offerings, modest.
  lootChest(s, 2, 1, 2, -Math.PI, ["accessory-mediumstatus", "ingot"]);
  lootChest(s, 6, 1, 2, -Math.PI, ["gear"]);
  lootChest(s, 2, 1, 6, 0, ["accessory-mediumstatus"]);
  lootChest(s, 6, 1, 6, 0, ["ingot", "lantern"]);
  s.clear(0, 1, 0, s.size.x - 1, s.size.y - 1, s.size.z - 1);
  return s;
};
// A marble plaza at ground level (layer 7) with a pool, rings of quartz pillars and a domed canopy; under it a sealed marble hall
// (layers 1-6) with pillars, water channels and a paper lantern, found only by digging.
const ancientShrine = (marble) => {
  const s = shrine("ancient_shrine");
  marbleize(s, marble);
  // Two collapsed chests in the sealed hall, against its west and east walls facing in: the best of the three, for digging to it.
  lootChest(s, 5, 2, 8, -Math.PI / 2, ["accessory-highstatus", "accessory-mediumstatus", "ingot"]);
  lootChest(s, 13, 2, 8, Math.PI / 2, ["gear", "ingot", "lantern"]);
  s.fillRooms(1, 6);
  s.clear(0, 8, 0, s.size.x - 1, 12, s.size.z - 1);
  return s;
};
// Buried whole: a quartz hall (pillars, a fountain, a paper lantern, four collapsed chests) under three layers of soil; only the
// tops of five quartz pillars show in the grass (layer 10, the ground).
const desertShrine = (marble) => {
  const s = shrine("desert_shrine");
  marbleize(s, marble);
  // Its four collapsed chests (empty as built), turned as built: better than the small shrine's, being buried.
  lootChest(s, 1, 2, 1, 0, ["accessory-highstatus", "ingot"]);
  lootChest(s, 7, 2, 1, 0, ["accessory-mediumstatus", "gear"]);
  lootChest(s, 1, 2, 7, Math.PI, ["ingot", "gear"]);
  lootChest(s, 7, 2, 7, Math.PI, ["accessory-highstatus"]);
  s.fillRooms(1, 6);
  return s;
};

// ground: the layer at the terrain's surface (the structure's offsetY is -(ground + 1)); cutaway: the top layer drawn in a second
// preview of what is buried.
const SCHEMATICS = {
  imperialreliquary: { build: imperialReliquary, ground: 1, view: [0.7, 0.7, 1.1] },
  // One file per marble, in a folder per shrine (surface/smallshrine/white.json...): the structure lists the folder.
  ...Object.fromEntries(MARBLES.flatMap((m) => [
    [`smallshrine/${m}`, { build: () => smallShrine(m), ground: 0, view: [0.6, 0.7, 1.1] }],
    [`ancientshrine/${m}`, { build: () => ancientShrine(m), ground: 7, view: [0.6, 0.7, 1.1], cutaway: 6 }],
    [`desertshrine/${m}`, { build: () => desertShrine(m), ground: 10, view: [0.6, 0.9, 1.1], cutaway: 6 }],
  ])),
};

mkdirSync(schematicDir, { recursive: true });
const screenshots = [];
for (const [name, { build, ground, view, cutaway }] of Object.entries(SCHEMATICS)) {
  const s = build();
  const file = join(schematicDir, `${name}.json`);
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, JSON.stringify(s.toJson(), null, 2) + "\n");
  console.log(`→ ${file}: ${s.blocks.size} blocks, ${s.size.x} x ${s.size.y} x ${s.size.z}`);
  screenshots.push({ name: name.replace("/", "-"), scene: s.previewParts(ground), view, grass: true, sky: true, size: 1600, height: 1000, output: `renders/reliquiarum/${name.replace("/", "-")}.png` });
  if (cutaway !== undefined)
    screenshots.push({ name: `${name.replace("/", "-")}-cutaway`, scene: s.previewParts(ground, cutaway), view: [0.6, 1.2, 1.1], size: 1600, height: 1000, output: `renders/reliquiarum/${name.replace("/", "-")}-cutaway.png` });
}
mkdirSync(dirname(previewFile), { recursive: true });
writeFileSync(previewFile, JSON.stringify({ screenshots }, null, 2) + "\n");
console.log(`→ ${previewFile}`);
