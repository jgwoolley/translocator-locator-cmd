// Textures of the first Reliquiarum relics (the fallen giant, the reliquary, the stasis pod, the scarab, the gilded eagle, the
// banners). Made with:
//   vsdev texture CreateImages/reliquiarum-relics.textures.mjs
// Ported from the Python generator they were first made with (reliquiarum_textures.py), pixel for pixel: the same algorithms on the
// same random numbers (`PyRandom` is Python's random.Random, a Mersenne Twister seeded the same way) and Python's rounding, each
// texture drawn by a "custom" layer. Worn metals (noise, grime, rust, verdigris, chips, scratches), a few decorated plates (rune
// bands, skull roundels, a keyhole), gilding and feathers for the eagle, and the banners' embroidered cloth. The banner's eagle is
// the eagle model itself seen from the front (vsdev's `project` of the shapes spec's eagle).
import { builders, GILT as GILT_TEXTURES } from "./reliquiarum-relics.shapes.mjs";

// ---- Python's random.Random and round(), so every pixel comes out as it always has.
class PyRandom {
  constructor(seed) {
    this.mt = new Uint32Array(624);
    this.index = 624;
    const key = [];
    for (let n = Math.abs(seed); ; n = Math.floor(n / 4294967296)) {
      key.push(n % 4294967296);
      if (n < 4294967296) break;
    }
    this.initByArray(key);
  }
  initGenrand(s) {
    const mt = this.mt;
    mt[0] = s >>> 0;
    for (let i = 1; i < 624; i++) mt[i] = (Math.imul(1812433253, mt[i - 1] ^ (mt[i - 1] >>> 30)) + i) >>> 0;
    this.index = 624;
  }
  initByArray(key) {
    const mt = this.mt;
    this.initGenrand(19650218);
    let i = 1, j = 0;
    for (let k = Math.max(624, key.length); k; k--) {
      mt[i] = ((mt[i] ^ Math.imul(mt[i - 1] ^ (mt[i - 1] >>> 30), 1664525)) + key[j] + j) >>> 0;
      i++; j++;
      if (i >= 624) { mt[0] = mt[623]; i = 1; }
      if (j >= key.length) j = 0;
    }
    for (let k = 623; k; k--) {
      mt[i] = ((mt[i] ^ Math.imul(mt[i - 1] ^ (mt[i - 1] >>> 30), 1566083941)) - i) >>> 0;
      i++;
      if (i >= 624) { mt[0] = mt[623]; i = 1; }
    }
    mt[0] = 0x80000000;
  }
  uint32() {
    const mt = this.mt;
    if (this.index >= 624) {
      for (let k = 0; k < 624; k++) {
        const y = (mt[k] & 0x80000000) | (mt[(k + 1) % 624] & 0x7fffffff);
        mt[k] = mt[(k + 397) % 624] ^ (y >>> 1) ^ (y & 1 ? 0x9908b0df : 0);
      }
      this.index = 0;
    }
    let y = mt[this.index++];
    y ^= y >>> 11;
    y ^= (y << 7) & 0x9d2c5680;
    y ^= (y << 15) & 0xefc60000;
    y ^= y >>> 18;
    return y >>> 0;
  }
  random() {
    const a = this.uint32() >>> 5, b = this.uint32() >>> 6;
    return (a * 67108864 + b) / 9007199254740992;
  }
  below(n) {
    const k = n.toString(2).length;
    let r = this.uint32() >>> (32 - k);
    while (r >= n) r = this.uint32() >>> (32 - k);
    return r;
  }
  randrange(n) { return this.below(n); }
  randint(a, b) { return a + this.below(b - a + 1); }
  choice(seq) { return seq[this.below(seq.length)]; }
}
const round = (v) => {
  const f = Math.floor(v), d = v - f;
  return d > 0.5 ? f + 1 : d < 0.5 ? f : f % 2 === 0 ? f : f + 1;
};

