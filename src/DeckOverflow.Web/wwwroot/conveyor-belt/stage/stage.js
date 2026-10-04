// stage.js - de stage van de Lopende Band: een werkvloer (docs/afdelingen/conveyor-belt/README.md, Het scherm).
// Tekent het rooster als een betonnen vloer met band die echt draait, laat de producten rijden, en speelt het einde af:
// goedgekeurd en weg naar het front, afgekeurd, gevallen, of een oneindige loop waarbij het licht uitgaat.
// Geen spelregels: de shell stuurt het bord, de tikken en de afloop; de stage meldt alleen klikken terug.

import { initAudio, sfx } from '../../audio/audio.js';

const CELL = 64;
const BW = 40;            // de breedte van de band
const SKY = 36;           // een smalle strook bovenaan; later komt hier het front in de verte
const MARGIN = 28;
const WALK = 150;         // ruimte rechts van de uitgang om weg te wandelen

/** De feel van de band, op één plek af te stellen. */
const JUICE = {
  rollerGap: 9,           // afstand tussen twee rollen
  rollerSpeed: 24,        // pixels per seconde
  // Een oneindige loop: het licht gaat uit en het product draait rondjes die steeds sneller gaan, tot de band stopt
  loop: { dim: 0.86, fadeS: 0.35, firstStepS: 0.16, speedUp: 0.72, minStepS: 0.035, totalS: 2.6, maxLaps: 30 },
};

const C = {
  paper: 0xf7f5ee, white: 0xffffff, ink: 0x161616, muted: 0x8a877e, rule: 0xd9d5c9, shade: 0xebe7dc,
  concrete: 0xefece3, joint: 0xddd8cb, speck: 0xd6d0c2, chevron: 0xc4bfb2, zss: 0xe2790a,
  int: 0x2f6fe4, byte: 0xe8a800, double: 0x8a5cf0, string: 0x2e9d5b,
};
const FONT = '"Helvetica Neue", Helvetica, Arial, sans-serif';
const MONO = 'Consolas, "Courier New", monospace';

const STEP = { Up: [0, -1], Right: [1, 0], Down: [0, 1], Left: [-1, 0] };
const OPPOSITE = { Up: 'Down', Right: 'Left', Down: 'Up', Left: 'Right' };
const ANG = { Up: -Math.PI / 2, Right: 0, Down: Math.PI / 2, Left: Math.PI };

const S = {
  app: null, dotnet: null, root: null, layers: {},
  level: null, cells: new Map(), segments: [], rollers: null,
  crate: null, textures: new Map(), phase: 0, selected: null, loopFx: null,
  calm: window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false,
  W: 600, H: 400,
};

// ---------------------------------------------------------------- publieke API

export async function init(host, dotnet) {
  S.dotnet = dotnet;
  initAudio();
  const app = new PIXI.Application();
  await app.init({ resizeTo: host, background: C.paper, antialias: true, resolution: Math.min(window.devicePixelRatio || 1, 2), autoDensity: true });
  host.appendChild(app.canvas);
  S.app = app;
  S.root = new PIXI.Container();
  app.stage.addChild(S.root);
  for (const name of ['sky', 'floor', 'belts', 'rollers', 'pieces', 'dark', 'crate', 'fx', 'top']) {
    S.layers[name] = new PIXI.Container();
    S.root.addChild(S.layers[name]);
  }
  app.ticker.add(tick);
  S.observer = new ResizeObserver(() => S.app?.resize());
  S.observer.observe(host);
}

/** Een nieuwe puzzel: rooster, achtergrond en de tekening van het product. */
export async function setLevel(level) {
  S.level = level;
  S.labels = level.labels ?? {};
  S.W = MARGIN * 2 + level.width * CELL + WALK;
  S.H = SKY + level.height * CELL + MARGIN;
  await loadTexture(level.product);
  for (const layer of Object.values(S.layers)) layer.removeChildren().forEach((c) => c.destroy({ children: true }));
  S.cells.clear();
  S.segments = [];
  S.crate = null;
  S.loopFx = null;
  drawSky();
  drawFloor();
  S.rollers = new PIXI.Graphics();
  S.layers.rollers.addChild(S.rollers);
  fit();
}

