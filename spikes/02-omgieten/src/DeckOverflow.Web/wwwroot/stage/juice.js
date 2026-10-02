// juice.js - alle afstelbare getallen en herbruikbare effecten.
// Startwaarden uit het design doc: af te stellen op gevoel tijdens de spike.

export const juice = {
  speed: 1,                                     // snelle modus zet dit op 0.5 (toets F)
  hitPauseMs: { small: 40, medium: 70, large: 120 },
  shakePx: { small: 2, medium: 5, large: 10 },
  rollMsPerUnit: 18,
  rollMaxMs: 700,
  comboPitchStep: 0.06,
  overflow: { freezeMs: 350, rollToMaxMs: 600 },
  cast: { meltMs: 380, holdMs: 260, pourMs: 420 },
  cardFlyMs: 220,
  dealStaggerMs: 70,
};

export const FONT_TITLE = '"Press Start 2P", monospace';
export const FONT_BODY = '"VT323", monospace';

export const COLORS = {
  bg: 0x14111b,
  grid: 0xf39200,
  orange: 0xf39200,
  orangeLight: 0xffb347,
  red: 0xe5484d,
  green: 0x5fd068,
  cyan: 0x4cc9f0,
  white: 0xffffff,
  ink: 0x1b1622,
  panel: 0x221d2b,
  muted: 0x8b84a0,
};

/**
 * Elk type een vaste kleur én vorm, zodat je het in één blik leest, ook bij kleurenblindheid.
 * Vorm: int hoekig, double rond, byte hoekig met een dubbele rand.
 */
export const TYPES = {
  int: { color: 0x4f8cff, shape: 'square' },
  double: { color: 0xb18cff, shape: 'round' },
  byte: { color: 0xffd166, shape: 'double' },
};

export const typeOf = (kind) => TYPES[(kind ?? '').toLowerCase()] ?? { color: COLORS.orange, shape: 'square' };

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

export function text(value, { size = 16, color = COLORS.white, font = FONT_TITLE, stroke = true, align = 'center', wrap = 0 } = {}) {
  const style = { fontFamily: font, fontSize: size, fill: color, align };
  if (stroke) style.stroke = { color: COLORS.ink, width: Math.max(3, size / 4), join: 'round' };
  if (wrap) { style.wordWrap = true; style.wordWrapWidth = wrap; }
  const t = new PIXI.Text({ text: String(value), style });
  t.anchor.set(0.5);
  return t;
}

/** Zwevend getal of label dat oppopt en wegdrijft. */
export function floatText(layer, value, x, y, { size = 28, color = COLORS.white, rise = 50, holdMs = 350 } = {}) {
  const t = text(value, { size, color });
  t.position.set(x, y);
  t.scale.set(0.2);
  layer.addChild(t);
  const tl = gsap.timeline({ onComplete: () => t.destroy() });
  tl.to(t.scale, { x: 1, y: 1, duration: sec(160), ease: 'back.out(3)' })
    .to(t, { y: y - rise, duration: sec(holdMs + 400), ease: 'power1.out' }, 0)
    .to(t, { alpha: 0, duration: sec(250) }, sec(holdMs + 150));
  return tl;
}

/** Kleine vierkante pixelpartikels. */
export function burst(layer, x, y, { color = COLORS.orange, count = 14, speed = 120, size = 4, up = 0, gravity = 120, ms = 600 } = {}) {
  const tweens = [];
  for (let i = 0; i < count; i++) {
    const p = new PIXI.Graphics().rect(-size / 2, -size / 2, size, size).fill(color);
    p.position.set(x, y);
    layer.addChild(p);
    const angle = Math.random() * Math.PI * 2;
    const v = speed * (0.4 + Math.random() * 0.8);
    const d = sec(ms * (0.7 + Math.random() * 0.6));
    gsap.to(p, { x: x + Math.cos(angle) * v, duration: d, ease: 'power2.out' });
    tweens.push(gsap.to(p, {
      y: y + Math.sin(angle) * v - up + gravity,
      alpha: 0,
      rotation: (Math.random() - 0.5) * 6,
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

/** Typebadge: tekst op een vlak in de kleur en vorm van het type. */
export function typeBadge(kind, { size = 9 } = {}) {
  const t = typeOf(kind);
  const label = text(kind.toLowerCase(), { size, color: COLORS.ink, stroke: false });
  const w = label.width + 10;
  const h = size + 7;
  const bg = new PIXI.Graphics();
  if (t.shape === 'round') bg.roundRect(-w / 2, -h / 2, w, h, h / 2).fill(t.color);
  else bg.rect(-w / 2, -h / 2, w, h).fill(t.color);
  if (t.shape === 'double') bg.rect(-w / 2 + 2, -h / 2 + 2, w - 4, h - 4).stroke({ width: 1, color: COLORS.ink });
  const badge = new PIXI.Container();
  badge.addChild(bg, label);
  badge.badgeWidth = w;
  return badge;
}