// ---- The painting helpers (as in the Python generator).
const range = (n) => Array.from({ length: n }, (_, i) => i);
/** Smooth noise in 0..1 that tiles: a random grid of w/cell x h/cell points, bilinearly blended. */
function valueNoise(w, h, cell, rng) {
  const gw = Math.max(1, Math.floor(w / cell)), gh = Math.max(1, Math.floor(h / cell));
  const grid = range(gh).map(() => range(gw).map(() => rng.random()));
  return range(h).map((y) => {
    const fy = y / cell, y0 = Math.trunc(fy) % gh, ty = fy - Math.trunc(fy);
    return range(w).map((x) => {
      const fx = x / cell, x0 = Math.trunc(fx) % gw;
      let tx = fx - Math.trunc(fx);
      const a = grid[y0][x0], b = grid[y0][(x0 + 1) % gw], c = grid[(y0 + 1) % gh][x0], d = grid[(y0 + 1) % gh][(x0 + 1) % gw];
      tx = tx * tx * (3 - 2 * tx);
      const ty2 = ty * ty * (3 - 2 * ty);
      return (a * (1 - tx) + b * tx) * (1 - ty2) + (c * (1 - tx) + d * tx) * ty2;
    });
  });
}
const mix = (a, b, t) => {
  t = Math.max(0, Math.min(1, t));
  return [0, 1, 2].map((i) => round(a[i] + (b[i] - a[i]) * t));
};
const shade = (c, f) => c.map((v) => Math.max(0, Math.min(255, round(v * f))));

/** A worn metal surface: base color with fine grain, grime in blotches, optional rust and verdigris patches, dark chips and a few
 * light scratches. */
function worn(w, h, seed, base, grime, { rust, patina, rustAmount = 0, patinaAmount = 0, chips = 0, scratches = 0 } = {}) {
  const rng = new PyRandom(seed);
  const fine = valueNoise(w, h, 2, rng);
  // Cells that divide the size, so the noise tiles without seams.
  const blotch = valueNoise(w, h, Math.max(2, Math.floor(w / 4)), rng);
  const rustN = valueNoise(w, h, Math.max(2, Math.floor(w / 8)), rng);
  const patinaN = valueNoise(w, h, Math.max(2, Math.floor(w / 8)), rng);
  const px = range(h).map((y) => range(w).map((x) => {
    let c = shade(base, 0.82 + 0.32 * fine[y][x]);
    c = mix(c, grime, (0.5 - blotch[y][x]) * 1.6);
    if (rust && rustN[y][x] > 1 - rustAmount) c = mix(c, shade(rust, 0.8 + 0.4 * fine[y][x]), 0.35 + (rustN[y][x] - (1 - rustAmount)) * 2);
    if (patina && patinaN[y][x] > 1 - patinaAmount) c = mix(c, shade(patina, 0.85 + 0.3 * fine[y][x]), 0.3 + (patinaN[y][x] - (1 - patinaAmount)) * 2);
    if (rng.random() < chips) c = shade(c, 0.45);
    return c;
  }));
  for (let s = 0; s < scratches; s++) {
    let x = rng.randrange(w), y = rng.randrange(h);
    const [dx, dy] = rng.choice([[1, 0], [1, 1], [1, -1], [0, 1]]);
    for (let n = rng.randint(3, 7); n > 0; n--) {
      const yy = ((y % h) + h) % h, xx = ((x % w) + w) % w;
      px[yy][xx] = shade(px[yy][xx], 1.35);
      x += dx; y += dy;
    }
  }
  return px;
}
/** Pixel art (characters mapped to colors; others left alone) at `at`, with an optional 1 px drop shadow. */
function stamp(px, art, [ox, oy], colors, shadow) {
  const inside = (y, x) => y >= 0 && y < px.length && x >= 0 && x < px[0].length;
  if (shadow) art.forEach((line, r) => [...line].forEach((ch, c) => {
    if (ch in colors && !(r + 1 < art.length && c + 1 < line.length && art[r + 1][c + 1] in colors) && inside(oy + r + 1, ox + c + 1)) {
      px[oy + r + 1][ox + c + 1] = mix(px[oy + r + 1][ox + c + 1], shadow, 0.7);
    }
  }));
  art.forEach((line, r) => [...line].forEach((ch, c) => {
    if (ch in colors && inside(oy + r, ox + c)) px[oy + r][ox + c] = mix(px[oy + r][ox + c], colors[ch], 0.85);
  }));
}