/** Het bord tekenen: elk vakje met wat erop ligt, en de band die de stukken verbindt. */
export function setBoard(cells) {
  clearLoopFx();
  for (const name of ['belts', 'pieces']) S.layers[name].removeChildren().forEach((c) => c.destroy({ children: true }));
  S.cells.clear();
  S.segments = [];

  // Waar komt een product binnen? Langs de zijden waar een buur zijn uitgang op dit vakje richt
  const feeds = new Map();
  for (const c of cells) {
    for (const d of outputs(c)) {
      const k = key(c.x + STEP[d][0], c.y + STEP[d][1]);
      if (!feeds.has(k)) feeds.set(k, new Set());
      feeds.get(k).add(OPPOSITE[d]);
    }
  }

  const base = new PIXI.Graphics();
  S.layers.belts.addChild(base);
  for (const cell of cells) {
    if (cell.kind === 'empty') continue;
    drawBelt(base, cell, [...(feeds.get(key(cell.x, cell.y)) ?? [])]);
    const g = drawPiece(cell);
    if (g) {
      g.position.set(cx(cell.x), cy(cell.y));
      S.layers.pieces.addChild(g);
    }
    S.cells.set(key(cell.x, cell.y), { cell, g });
  }
  drawRollers();
  drawSelection();
}

export function select(x, y) {
  S.selected = x === null || x === undefined ? null : { x, y };
  drawSelection();
}

/** Eén tik: het product rijdt naar zijn vakje. Bij een machine of poort speelt die even. */
export async function frame(f, ms, instant) {
  clearLoopFx();
  if (!S.crate || S.crate.product !== f.product) newCrate(f);
  setCrate(f);
  updateCounters(f.counters);
  const x = cx(f.x), y = cy(f.y);
  if (instant || document.hidden) {
    gsap.killTweensOf(S.crate.c);
    S.crate.c.position.set(x, y);
    S.crate.c.alpha = 1;
    S.crate.c.rotation = 0;
    return;
  }
  await tween(S.crate.c, { x, y, duration: ms / 1000, ease: 'none' });
  const here = S.cells.get(key(f.x, f.y));
  if (here) react(here, f);
}

/** De afloop van één testgeval: goedgekeurd en weg naar het front, afgekeurd, gevallen, of een oneindige loop. */
export async function ending(e) {
  if (!S.crate || document.hidden) return;
  const c = S.crate.c;
  if (e.ending === 'Delivered') {
    stamp(c.x, c.y - 40, L('approved'), C.ink);
    sfx('stamp');
    sfx('approve', { volume: 0.6 });
    await wait(550);
    // Van de band af, naar rechts, en dan de mist in, richting het front
    const tl = gsap.timeline();
    tl.to(c, { x: S.W + 30, duration: 1.2, ease: 'power1.in' })
      .to(c, { alpha: 0, duration: 0.3 }, '-=0.3');
    hop(c, 1.2);
    await tl;
  } else if (e.ending === 'Wrong') {
    stamp(c.x, c.y - 40, '✗ ' + L('rejected'), C.zss);
    sfx('stamp');
    sfx('buzz', { volume: 0.5 });
    await wait(550);
    gsap.delayedCall(0.45, () => sfx('bin', { volume: 0.7 }));
    await gsap.to(c, { y: c.y + 70, rotation: 0.9, alpha: 0, duration: 0.6, ease: 'power2.in' });
  } else if (e.ending === 'FellOff') {
    gsap.delayedCall(0.4, () => sfx('fall'));
    await gsap.to(c, { y: c.y + 50, rotation: -1.4, alpha: 0, duration: 0.6, ease: 'power2.in' });
  } else {
    await lightsOut(e.loop ?? []);
  }
}

/** Het product van de band halen, voor een nieuw testgeval of als de speler weer bouwt. */
export function clearCrate() {
  clearLoopFx();
  if (S.crate) S.crate.c.destroy({ children: true });
  S.crate = null;
  updateCounters([]);
}

export function dispose() {
  S.observer?.disconnect();
  S.app?.destroy(true, { children: true });
  S.app = null;
}

// ---------------------------------------------------------------- hulp

const key = (x, y) => `${x},${y}`;
const cx = (x) => MARGIN + x * CELL + CELL / 2;
const cy = (y) => SKY + y * CELL + CELL / 2;
const wait = (ms) => new Promise((r) => setTimeout(r, ms));
/** Een tween om op te wachten die ook klaar is als hij afgebroken wordt (terugspoelen), zodat de shell nooit blijft hangen. */
const tween = (target, vars) => new Promise((resolve) => gsap.to(target, { ...vars, onComplete: resolve, onInterrupt: resolve }));
/** Een woord van de shell: teksten staan nooit in de stage zelf. */
const L = (key) => S.labels?.[key] ?? key;
const rnd = (a, b) => a + Math.random() * (b - a);

function text(value, size = 14, color = C.ink, font = FONT, bold = true) {
  const t = new PIXI.Text({ text: String(value), style: { fontFamily: font, fontSize: size, fontWeight: bold ? '700' : '400', fill: color } });
  t.anchor.set(0.5);
  return t;
}

