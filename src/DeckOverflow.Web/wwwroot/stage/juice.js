// juice.js - alle afstelbare getallen en herbruikbare effecten, in de taal van een
// montagehandleiding: zwarte lijnen op papier, pijlen, stippellijnen, onderdelen.
// Er bestaan geen pixels meer: wat wegvliegt zijn schroefjes en splinters.
// Kleur is voorbehouden aan de types (int, double, byte).

export const juice = {
  speed: 1,                                     // snelle modus zet dit op 0.5 (toets F)
  hitPauseMs: { small: 40, medium: 70, large: 120 },
  shakePx: { small: 2, medium: 5, large: 10 },
  rollMsPerUnit: 18,
  rollMaxMs: 700,
  comboPitchStep: 0.06,
  overflow: { freezeMs: 350, rollToMaxMs: 600 },
  cast: { meltMs: 380, holdMs: 260, pourMs: 420 },
  truncate: { snipMs: 260 },
  explode: { pieces: [3, 4], distance: 70, ms: 900 },   // kolommen x rijen, hoe ver, hoe lang
  summaryHoldMs: 1600,
  cardFlyMs: 220,
  dealStaggerMs: 70,
  line: 2.5,                                    // dikte van een gewone lijn
};

export const FONT = '"Helvetica Neue", Helvetica, Arial, sans-serif';

export const COLORS = {
  paper: 0xf7f5ee,
  white: 0xffffff,
  ink: 0x161616,
  muted: 0x8a877e,
  rule: 0xd9d5c9,     // hulplijnen, raster
  shade: 0xebe7dc,    // vlak dat net iets donkerder is dan papier
};

/**
 * Elk type een vaste kleur én vorm, zodat je het in één blik leest, ook bij kleurenblindheid.
 * Vorm: int hoekig, double rond, byte hoekig met een dubbele rand. Dit zijn de enige kleuren
 * in het spel: zie je kleur, dan zie je een type.
 */
export const TYPES = {
  int: { color: 0x2f6fe4, shape: 'square' },
  double: { color: 0x8a5cf0, shape: 'round' },
  byte: { color: 0xe8a800, shape: 'double' },
  string: { color: 0x2e9d5b, shape: 'round' },
};

export const typeOf = (kind) => TYPES[(kind ?? '').toLowerCase()] ?? { color: COLORS.ink, shape: 'square' };

/** Getallen zoals C# ze toont: 24.5, 12, nooit 24.500000001. */
export const num = (v) => String(Math.round(v * 100) / 100);

/** Duur in seconden voor GSAP, geschaald door de snelle modus. */
export const sec = (ms) => (ms * juice.speed) / 1000;

/** Wachten, geschaald door de snelle modus. */
export const wait = (ms) => new Promise((r) => setTimeout(r, ms * juice.speed));

/** Hit pause: nooit geschaald, het moment moet voelbaar blijven. */
export const hitPause = (ms) => new Promise((r) => setTimeout(r, ms));

export function sizeOf(amount) {
  if (amount >= 20) return 'large';
  if (amount >= 6) return 'medium';
  return 'small';
}

export function shake(node, px, ms = 240) {
  const steps = 6;
  const tl = gsap.timeline();
  for (let i = 0; i < steps; i++) {
    const falloff = 1 - i / steps;
    tl.to(node, {
      x: (Math.random() * 2 - 1) * px * falloff,
      y: (Math.random() * 2 - 1) * px * falloff,
      duration: sec(ms / steps),
      ease: 'none',
    });
  }
  tl.to(node, { x: 0, y: 0, duration: sec(ms / steps) });
  return tl;
}

/**
 * Tekst in het handleidingletter. halo: een rand in papierkleur, zodat tekst over een
 * tekening leesbaar blijft.
 */
export function text(value, { size = 16, color = COLORS.ink, bold = true, halo = false, align = 'center', wrap = 0, anchor = 0.5 } = {}) {
  const style = { fontFamily: FONT, fontSize: size, fontWeight: bold ? '700' : '400', fill: color, align };
  if (halo) style.stroke = { color: COLORS.paper, width: Math.max(3, size / 4), join: 'round' };
  if (wrap) { style.wordWrap = true; style.wordWrapWidth = wrap; }
  const t = new PIXI.Text({ text: String(value), style });
  t.anchor.set(anchor);
  return t;
}

