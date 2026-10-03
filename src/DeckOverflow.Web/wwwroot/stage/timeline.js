// timeline.js - wachtrij die events één na één omzet in animaties.
// Elke handler wacht op het deel dat de speler moet zien voor het volgende event,
// en zet uitdovende staarten (oprollende getallen, onderdelen) in ctx.tails.
// Onbekende events worden genegeerd, zodat de motor kan groeien zonder de stage te breken.
//
// De taal van een montagehandleiding. Een treffer is een inslagster met
// snelheidslijnen, afkappen is een schaar langs een stippellijn, een overflow is een
// rondgaande pijl, en wie sterft valt uiteen in een explosietekening.

import { juice, sec, wait, hitPause, sizeOf, shake, floatText, burst, text, num, typeOf, impactStar, speedLines, stamp, circled, dashed, COLORS } from './juice.js';
import { sfx } from './audio.js';
import { logEvent } from './log.js';
import { t, relicName } from './strings.js';
import { iconTexture } from './art.js';

export async function runQueue(S, events) {
  const ctx = { tails: [], combo: 0 };
  clearSummaries();

  for (const e of events) {
    logEvent(S, e);
    const handler = handlers[e.type];
    if (!handler) { console.debug('Stage negeert onbekend event', e); continue; }
    await handler(S, e, ctx);
  }

  await Promise.all(ctx.tails);
  showSummaries(S, events);
}

// ---------------------------------------------------------------- samenvatting

/**
 * Na een kaart of een vijandbeurt: per doelwit één tekstballon met het resultaat,
 * zodat de speler niet uit zes losse animaties hoeft af te leiden wat er gebeurde.
 */
const summaries = new Set();

/** Een nieuwe actie begint: het resultaat van de vorige mag weg, anders lopen ze door elkaar. */
function clearSummaries() {
  for (const box of summaries) gsap.to(box, { alpha: 0, duration: sec(120), overwrite: true, onComplete: () => destroyBox(box) });
  summaries.clear();
}

const destroyBox = (box) => { if (!box.destroyed) box.destroy({ children: true }); };