const BRONZE = [118, 96, 60], BRONZE_DARK = [74, 59, 38], BRONZE_HI = [170, 146, 98], GRIME = [48, 40, 28], VERDIGRIS = [88, 124, 100];
const CARVED = [36, 29, 20], BONE = [196, 180, 140];
const SKULL = ["....######....", "..##########..", ".############.", ".############.", "##############", "###oo####oo###", "##oooo##oooo##",
  "##oooo##oooo##", "###oo#oo#oo###", ".######oo####.", "..####....###.", "...#.#.#.#.#..", "...#########.."];
const SMALL_SKULL = [".####.", "######", "#o##o#", "##oo##", ".#..#.", ".####."];
const GLYPHS = [
  ["###", "#..", "##.", "#..", "###"], ["#.#", "###", "#.#", "#.#", "#.#"], ["###", ".#.", ".#.", ".#.", "###"],
  ["#..", "#..", "###", "..#", "..#"], ["###", "#.#", "#.#", "#.#", "#.#"], [".#.", "###", ".#.", ".#.", "#.#"],
  ["##.", "#.#", "##.", "#.#", "##."], ["#.#", "#.#", ".#.", "#.#", "#.#"], ["###", "..#", ".##", "..#", "###"],
];

/** A band of carved glyphs between raised rims, with a small skull in the middle. */
function runeBand(w, h, seed, vertical = false) {
  const [long, short] = vertical ? [h, w] : [w, h];
  let px = worn(long, short, seed, BRONZE, GRIME, { patina: VERDIGRIS, patinaAmount: 0.2, scratches: 4 });
  const rng = new PyRandom(seed + 1);
  for (let i = 0; i < long; i++) {
    for (const j of [0, short - 1]) px[j][i] = shade(BRONZE_HI, 0.9 + 0.2 * rng.random());
    for (const j of [1, short - 2]) px[j][i] = shade(px[j][i], 0.6);
  }
  const gy = Math.floor((short - 5) / 2), mid = Math.floor(long / 2);
  for (let x = 2; x + 3 <= long - 2;) {
    if (Math.abs(x + 1 - mid) < 6) { x = mid + 5; continue; }
    stamp(px, rng.choice(GLYPHS), [x, gy], { "#": CARVED });
    x += 5;
  }
  stamp(px, SMALL_SKULL, [mid - 3, Math.floor((short - 6) / 2)], { "#": BRONZE_HI, o: CARVED }, CARVED);
  if (vertical) px = range(long).map((j) => range(short).map((i) => px[i][j]));
  return px;
}
/** A 32 px square plate: worn bronze with a raised rim and a recessed field. */
function framed(seed, inner) {
  const px = worn(32, 32, seed, BRONZE, GRIME, { patina: VERDIGRIS, patinaAmount: 0.25, scratches: 3 });
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    const edge = Math.min(x, y, 31 - x, 31 - y);
    if (edge < 2) px[y][x] = shade(BRONZE_HI, 0.85 + 0.1 * ((x + y) % 2));
    else if (edge < 3) px[y][x] = shade(px[y][x], 0.55);
    else if (inner) px[y][x] = mix(px[y][x], inner, 0.6);
  }
  return px;
}
function skullRoundel(seed) {
  const px = framed(seed, BRONZE_DARK);
  stamp(px, SKULL, [9, 9], { "#": BRONZE_HI, o: CARVED }, CARVED);
  return px;
}
function keyhole(seed) {
  const px = framed(seed, [60, 56, 50]);
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    if ((x - 15.5) ** 2 + (y - 12) ** 2 <= 12 || (y >= 14 && y <= 22 && Math.abs(x - 15.5) <= 0.8 + (y - 14) / 5)) px[y][x] = [14, 12, 10];
  }
  for (const [cx, cy] of [[6, 6], [25, 6], [6, 25], [25, 25]]) for (const [dx, dy] of [[0, 0], [1, 0], [0, 1], [1, 1]]) px[cy + dy][cx + dx] = dx === 0 && dy === 0 ? BRONZE_HI : BRONZE_DARK;
  return px;
}
/** Faded cloth behind the relic: lighter in the middle, dark at the edges. */
function backdrop(seed) {
  const px = worn(32, 32, seed, [150, 128, 90], [80, 66, 46]);
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    const d = Math.max(Math.abs(x - 15.5), Math.abs(y - 15.5)) / 16;
    px[y][x] = shade(px[y][x], 1.1 - 0.55 * d * d);
  }
  return px;
}
function parchment(seed) {
  const px = worn(16, 32, seed, [196, 176, 130], [120, 98, 64]);
  const rng = new PyRandom(seed);
  for (let y = 4; y < 30; y += 3) {
    for (let x = 3; x < 13;) {
      const n = rng.randint(1, 3);
      for (let i = 0; i < n; i++) if (x + i < 13) px[y][x + i] = mix(px[y][x + i], [70, 52, 36], 0.6);
      x += n + 1;
    }
  }
  return px;
}
/** A black panel with a circuit sigil (an original pattern: ring, bar and nodes), lit or dead. */
function glyphPanel(seed, glow, dim) {
  const px = worn(32, 32, seed, [34, 37, 36], [16, 18, 17]);
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    const r = ((x - 15.5) ** 2 + (y - 15.5) ** 2) ** 0.5, edge = Math.min(x, y, 31 - x, 31 - y);
    if ((r >= 9 && r <= 10.4) || (Math.abs(x - 15.5) <= 1 && y >= 4 && y <= 27) || (Math.abs(y - 15.5) <= 0.6 && x >= 6 && x <= 25)) px[y][x] = glow;
    else if (edge === 2 || (r >= 8 && r < 9) || (r > 10.4 && r <= 11.2)) px[y][x] = dim;
  }
  for (const [cx, cy] of [[15, 3], [15, 27], [5, 15], [26, 15]]) for (const dx of [0, 1]) for (const dy of [0, 1]) px[cy + dy][cx + dx] = glow;
  return px;
}
/** Glass with streaks (the renderer draws it solid, so it is painted as glass, not made see-through). */
function shardGlass(seed, base, grime) {
  const px = worn(16, 16, seed, base, grime);
  for (let i = 0; i < 16; i++) for (const j of [i, (i + 7) % 16]) px[j][i] = shade(px[j][i], 1.25);
  return px;
}