async function loadTexture(name) {
  if (!name || S.textures.has(name)) return;
  try { S.textures.set(name, await PIXI.Assets.load(name)); } catch { S.textures.set(name, null); }
}

function fit() {
  if (!S.app) return;
  const { width, height } = S.app.screen;
  const scale = Math.min(width / S.W, height / S.H);
  S.root.scale.set(scale);
  S.root.position.set((width - S.W * scale) / 2, (height - S.H * scale) / 2);
}

/** Waar een stuk het product naartoe stuurt. Een poort en een teller hebben er twee. */
function outputs(cell) {
  switch (cell.kind) {
    case 'belt': case 'machine': case 'source': return [cell.out];
    case 'gate': return [...new Set([cell.ifTrue, cell.ifFalse])];
    case 'counter': return [...new Set([cell.loop, cell.done])];
    default: return [];
  }
}

// ---------------------------------------------------------------- het front in de verte

function drawSky() {
  // Voorlopig leeg: de vloer heeft zelf een rand. Het front in de verte komt hier later, strakker (zie de todo).
}

// ---------------------------------------------------------------- de werkvloer

function drawFloor() {
  const g = new PIXI.Graphics();
  const w = S.level.width * CELL, h = S.level.height * CELL;
  g.rect(MARGIN, SKY, w, h).fill(C.concrete);
  // Voegen tussen de tegels, en een paar spikkels per tegel: beton, geen ruitjespapier. Vast per vakje, geen toeval
  for (let x = 1; x < S.level.width; x++) g.moveTo(MARGIN + x * CELL, SKY).lineTo(MARGIN + x * CELL, SKY + h);
  for (let y = 1; y < S.level.height; y++) g.moveTo(MARGIN, SKY + y * CELL).lineTo(MARGIN + w, SKY + y * CELL);
  g.stroke({ width: 2, color: C.joint });
  for (let x = 0; x < S.level.width; x++) {
    for (let y = 0; y < S.level.height; y++) {
      for (let i = 0; i < 3; i++) {
        const h1 = ((x * 73 + y * 151 + i * 37) % 97) / 97, h2 = ((x * 41 + y * 89 + i * 59) % 89) / 89;
        g.circle(MARGIN + x * CELL + 8 + h1 * (CELL - 16), SKY + y * CELL + 8 + h2 * (CELL - 16), 1.3);
      }
    }
  }
  g.fill(C.speck);
  g.rect(MARGIN, SKY, w, h).stroke({ width: 3, color: C.ink });
  S.layers.floor.addChild(g);

  // Klikken op een vakje gaat naar de shell
  const hit = new PIXI.Graphics().rect(MARGIN, SKY, w, h).fill({ color: C.white, alpha: 0.001 });
  hit.eventMode = 'static';
  hit.cursor = 'pointer';
  hit.on('pointertap', (e) => {
    const p = e.getLocalPosition(S.layers.floor);
    const x = Math.floor((p.x - MARGIN) / CELL), y = Math.floor((p.y - SKY) / CELL);
    if (x >= 0 && y >= 0 && x < S.level.width && y < S.level.height) S.dotnet?.invokeMethodAsync('CellClicked', x, y);
  });
  S.layers.top.addChild(hit);
  S.selection = new PIXI.Graphics();
  S.layers.top.addChild(S.selection);
}

function drawSelection() {
  if (!S.selection) return;
  S.selection.clear();
  if (!S.selected) return;
  const x = MARGIN + S.selected.x * CELL + 2, y = SKY + S.selected.y * CELL + 2, s = CELL - 4;
  for (const [x1, y1, x2, y2] of [[x, y, x + s, y], [x + s, y, x + s, y + s], [x + s, y + s, x, y + s], [x, y + s, x, y]]) {
    const len = Math.hypot(x2 - x1, y2 - y1);
    for (let d = 0; d < len; d += 10) {
      const a = d / len, b = Math.min(1, (d + 5) / len);
      S.selection.moveTo(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a).lineTo(x1 + (x2 - x1) * b, y1 + (y2 - y1) * b);
    }
  }
  S.selection.stroke({ width: 3, color: C.ink });
}

// ---------------------------------------------------------------- de band: rollen die draaien

/**
 * Band onder een vakje. Een rechte band loopt van rand tot rand. Een bocht of een samenkomst is een draaischijf in het
 * midden met een halve band naar elke rand die meedoet. Onder een machine, poort, teller, trechter of perron ligt een
 * halve band naar elke ingang en uitgang, zodat de vloer één doorlopende band wordt.
 */
