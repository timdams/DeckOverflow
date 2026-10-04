// stage.js - de stage van de Lopende Band. Tekent het rooster, laat de producten rijden, en speelt het einde af:
// goedgekeurd en weg naar het front, of afgekeurd. Het front zelf staat nog niet in beeld (zie de README).
// Geen spelregels: de shell stuurt het bord, de tikken en de afloop; de stage meldt alleen klikken terug.

const CELL = 64;
const SKY = 36;           // een smalle strook bovenaan; later komt hier het front in de verte
const MARGIN = 28;
const WALK = 150;         // ruimte rechts van de uitgang om weg te wandelen

const C = {
  paper: 0xf7f5ee, white: 0xffffff, ink: 0x161616, muted: 0x8a877e, rule: 0xd9d5c9, shade: 0xebe7dc,
  fog: 0xdedad0, smoke: 0x9a968c, flash: 0xf3d98a,
  int: 0x2f6fe4, byte: 0xe8a800, double: 0x8a5cf0, string: 0x2e9d5b,
  pass: 0x2e9d5b, fail: 0xd8432f,
};
const FONT = '"Helvetica Neue", Helvetica, Arial, sans-serif';
const MONO = 'Consolas, "Courier New", monospace';

const S = {
  app: null, dotnet: null, root: null, layers: {},
  level: null, cells: new Map(), counters: new Map(),
  crate: null, textures: new Map(), beltPhase: 0, selected: null,
  W: 600, H: 400,
};

// ---------------------------------------------------------------- publieke API