function showSummaries(S, events) {
  const by = new Map();
  const get = (id) => {
    if (!by.has(id)) by.set(id, { hp: 0, blocked: 0, lost: 0, healed: 0, gained: 0, overflow: false, cast: null, hit: false });
    return by.get(id);
  };

  for (const e of events) {
    switch (e.type) {
      case 'DamageDealt': { const s = get(e.targetId); s.hp += e.amount; s.hit = true; break; }
      case 'BlockAbsorbed': { const s = get(e.targetId); s.blocked += e.absorbed; s.hit = true; break; }
      case 'ValueTruncated':
        if (e.subject === 'Damage') { const s = get(e.targetId); s.lost += e.lost; s.hit = true; }
        break;
      case 'Healed': get(e.targetId).healed += e.amount; break;
      case 'ValueOverflowed': get(e.targetId).overflow = true; break;
      case 'BlockGained': get(e.targetId).gained += e.amount; break;
      case 'TypeChanged': get(e.targetId).cast = e; break;
    }
  }

  for (const [id, s] of by) {
    const a = S.actor(id);
    if (!a) continue;
    const lines = [];   // [tekst, kleur, groot?]
    if (s.hit) {
      lines.push(s.hp > 0 ? [`-${num(s.hp)} HP`, COLORS.ink, true] : [t('stage.sum.blocked'), COLORS.ink, true]);
      if (s.blocked > 0) lines.push([t('stage.on-block', { amount: num(s.blocked) }), COLORS.muted]);
      if (s.lost > 0) lines.push([t('stage.sum.truncated', { amount: num(s.lost) }), COLORS.muted]);
    }
    if (s.overflow) lines.push(['OVERFLOW', COLORS.ink, true]);
    if (s.healed > 0) lines.push([`+${num(s.healed)} HP`, COLORS.ink, !s.hit]);
    if (s.cast) lines.push([`${s.cast.from.toLowerCase()} → ${s.cast.to.toLowerCase()}`, typeOf(s.cast.to).color, !s.hit]);
    if (s.gained > 0) lines.push([t('stage.sum.block', { amount: num(s.gained) }), COLORS.ink, lines.length === 0]);
    if (lines.length === 0) continue;

    const box = new PIXI.Container();
    const content = new PIXI.Container();
    let y = 0;
    let w = 0;
    for (const [value, color, big] of lines) {
      const line = text(value, { size: big ? 16 : 12, color, bold: big, anchor: 0 });
      line.anchor.set(0.5, 0);
      line.position.set(0, y);
      y += line.height + 2;
      w = Math.max(w, line.width);
      content.addChild(line);
    }
    // Een kadertje zoals de detailcirkels in een handleiding
    const frame = new PIXI.Graphics().roundRect(-w / 2 - 10, -6, w + 20, y + 10, 6).fill(COLORS.white).stroke({ width: 2, color: COLORS.ink });
    box.addChild(frame, content);
    // Onderkant net boven de intent, groeit naar boven
    const head = a.head();
    box.position.set(head.x, head.y - 80 - y);
    box.scale.set(0.3);
    S.layers.top.addChild(box);
    summaries.add(box);
    gsap.timeline({ onComplete: () => { summaries.delete(box); destroyBox(box); } })
      .to(box.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(2.5)' })
      .to(box, { alpha: 0, duration: sec(300) }, `+=${sec(juice.summaryHoldMs)}`);
  }
}

// ---------------------------------------------------------------- explosietekening

/** Welke stukken van een tekening echt iets bevatten. Eén keer per texture uitgerekend. */
const coverage = new Map();

function filledPieces(tex, cols, rows) {
  const key = `${tex.uid}:${cols}x${rows}`;
  if (coverage.has(key)) return coverage.get(key);
  const result = [];
  try {
    const { width, height } = tex.frame;
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const g = canvas.getContext('2d');
    g.drawImage(tex.source.resource, tex.frame.x, tex.frame.y, width, height, 0, 0, width, height);
    const data = g.getImageData(0, 0, width, height).data;
    const fw = width / cols;
    const fh = height / rows;
    for (let j = 0; j < rows; j++) {
      for (let i = 0; i < cols; i++) {
        let filled = 0;
        let total = 0;
        for (let y = Math.floor(j * fh); y < (j + 1) * fh; y += 3) {
          for (let x = Math.floor(i * fw); x < (i + 1) * fw; x += 3) {
            total++;
            if (data[(y * width + x) * 4 + 3] > 40) filled++;
          }
        }
        if (filled / total > 0.06) result.push([i, j]);
      }
    }
  } catch {
    for (let j = 0; j < rows; j++) for (let i = 0; i < cols; i++) result.push([i, j]);
  }
  coverage.set(key, result);
  return result;
}

/**
 * De figuur valt uiteen in zijn onderdelen, die langs gestippelde hulplijnen wegschuiven,
 * met stapnummers erbij. Het omgekeerde van in elkaar zetten.
 */
async function explode(S, a) {
  const img = a.sprite.img;
  const tex = img.texture;
  const [cols, rows] = juice.explode.pieces;
  const scale = img.scale.x;
  const fw = tex.frame.width / cols;
  const fh = tex.frame.height / rows;
  const origin = { x: a.container.x, y: a.container.y + a.sprite.container.y };
  const middle = { x: origin.x, y: origin.y - a.sprite.h / 2 };

  const group = new PIXI.Container();
  const guides = new PIXI.Graphics();
  group.addChild(guides);
  S.layers.fx.addChild(group);

  const pieces = filledPieces(tex, cols, rows).map(([i, j], n) => {
    const frame = new PIXI.Rectangle(tex.frame.x + i * fw, tex.frame.y + j * fh, fw, fh);
    const piece = new PIXI.Sprite(new PIXI.Texture({ source: tex.source, frame }));
    piece.anchor.set(0.5);
    piece.scale.set(scale);
    piece.tint = img.tint;
    const x = origin.x + (-tex.frame.width / 2 + (i + 0.5) * fw) * scale;
    const y = origin.y + (-tex.frame.height + (j + 0.5) * fh) * scale;
    piece.position.set(x, y);
    group.addChild(piece);

    const dx = x - middle.x;
    const dy = y - middle.y;
    const len = Math.hypot(dx, dy) || 1;
    const dist = juice.explode.distance * (0.8 + Math.random() * 0.5);
    return { piece, from: { x, y }, to: { x: x + (dx / len) * dist, y: y + (dy / len) * dist * 0.8 }, n };
  });

  a.die();
  const ms = juice.explode.ms;
  await Promise.all(pieces.map(({ piece, to }) =>
    gsap.to(piece, { x: to.x, y: to.y, rotation: (Math.random() - 0.5) * 0.5, duration: sec(ms * 0.45), ease: 'power3.out' })));

  // Hulplijnen van waar elk stuk zat naar waar het nu ligt, en stapnummers op een paar stukken
  for (const { from, to } of pieces) dashed(guides, from.x, from.y, to.x, to.y, { color: COLORS.muted, width: 1.2, dash: 4, gap: 4 });
  guides.alpha = 0;
  gsap.to(guides, { alpha: 1, duration: sec(150) });
  pieces.filter((_, k) => k % Math.max(1, Math.floor(pieces.length / 4)) === 0).slice(0, 4).forEach(({ to }, k) => {
    const label = circled(k + 1, { radius: 9, size: 10 });
    label.position.set(to.x + fw * scale * 0.45, to.y - fh * scale * 0.45);
    label.scale.set(0);
    group.addChild(label);
    gsap.to(label.scale, { x: 1, y: 1, delay: sec(k * 70), duration: sec(160), ease: 'back.out(3)' });
  });

  gsap.to(group, { alpha: 0, delay: sec(ms * 0.9), duration: sec(400), onComplete: () => group.destroy({ children: true }) });
  await wait(ms * 0.55);
}

// ---------------------------------------------------------------- handlers

function iconAt(layer, name, x, y, size) {
  const tex = iconTexture(name);
  if (!tex) return null;
  const img = new PIXI.Sprite(tex);
  img.anchor.set(0.5);
  img.width = img.height = size;
  img.position.set(x, y);
  layer.addChild(img);
  return img;
}

const handlers = {
  async CardPlayed(S, e) {
    const card = S.takePendingCard();
    if (!card) return;
    const target = S.actor(e.targetId);
    const to = target.center();
    sfx('whoosh', { volume: 0.5 });
    await gsap.timeline()
      .to(card, { x: to.x, y: to.y, rotation: 0, duration: sec(juice.cardFlyMs), ease: 'power2.in' }, 0)
      .to(card.scale, { x: 0.35, y: 0.35, duration: sec(juice.cardFlyMs), ease: 'power2.in' }, 0)
      .to(card, { alpha: 0, duration: sec(80) }, sec(juice.cardFlyMs - 40));
    card.destroy({ children: true });
  },

  async DamageDealt(S, e, ctx) {
    const a = S.actor(e.targetId);
    const size = sizeOf(e.amount);
    const hit = a.center();
    hit.y -= 20;
    const dir = a.isEnemy ? 1 : -1;

    sfx(size === 'small' ? 'hit' : 'hitBig', { combo: ctx.combo++ });
    a.knock(dir, juice.shakePx[size] * 2);
    const radius = 26 + juice.shakePx[size] * 2;
    impactStar(S.layers.fx, hit.x, hit.y, { radius });
    speedLines(S.layers.fx, hit.x, hit.y, { count: 8 + juice.shakePx[size], inner: radius + 4, length: 14 + juice.shakePx[size] * 2 });
    floatText(S.layers.fx, `-${num(e.amount)}`, hit.x, hit.y, { size: 20 + juice.shakePx[size] * 2, rise: 40 });
    burst(S.layers.fx, hit.x, hit.y + 20, { count: 3 + Math.round(Math.min(e.amount, 20) / 2), speed: 90 });

    await hitPause(juice.hitPauseMs[size]);

    shake(S.shaker, juice.shakePx[size]);
    ctx.tails.push(a.rollHp(e.hpBefore, e.hpAfter));
    await wait(120);
  },

  /** Afkappen: een schaar knipt langs de stippellijn tussen "2" en ".5", en ".5" valt eraf. */
  async ValueTruncated(S, e) {
    const a = S.actor(e.targetId);
    // Schade en HP naast de figuur, aan de kant van de tegenstander (boven het hoofd staat de intent);
    // een schild bij het schild
    const c = a.center();
    let anchor = { x: c.x + (a.isEnemy ? -1 : 1) * (a.sprite.w / 2 + 60), y: c.y };
    let y = c.y - 40;
    if (e.subject === 'Block') {
      anchor = S.layers.fx.toLocal(a.blockBox.getGlobalPosition());
      y = anchor.y - 40;
    }
    const label = t({ Damage: 'stage.truncated.damage', Block: 'stage.truncated.block', Hp: 'stage.truncated.hp' }[e.subject] ?? '(int)');

    const full = num(e.before);
    const dot = full.indexOf('.');
    const size = e.subject === 'Damage' ? 28 : 20;
    const left = text(dot < 0 ? full : full.slice(0, dot), { size, halo: true });
    const right = text(dot < 0 ? '' : full.slice(dot), { size, halo: true });
    const group = new PIXI.Container();
    const gap = 14;
    left.anchor.set(1, 0.5);
    right.anchor.set(0, 0.5);
    left.x = -gap / 2;
    right.x = gap / 2;
    const cut = new PIXI.Graphics();
    dashed(cut, 0, -size * 0.9, 0, size * 0.9, { dash: 4, gap: 3, width: 1.5 });
    group.addChild(cut, left, right);
    group.position.set(anchor.x, y);
    group.scale.set(0.2);
    S.layers.fx.addChild(group);

    await gsap.to(group.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(3)' });

    // De schaar komt van boven en knipt
    const scissors = iconAt(group, 'truncate', 0, -size * 1.6, size * 1.4);
    if (scissors) {
      scissors.rotation = Math.PI / 2;
      await gsap.to(scissors, { y: size * 0.4, duration: sec(juice.truncate.snipMs), ease: 'power2.in' });
    } else {
      await wait(juice.truncate.snipMs);
    }

    sfx('tinkle', { volume: 0.7 });
    if (scissors) gsap.to(scissors, { alpha: 0, y: size * 1.2, duration: sec(200) });
    gsap.to(right, { y: 160, x: right.x + 30, rotation: 1.6, alpha: 0, duration: sec(800), ease: 'power2.in' });
    gsap.to(cut, { alpha: 0, duration: sec(200) });

    const tag = text(label, { size: 11, color: COLORS.muted, halo: true });
    tag.position.set(0, size + 4);
    tag.alpha = 0;
    group.addChild(tag);
    gsap.to(tag, { alpha: 1, duration: sec(150) });
    gsap.to(group, { alpha: 0, delay: sec(900), duration: sec(300), onComplete: () => group.destroy({ children: true }) });

    await wait(380);
  },

  /** Overflow: het getal rolt naar het maximum, alles bevriest, en een pijl draait het rond naar het begin. */
  async ValueOverflowed(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    const { rollToMaxMs, freezeMs } = juice.overflow;

    const dimmed = S.dimExcept(a);
    gsap.to(a.intentNode, { alpha: 0, duration: sec(120) });

    const cy = head.y - 60;
    const plate = new PIXI.Graphics().circle(0, 0, 48).fill(COLORS.white).stroke({ width: 3, color: COLORS.ink });
    plate.position.set(head.x, cy);
    const counter = text(e.before, { size: 34 });
    counter.position.set(head.x, cy);
    const label = text(`${e.before} + ${e.added}`, { size: 13, color: COLORS.muted, halo: true });
    label.position.set(head.x, cy - 66);
    S.layers.fx.addChild(plate, counter, label);

    sfx('heal', { volume: 0.5 });
    gsap.from(plate.scale, { x: 0.2, y: 0.2, duration: sec(180), ease: 'back.out(3)' });
    await gsap.from(counter.scale, { x: 0.2, y: 0.2, duration: sec(180), ease: 'back.out(3)' });

    // Oprollen naar het maximum, elke stap een tik hoger
    const steps = Math.max(1, e.max - e.before);
    for (let i = 1; i <= steps; i++) {
      counter.text = String(e.before + i);
      a.setHp(e.before + i);
      sfx('tick', { combo: ctx.combo++, volume: 0.6 });
      gsap.fromTo(counter.scale, { x: 1.15, y: 1.15 }, { x: 1, y: 1, duration: sec(80) });
      await wait(rollToMaxMs / steps);
    }

    // Alles bevriest: het bord wordt zwart, het getal wit
    plate.tint = COLORS.ink;
    counter.style.fill = COLORS.white;
    sfx('freeze', { volume: 0.8 });
    await hitPause(freezeMs);

    // Klik: de pijl draait één keer rond, terug naar het begin
    const loop = iconAt(S.layers.fx, 'overflow', head.x, cy, 150);
    if (loop) {
      // Achter het bord: de pijl draait rond het getal, niet erover
      S.layers.fx.setChildIndex(loop, S.layers.fx.getChildIndex(plate));
      loop.alpha = 0.9;
      gsap.to(loop, { rotation: -Math.PI * 2, duration: sec(450), ease: 'power2.inOut' });
      gsap.to(loop, { alpha: 0, delay: sec(500), duration: sec(250), onComplete: () => loop.destroy() });
    }
    sfx('click');
    sfx('glitch', { volume: 0.8 });
    plate.tint = 0xffffff;
    counter.style.fill = COLORS.ink;
    counter.text = String(e.after);
    a.setHp(e.after);
    S.flashScreen(0.9);
    shake(S.shaker, juice.shakePx.large * 1.5, 400);
    speedLines(S.layers.fx, head.x, cy, { count: 16, inner: 56, length: 26 });
    burst(S.layers.fx, head.x, cy, { count: 14, speed: 200 });
    label.text = `${e.max} + ${e.before + e.added - e.max} → ${e.after}`;
    label.style.fill = COLORS.ink;
    gsap.fromTo(counter.scale, { x: 2, y: 2 }, { x: 1, y: 1, duration: sec(300), ease: 'elastic.out(1, 0.4)' });

    await hitPause(juice.hitPauseMs.large);
    dimmed.restore();
    gsap.to(a.intentNode, { alpha: 1, duration: sec(300), delay: sec(600) });

    ctx.tails.push(
      gsap.to([plate, counter, label], {
        alpha: 0, delay: sec(e.after === 0 ? 300 : 900), duration: sec(400),
        onComplete: () => { plate.destroy(); counter.destroy(); label.destroy(); },
      }));
    await wait(200);
  },

  async BlockGained(S, e) {
    const a = S.actor(e.targetId);
    sfx('shield', { volume: 0.6 });
    a.setBlock(e.total);
    const p = S.layers.fx.toLocal(a.blockBox.getGlobalPosition());
    speedLines(S.layers.fx, p.x, p.y, { count: 8, inner: 22, length: 10 });
    await gsap.fromTo(a.blockBox.scale, { x: 1.8, y: 1.8 }, { x: 1, y: 1, duration: sec(260), ease: 'elastic.out(1, 0.5)' });
  },

  /** Een pijl ketst af op het schild; het vinkje zegt: tegengehouden. */
  async BlockAbsorbed(S, e) {
    const a = S.actor(e.targetId);
    const p = S.layers.fx.toLocal(a.blockBox.getGlobalPosition());
    const dir = a.isEnemy ? 1 : -1;
    sfx(e.remaining === 0 ? 'shatter' : 'shield', { volume: 0.7 });

    const arrow = new PIXI.Graphics()
      .moveTo(-60 * dir, -30).lineTo(-14 * dir, 0).lineTo(-50 * dir, -46)
      .stroke({ width: 2.5, color: COLORS.ink, cap: 'round', join: 'round' });
    arrow.poly([-50 * dir, -46, -42 * dir, -48, -48 * dir, -38]).fill(COLORS.ink);
    arrow.position.set(p.x, p.y);
    arrow.alpha = 0;
    S.layers.fx.addChild(arrow);
    gsap.timeline({ onComplete: () => arrow.destroy() })
      .to(arrow, { alpha: 1, duration: sec(60) })
      .to(arrow, { alpha: 0, duration: sec(250) }, `+=${sec(300)}`);

    stamp(S.layers.fx, p.x + 26 * dir, p.y - 26, 'check', { radius: 12 });
    if (e.remaining === 0) burst(S.layers.fx, p.x, p.y, { count: 8, speed: 110 });
    floatText(S.layers.fx, `-${num(e.absorbed)}`, p.x, p.y - 30, { size: 16, rise: 30 });
    shake(a.blockIcon, 4, 200);
    a.setBlock(e.remaining);
    await wait(220);
  },

  async BlockExpired(S, e) {
    const a = S.actor(e.targetId);
    await gsap.to(a.blockBox, { alpha: 0, duration: sec(200) });
    a.setBlock(0);
  },

  async Healed(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    sfx('heal', { combo: ctx.combo++, volume: 0.6 });
    floatText(S.layers.fx, `+${num(e.amount)}`, head.x, head.y, { size: 24 });
    // Kleine plusjes die opstijgen, zoals de "nieuw"-tekentjes in een handleiding
    for (let i = 0; i < 5; i++) {
      const plus = text('+', { size: 16 + Math.random() * 8 });
      plus.position.set(head.x + (Math.random() - 0.5) * a.sprite.w, head.y + a.sprite.h * (0.3 + Math.random() * 0.5));
      plus.alpha = 0;
      S.layers.fx.addChild(plus);
      gsap.timeline({ delay: sec(i * 60), onComplete: () => plus.destroy() })
        .to(plus, { alpha: 1, duration: sec(100) })
        .to(plus, { y: plus.y - 50, alpha: 0, duration: sec(600), ease: 'power1.out' });
    }
    ctx.tails.push(a.rollHp(e.hpAfter - e.amount, e.hpAfter));
    await wait(260);
  },

  async IntentRevealed(S, e) {
    const a = S.actor(e.enemyId);
    await a.typeIntent(e.expression, e.value);
  },

  async AttackLaunched(S, e) {
    const source = S.actor(e.sourceId);
    const dir = source.isEnemy ? -1 : 1;
    sfx('whoosh', { volume: 0.6 });
    const c = source.center();
    // Snelheidslijnen achter de aanvaller, in tegengestelde richting
    speedLines(S.layers.fx, c.x - dir * 30, c.y, { count: 6, inner: 10, length: 30, spread: 0.7, angle: dir > 0 ? Math.PI : 0, ms: 300 });
    await gsap.to(source.container, { x: source.home.x + dir * 90, duration: sec(130), ease: 'power3.in' });
    gsap.to(source.container, { x: source.home.x, duration: sec(320), ease: 'power2.out', delay: sec(60) });
  },

  async CombatantDied(S, e) {
    const a = S.actor(e.targetId);
    await hitPause(juice.hitPauseMs.large);

    sfx('bass');
    sfx('hitBig', { rate: 0.7 });
    S.flashScreen(0.6);
    shake(S.shaker, juice.shakePx.large * 2, 500);
    const c = a.center();
    burst(S.layers.fx, c.x, c.y, { count: 14, speed: 180 });
    await explode(S, a);
  },

  async TurnEnded(S) {
    sfx('whoosh', { volume: 0.4, rate: 0.8 });
    await S.discardHand();
  },

  async TurnStarted(S, e) {
    await S.announce(e.turn);
  },

  /** Mag niet: de kaart schudt nee en krijgt een kruis. */
  async PlayRejected(S, e) {
    sfx('buzz', { volume: 0.6 });
    const card = S.takePendingCard({ keep: true });
    if (card) {
      const p = S.layers.fx.toLocal(card.getGlobalPosition());
      stamp(S.layers.fx, p.x, p.y - 20, 'cross', { radius: 22 });
      await gsap.timeline()
        .to(card, { rotation: 0.12, duration: sec(45) })
        .to(card, { rotation: -0.12, duration: sec(70) })
        .to(card, { rotation: 0.08, duration: sec(60) })
        .to(card, { rotation: 0, duration: sec(50) });
    }
    floatText(S.layers.fx, t(e.reason), 480, 360, { size: 14, rise: 20, holdMs: 1200 });
    S.layoutHand();
    await wait(150);
  },

  /**
   * Omgieten: de figuur plet tot een plas en rijst op, ingekleurd in zijn nieuwe type.
   * Twee stapcirkels tonen de volgorde: ① smelten, ② gieten. Past de waarde niet, dan klapt ze om.
   */
  async TypeChanged(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    const body = a.sprite.container;
    const to = e.to.toLowerCase();
    const { meltMs, holdMs, pourMs } = juice.cast;

    const dimmed = S.dimExcept(a);
    gsap.to(a.intentNode, { alpha: 0, duration: sec(120) });
    gsap.killTweensOf(body.scale);
    gsap.killTweensOf(body, 'y,rotation');

    // De cast zelf verschijnt boven de vijand: (byte) 506
    const label = text(`(${to}) ${num(e.hpBefore)}`, { size: 18, color: typeOf(e.to).color, halo: true });
    label.position.set(head.x, head.y - 100);   // boven een eventueel afgekapt getal
    label.scale.set(0.2);
    S.layers.fx.addChild(label);
    sfx('whoosh', { volume: 0.5, rate: 0.7 });
    await gsap.to(label.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(3)' });

    // ① Smelten
    const center = a.center();
    const step1 = circled(1, { radius: 12, size: 12 });
    step1.position.set(center.x - a.sprite.w / 2 - 20, center.y);
    S.layers.fx.addChild(step1);
    sfx('glitch', { volume: 0.4 });
    burst(S.layers.fx, center.x, center.y + 30, { color: typeOf(e.from).color, count: 8, speed: 70, gravity: 180 });
    await gsap.to(body.scale, { x: 1.4, y: 0.1, duration: sec(meltMs), ease: 'power2.in' });
    await wait(holdMs);

    // De nieuwe waarde
    a.setKind(e.to, e.maxHpAfter);
    a.setBlock(e.blockAfter);
    label.text = `(${to}) ${num(e.hpBefore)} = ${num(e.hpAfter)}`;
    if (e.wrapped) {
      // Past niet in het nieuwe type: alles bevriest, klik, de rest blijft over
      label.style.fill = COLORS.ink;
      sfx('freeze', { volume: 0.8 });
      await hitPause(juice.overflow.freezeMs);
      sfx('click');
      sfx('glitch', { volume: 0.8 });
      label.style.fill = typeOf(e.to).color;
      a.setHp(e.hpAfter);
      S.flashScreen(0.9);
      shake(S.shaker, juice.shakePx.large * 1.5, 400);
      gsap.fromTo(label.scale, { x: 1.8, y: 1.8 }, { x: 1, y: 1, duration: sec(300), ease: 'elastic.out(1, 0.4)' });
    } else {
      ctx.tails.push(a.rollHp(e.hpBefore, e.hpAfter));
    }

    // ② Gieten
    const step2 = circled(2, { radius: 12, size: 12 });
    step2.position.set(center.x + a.sprite.w / 2 + 20, center.y);
    S.layers.fx.addChild(step2);
    sfx('shield', { volume: 0.6, rate: 0.8 });
    speedLines(S.layers.fx, center.x, center.y, { count: 12, inner: a.sprite.w / 2, length: 18 });
    gsap.fromTo(a.badgeSlot.scale, { x: 2, y: 2 }, { x: 1, y: 1, duration: sec(400), ease: 'elastic.out(1, 0.4)' });
    await gsap.to(body.scale, { x: 1, y: 1, duration: sec(pourMs), ease: 'elastic.out(1, 0.45)' });
    a.idle();

    dimmed.restore();
    gsap.to(a.intentNode, { alpha: 1, duration: sec(300), delay: sec(300) });
    ctx.tails.push(gsap.to([label, step1, step2], {
      alpha: 0, delay: sec(e.hpAfter === 0 ? 300 : 900), duration: sec(300),
      onComplete: () => { label.destroy(); step1.destroy({ children: true }); step2.destroy({ children: true }); },
    }));
    await wait(150);
  },

  /** Math.Round of Convert rondde af: het oude getal schuift naar het nieuwe, met de naam eronder. */
  async ValueRounded(S, e) {
    const a = S.actor(e.targetId);
    const c = a.center();
    const x = c.x + (a.isEnemy ? -1 : 1) * (a.sprite.w / 2 + 60);
    sfx('tick', { volume: 0.6 });
    floatText(S.layers.fx, `${num(e.before)} → ${num(e.after)}`, x, c.y - 40, { size: e.subject === 'Damage' ? 24 : 20, rise: 24 });
    floatText(S.layers.fx, t('stage.rounded'), x, c.y - 12, { size: 11, rise: 24, color: COLORS.muted });
    await wait(260);
  },

  /** Groei: HP maal de factor, en wat het type ervan overhoudt. Een afgekapt restje volgt als ValueTruncated. */
  async ValueGrew(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    sfx('heal', { volume: 0.5 });
    floatText(S.layers.fx, t('stage.grew', { factor: num(e.factor) }), head.x, head.y - 70, { size: 20, rise: 24 });
    ctx.tails.push(a.rollHp(e.before, e.after));
    await wait(260);
  },

  /** Convert paste niet: een OverflowException. De vijand schudt en zijn intent verdwijnt. */
  async ConversionCrashed(S, e) {
    const a = S.actor(e.targetId);
    const head = a.head();
    sfx('glitch');
    shake(S.shaker, juice.shakePx.large);
    floatText(S.layers.fx, t('stage.crash'), head.x, head.y - 60, { size: 22, rise: 30, holdMs: 700 });
    gsap.to(a.intentNode, { alpha: 0.25, duration: sec(200) });
    await wait(420);
  },

  /** De vijand crashte vorige beurt: geen aanval. */
  async AttackSkipped(S, e) {
    const a = S.actor(e.enemyId);
    const head = a.head();
    sfx('click', { volume: 0.6 });
    floatText(S.layers.fx, t('stage.crash'), head.x, head.y - 60, { size: 18, rise: 20 });
    gsap.to(a.intentNode, { alpha: 1, duration: sec(200), delay: sec(400) });
    await wait(400);
  },

  /** Toekenning: de oude expressie maakt plaats voor het nieuwe getal. */
  async IntentAssigned(S, e) {
    const a = S.actor(e.enemyId);
    const head = a.head();
    sfx('click', { volume: 0.6 });
    floatText(S.layers.fx, `= ${num(e.value)}`, head.x, head.y - 70, { size: 22, rise: 20 });
    gsap.fromTo(a.intentNode.scale, { x: 1.4, y: 1.4 }, { x: 1, y: 1, duration: sec(260), ease: 'back.out(2)' });
    await a.typeIntent(String(e.value), e.value);
  },

  /** Een modifier wacht op de volgende kaart: hij springt op boven de batterij. */
  async ModifierQueued(S, e, ctx) {
    sfx('tick', { combo: ctx.combo++, volume: 0.6 });
    floatText(S.layers.fx, e.label, 70, 400, { size: 22, rise: 30 });
    S.setModifiers(e.pending.split(' '));
    await wait(200);
  },

  /** De wachtende modifiers vallen op de kaart: de som verschijnt en rekent uit. */
  async ModifiersApplied(S, e, ctx) {
    const p = S.actor(0)?.head() ?? { x: 255, y: 150 };
    const label = text(e.expression, { size: 16, halo: true });
    label.position.set(p.x, p.y - 40);
    label.scale.set(0.2);
    S.layers.fx.addChild(label);
    sfx('tick', { combo: ctx.combo++, volume: 0.6 });
    await gsap.to(label.scale, { x: 1, y: 1, duration: sec(160), ease: 'back.out(3)' });
    await wait(260);
    label.text = `${e.expression} = ${num(e.after)}`;
    sfx('tick', { combo: ctx.combo++, volume: 0.7 });
    gsap.fromTo(label.scale, { x: 1.3, y: 1.3 }, { x: 1, y: 1, duration: sec(200), ease: 'back.out(2)' });
    S.setModifiers([]);
    ctx.tails.push(gsap.to(label, { alpha: 0, delay: sec(700), duration: sec(300), onComplete: () => label.destroy() }));
    await wait(200);
  },

  /** Een relic doet iets: zijn naam licht op. Wat hij doet, volgt als gewone events. */
  async RelicTriggered(S, e) {
    const name = relicName(e.relicId);
    sfx('shield', { volume: 0.4, rate: 1.4 });
    floatText(S.layers.fx, name, 820, 90, { size: 13, rise: 20, holdMs: 500 });
    await wait(180);
  },

  async CombatEnded(S, e) {
    sfx(e.won ? 'win' : 'lose');
    await S.banner(t(e.won ? 'stage.won' : 'stage.crashed'), e.won);
  },
};