const GILT = [198, 156, 62], GILT_SHADE = [138, 104, 40], GILT_TARNISH = [82, 62, 34];
/** Gilding: still gold, but dulled, and worn through to the dark metal under it in patches. */
const gilt = (seed, base = GILT, grime = GILT_SHADE, tarnish = 0.09) => worn(32, 32, seed, base, grime, { patina: GILT_TARNISH, patinaAmount: tarnish, chips: 0.003, scratches: 4 });
/** One feather, stretched over a whole face: lit along the top edge, a dark line along the bottom, so stacked feathers read as
 * separate even seen flat from the front. */
function giltFeather(seed) {
  const w = 32, h = 8, rng = new PyRandom(seed);
  const n = valueNoise(w, h, 2, rng), wear = valueNoise(w, h, 8, rng);
  const rows = [1.2, 1.1, 1.02, 0.96, 0.9, 0.82, 0.72, 0.42];
  return range(h).map((y) => range(w).map((x) => {
    const c = shade(GILT, rows[y] * (0.9 + 0.2 * n[y][x]));
    return wear[y][x] > 0.8 ? mix(c, GILT_TARNISH, 0.45) : c;
  }));
}
/** Breast feathers: rows of scallops (8 px each, four to a row), every other row shifted by half a scallop; each bright at its top,
 * darkening to a rim where it ends over the next row. */
function giltPlumage(seed) {
  const w = 32, h = 32, rng = new PyRandom(seed);
  const n = valueNoise(w, h, 2, rng), wear = valueNoise(w, h, 8, rng);
  const light = [1.14, 1.08, 1.03, 1.0, 0.96, 0.9, 0.84, 0.72];
  return range(h).map((y) => range(w).map((x) => {
    const r = Math.floor(y / 8), v = y % 8, u = (x + (r % 2 ? 4 : 0)) % 8;
    let f = light[v];
    if (v >= 5 && (u < v - 4 || u > 11 - v)) f = 0.64; // the rounded lower corners: the gap between two scallops
    const c = shade(GILT, f * (0.94 + 0.12 * n[y][x]));
    return wear[y][x] > 0.84 ? mix(c, GILT_TARNISH, 0.35) : c;
  }));
}