function drawBelt(g, cell, into) {
  const outs = outputs(cell);
  const ins = into.filter((d) => !outs.includes(d));
  const x = cx(cell.x), y = cy(cell.y);
  if (cell.kind === 'belt' && (ins.length === 0 || (ins.length === 1 && ins[0] === OPPOSITE[cell.out]))) {
    segment(g, x, y, cell.out, -CELL / 2, CELL / 2, 1, -CELL / 2);
    return;
  }
  for (const d of outs) segment(g, x, y, d, 0, CELL / 2, 1, BW / 2);
  for (const d of ins) segment(g, x, y, d, 0, CELL / 2, -1, BW / 2);
  if (cell.kind === 'belt') {
    // De draaischijf: een plaat met een ring, met een rand aan de kanten waar geen band aansluit
    const open = new Set([...outs, ...ins]);
    g.rect(x - BW / 2, y - BW / 2, BW, BW).fill(C.white);
    g.circle(x, y, BW / 2 - 6).stroke({ width: 2, color: C.rule });
    for (const d of ['Up', 'Right', 'Down', 'Left']) {
      if (open.has(d)) continue;
      const [dx, dy] = STEP[d];
      const px = -dy, py = dx;
      g.moveTo(x + dx * BW / 2 + px * BW / 2, y + dy * BW / 2 + py * BW / 2)
        .lineTo(x + dx * BW / 2 - px * BW / 2, y + dy * BW / 2 - py * BW / 2)
        .stroke({ width: 3, color: C.ink });
    }
  }
}

/**
 * Een stuk band langs richting d, van t0 tot t1 gemeten vanuit het midden van het vakje. sign +1 loopt naar buiten,
 * -1 naar het midden. Randen pas vanaf edgeFrom, zodat een bocht geen lijn dwars over de andere helft trekt.
 */
function segment(g, x, y, d, t0, t1, sign, edgeFrom) {
  const [dx, dy] = STEP[d];
  const px = -dy, py = dx;
  const at = (t, s) => [x + dx * t + px * s, y + dy * t + py * s];
  const [ax, ay] = at(t0, -BW / 2), [bx, by] = at(t1, BW / 2);
  g.rect(Math.min(ax, bx), Math.min(ay, by), Math.abs(bx - ax), Math.abs(by - ay)).fill(C.white);
  for (const s of [-BW / 2, BW / 2]) {
    const [x1, y1] = at(Math.max(t0, edgeFrom), s), [x2, y2] = at(t1, s);
    g.moveTo(x1, y1).lineTo(x2, y2).stroke({ width: 3, color: C.ink });
  }
  // Een pijlpunt die zegt waar de band heen loopt, ook als hij stilstaat
  const tm = (Math.max(t0, edgeFrom) + t1) / 2, k = 5 * sign;
  const [c1x, c1y] = at(tm - k, -8), [c2x, c2y] = at(tm + k, 0), [c3x, c3y] = at(tm - k, 8);
  g.moveTo(c1x, c1y).lineTo(c2x, c2y).lineTo(c3x, c3y).stroke({ width: 3, color: C.chevron, cap: 'round', join: 'round' });
  S.segments.push({ x, y, dx, dy, px, py, from: Math.max(t0, edgeFrom) + 2, to: t1 - 1, sign });
}

/** De rollen: dunne lijnen dwars op de band, die met de band meeschuiven. Elke frame opnieuw getekend. */
function drawRollers() {
  const g = S.rollers;
  if (!g) return;
  g.clear();
  const gap = JUICE.rollerGap;
  for (const s of S.segments) {
    const shift = ((S.phase * s.sign) % gap + gap) % gap;
    for (let t = s.from + shift; t < s.to; t += gap) {
      g.moveTo(s.x + s.dx * t + s.px * (-BW / 2 + 4), s.y + s.dy * t + s.py * (-BW / 2 + 4))
        .lineTo(s.x + s.dx * t + s.px * (BW / 2 - 4), s.y + s.dy * t + s.py * (BW / 2 - 4));
    }
  }
  g.stroke({ width: 2, color: C.ink, alpha: 0.78 });
}

// ---------------------------------------------------------------- de stukken

/** Een pijlpunt net buiten een stuk, naar de rand van het vakje. */
function arrowHead(g, dir, r = 30, color = C.ink) {
  const a = ANG[dir];
  const tip = [Math.cos(a) * r, Math.sin(a) * r];
  const back = [Math.cos(a) * (r - 8), Math.sin(a) * (r - 8)];
  const px = Math.cos(a + Math.PI / 2) * 6, py = Math.sin(a + Math.PI / 2) * 6;
  g.poly([tip[0], tip[1], back[0] + px, back[1] + py, back[0] - px, back[1] - py]).fill(color);
}