/** Zwevend getal of label dat oppopt en wegdrijft. */
export function floatText(layer, value, x, y, { size = 28, color = COLORS.ink, rise = 50, holdMs = 350 } = {}) {
  const t = text(value, { size, color, halo: true });
  t.position.set(x, y);
  t.scale.set(0.2);
  layer.addChild(t);
  const tl = gsap.timeline({ onComplete: () => t.destroy() });
  tl.to(t.scale, { x: 1, y: 1, duration: sec(160), ease: 'back.out(3)' })
    .to(t, { y: y - rise, duration: sec(holdMs + 400), ease: 'power1.out' }, 0)
    .to(t, { alpha: 0, duration: sec(250) }, sec(holdMs + 150));
  return tl;
}

// ---------------------------------------------------------------- tekenhulp

/** Stippellijn op een Graphics, zoals de hulplijnen in een explosietekening. */
export function dashed(g, x1, y1, x2, y2, { dash = 6, gap = 5, width = 1.5, color = COLORS.ink } = {}) {
  const len = Math.hypot(x2 - x1, y2 - y1);
  const dx = (x2 - x1) / len;
  const dy = (y2 - y1) / len;
  for (let d = 0; d < len; d += dash + gap) {
    const e = Math.min(len, d + dash);
    g.moveTo(x1 + dx * d, y1 + dy * d).lineTo(x1 + dx * e, y1 + dy * e);
  }
  return g.stroke({ width, color, cap: 'round' });
}

/** Een omcirkeld cijfer: de stapnummers van een handleiding. */
export function circled(value, { radius = 14, size = 14, fill = COLORS.white, color = COLORS.ink } = {}) {
  const c = new PIXI.Container();
  c.addChild(new PIXI.Graphics().circle(0, 0, radius).fill(fill).stroke({ width: juice.line, color }));
  c.addChild(text(value, { size, color }));
  return c;
}

/** Stempel: een ✓ (gelukt) of ✗ (zo niet) in een cirkel, met een plof. */
export function stamp(layer, x, y, kind = 'check', { radius = 20 } = {}) {
  const g = new PIXI.Graphics().circle(0, 0, radius).fill(COLORS.white).stroke({ width: 3, color: COLORS.ink });
  const r = radius * 0.5;
  if (kind === 'check') g.moveTo(-r, 0).lineTo(-r * 0.25, r * 0.7).lineTo(r, -r * 0.7);
  else g.moveTo(-r, -r).lineTo(r, r).moveTo(r, -r).lineTo(-r, r);
  g.stroke({ width: 4, color: COLORS.ink, cap: 'round', join: 'round' });
  g.position.set(x, y);
  g.scale.set(2.2);
  g.alpha = 0;
  layer.addChild(g);
  return gsap.timeline({ onComplete: () => g.destroy() })
    .to(g, { alpha: 1, duration: sec(60) })
    .to(g.scale, { x: 1, y: 1, duration: sec(160), ease: 'back.out(3)' }, 0)
    .to(g, { alpha: 0, duration: sec(250) }, `+=${sec(500)}`);
}

// ---------------------------------------------------------------- effecten

/**
 * Getande inslagster: de "pow" van een handleiding, als omtrek op wit.
 * Staat achter het schadegetal.
 */
export function impactStar(layer, x, y, { radius = 34, points = 9 } = {}) {
  const pts = [];
  for (let i = 0; i < points * 2; i++) {
    const r = i % 2 === 0 ? radius : radius * 0.55;
    const a = (i / (points * 2)) * Math.PI * 2 + Math.random() * 0.15;
    pts.push(Math.cos(a) * r, Math.sin(a) * r);
  }
  const g = new PIXI.Graphics().poly(pts).fill(COLORS.white).stroke({ width: juice.line, color: COLORS.ink, join: 'miter' });
  g.position.set(x, y);
  g.rotation = Math.random() * 0.6;
  g.scale.set(0.3);
  layer.addChild(g);
  return gsap.timeline({ onComplete: () => g.destroy() })
    .to(g.scale, { x: 1, y: 1, duration: sec(110), ease: 'back.out(3)' })
    .to(g, { alpha: 0, duration: sec(220) }, `+=${sec(240)}`);
}