// ---- The banners' cloth: 4 px per unit, embroidered.
const [BW, BH] = [64, 112]; // 16 x 28 units of cloth
const TEETH = 12;
/** A w x h map of thread keys: every pixel takes the key most of its fine x fine sample points get from classify(x, y) (pixel units,
 * y down; null for bare cloth); on a tie, the first key met. */
function raster(w, h, classify, fine = 4) {
  return range(h).map((y) => range(w).map((x) => {
    const votes = new Map();
    for (let j = 0; j < fine; j++) for (let i = 0; i < fine; i++) {
      const key = classify(x + (i + 0.5) / fine, y + (j + 0.5) / fine) ?? null;
      votes.set(key, (votes.get(key) ?? 0) + 1);
    }
    let best = null, most = -1;
    for (const [k, n] of votes) if (n > most) { best = k; most = n; }
    return best;
  }));
}
/** Pixel art as a map of thread keys, every pixel k x k. */
const grow = (art, k, keys) => art.flatMap((line) => range(k).map(() => [...line].flatMap((ch) => range(k).map(() => keys[ch] ?? null))));
/** A skull facing front, from ellipses and lines: "skull", "hole" (sockets, nose, the gaps between the teeth) or null outside it.
 * 0.94 wide, from y -0.61 to 0.62 (y down). A machine's skull has a round lens for an eye and vent slats for teeth. */
function skullPart(x, y, machine = false) {
  const ax = Math.abs(x);
  const cranium = (x / 0.47) ** 2 + ((y + 0.16) / 0.45) ** 2 <= 1;
  const cheeks = (x / 0.4) ** 2 + ((y - 0.12) / 0.22) ** 2 <= 1;
  const jaw = y >= 0.1 && y <= 0.62 && ax <= 0.3 - 0.08 * (y - 0.1);
  if (!(cranium || cheeks || jaw)) return null;
  const dx = ax - 0.2, dy = y - 0.04;
  if (machine) {
    if ((Math.hypot(dx, dy) > 0.06 && Math.hypot(dx, dy) <= 0.14) || (y >= 0.42 && ax > 0.04 && pyMod(y - 0.42, 0.1) > 0.05)) return "hole";
  } else {
    // A slanted socket, its outer end higher.
    const u = dx * 0.94 - dy * 0.34, v = dx * 0.34 + dy * 0.94;
    if ((u / 0.15) ** 2 + (v / 0.085) ** 2 <= 1 || (y >= 0.46 && pyMod(ax, 0.1) > 0.06)) return "hole";
  }
  if (y >= 0.14 && y <= 0.3 && ax <= 0.01 + (0.07 * (y - 0.14)) / 0.16) return "hole";
  return "skull";
}
// Python's float %: the remainder takes the divisor's sign.
const pyMod = (a, b) => { const r = a % b; return r !== 0 && (r < 0) !== (b < 0) ? r + b : r; };
/** A cog wheel with a skull on it, split down the middle: black wheel and bone skull on the left, bone wheel and black machine skull
 * on the right. x and y from -1 to 1 (the tips of the teeth), y down. */
function cogSkull(x, y) {
  const [wheel, skull] = x < 0 ? ["dark", "light"] : ["light", "dark"];
  const part = skullPart(x, y, x >= 0);
  if (part) return part === "skull" ? skull : wheel;
  const r = Math.hypot(x, y);
  // Square teeth every 30 degrees from the top, so the split runs down the middle of the top and bottom ones.
  const pitch = (2 * Math.PI) / TEETH, off = pyMod(Math.atan2(y, x) + pitch / 2, pitch) - pitch / 2;
  return r <= 0.79 || (r * Math.cos(off) <= 1 && Math.abs(r * Math.sin(off)) <= 0.12) ? wheel : null;
}
const cogEmblem = (size) => raster(size, size, (x, y) => cogSkull((x / size) * 2 - 1, (y / size) * 2 - 1));
/** The statue's eagle seen from the front, as embroidery `width` px wide: the nearest face's texture under each point. Feathers keep
 * a dark lower edge (as their texture has); breast feathers become a pattern of dark stitches. */