/** Een klein label op de band bij een uitgang: ja of nee, nog eens of klaar. Vol in inkt, of hol. */
function tag(c, dir, word, solid) {
  const a = ANG[dir];
  const t = text(word, 9, solid ? C.white : C.ink);
  const w = Math.max(18, t.width + 8);
  const bg = new PIXI.Graphics().roundRect(-w / 2, -7, w, 14, 3).fill(solid ? C.ink : C.white).stroke({ width: 1.5, color: C.ink });
  const tg = new PIXI.Container();
  tg.addChild(bg, t);
  tg.position.set(Math.cos(a) * 31, Math.sin(a) * 31);
  c.addChild(tg);
}

/** Vier schroefjes in de hoeken: dit stuk zit vast en krijg je niet los. */
function screws(g) {
  for (const [x, y] of [[-27, -27], [27, -27], [-27, 27], [27, 27]]) {
    g.circle(x, y, 3.2).fill(C.white).stroke({ width: 1.5, color: C.ink });
    g.moveTo(x - 2, y + 2).lineTo(x + 2, y - 2).stroke({ width: 1.2, color: C.ink });
  }
}

/** Arcering zoals op een laadperron: schuine lijnen binnen een rechthoek. */
function hatch(g, x0, y0, w, h, step) {
  for (let c = -h; c < w; c += step) {
    const xa = Math.max(0, c), ya = xa - c;
    const xb = Math.min(w, c + h), yb = xb - c;
    if (ya > h || yb < 0) continue;
    g.moveTo(x0 + xa, y0 + Math.min(h, ya)).lineTo(x0 + xb, y0 + Math.max(0, Math.min(h, yb)));
  }
}

function drawPiece(cell) {
  if (cell.kind === 'belt') return null;
  const c = new PIXI.Container();
  const g = new PIXI.Graphics();
  c.addChild(g);
  switch (cell.kind) {
    case 'machine': {
      // Een kast met een meter: wat erdoor komt, wordt bewerkt
      g.roundRect(-25, -21, 50, 46, 6).fill(C.ink);
      g.roundRect(-25, -25, 50, 46, 6).fill(C.white).stroke({ width: 3.5, color: C.ink });
      g.circle(17, -24, 7).fill(C.white).stroke({ width: 2.5, color: C.ink });
      g.moveTo(17, -24).lineTo(20, -29).stroke({ width: 2, color: C.ink });
      arrowHead(g, cell.out);
      const t = text(cell.text, cell.text.length > 4 ? 14 : 18, C.ink, MONO);
      t.y = -2;
      c.addChild(t);
      break;
    }
    case 'gate': {
      // Een rond ventiel: klopt de voorwaarde, dan "ja", anders "nee"
      g.circle(0, 0, 25).fill(C.white).stroke({ width: 4, color: C.ink });
      g.circle(0, 0, 19).stroke({ width: 1.5, color: C.rule });
      const t = text(cell.text, cell.text.length > 6 ? 9 : 13, C.ink, MONO);
      c.addChild(t);
      tag(c, cell.ifTrue, L('yes'), true);
      if (cell.ifFalse !== cell.ifTrue) tag(c, cell.ifFalse, L('no'), false);
      break;
    }
    case 'counter': {
      // Een telwerk, zoals een kilometerteller: nog eens rond, of klaar
      g.roundRect(-25, -23, 50, 46, 5).fill(C.white).stroke({ width: 3.5, color: C.ink });
      g.roundRect(-20, -18, 40, 36, 3).stroke({ width: 1.2, color: C.ink });
      g.roundRect(-17, -10, 34, 20, 3).fill(C.ink);
      const t = text(`0/${cell.times}`, 12, C.white, MONO);
      c.addChild(t);
      c.counterText = t;
      c.times = cell.times;
      tag(c, cell.loop, '↻', true);
      if (cell.done !== cell.loop) tag(c, cell.done, '⇥', false);
      break;
    }
    case 'source': {
      // De trechter waar de producten binnenkomen
      g.poly([-28, -25, 28, -25, 15, 6, -15, 6]).fill(C.white).stroke({ width: 3.5, color: C.ink, join: 'round' });
      g.moveTo(-21, -16).lineTo(21, -16).stroke({ width: 2, color: C.ink });
      g.rect(-9, 6, 18, 12).fill(C.ink);
      const t = text(L('in'), 10, C.ink);
      t.y = -5;
      c.addChild(t);
      arrowHead(g, cell.out);
      break;
    }
    case 'output': {
      // Het laadperron naar het front: gearceerd, met een bordje
      g.rect(-29, -29, 58, 58).fill(C.white);
      hatch(g, -29, -29, 58, 58, 10);
      g.stroke({ width: 3, color: C.ink });
      g.rect(-29, -29, 58, 58).stroke({ width: 3.5, color: C.ink });
      g.rect(-24, -14, 48, 28).fill(C.white).stroke({ width: 2, color: C.ink });
      const t = new PIXI.Text({
        text: L('out'),
        style: { fontFamily: FONT, fontSize: 8, fontWeight: '900', fill: C.ink, align: 'center', wordWrap: true, wordWrapWidth: 44, lineHeight: 9 },
      });
      t.anchor.set(0.5);
      c.addChild(t);
      break;
    }
  }
  if (cell.fixed && cell.kind !== 'source' && cell.kind !== 'output') screws(g);
  return c;
}