export async function init(host, dotnet) {
  S.dotnet = dotnet;
  const app = new PIXI.Application();
  await app.init({ resizeTo: host, background: C.paper, antialias: true, resolution: Math.min(window.devicePixelRatio || 1, 2), autoDensity: true });
  host.appendChild(app.canvas);
  S.app = app;
  S.root = new PIXI.Container();
  app.stage.addChild(S.root);
  for (const name of ['sky', 'front', 'fog', 'floor', 'pieces', 'crate', 'fx', 'top']) {
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
  S.W = MARGIN * 2 + level.width * CELL + WALK;
  S.H = SKY + level.height * CELL + MARGIN;
  await loadTexture(level.product);
  for (const layer of Object.values(S.layers)) layer.removeChildren().forEach((c) => c.destroy({ children: true }));
  S.cells.clear();
  S.crate = null;
  drawSky();
  drawFloor();
  fit();
}

/** Het bord tekenen: elk vakje met wat erop ligt. */
export function setBoard(cells) {
  S.layers.pieces.removeChildren().forEach((c) => c.destroy({ children: true }));
  S.cells.clear();
  for (const cell of cells) {
    const g = drawPiece(cell);
    g.position.set(cx(cell.x), cy(cell.y));
    S.layers.pieces.addChild(g);
    S.cells.set(key(cell.x, cell.y), { cell, g });
  }
  drawSelection();
}

export function select(x, y) {
  S.selected = x === null || x === undefined ? null : { x, y };
  drawSelection();
}

/** Eén tik: het product rijdt naar zijn vakje. Bij een machine of poort speelt die even. */
export async function frame(f, ms, instant) {
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
  await gsap.to(S.crate.c, { x, y, duration: ms / 1000, ease: 'none' });
  const here = S.cells.get(key(f.x, f.y));
  if (here) react(here, f);
}

/** De afloop van één testgeval: goedgekeurd en weg naar het front, afgekeurd, gevallen of oververhit. */
export async function ending(e) {
  if (!S.crate || document.hidden) return;
  const c = S.crate.c;
  if (e.ending === 'Delivered') {
    stamp(c.x, c.y - 40, 'GOEDGEKEURD', C.pass);
    await wait(550);
    // Van de band af, naar rechts, en dan de mist in, richting het front
    const tl = gsap.timeline();
    tl.to(c, { x: S.W + 30, duration: 1.2, ease: 'power1.in' })
      .to(c, { alpha: 0, duration: 0.3 }, '-=0.3');
    hop(c, 1.2);
    await tl;
  } else if (e.ending === 'Wrong') {
    stamp(c.x, c.y - 40, 'AFGEKEURD', C.fail);
    await wait(550);
    await gsap.to(c, { y: c.y + 70, rotation: 0.9, alpha: 0, duration: 0.6, ease: 'power2.in' });
  } else if (e.ending === 'FellOff') {
    await gsap.to(c, { y: c.y + 50, rotation: -1.4, alpha: 0, duration: 0.6, ease: 'power2.in' });
  } else {
    // Een oneindige lus: de band raakt oververhit
    overheat(c.x, c.y);
    await wait(1400);
  }
}

/** Het product van de band halen, voor een nieuw testgeval of als de speler weer bouwt. */
export function clearCrate() {
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
const rnd = (a, b) => a + Math.random() * (b - a);
const ANG = { Up: -Math.PI / 2, Right: 0, Down: Math.PI / 2, Left: Math.PI };

function text(value, size = 14, color = C.ink, font = FONT, bold = true) {
  const t = new PIXI.Text({ text: String(value), style: { fontFamily: font, fontSize: size, fontWeight: bold ? '700' : '400', fill: color } });
  t.anchor.set(0.5);
  return t;
}

async function loadTexture(name) {
  if (!name || S.textures.has(name)) return;
  try { S.textures.set(name, await PIXI.Assets.load(`art/${name}.png`)); } catch { S.textures.set(name, null); }
}

function fit() {
  if (!S.app) return;
  const { width, height } = S.app.screen;
  const scale = Math.min(width / S.W, height / S.H);
  S.root.scale.set(scale);
  S.root.position.set((width - S.W * scale) / 2, (height - S.H * scale) / 2);
}

// ---------------------------------------------------------------- het front in de verte

function drawSky() {
  // Voorlopig alleen de lijn van de fabrieksvloer, zoals in de handleiding. Het front komt later, strakker.
  const floor = new PIXI.Graphics().moveTo(0, SKY - 4).lineTo(S.W, SKY - 4).stroke({ width: 2, color: C.ink });
  S.layers.sky.addChild(floor);
}

// ---------------------------------------------------------------- de werkvloer

function drawFloor() {
  const g = new PIXI.Graphics();
  const w = S.level.width * CELL, h = S.level.height * CELL;
  g.rect(MARGIN, SKY, w, h).fill(C.white).stroke({ width: 2, color: C.ink });
  for (let x = 1; x < S.level.width; x++) g.moveTo(MARGIN + x * CELL, SKY).lineTo(MARGIN + x * CELL, SKY + h);
  for (let y = 1; y < S.level.height; y++) g.moveTo(MARGIN, SKY + y * CELL).lineTo(MARGIN + w, SKY + y * CELL);
  g.stroke({ width: 1, color: C.rule });
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
  const x = MARGIN + S.selected.x * CELL, y = SKY + S.selected.y * CELL;
  for (let i = 0; i < 4; i++) {
    const [x1, y1, x2, y2] = [[x, y, x + CELL, y], [x + CELL, y, x + CELL, y + CELL], [x + CELL, y + CELL, x, y + CELL], [x, y + CELL, x, y]][i];
    const len = Math.hypot(x2 - x1, y2 - y1);
    for (let d = 0; d < len; d += 10) {
      const a = d / len, b = Math.min(1, (d + 5) / len);
      S.selection.moveTo(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a).lineTo(x1 + (x2 - x1) * b, y1 + (y2 - y1) * b);
    }
  }
  S.selection.stroke({ width: 3, color: C.ink });
}

/** Een pijltje naar de rand van het vakje, voor een uitgang. Gestippeld voor "klopt niet". */
function outArrow(g, dir, { dashed = false, color = C.ink } = {}) {
  const a = ANG[dir];
  const r1 = 18, r2 = 29;
  const [x1, y1, x2, y2] = [Math.cos(a) * r1, Math.sin(a) * r1, Math.cos(a) * r2, Math.sin(a) * r2];
  if (dashed) {
    g.moveTo(x1, y1).lineTo(x1 + (x2 - x1) * 0.4, y1 + (y2 - y1) * 0.4).moveTo(x1 + (x2 - x1) * 0.65, y1 + (y2 - y1) * 0.65).lineTo(x2, y2);
  } else {
    g.moveTo(x1, y1).lineTo(x2, y2);
  }
  g.stroke({ width: 3, color });
  const px = Math.cos(a + Math.PI / 2) * 5, py = Math.sin(a + Math.PI / 2) * 5;
  g.poly([x2 + Math.cos(a) * 3, y2 + Math.sin(a) * 3, x2 - Math.cos(a) * 5 + px, y2 - Math.sin(a) * 5 + py, x2 - Math.cos(a) * 5 - px, y2 - Math.sin(a) * 5 - py]).fill(color);
}

function drawPiece(cell) {
  const c = new PIXI.Container();
  const g = new PIXI.Graphics();
  c.addChild(g);
  const half = CELL / 2 - 3;
  switch (cell.kind) {
    case 'belt': {
      g.roundRect(-half, -half, half * 2, half * 2, 6).fill(C.shade).stroke({ width: 1.5, color: C.rule });
      // Twee pijlpunten die meelopen met de band
      const chev = new PIXI.Graphics();
      for (const off of [-10, 10]) chev.moveTo(off - 5, -8).lineTo(off + 3, 0).lineTo(off - 5, 8);
      chev.stroke({ width: 3, color: C.muted, cap: 'round', join: 'round' });
      chev.rotation = ANG[cell.out];
      c.addChild(chev);
      c.chevrons = chev;
      break;
    }
    case 'machine': {
      g.roundRect(-half, -half, half * 2, half * 2, 5).fill(C.white).stroke({ width: 3, color: C.ink });
      for (const [x, y] of [[-half + 6, -half + 6], [half - 6, -half + 6], [-half + 6, half - 6], [half - 6, half - 6]]) g.circle(x, y, 2).stroke({ width: 1.2, color: C.ink });
      outArrow(g, cell.out);
      const t = text(cell.text, 17, C.ink, MONO);
      c.addChild(t);
      break;
    }
    case 'gate': {
      g.circle(0, 0, half - 2).fill(C.white).stroke({ width: 3, color: C.ink });
      outArrow(g, cell.ifTrue);
      outArrow(g, cell.ifFalse, { dashed: true, color: C.muted });
      const t = text(cell.text, cell.text.length > 6 ? 10 : 13, C.ink, MONO);
      c.addChild(t);
      break;
    }
    case 'counter': {
      g.roundRect(-half, -half, half * 2, half * 2, 5).fill(C.white).stroke({ width: 3, color: C.ink });
      g.roundRect(-half + 4, -half + 4, half * 2 - 8, half * 2 - 8, 3).stroke({ width: 1, color: C.ink });
      outArrow(g, cell.loop);
      outArrow(g, cell.done, { dashed: true, color: C.muted });
      // Een telwerk: cijfers in een venstertje, zoals een kilometerteller
      const win = new PIXI.Graphics().rect(-16, -11, 32, 22).fill(C.ink);
      c.addChild(win);
      const t = text(`0/${cell.times}`, 12, C.white, MONO);
      c.addChild(t);
      c.counterText = t;
      c.times = cell.times;
      break;
    }
    case 'source': {
      g.poly([-half, -half, half, -half, half - 8, half, -half + 8, half]).fill(C.ink);
      const t = text('IN', 13, C.white);
      t.y = -6;
      c.addChild(t);
      outArrow(g, cell.out, { color: C.white });
      break;
    }
    case 'output': {
      g.roundRect(-half, -half, half * 2, half * 2, 4).fill(C.ink);
      for (let y = -half + 8; y < half; y += 8) g.moveTo(-half + 6, y).lineTo(half - 6, y);
      g.stroke({ width: 1, color: C.muted });
      const t = text('VERZENDING', 8, C.white);
      t.y = half - 9;
      c.addChild(t);
      break;
    }
  }
  if (cell.fixed && cell.kind !== 'source' && cell.kind !== 'output') {
    const lock = new PIXI.Graphics().roundRect(-half - 2, -half - 2, half * 2 + 4, half * 2 + 4, 6).stroke({ width: 2, color: C.muted, alpha: 0.8 });
    c.addChild(lock);
  }
  return c;
}

function updateCounters(counters) {
  const live = new Map((counters ?? []).map(([x, y, n]) => [key(x, y), n]));
  for (const [k, { g }] of S.cells) {
    if (!g.counterText) continue;
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
    const s = 44 / Math.max(tex.width, tex.height);
    body.scale.set(s);
    body.baseScale = s;
  }
  const tag = new PIXI.Container();
  const tagBg = new PIXI.Graphics();
  const tagText = text('', 11, C.white, MONO);
  tag.addChild(tagBg, tagText);
  tag.y = 18;
  c.addChild(body, tag);
  S.layers.crate.addChild(c);
  S.crate = { c, body, tag, tagBg, tagText, product: f.product };
}

function setCrate(f) {
  const { body, tagBg, tagText } = S.crate;
  tagText.text = f.value;
  const w = Math.max(28, tagText.width + 10);
  tagBg.clear().roundRect(-w / 2, -8, w, 16, 3).fill(C[f.kind] ?? C.ink).stroke({ width: 1.5, color: C.ink });
  // Het product groeit naarmate het dichter bij de bestelling komt
  const grow = 0.7 + Math.min(1.2, Math.max(0, f.growth)) * 0.45;
  if (body.baseScale) gsap.to(body.scale, { x: body.baseScale * grow, y: body.baseScale * grow, duration: 0.18 });
}

/** Een machine of poort reageert als het product erop komt. */
function react(here, f) {
  const g = here.g;
  if (here.cell.kind === 'machine') {
    gsap.fromTo(g.scale, { x: 1.12, y: 0.9 }, { x: 1, y: 1, duration: 0.25, ease: 'back.out(3)' });
    sparks(cx(f.x), cy(f.y), f.note === 'Overflow' ? 14 : 6);
    if (f.note === 'Truncated') crumbs(cx(f.x), cy(f.y));
    if (f.note === 'Overflow') {
      const t = text('byte loopt over!', 13, C.ink);
      t.position.set(cx(f.x), cy(f.y) - 38);
      S.layers.fx.addChild(t);
      gsap.to(t, { y: t.y - 20, alpha: 0, duration: 1.2, onComplete: () => t.destroy() });
      gsap.fromTo(S.crate.c, { rotation: -0.3 }, { rotation: 0, duration: 0.4, ease: 'elastic.out(1, 0.3)' });
    }
  } else if (here.cell.kind === 'gate') {
    gsap.fromTo(g, { rotation: f.gate ? 0.18 : -0.18 }, { rotation: 0, duration: 0.3, ease: 'back.out(3)' });
  } else if (here.cell.kind === 'counter') {
    gsap.fromTo(g.scale, { x: 1.08, y: 1.08 }, { x: 1, y: 1, duration: 0.2 });
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

function stamp(x, y, word, color) {
  const c = new PIXI.Container();
  const t = text(word, 15, color);
  const g = new PIXI.Graphics().roundRect(-t.width / 2 - 8, -13, t.width + 16, 26, 4).stroke({ width: 3, color });
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

/** Een oneindige lus: rook, een zwaailicht, en het woord dat elke ploegbaas vreest. */
function overheat(x, y) {
  for (let i = 0; i < 10; i++) {
    const puff = new PIXI.Graphics().circle(0, 0, rnd(5, 10)).fill({ color: C.smoke, alpha: 0.6 });
    puff.position.set(x + rnd(-14, 14), y - 10);
    S.layers.fx.addChild(puff);
    gsap.to(puff, { y: puff.y - rnd(40, 80), x: puff.x + rnd(-20, 20), alpha: 0, duration: rnd(1, 1.8), delay: i * 0.08, onComplete: () => puff.destroy() });
    gsap.to(puff.scale, { x: 2.2, y: 2.2, duration: 1.6, delay: i * 0.08 });
  }
  const beacon = new PIXI.Graphics().circle(0, 0, 9).fill(C.fail);
  beacon.position.set(x, y - 40);
  S.layers.fx.addChild(beacon);
  gsap.to(beacon, { alpha: 0.2, duration: 0.18, yoyo: true, repeat: 6, onComplete: () => beacon.destroy() });
  const t = text('OVERHIT', 15, C.fail);
  t.position.set(x, y - 62);
  S.layers.fx.addChild(t);
  gsap.to(t, { alpha: 0, duration: 0.4, delay: 1.2, onComplete: () => t.destroy() });
}

// ---------------------------------------------------------------- elke frame

function tick(t) {
  fit();
  const dt = t.deltaMS / 1000;
  // De band draait altijd: de pijltjes schuiven mee
  S.beltPhase = (S.beltPhase + dt * 22) % 20;
  for (const { g } of S.cells.values()) {
    if (g.chevrons) {
      const a = g.chevrons.rotation;
      g.chevrons.position.set(Math.cos(a) * (S.beltPhase - 10) * 0.4, Math.sin(a) * (S.beltPhase - 10) * 0.4);
    }
  }
}
