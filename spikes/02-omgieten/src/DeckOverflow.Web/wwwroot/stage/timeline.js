// timeline.js - wachtrij die events één na één omzet in animaties.
// Elke handler wacht op het deel dat de speler moet zien voor het volgende event,
// en zet uitdovende staarten (oprollende getallen, partikels) in ctx.tails.
// Onbekende events worden genegeerd, zodat de motor kan groeien zonder de stage te breken.

import { juice, sec, wait, hitPause, sizeOf, shake, floatText, burst, text, num, typeOf, COLORS } from './juice.js';
import { sfx } from './audio.js';

export async function runQueue(S, events) {
  const ctx = { tails: [], combo: 0 };

  for (const e of events) {
    const handler = handlers[e.type];
    if (!handler) { console.debug('Stage negeert onbekend event', e); continue; }
    await handler(S, e, ctx);
  }

  await Promise.all(ctx.tails);
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
    const head = a.head();
    const y = head.y - 30;

    // "2.5" valt uiteen in "2" en ".5"
    const full = num(e.before);
    const dot = full.indexOf('.');
    const left = text(dot < 0 ? full : full.slice(0, dot), { size: 26, color: COLORS.orangeLight });
    const right = text(dot < 0 ? '' : full.slice(dot), { size: 26, color: COLORS.orangeLight });
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
    burst(S.layers.fx, group.x + right.x / 2, y, { color: COLORS.orangeLight, count: 8, speed: 40, gravity: 140 });

    const tag = text('(int)', { size: 10, color: COLORS.muted });
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
    await S.announce(`BEURT ${e.turn}`);
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
    label.text = `(${to}) ${num(Math.trunc(e.hpBefore))} = ${num(e.hpAfter)}`;
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

  async CombatEnded(S, e) {
    sfx(e.won ? 'win' : 'lose');
    await S.banner(e.won ? 'GEWONNEN' : 'GECRASHT', e.won ? COLORS.orange : COLORS.red);
  },
};