function eagleEmblem(width, helpers) {
  const { eagle } = builders(helpers);
  const view = helpers.project({ textures: GILT_TEXTURES, elements: [eagle("eagle", [0, 0, 0], 1)] }, { width });
  const art = raster(width, view.height, (x, y) => {
    const hit = view.at(x - 0.5, y - 0.5);
    if (!hit) return null;
    if (hit.texture === "feather") return (hit.face.rotation ? hit.u : hit.v) < 0.3 ? "dark" : "light";
    return { giltdark: "dark", shadow: "black", plumage: "plumage" }[hit.texture] ?? "light";
  });
  art.forEach((row, y) => row.forEach((key, x) => {
    if (key === "plumage") row[x] = y % 2 && (x + 2 * (Math.floor(y / 2) % 2)) % 4 === 0 ? "dark" : "light";
  }));
  return art;
}
/** Woven cloth: a fine weave, lighter where it has faded, with the soft bands of hanging folds if wanted. */
function cloth(w, h, seed, field, faded, folds = 0) {
  const rng = new PyRandom(seed);
  const fine = valueNoise(w, h, 2, rng), blotch = valueNoise(w, h, Math.max(2, Math.floor(w / 4)), rng);
  return range(h).map((y) => range(w).map((x) => {
    let f = (0.9 + 0.2 * fine[y][x]) * ((x + y) % 2 ? 1.05 : 0.95);
    f *= 1 + folds * Math.sin((x / w) * 2 * Math.PI * 2.5 + 2 * blotch[y][x]);
    return mix(shade(field, f), faded, (blotch[y][x] - 0.5) * 1.2);
  }));
}
/** Stitches a map of thread keys onto the cloth at `at`: lit along each shape's upper left edge, dark along its lower right. */
function embroider(px, art, [ox, oy], threads, seed) {
  const rng = new PyRandom(seed);
  const key = (r, c) => (r >= 0 && r < art.length && c >= 0 && c < art[r].length ? art[r][c] : null);
  const inside = (r, c) => oy + r >= 0 && oy + r < px.length && ox + c >= 0 && ox + c < px[0].length;
  art.forEach((line, r) => line.forEach((k, c) => {
    if (k === null || !inside(r, c)) return;
    let f = 0.92 + 0.16 * rng.random();
    if (key(r + 1, c) === null || key(r, c + 1) === null) f *= 0.78;
    else if (key(r - 1, c) === null || key(r, c - 1) === null) f *= 1.1;
    px[oy + r][ox + c] = shade(threads[k], f);
  }));
  // A dark stitch line on the cloth under and right of the embroidery.
  art.forEach((line, r) => line.forEach((k, c) => {
    if (k === null && (key(r - 1, c) || key(r, c - 1)) && inside(r, c)) px[oy + r][ox + c] = shade(px[oy + r][ox + c], 0.7);
  }));
}
/** A line of trim all round, `inset` px from the edge. */
function trim(px, inset, color, seed, dashed = false) {
  const rng = new PyRandom(seed), w = px[0].length, h = px.length;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (Math.min(x, y, w - 1 - x, h - 1 - y) === inset && !(dashed && (x + y) % 3 === 0)) px[y][x] = shade(color, 0.85 + 0.25 * rng.random());
  }
}
/** Age: dirtier toward the bottom, stained in patches, a few threads pulled. */
function weather(px, seed, grime) {
  const rng = new PyRandom(seed), w = px[0].length, h = px.length;
  const stain = valueNoise(w, h, Math.max(2, Math.floor(w / 4)), rng);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) px[y][x] = mix(px[y][x], grime, 0.45 * (y / h) ** 2 + Math.max(0, stain[y][x] - 0.62) * 1.6);
  for (let s = 0; s < Math.floor(w / 5); s++) {
    const x = rng.randrange(w), y = rng.randrange(h);
    for (let i = 0, n = rng.randint(4, 14); i < n; i++) if (y + i < h) px[y + i][x] = shade(px[y + i][x], 0.72);
  }
  return px;
}
const BLACK_CLOTH = [34, 32, 34], BLACK_FADED = [64, 60, 58], RUST_CLOTH = [122, 44, 30], RUST_FADED = [150, 86, 60];
const THREAD_GOLD = [206, 164, 70], THREAD_GOLD_DARK = [130, 96, 40], THREAD_BLACK = [24, 22, 22], CLOTH_GRIME = [30, 26, 22];
/** Black cloth, gold trim; the two-headed eagle, a line of glyphs, a skull. */
function eagleBanner(seed, helpers) {
  const px = cloth(BW, BH, seed, BLACK_CLOTH, BLACK_FADED, 0.1);
  for (const inset of [3, 4]) trim(px, inset, THREAD_GOLD, seed + inset);
  trim(px, 7, THREAD_GOLD_DARK, seed + 7, true);
  const threads = { light: THREAD_GOLD, dark: THREAD_GOLD_DARK, black: THREAD_BLACK, bone: BONE };
  const art = eagleEmblem(48, helpers);
  embroider(px, art, [Math.floor((BW - art[0].length) / 2), 12], threads, seed + 1);
  const rng = new PyRandom(seed + 2);
  for (let i = 0; i < 6; i++) embroider(px, grow(rng.choice(GLYPHS), 2, { "#": "dark" }), [9 + i * 8, 50], threads, seed + 3 + i);
  const parts = { skull: "bone", hole: "black" };
  const skull = raster(24, 31, (x, y) => parts[skullPart(x / 25 - 0.48, y / 25 - 0.61)] ?? null);
  embroider(px, skull, [Math.floor((BW - 24) / 2), 66], threads, seed + 9);
  return weather(px, seed + 10, CLOTH_GRIME);
}
/** Rust-red cloth, black and bone trim; the cog and skull, and a band of punched-tape marks under it. */
function cogBanner(seed) {
  const px = cloth(BW, BH, seed, RUST_CLOTH, RUST_FADED, 0.1);
  for (const inset of [3, 4]) trim(px, inset, THREAD_BLACK, seed + inset);
  trim(px, 7, BONE, seed + 7, true);
  const threads = { light: BONE, dark: THREAD_BLACK };
  embroider(px, cogEmblem(48), [Math.floor((BW - 48) / 2), 12], threads, seed + 1);
  const rng = new PyRandom(seed + 2);
  [68, 76, 84].forEach((y, row) => {
    const on = range(12).map(() => rng.random() < 0.6);
    const marks = range(4).map(() => range(46).map((x) => (on[Math.floor(x / 4)] && x % 4 < 2 ? (row === 1 ? "light" : "dark") : null)));
    embroider(px, marks, [9, y], threads, seed + 3 + row);
  });
  for (const y of [65, 91]) for (let x = 9; x < 55; x++) if (x % 3) px[y][x] = shade(BONE, 0.9);
  return weather(px, seed + 10, CLOTH_GRIME);
}