function updateCounters(counters) {
  const live = new Map((counters ?? []).map(([x, y, n]) => [key(x, y), n]));
  for (const [k, { g }] of S.cells) {
    if (!g?.counterText) continue;
    g.counterText.text = `${live.get(k) ?? 0}/${g.times}`;
  }
}

// ---------------------------------------------------------------- het product

function newCrate(f) {
  clearCrate();
  const c = new PIXI.Container();
  const tex = S.textures.get(f.product);
  const body = tex ? new PIXI.Sprite(tex) : new PIXI.Graphics().roundRect(-18, -18, 36, 36, 4).fill(C.white).stroke({ width: 2, color: C.ink });
  if (tex) {
    body.anchor.set(0.5, 0.8);
    const s = 46 / Math.max(tex.width, tex.height);
    body.scale.set(s);
    body.baseScale = s;
  }
  const tag = new PIXI.Container();
  const tagBg = new PIXI.Graphics();
  const tagText = text('', 12, C.white, MONO);
  tag.addChild(tagBg, tagText);
  tag.y = 18;
  c.addChild(body, tag);
  S.layers.crate.addChild(c);
  S.crate = { c, body, tag, tagBg, tagText, product: f.product };
}

function setCrate(f) {
  const { body, tagBg, tagText } = S.crate;
  tagText.text = f.value;
  const w = Math.max(30, tagText.width + 12);
  // Het label van de waarde in de kleur van haar type, met een harde schaduw zoals een kaartje
  tagBg.clear().roundRect(-w / 2 + 2, -7, w, 18, 3).fill(C.ink).roundRect(-w / 2, -9, w, 18, 3).fill(C[f.kind] ?? C.ink).stroke({ width: 2, color: C.ink });
  // Het product groeit naarmate het dichter bij de bestelling komt
  const grow = 0.7 + Math.min(1.2, Math.max(0, f.growth)) * 0.45;
  if (body.baseScale) gsap.to(body.scale, { x: body.baseScale * grow, y: body.baseScale * grow, duration: 0.18 });
}

/** Een machine of poort reageert als het product erop komt. */
function react(here, f) {
  const g = here.g;
  if (!g) return;
  if (here.cell.kind === 'machine') {
    gsap.fromTo(g.scale, { x: 1.12, y: 0.9 }, { x: 1, y: 1, duration: 0.25, ease: 'back.out(3)' });
    sfx('machine', { volume: 0.6 });
    if (f.note === 'Truncated') sfx('tinkle', { volume: 0.5 });
    if (f.note === 'Overflow') sfx('glitch', { volume: 0.7 });
    sparks(cx(f.x), cy(f.y), f.note === 'Overflow' ? 14 : 6);
    if (f.note === 'Truncated') crumbs(cx(f.x), cy(f.y));
    if (f.note === 'Overflow') {
      const t = text(L('overflow'), 13, C.ink);
      t.position.set(cx(f.x), cy(f.y) - 38);
      S.layers.fx.addChild(t);
      gsap.to(t, { y: t.y - 20, alpha: 0, duration: 1.2, onComplete: () => t.destroy() });
      gsap.fromTo(S.crate.c, { rotation: -0.3 }, { rotation: 0, duration: 0.4, ease: 'elastic.out(1, 0.3)' });
    }
  } else if (here.cell.kind === 'gate') {
    gsap.fromTo(g, { rotation: f.gate ? 0.18 : -0.18 }, { rotation: 0, duration: 0.3, ease: 'back.out(3)' });
    sfx('latch', { volume: 0.6, rate: f.gate ? 1.1 : 0.9 });
  } else if (here.cell.kind === 'counter') {
    gsap.fromTo(g.scale, { x: 1.08, y: 1.08 }, { x: 1, y: 1, duration: 0.2 });
    sfx('tick', { volume: 0.6 });
  }
}

