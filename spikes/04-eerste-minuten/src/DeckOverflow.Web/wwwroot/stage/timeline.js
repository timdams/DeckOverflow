// timeline.js - wachtrij die events één na één omzet in animaties.
// Elke handler wacht op het deel dat de speler moet zien voor het volgende event,
// en zet uitdovende staarten (oprollende getallen, partikels) in ctx.tails.
// Onbekende events worden genegeerd, zodat de motor kan groeien zonder de stage te breken.

import { juice, sec, wait, hitPause, sizeOf, shake, floatText, burst, text, num, typeOf, COLORS } from './juice.js';
import { sfx } from './audio.js';
import { logEvent } from './log.js';
import { t, relicName } from './strings.js';

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
 * Na een kaart of een vijandbeurt: per doelwit één blok met het resultaat,
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
      lines.push(s.hp > 0 ? [`-${num(s.hp)} HP`, COLORS.red, true] : ['GEBLOKT', COLORS.cyan, true]);
      if (s.blocked > 0) lines.push([t('stage.on-block', { amount: num(s.blocked) }), COLORS.cyan]);
      if (s.lost > 0) lines.push([`${num(s.lost)} afgekapt`, COLORS.orangeLight]);
    }
    if (s.overflow) lines.push(['OVERFLOW', COLORS.orange, true]);
    if (s.healed > 0) lines.push([`+${num(s.healed)} HP`, COLORS.green, !s.hit]);
    if (s.cast) lines.push([`${s.cast.from.toLowerCase()} → ${s.cast.to.toLowerCase()}`, typeOf(s.cast.to).color, !s.hit]);
    if (s.gained > 0) lines.push([`+${num(s.gained)} schild`, COLORS.cyan, lines.length === 0]);
    if (lines.length === 0) continue;

    const box = new PIXI.Container();
    let y = 0;
    for (const [value, color, big] of lines) {
      const t = big ? text(value, { size: 16, color }) : text(value, { size: 20, color, font: '"VT323", monospace' });
      t.position.set(0, y);
      y += t.height + 2;
      box.addChild(t);
    }
    // Onderkant net boven de intent, groeit naar boven
    const head = a.head();
    box.position.set(head.x, head.y - 64 - y);
    box.scale.set(0.3);
    S.layers.top.addChild(box);
    summaries.add(box);
    gsap.timeline({ onComplete: () => { summaries.delete(box); destroyBox(box); } })
      .to(box.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(2.5)' })
      .to(box, { alpha: 0, duration: sec(300) }, `+=${sec(juice.summaryHoldMs)}`);
  }
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
    const head = a.center();
    head.y -= 20;

    sfx(size === 'small' ? 'hit' : 'hitBig', { combo: ctx.combo++ });
    a.sprite.flash.alpha = 1;
    a.knock(a.isEnemy ? 1 : -1, juice.shakePx[size] * 2);
    floatText(S.layers.fx, `-${num(e.amount)}`, head.x, head.y, { size: 22 + juice.shakePx[size] * 2, color: COLORS.red });
    burst(S.layers.fx, head.x, head.y + 20, { color: COLORS.white, count: 6 + Math.round(e.amount), speed: 90 });

    await hitPause(juice.hitPauseMs[size]);

    gsap.to(a.sprite.flash, { alpha: 0, duration: sec(120) });
    shake(S.shaker, juice.shakePx[size]);
    ctx.tails.push(a.rollHp(e.hpBefore, e.hpAfter));
    await wait(120);
  },

  async ValueTruncated(S, e) {
    const a = S.actor(e.targetId);
    // Schade en HP boven het hoofd, een schild bij het schild
    let anchor = a.head();
    let y = anchor.y - 30;
    if (e.subject === 'Block') {
      anchor = S.layers.fx.toLocal(a.blockBox.getGlobalPosition());
      y = anchor.y - 34;
    }
    const head = anchor;
    const label = t({ Damage: 'stage.truncated.damage', Block: 'stage.truncated.block', Hp: 'stage.truncated.hp' }[e.subject] ?? '(int)');
    const color = e.subject === 'Block' ? COLORS.cyan : e.subject === 'Hp' ? COLORS.white : COLORS.orangeLight;

    // "2.5" valt uiteen in "2" en ".5"
    const full = num(e.before);
    const dot = full.indexOf('.');
    const size = e.subject === 'Damage' ? 26 : 18;
    const left = text(dot < 0 ? full : full.slice(0, dot), { size, color });
    const right = text(dot < 0 ? '' : full.slice(dot), { size, color });
    const group = new PIXI.Container();
    right.x = left.width / 2 + right.width / 2;
    group.addChild(left, right);
    group.pivot.x = right.x / 2;
    group.position.set(head.x, y);
    group.scale.set(0.2);
    S.layers.fx.addChild(group);

    await gsap.to(group.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(3)' });
    await wait(280);

    sfx('tinkle', { volume: 0.7 });
    gsap.to(right, { y: 170, x: right.x + 30, rotation: 2.2, alpha: 0, duration: sec(800), ease: 'power2.in' });
    burst(S.layers.fx, group.x + right.x / 2, y, { color, count: 8, speed: 40, gravity: 140 });

    const tag = text(label, { size: 10, color: COLORS.muted });
    tag.position.set(0, 24);
    tag.alpha = 0;
    group.addChild(tag);
    gsap.to(tag, { alpha: 1, duration: sec(150) });
    gsap.to(group, { alpha: 0, delay: sec(900), duration: sec(300), onComplete: () => group.destroy({ children: true }) });

    await wait(420);
  },

  async ValueOverflowed(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    const { rollToMaxMs, freezeMs } = juice.overflow;

    // Al de rest wordt stil en donker, de vijand staat in de spotlight
    const dimmed = S.dimExcept(a);
    gsap.to(a.intentNode, { alpha: 0, duration: sec(120) });

    const counter = text(e.before, { size: 44, color: COLORS.orangeLight });
    counter.position.set(head.x, head.y - 50);
    const label = text(`${e.before} + ${e.added}`, { size: 12, color: COLORS.muted });
    label.position.set(head.x, head.y - 90);
    S.layers.fx.addChild(counter, label);

    sfx('heal', { volume: 0.5 });
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

    // Alles bevriest
    counter.style.fill = COLORS.white;
    sfx('freeze', { volume: 0.8 });
    await hitPause(freezeMs);

    // Klik: terug naar het begin
    sfx('click');
    sfx('glitch', { volume: 0.8 });
    counter.text = String(e.after);
    counter.style.fill = COLORS.orange;
    a.setHp(e.after);
    S.flashScreen(0.9);
    shake(S.shaker, juice.shakePx.large * 1.5, 400);
    burst(S.layers.fx, counter.x, counter.y, { color: COLORS.orange, count: 30, speed: 220 });
    label.text = `${e.max} + ${e.before + e.added - e.max} → ${e.after}`;
    label.style.fill = COLORS.orangeLight;
    gsap.fromTo(counter.scale, { x: 2, y: 2 }, { x: 1, y: 1, duration: sec(300), ease: 'elastic.out(1, 0.4)' });

    await hitPause(juice.hitPauseMs.large);
    dimmed.restore();
    gsap.to(a.intentNode, { alpha: 1, duration: sec(300), delay: sec(600) });

    ctx.tails.push(
      gsap.to([counter, label], {
        alpha: 0, delay: sec(e.after === 0 ? 300 : 900), duration: sec(400),
        onComplete: () => { counter.destroy(); label.destroy(); },
      }));
    await wait(200);
  },

  async BlockGained(S, e) {
    const a = S.actor(e.targetId);
    sfx('shield', { volume: 0.6 });
    a.setBlock(e.total);
    await gsap.fromTo(a.blockBox.scale, { x: 1.8, y: 1.8 }, { x: 1, y: 1, duration: sec(260), ease: 'elastic.out(1, 0.5)' });
  },

  async BlockAbsorbed(S, e) {
    const a = S.actor(e.targetId);
    const p = a.blockBox.getGlobalPosition();
    const local = S.layers.fx.toLocal(p);
    sfx(e.remaining === 0 ? 'shatter' : 'shield', { volume: 0.7 });
    burst(S.layers.fx, local.x, local.y, { color: COLORS.cyan, count: 8 + Math.round(e.absorbed * 2), speed: 110 });
    floatText(S.layers.fx, `-${num(e.absorbed)}`, local.x, local.y - 14, { size: 16, color: COLORS.cyan, rise: 30 });
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
    floatText(S.layers.fx, `+${num(e.amount)}`, head.x, head.y, { size: 24, color: COLORS.green });
    burst(S.layers.fx, head.x, head.y + a.sprite.h * 0.5, { color: COLORS.green, count: 16, speed: 50, up: 90, gravity: -40, ms: 800 });
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
    await gsap.to(source.container, { x: source.home.x + dir * 90, duration: sec(130), ease: 'power3.in' });
    gsap.to(source.container, { x: source.home.x, duration: sec(320), ease: 'power2.out', delay: sec(60) });
  },

  async CombatantDied(S, e) {
    const a = S.actor(e.targetId);
    const origin = a.container.position;
    a.sprite.flash.alpha = 1;
    await hitPause(juice.hitPauseMs.large);

    sfx('bass');
    sfx('hitBig', { rate: 0.7 });
    S.flashScreen(0.6);
    shake(S.shaker, juice.shakePx.large * 2, 500);

    const scale = a.sprite.container.scale.x;
    for (const p of a.sprite.pixels) {
      const g = new PIXI.Graphics().rect(-a.sprite.px / 2, -a.sprite.px / 2, a.sprite.px, a.sprite.px).fill(p.color);
      const x = origin.x + p.x * scale;
      const y = origin.y + p.y * scale;
      g.position.set(x, y);
      S.layers.fx.addChild(g);
      const d = sec(700 + Math.random() * 600);
      gsap.to(g, { x: x + (p.x / a.sprite.w) * 500 + (Math.random() - 0.5) * 200, duration: d, ease: 'power2.out' });
      gsap.to(g, {
        y: y + 200 + Math.random() * 200, rotation: (Math.random() - 0.5) * 8, alpha: 0,
        duration: d, ease: 'back.in(2.5)', onComplete: () => g.destroy(),
      });
    }
    a.die();
    await wait(600);
  },

  async TurnEnded(S) {
    sfx('whoosh', { volume: 0.4, rate: 0.8 });
    await S.discardHand();
  },

  async TurnStarted(S, e) {
    await S.announce(t('stage.turn', { turn: e.turn }));
  },

  async PlayRejected(S, e) {
    sfx('buzz', { volume: 0.6 });
    const card = S.takePendingCard({ keep: true });
    if (card) {
      await gsap.timeline()
        .to(card, { rotation: 0.12, duration: sec(45) })
        .to(card, { rotation: -0.12, duration: sec(70) })
        .to(card, { rotation: 0.08, duration: sec(60) })
        .to(card, { rotation: 0, duration: sec(50) });
    }
    floatText(S.layers.fx, e.reason, 480, 360, { size: 11, color: COLORS.orangeLight, rise: 20, holdMs: 1200 });
    S.layoutHand();
    await wait(150);
  },

  /**
   * Omgieten: de vijand smelt tot een plas in de kleur van zijn oude type
   * en rijst op in de kleur van zijn nieuwe. Past de waarde niet, dan klapt ze om.
   */
  async TypeChanged(S, e, ctx) {
    const a = S.actor(e.targetId);
    const head = a.head();
    const sprite = a.sprite.container;
    const to = e.to.toLowerCase();
    const { meltMs, holdMs, pourMs } = juice.cast;

    const dimmed = S.dimExcept(a);
    gsap.to(a.intentNode, { alpha: 0, duration: sec(120) });
    gsap.killTweensOf(sprite.scale);
    gsap.killTweensOf(sprite, 'y');

    // De cast zelf verschijnt boven de vijand: (byte) 506
    const label = text(`(${to}) ${num(e.hpBefore)}`, { size: 18, color: typeOf(e.to).color });
    label.position.set(head.x, head.y - 100);   // boven een eventueel afgekapt getal
    label.scale.set(0.2);
    S.layers.fx.addChild(label);
    sfx('whoosh', { volume: 0.5, rate: 0.7 });
    await gsap.to(label.scale, { x: 1, y: 1, duration: sec(180), ease: 'back.out(3)' });

    // Smelten
    const center = a.center();
    a.sprite.flash.tint = typeOf(e.from).color;
    gsap.to(a.sprite.flash, { alpha: 0.85, duration: sec(meltMs * 0.5) });
    sfx('glitch', { volume: 0.4 });
    burst(S.layers.fx, center.x, center.y + 30, { color: typeOf(e.from).color, count: 18, speed: 70, gravity: 180 });
    await gsap.to(sprite.scale, { x: 1.4, y: 0.1, duration: sec(meltMs), ease: 'power2.in' });
    await wait(holdMs);

    // De nieuwe waarde
    a.setKind(e.to, e.maxHpAfter);
    a.setBlock(e.blockAfter);
    label.text = `(${to}) ${num(e.hpBefore)} = ${num(e.hpAfter)}`;
    if (e.wrapped) {
      // Past niet in het nieuwe type: alles bevriest, klik, de rest blijft over
      label.style.fill = COLORS.white;
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

    // Gieten
    a.sprite.flash.tint = typeOf(e.to).color;
    sfx('shield', { volume: 0.6, rate: 0.8 });
    burst(S.layers.fx, center.x, center.y, { color: typeOf(e.to).color, count: 30, speed: 160, up: 40 });
    gsap.fromTo(a.badgeSlot.scale, { x: 2, y: 2 }, { x: 1, y: 1, duration: sec(400), ease: 'elastic.out(1, 0.4)' });
    await gsap.to(sprite.scale, { x: 1, y: 1, duration: sec(pourMs), ease: 'elastic.out(1, 0.45)' });
    gsap.to(a.sprite.flash, { alpha: 0, duration: sec(300), onComplete: () => { a.sprite.flash.tint = 0xffffff; } });
    a.idle();

    dimmed.restore();
    gsap.to(a.intentNode, { alpha: 1, duration: sec(300), delay: sec(300) });
    ctx.tails.push(gsap.to(label, { alpha: 0, delay: sec(e.hpAfter === 0 ? 300 : 900), duration: sec(300), onComplete: () => label.destroy() }));
    await wait(150);
  },

  /** Toekenning: de oude expressie maakt plaats voor het nieuwe getal. */
  async IntentAssigned(S, e) {
    const a = S.actor(e.enemyId);
    const head = a.head();
    sfx('click', { volume: 0.6 });
    floatText(S.layers.fx, `= ${num(e.value)}`, head.x, head.y - 60, { size: 22, color: COLORS.cyan, rise: 20 });
    gsap.fromTo(a.intentNode.scale, { x: 1.4, y: 1.4 }, { x: 1, y: 1, duration: sec(260), ease: 'back.out(2)' });
    await a.typeIntent(String(e.value), e.value);
  },

  /** Een modifier wacht op de volgende kaart: hij springt op bij de energiebol. */
  async ModifierQueued(S, e, ctx) {
    sfx('tick', { combo: ctx.combo++, volume: 0.6 });
    floatText(S.layers.fx, e.label, 70, 400, { size: 22, color: COLORS.orangeLight, rise: 30 });
    S.setModifiers(e.pending.split(' '));
    await wait(200);
  },

  /** De wachtende modifiers vallen op de kaart: de som verschijnt en rekent uit. */
  async ModifiersApplied(S, e, ctx) {
    const p = S.actor(0)?.head() ?? { x: 255, y: 150 };
    const label = text(e.expression, { size: 16, color: COLORS.orangeLight });
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
    floatText(S.layers.fx, name, 840, 70, { size: 12, color: COLORS.orangeLight, rise: 20, holdMs: 500 });
    await wait(180);
  },

  async CombatEnded(S, e) {
    sfx(e.won ? 'win' : 'lose');
    await S.banner(t(e.won ? 'stage.won' : 'stage.crashed'), e.won ? COLORS.orange : COLORS.red);
  },
};