// One texture: the pixels made by `make`, laid onto the canvas by a custom layer.
const pixels = (make) => {
  const px = make();
  return { size: [px[0].length, px.length], layers: [{ type: "custom", draw: (c) => px.forEach((row, y) => row.forEach((p, x) => c.set(x, y, [...p, 255]))) }] };
};

export default (helpers) => ({
  out: "../ReliquiarumMod/assets/nf3treliquiarum/textures/block",
  textures: {
    // The fallen giant: armor faded to grey-brown, eaten by rust and verdigris.
    "armor-worn": pixels(() => worn(32, 32, 11, [110, 100, 86], [54, 46, 38], { rust: [122, 70, 38], patina: [92, 132, 110], rustAmount: 0.24, patinaAmount: 0.16, chips: 0.006, scratches: 6 })),
    "armor-trim-worn": pixels(() => worn(32, 32, 12, [84, 76, 66], [40, 35, 30], { rust: [110, 62, 34], patina: [80, 116, 96], rustAmount: 0.3, patinaAmount: 0.12, chips: 0.008, scratches: 3 })),
    "joint-worn": pixels(() => worn(16, 16, 13, [40, 37, 34], [20, 18, 16], { rust: [80, 48, 30], rustAmount: 0.2 })),
    "wax-worn": pixels(() => worn(16, 16, 14, [128, 30, 24], [64, 16, 14], { chips: 0.04 })),
    "parchment-worn": pixels(() => parchment(15)),
    // The reliquary: worn bronze and its decorations.
    "bronze-worn": pixels(() => worn(32, 32, 21, BRONZE, GRIME, { patina: VERDIGRIS, patinaAmount: 0.25, chips: 0.004, scratches: 5 })),
    "bronze-dark": pixels(() => worn(32, 32, 22, BRONZE_DARK, GRIME, { patina: VERDIGRIS, patinaAmount: 0.2, chips: 0.005 })),
    // Raised ornaments (eagle, crest): brighter where hands and time rubbed the grime off.
    "bronze-bright": pixels(() => worn(32, 32, 29, BRONZE_HI, BRONZE, { patina: VERDIGRIS, patinaAmount: 0.12 })),
    "iron-worn": pixels(() => worn(16, 16, 23, [66, 62, 58], [30, 28, 26], { rust: [110, 62, 34], rustAmount: 0.35 })),
    "rune-band": pixels(() => runeBand(96, 12, 24)),
    "rune-column": pixels(() => runeBand(12, 72, 25, true)),
    "skull-roundel": pixels(() => skullRoundel(26)),
    keyhole: pixels(() => keyhole(27)),
    backdrop: pixels(() => backdrop(28)),
    // The stasis pod and the automaton inside it.
    "pod-frame": pixels(() => worn(32, 32, 31, [44, 46, 46], [18, 19, 19], { scratches: 4 })),
    // The automaton's frame: blackened metal, only scratches catching the light.
    "automaton-metal": pixels(() => worn(32, 32, 32, [46, 47, 48], [16, 16, 17], { rust: [70, 50, 36], rustAmount: 0.1, chips: 0.004, scratches: 10 })),
    "automaton-dark": pixels(() => worn(32, 32, 33, [28, 29, 30], [10, 10, 11], { scratches: 4 })),
    // Dead now: the pod's inside is dull, stained grey; the spilled fluid has dried to a crust.
    "pod-interior": pixels(() => worn(32, 32, 39, [64, 65, 63], [34, 34, 32], { rust: [88, 74, 56], rustAmount: 0.15, scratches: 3 })),
    residue: pixels(() => worn(16, 16, 40, [58, 54, 44], [30, 28, 22], { chips: 0.03 })),
    // The scarab's shell: worn grey metal, scratched, lighter than its black legs.
    "scarab-shell": pixels(() => worn(32, 32, 43, [82, 84, 84], [40, 41, 41], { rust: [84, 70, 54], rustAmount: 0.12, chips: 0.005, scratches: 8 })),
    "glass-grey": pixels(() => shardGlass(41, [70, 74, 76], [48, 51, 53])),
    "cloth-tattered": pixels(() => worn(16, 16, 34, [38, 44, 36], [16, 18, 15], { chips: 0.05 })),
    "glyph-panel-dead": pixels(() => glyphPanel(42, [92, 94, 90], [58, 60, 57])),
    // The eagle (the statue, and the relief on the reliquary): worn gilding.
    "gilt-worn": pixels(() => gilt(51)),
    "gilt-dark": pixels(() => gilt(52, [124, 94, 40], [78, 58, 26], 0.14)),
    "gilt-feather": pixels(() => giltFeather(53)),
    "gilt-plumage": pixels(() => giltPlumage(54)),
    // The banners: embroidered cloth, and the plain cloth of their hems and loops.
    "banner-eagle": pixels(() => eagleBanner(61, helpers)),
    "banner-cog": pixels(() => cogBanner(62)),
    "cloth-black": pixels(() => weather(cloth(16, 16, 63, BLACK_CLOTH, BLACK_FADED), 64, CLOTH_GRIME)),
    "cloth-rust": pixels(() => weather(cloth(16, 16, 65, RUST_CLOTH, RUST_FADED), 66, CLOTH_GRIME)),
  },
});