function sparks(x, y, n) {
  for (let i = 0; i < n; i++) {
    const s = new PIXI.Graphics().rect(-1.5, -1.5, 3, 3).fill(i % 2 ? C.ink : C.muted);
    s.position.set(x, y);
    S.layers.fx.addChild(s);
    const a = rnd(0, Math.PI * 2), d = rnd(14, 30);
    gsap.to(s, { x: x + Math.cos(a) * d, y: y + Math.sin(a) * d, alpha: 0, rotation: rnd(-3, 3), duration: rnd(0.3, 0.6), onComplete: () => s.destroy() });
  }
}

/** Afkappen: wat na de komma stond, valt als zaagsel op de grond. */
function crumbs(x, y) {
  for (let i = 0; i < 6; i++) {
    const s = new PIXI.Graphics().circle(0, 0, 1.6).fill(C.muted);
    s.position.set(x + rnd(-8, 8), y + 10);
    S.layers.fx.addChild(s);
    gsap.to(s, { y: s.y + rnd(14, 24), alpha: 0, duration: rnd(0.5, 0.9), ease: 'power2.in', onComplete: () => s.destroy() });
  }
  const t = text('✂ .5', 12, C.muted);
  t.position.set(x + 20, y + 14);
  S.layers.fx.addChild(t);
  gsap.to(t, { y: t.y + 14, alpha: 0, duration: 1, onComplete: () => t.destroy() });
}

/** Een stempel die neerkomt: gelukt in inkt, mislukt in oranje. */
function stamp(x, y, word, color) {
  const c = new PIXI.Container();
  const t = text(word, 15, color);
  const g = new PIXI.Graphics().roundRect(-t.width / 2 - 8, -13, t.width + 16, 26, 4).fill(C.white).stroke({ width: 3, color });
  c.addChild(g, t);
  c.position.set(x, y);
  c.rotation = -0.12;
  c.scale.set(2.4);
  c.alpha = 0;
  S.layers.fx.addChild(c);
  gsap.to(c, { alpha: 1, duration: 0.12 });
  gsap.to(c.scale, { x: 1, y: 1, duration: 0.3, ease: 'back.out(2.5)' });
  gsap.to(c, { alpha: 0, duration: 0.4, delay: 1.3, onComplete: () => c.destroy({ children: true }) });
}

/** Een klein huppeltje tijdens het wegwandelen. */
function hop(c, seconds) {
  const steps = Math.round(seconds / 0.18);
  const tl = gsap.timeline();
  for (let i = 0; i < steps; i++) tl.to(c, { rotation: i % 2 ? 0.12 : -0.12, duration: 0.09 }).to(c, { rotation: 0, duration: 0.09 });
}

// ---------------------------------------------------------------- een oneindige loop: het licht gaat uit

/**
 * De vloer wordt donker, behalve het rondje dat de motor bewees: dat brandt in oranje. Het product draait rondjes die
 * steeds sneller gaan, een telwerk telt ze, en dan stopt de band zichzelf. Wat blijft staan tot de volgende tik.
 */