/** Korte streepjes die uit een punt wegschieten: snelheid en impact. */
export function speedLines(layer, x, y, { count = 10, inner = 26, length = 22, spread = Math.PI * 2, angle = 0, ms = 260 } = {}) {
  const tweens = [];
  for (let i = 0; i < count; i++) {
    const a = angle - spread / 2 + (spread * (i + Math.random() * 0.6)) / count;
    const g = new PIXI.Graphics().moveTo(0, 0).lineTo(length, 0).stroke({ width: 2.5, color: COLORS.ink, cap: 'round' });
    g.rotation = a;
    g.position.set(x + Math.cos(a) * inner, y + Math.sin(a) * inner);
    g.scale.x = 0.2;
    layer.addChild(g);
    tweens.push(gsap.timeline({ onComplete: () => g.destroy() })
      .to(g.scale, { x: 1, duration: sec(ms * 0.4), ease: 'power2.out' })
      .to(g, { x: g.x + Math.cos(a) * length, y: g.y + Math.sin(a) * length, alpha: 0, duration: sec(ms * 0.6), ease: 'power1.in' }));
  }
  return tweens;
}

/** Eén los onderdeeltje: een schroef, een splinter of een moertje. */
function part(color) {
  const g = new PIXI.Graphics();
  const kind = Math.random();
  if (kind < 0.4) {
    // schroef: kopje met gleuf en een draadje
    g.circle(0, 0, 3.5).fill(COLORS.white).stroke({ width: 1.5, color })
      .moveTo(-2, 0).lineTo(2, 0).stroke({ width: 1.2, color })
      .moveTo(0, 3.5).lineTo(0, 9).stroke({ width: 1.5, color });
  } else if (kind < 0.75) {
    // splinter
    g.poly([0, -5, 2, 4, -2, 4]).fill(COLORS.white).stroke({ width: 1.5, color, join: 'round' });
  } else {
    // moertje
    const pts = [];
    for (let i = 0; i < 6; i++) pts.push(Math.cos((i / 6) * Math.PI * 2) * 4, Math.sin((i / 6) * Math.PI * 2) * 4);
    g.poly(pts).fill(COLORS.white).stroke({ width: 1.5, color }).circle(0, 0, 1.4).stroke({ width: 1, color });
  }
  return g;
}

/** Onderdelen die wegvliegen en vallen. Vervangt de pixelpartikels. */
export function burst(layer, x, y, { color = COLORS.ink, count = 10, speed = 120, up = 0, gravity = 140, ms = 650 } = {}) {
  const tweens = [];
  for (let i = 0; i < count; i++) {
    const p = part(color);
    p.position.set(x, y);
    p.rotation = Math.random() * Math.PI * 2;
    layer.addChild(p);
    const angle = Math.random() * Math.PI * 2;
    const v = speed * (0.4 + Math.random() * 0.8);
    const d = sec(ms * (0.7 + Math.random() * 0.6));
    gsap.to(p, { x: x + Math.cos(angle) * v, duration: d, ease: 'power2.out' });
    tweens.push(gsap.to(p, {
      y: y + Math.sin(angle) * v - up + gravity,
      alpha: 0,
      rotation: p.rotation + (Math.random() - 0.5) * 8,
      duration: d,
      ease: 'power1.in',
      onComplete: () => p.destroy(),
    }));
  }
  return tweens;
}

/**
 * Laat een getal oprollen. onValue krijgt elke tussenwaarde, afgerond op hele getallen
 * of, met decimals: 1, op tienden (voor een double).
 */
export function roll(from, to, onValue, { msPerUnit = juice.rollMsPerUnit, maxMs = juice.rollMaxMs, decimals = 0 } = {}) {
  const box = { v: from };
  const f = 10 ** decimals;
  const ms = Math.min(maxMs, Math.abs(to - from) * msPerUnit);
  return gsap.to(box, {
    v: to,
    duration: sec(ms),
    ease: 'power2.out',
    onUpdate: () => onValue(Math.round(box.v * f) / f),
    onComplete: () => onValue(to),
  });
}

/** Typebadge: tekst op een vlak in de kleur en vorm van het type, met een zwarte rand. */
export function typeBadge(kind, { size = 11 } = {}) {
  const t = typeOf(kind);
  const label = text(kind.toLowerCase(), { size, color: COLORS.white });
  const w = label.width + 12;
  const h = size + 9;
  const bg = new PIXI.Graphics();
  if (t.shape === 'round') bg.roundRect(-w / 2, -h / 2, w, h, h / 2);
  else bg.rect(-w / 2, -h / 2, w, h);
  bg.fill(t.color).stroke({ width: 2, color: COLORS.ink });
  if (t.shape === 'double') bg.rect(-w / 2 + 3, -h / 2 + 3, w - 6, h - 6).stroke({ width: 1, color: COLORS.white });
  const badge = new PIXI.Container();
  badge.addChild(bg, label);
  badge.badgeWidth = w;
  return badge;
}