async function lightsOut(loop) {
  clearLoopFx();
  const crate = S.crate.c;
  const lit = [...new Map(loop.map(([x, y]) => [key(x, y), [x, y]])).values()];
  const w = S.level.width * CELL, h = S.level.height * CELL;
  const fx = new PIXI.Container(), shade = new PIXI.Container();
  S.layers.fx.addChild(fx);
  S.layers.dark.addChild(shade);
  S.loopFx = { fx, shade };

  // Donker over de vloer, met gaten waar het rondje ligt
  const dark = new PIXI.Graphics().rect(MARGIN, SKY, w, h).fill({ color: C.ink, alpha: JUICE.loop.dim });
  for (const [x, y] of lit) dark.rect(MARGIN + x * CELL, SKY + y * CELL, CELL, CELL).cut();
  const glow = new PIXI.Graphics();
  for (const [x, y] of lit) glow.roundRect(MARGIN + x * CELL + 2, SKY + y * CELL + 2, CELL - 4, CELL - 4, 6);
  glow.stroke({ width: 4, color: C.zss });
  shade.addChild(dark, glow);
  shade.alpha = 0;
  gsap.to(shade, { alpha: 1, duration: JUICE.loop.fadeS });

  // Twee pijlen die rond het rondje draaien
  const xs = lit.map(([x]) => cx(x)), ys = lit.map(([, y]) => cy(y));
  const mx = (Math.min(...xs) + Math.max(...xs)) / 2, my = (Math.min(...ys) + Math.max(...ys)) / 2;
  // Rond het rondje, niet erdoor: de halve breedte van de vakjes samen, tot over de hoeken
  const half = (Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys)) + CELL) / 2;
  const r = half * 1.3 + 4;
  const arrows = new PIXI.Graphics();
  for (const start of [0, Math.PI]) {
    arrows.arc(0, 0, r, start + 0.25, start + Math.PI - 0.35).stroke({ width: 6, color: C.zss, cap: 'round' });
    const a = start + Math.PI - 0.35, tx = Math.cos(a) * r, ty = Math.sin(a) * r;
    const t2 = a + Math.PI / 2;
    arrows.poly([tx + Math.cos(t2) * 12, ty + Math.sin(t2) * 12, tx + Math.cos(a) * 10, ty + Math.sin(a) * 10, tx - Math.cos(a) * 10, ty - Math.sin(a) * 10]).fill(C.zss);
  }
  arrows.position.set(mx, my);
  arrows.alpha = 0;
  fx.addChild(arrows);
  gsap.to(arrows, { alpha: 1, duration: 0.3 });
  if (!S.calm) gsap.to(arrows, { rotation: Math.PI * 2, duration: 1.4, ease: 'none', repeat: -1 });

  // Het telwerk met de rondjes, rechtsboven op de vloer, en de zin onderaan
  const counter = new PIXI.Container();
  const counterBg = new PIXI.Graphics();
  const counterText = text(`${L('lap')} 0`, 15, C.ink, MONO);
  counter.addChild(counterBg, counterText);
  const drawCounter = (n) => {
    counterText.text = `${L('lap')} ${n}`;
    const cw = counterText.width + 18;
    counterBg.clear().roundRect(-cw / 2, -13, cw, 26, 4).fill(C.zss).stroke({ width: 2.5, color: C.ink });
  };
  drawCounter(0);
  counter.position.set(MARGIN + w - 70, SKY + 22);
  fx.addChild(counter);
  const line = new PIXI.Text({ text: L('forever'), style: { fontFamily: FONT, fontSize: 24, fontWeight: '900', fill: C.paper } });
  line.anchor.set(0, 1);
  line.position.set(MARGIN + 16, my > SKY + h / 2 ? SKY + 40 : SKY + h - 14);
  line.alpha = 0;
  fx.addChild(line);
  gsap.to(line, { alpha: 1, duration: 0.4, delay: 0.3 });

  sfx('overheat', { volume: 0.6 });
  sfx('alarm', { volume: 0.45 });

  // Rondjes, steeds sneller, met een tik die steeds hoger klinkt, tot de band zichzelf stopt
  const path = loop.slice(1).map(([x, y]) => [cx(x), cy(y)]);
  const J = JUICE.loop;
  let spent = 0, lap = 0, step = J.firstStepS;
  while (path.length && spent < J.totalS && lap < J.maxLaps && S.loopFx?.fx === fx) {
    for (const [x, y] of path) {
      await tween(crate, { x, y, duration: step, ease: 'none' });
      if (S.loopFx?.fx !== fx) return;
    }
    lap++;
    spent += step * path.length;
    step = Math.max(J.minStepS, step * J.speedUp);
    drawCounter(lap);
    gsap.fromTo(counter.scale, { x: 1.25, y: 1.25 }, { x: 1, y: 1, duration: 0.18 });
    sfx('tick', { volume: 0.35, rate: Math.min(2, 1 + lap * 0.06) });
  }
  if (S.loopFx?.fx !== fx) return;
  gsap.killTweensOf(arrows);
  gsap.fromTo(crate, { rotation: -0.25 }, { rotation: 0, duration: 0.5, ease: 'elastic.out(1, 0.3)' });
  sfx('alarm', { volume: 0.5 });
  await wait(500);
}

function clearLoopFx() {
  if (!S.loopFx) return;
  for (const part of [S.loopFx.fx, S.loopFx.shade]) {
    gsap.killTweensOf(part);
    part.children.forEach((c) => gsap.killTweensOf(c));
    part.destroy({ children: true });
  }
  S.loopFx = null;
  if (S.crate) gsap.killTweensOf(S.crate.c);
}

// ---------------------------------------------------------------- elke frame

function tick(t) {
  fit();
  if (S.calm || document.hidden) return;
  // De band draait altijd: de rollen schuiven mee
  S.phase += (t.deltaMS / 1000) * JUICE.rollerSpeed;
  drawRollers();
}
