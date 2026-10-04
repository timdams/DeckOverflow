// stage.js - de stage van de Controlekamer: twee automaten die hun regels volgen.
// Zet events om in animatie en geluid; kent geen spelregels en leest nooit zelf het duel.
// Na elke play volgt een sync met de momentopname als waarheid; de tijdlijn gebruikt alleen sync.
// De beeldtaal (juice), het geluid, de tekeningen en de teksten deelt ze met de stage van de Card Hall.

import {
  juice, COLORS, sec, wait, hitPause, sizeOf, shake, text, floatText, dashed, stamp, impactStar, speedLines, burst, roll, typeBadge, typeOf,
} from '../../card-hall/stage/juice.js';
import { initAudio, sfx, setMuted, isMuted } from '../../audio/audio.js';
import { loadArt, actorTexture, iconTexture } from '../../card-hall/stage/art.js';
import { loadStrings, t } from '../../card-hall/stage/strings.js';

// Een vaste wereld die meeschaalt met het vak, zoals de 960×540 van de Card Hall
const W = 720;
const H = 380;
const GROUND_Y = 270;
const HOME = { Player: { x: 190, y: GROUND_Y }, Enemy: { x: 530, y: GROUND_Y } };
const ACTOR_H = 150;
const BAR_W = 140;

const S = {
  app: null,
  root: null,
  shaker: null,
  layers: {},
  actors: {},          // Player en Enemy
  turnLabel: null,
  banner: null,
  baseSpeed: 1,
};

// ---------------------------------------------------------------- publieke API

export async function init(host) {
  await Promise.all([loadStrings(), loadArt(), initAudio()]).catch(() => {});

  const app = new PIXI.Application();
  await app.init({
    resizeTo: host,
    background: COLORS.paper,
    antialias: true,
    resolution: Math.min(window.devicePixelRatio || 1, 2),
    autoDensity: true,
  });
  host.appendChild(app.canvas);
  S.app = app;

  S.root = new PIXI.Container();
  S.shaker = new PIXI.Container();
  app.stage.addChild(S.root);
  S.root.addChild(S.shaker);
  for (const name of ['bg', 'actors', 'fx']) {
    S.layers[name] = new PIXI.Container();
    S.shaker.addChild(S.layers[name]);
  }
  S.layers.top = new PIXI.Container();   // niet geschud: banners
  S.root.addChild(S.layers.top);

  buildBackground();
  S.turnLabel = text('', { size: 14, color: COLORS.muted });
  S.turnLabel.position.set(W / 2, 30);
  S.layers.bg.addChild(S.turnLabel);

  app.ticker.add(fit);
  fit();
  // resizeTo volgt alleen het venster; het vak kan ook zelf van maat veranderen
  S.observer = new ResizeObserver(() => S.app?.resize());
  S.observer.observe(host);
}

/** Een nieuw gevecht of terug naar het bord: de automaten op hun plaats, alles wat nog zweefde weg. */
export function show(snapshot) {
  clearFx();
  for (const side of ['Player', 'Enemy']) {
    const state = side === 'Player' ? snapshot.player : snapshot.enemy;
    if (S.actors[side]?.key !== state.key || S.actors[side]?.kind !== state.kind) {
      S.actors[side]?.container.destroy({ children: true });
      S.actors[side] = createActor(side, state);
    }
  }
  sync(snapshot);
}

export async function play(events) {
  // Een verborgen tabblad (een andere app, een ander tabblad) tekent geen frames: dan zou elke animatie
  // blijven wachten. Overslaan mag, want na elke zet volgt een sync met de waarheid.
  if (document.hidden) return;
  for (const e of events) {
    const handler = HANDLERS[e.type];
    if (handler) await handler(e);   // onbekende types negeert de stage
  }
}

export function sync(snapshot) {
  S.limits = { Player: snapshot.player.crashLength, Enemy: snapshot.enemy.crashLength };
  setActor(S.actors.Player, snapshot.player);
  setActor(S.actors.Enemy, snapshot.enemy);
  S.turnLabel.text = snapshot.turn > 0 ? t('room.stage.turn', { turn: snapshot.turn }) : '';
  if (!snapshot.outcome) clearBanner();
}

/** 1, 2 of 4 keer zo snel. */
export function setSpeed(speed) {
  juice.speed = 1 / Math.max(1, speed);
}

export function setSound(on) {
  setMuted(!on);
}

export function soundOn() {
  return !isMuted();
}

export function setLanguage(lang) {
  return loadStrings(lang);
}

export function dispose() {
  juice.speed = 1;
  S.observer?.disconnect();
  S.app?.destroy(true, { children: true });
  S.app = null;
  S.actors = {};
}

// ---------------------------------------------------------------- scene

function fit() {
  const { width, height } = S.app.screen;
  const scale = Math.min(width / W, height / H);
  S.root.scale.set(scale);
  S.root.position.set((width - W * scale) / 2, (height - H * scale) / 2);
}

function buildBackground() {
  const bg = S.layers.bg;
  const page = new PIXI.Graphics().rect(8, 8, W - 16, H - 16).stroke({ width: 1, color: COLORS.rule });
  bg.addChild(page);

  // De vloer: één lijn met arcering, zoals in een technische tekening
  const floor = new PIXI.Graphics().moveTo(60, GROUND_Y + 6).lineTo(W - 60, GROUND_Y + 6).stroke({ width: 2, color: COLORS.ink });
  for (let x = 66; x < W - 60; x += 12) floor.moveTo(x, GROUND_Y + 7).lineTo(x - 8, GROUND_Y + 15);
  floor.stroke({ width: 1, color: COLORS.muted });
  bg.addChild(floor);

  // Twee automaten met een stippellijn ertussen: ze kijken naar elkaar
  const guide = new PIXI.Graphics();
  dashed(guide, HOME.Player.x + 70, GROUND_Y - 120, HOME.Enemy.x - 70, GROUND_Y - 120, { color: COLORS.rule, width: 2, dash: 10, gap: 8 });
  bg.addChild(guide);
}

function clearFx() {
  S.layers.fx.removeChildren().forEach((c) => c.destroy({ children: true }));
  clearBanner();
}

function clearBanner() {
  if (!S.banner) return;
  S.banner.destroy({ children: true });
  S.banner = null;
}

// ---------------------------------------------------------------- automaten

function iconSprite(name, size) {
  const tex = iconTexture(name);
  if (!tex) return new PIXI.Container();
  const img = new PIXI.Sprite(tex);
  img.anchor.set(0.5);
  img.width = img.height = size;
  return img;
}

function createActor(side, state) {
  const home = HOME[side];
  const container = new PIXI.Container();
  container.position.set(home.x, home.y);

  // De tekening staat met haar voeten op de vloer; de vijand kijkt naar links
  const body = new PIXI.Container();
  const tex = actorTexture(state.key) ?? actorTexture(side === 'Player' ? 'player' : 'golem');
  if (tex) {
    const img = new PIXI.Sprite(tex);
    img.anchor.set(0.5, 1);
    img.scale.set(ACTOR_H / tex.height);
    body.addChild(img);
  }
  container.addChild(body);

  // HP-balk met het C#-type ernaast, in de kleur van dat type
  const bar = new PIXI.Container();
  bar.position.set(-BAR_W / 2, 18);
  const barBg = new PIXI.Graphics().rect(0, 0, BAR_W, 18).fill(COLORS.white);
  const barFill = new PIXI.Graphics();
  const barLine = new PIXI.Graphics().rect(0, 0, BAR_W, 18).stroke({ width: 2, color: COLORS.ink });
  const hpText = text('', { size: 12, halo: true });
  hpText.position.set(BAR_W / 2, 9);
  const badge = typeBadge(state.kind ?? 'Int');
  badge.position.set(BAR_W + 6 + badge.badgeWidth / 2, 9);
  bar.addChild(barBg, barFill, barLine, hpText, badge);
  const badgeAt = { x: badge.x, y: badge.y };

  // Blok: een beschermplaat links van de balk, met het getal erin
  const blockBox = new PIXI.Container();
  const plate = new PIXI.Graphics().roundRect(-16, -16, 32, 32, 4).fill(COLORS.white).stroke({ width: juice.line, color: COLORS.ink });
  for (const [x, y] of [[-12, -12], [12, -12], [-12, 12], [12, 12]]) plate.circle(x, y, 1.8).stroke({ width: 1.2, color: COLORS.ink });
  const blockText = text('', { size: 13, halo: true });
  blockBox.addChild(plate, blockText);
  blockBox.position.set(-BAR_W / 2 - 24, 27);
  blockBox.visible = false;

  // Opgeladen: een veer met ×2, boven het hoofd
  const charge = new PIXI.Container();
  charge.addChild(iconSprite('intent-buff', 30));
  const chargeText = text('×2', { size: 16, halo: true });
  chargeText.position.set(26, 0);
  charge.addChild(chargeText);
  charge.position.set(-10, -ACTOR_H - 22);
  charge.visible = false;

  // Herstellingen: een sleutel met het aantal dat er nog over is
  const repairs = new PIXI.Container();
  repairs.addChild(iconSprite('intent-heal', 20));
  const repairsText = text('', { size: 12, halo: true, anchor: 0 });
  repairsText.anchor.set(0, 0.5);
  repairsText.position.set(13, 0);
  repairs.addChild(repairsText);
  repairs.position.set(-BAR_W / 2 + 10, 50);

  // Onder de balk, rechts: de lengte van een tekst, of de teller van een automaat die telt
  const extra = text('', { size: 12, color: COLORS.muted, anchor: 1 });
  extra.anchor.set(1, 0.5);
  extra.position.set(BAR_W / 2, 50);

  container.addChild(bar, blockBox, charge, repairs, extra);
  S.layers.actors.addChild(container);

  return { side, key: state.key, kind: state.kind, tint: marker(typeOf(state.kind).color), container, body, bar, badge, badgeAt, barFill, hpText, blockBox, blockText, charge, repairs, repairsText, extra, hp: state.hp, maxHp: state.maxHp };
}

function drawHp(a, hp) {
  a.hp = hp;
  a.hpText.text = `${Math.round(hp)}/${a.maxHp}`;
  // Een byte kan boven zijn startwaarde opgelapt worden (tot 255): de balk loopt dan vol
  const w = Math.max(0, Math.min(BAR_W - 2, (BAR_W - 2) * hp / a.maxHp));
  a.barFill.clear().rect(1, 1, w, 16).fill(a.tint);   // getint in de kleur van het type, zoals in de Card Hall
}

/** Een tekst in plaats van HP: de balk vult zich naarmate de tekst de crashlengte nadert. */
function drawText(a, value, limit) {
  // Een lange tekst past niet in de balk: het begin, en de lengte ernaast
  a.hpText.text = value.length > 13 ? `"${value.slice(0, 12)}…` : `"${value}"`;
  const w = limit > 0 ? Math.min(BAR_W - 2, (BAR_W - 2) * value.length / limit) : 0;
  a.barFill.clear().rect(1, 1, w, 16).fill(a.tint);
  a.extra.text = limit > 0 ? `.Length ${value.length}/${limit}` : '';
}

/** Een ander type na omgieten of converteren: een nieuwe badge en een andere tint. */
function setKind(a, kind) {
  if (a.kind === kind) return;
  a.kind = kind;
  a.tint = marker(typeOf(kind).color);
  a.badge.destroy({ children: true });
  a.badge = typeBadge(kind);
  a.badge.position.set(BAR_W + 6 + a.badge.badgeWidth / 2, a.badgeAt.y);
  a.bar.addChild(a.badge);
}

function setActor(a, state) {
  if (!a) return;
  setKind(a, state.kind);
  a.maxHp = state.maxHp;
  if (state.kind === 'String') drawText(a, state.text ?? '', state.crashLength);
  else drawHp(a, state.hp);
  if (state.counter !== null && state.counter !== undefined) a.extra.text = t('room.stage.counter', { after: state.counter });
  a.blockBox.visible = state.block > 0;
  a.blockText.text = String(state.block);
  a.charge.visible = state.charged;
  a.repairs.visible = state.repairs > 0;
  a.repairs.alpha = state.repairsLeft > 0 ? 1 : 0.4;
  a.repairsText.text = `${state.repairsLeft}/${state.repairs}`;
  // Een automaat die terugkomt na terugspoelen staat weer recht
  if (!state.dead) {
    gsap.killTweensOf(a.body);
    a.body.rotation = 0;
    a.body.alpha = 1;
    a.body.position.set(0, 0);
    a.body.scale.set(1);
  }
}

/** Een lichte tint van een typekleur, zoals de markeerstift in de Card Hall. */
function marker(color) {
  const mix = (c) => Math.round(c + (255 - c) * 0.45);
  return (mix((color >> 16) & 0xff) << 16) | (mix((color >> 8) & 0xff) << 8) | mix(color & 0xff);
}

const other = (side) => (side === 'Player' ? 'Enemy' : 'Player');
const head = (a) => ({ x: a.container.x, y: a.container.y - ACTOR_H * 0.6 });

// ---------------------------------------------------------------- events

const HANDLERS = {
  TurnStarted: async (e) => {
    S.turnLabel.text = t('room.stage.turn', { turn: e.turn });
  },

  RuleFired: async (e) => {
    // Een kort knikje: deze regel klopte als eerste
    const a = S.actors[e.side];
    await gsap.to(a.body, { y: -8, duration: sec(80), yoyo: true, repeat: 1, ease: 'power1.out' });
  },

  NoRuleMatched: async (e) => {
    const a = S.actors[e.side];
    const p = head(a);
    sfx('tick', { volume: 0.4 });
    floatText(S.layers.fx, t('room.stage.idle'), p.x, p.y - 40, { size: 18, color: COLORS.muted, rise: 20 });
    await wait(450);
  },

  BlockExpired: async (e) => {
    const a = S.actors[e.side];
    await gsap.to(a.blockBox, { alpha: 0, duration: sec(120) });
    a.blockBox.visible = false;
    a.blockBox.alpha = 1;
  },

  Whacked: async (e) => {
    const attacker = S.actors[e.side];
    const target = S.actors[other(e.side)];
    const dir = e.side === 'Player' ? 1 : -1;
    const hurt = e.dealt;
    const p = head(target);

    // Aanloop en klap
    await gsap.to(attacker.body, { x: dir * 70, duration: sec(130), ease: 'power2.in' });
    if (e.wasCharged) {
      impactStar(S.layers.fx, p.x - dir * 30, p.y);
      speedLines(S.layers.fx, p.x - dir * 30, p.y, { count: 12 });
    }
    if (e.absorbed > 0) {
      sfx('shield');
      floatText(S.layers.fx, t('room.stage.absorbed', { amount: e.absorbed }), p.x, p.y - 30, { size: 20, color: COLORS.muted });
    }
    if (hurt > 0) {
      const size = sizeOf(hurt);
      sfx(e.wasCharged || size === 'large' ? 'hitBig' : 'hit');
      shake(target.body, juice.shakePx[size]);
      if (e.wasCharged) shake(S.shaker, juice.shakePx.medium);
      burst(S.layers.fx, p.x - dir * 20, p.y, { count: e.wasCharged ? 14 : 8 });
      floatText(S.layers.fx, `-${hurt}`, p.x, p.y, { size: e.wasCharged ? 36 : 28 });
      await hitPause(juice.hitPauseMs[size]);
      roll(target.hp, e.hpAfter, (v) => drawHp(target, v));
    }
    await gsap.to(attacker.body, { x: 0, duration: sec(180), ease: 'power2.out' });
  },

  // Een kommagetal op een geheel type: wat na de komma staat, valt eraf
  ValueTruncated: async (e) => {
    const target = S.actors[e.target];
    const p = head(target);
    sfx('tinkle', { volume: 0.6 });
    floatText(S.layers.fx, String(e.before), p.x, p.y - 56, { size: 18, color: COLORS.muted, rise: 10, holdMs: 250 });
    await wait(260);
    floatText(S.layers.fx, t('room.stage.truncated', { after: e.after }), p.x, p.y - 34, { size: 18, rise: 16 });
    await wait(200);
  },

  ValueRounded: async (e) => {
    const target = S.actors[e.target];
    const p = head(target);
    sfx('tick', { volume: 0.6 });
    floatText(S.layers.fx, String(e.before), p.x, p.y - 56, { size: 18, color: COLORS.muted, rise: 10, holdMs: 250 });
    await wait(260);
    floatText(S.layers.fx, t('room.stage.rounded', { after: e.after }), p.x, p.y - 34, { size: 18, rise: 16 });
    await wait(200);
  },

  // Een klap op tekst trekt niets af: het getal plakt erachter, en de tekst groeit
  TextAppended: async (e) => {
    const target = S.actors[e.target];
    const p = head(target);
    sfx('click');
    floatText(S.layers.fx, `+ ${e.added}`, p.x, p.y - 20, { size: 22, rise: 30 });
    drawText(target, e.after, S.limits?.[e.target] ?? 0);
    await shake(target.hpText, juice.shakePx.small);
  },

  TextCrashed: async (e) => {
    const target = S.actors[e.target];
    sfx('glitch');
    shake(S.shaker, juice.shakePx.large);
    floatText(S.layers.fx, t('room.stage.text-crash'), target.container.x, target.container.y - ACTOR_H - 10, { size: 22 });
    await hitPause(juice.hitPauseMs.large);
    await wait(300);
  },

  // Een teller van het type byte loopt over: 255 + 1 is 0
  CounterOverflowed: async (e) => {
    const a = S.actors[e.side];
    sfx('freeze');
    floatText(S.layers.fx, `${e.before} + 1 → ${e.after}`, a.container.x, a.container.y - ACTOR_H - 10, { size: 22 });
    a.extra.text = t('room.stage.counter', { after: e.after });
    shake(a.body, juice.shakePx.medium);
    await hitPause(juice.overflow.freezeMs);
    sfx('glitch');
    await wait(300);
  },

  // Omgieten of converteren: een nieuw type. Een cast die overloopt, bevriest eerst en klapt dan om
  TypeChanged: async (e) => {
    const a = S.actors[e.target];
    const p = head(a);
    sfx(e.wrapped ? 'freeze' : 'click');
    if (e.wrapped) {
      shake(a.body, juice.shakePx.medium);
      await hitPause(juice.overflow.freezeMs);
      sfx('glitch');
    }
    setKind(a, e.to);
    drawHp(a, e.after);
    floatText(S.layers.fx, t('room.stage.cast', { to: e.to.toLowerCase(), after: e.after }), p.x, p.y - 30, { size: 24 });
    await wait(450);
  },

  TypeUnchanged: async (e) => {
    const a = S.actors[e.target];
    sfx('buzz', { volume: 0.5 });
    floatText(S.layers.fx, t('room.stage.unchanged', { kind: e.kind.toLowerCase() }), a.container.x, a.container.y - ACTOR_H - 10, { size: 16, color: COLORS.muted });
    await wait(350);
  },

  // Convert controleert: te groot werpt een exception op, en de automaat crasht
  ConversionCrashed: async (e) => {
    const a = S.actors[e.target];
    sfx('glitch');
    shake(a.body, juice.shakePx.large);
    floatText(S.layers.fx, t('room.stage.convert-crash'), a.container.x, a.container.y - ACTOR_H - 10, { size: 22 });
    await hitPause(juice.hitPauseMs.medium);
    await wait(350);
  },

  MoveSkipped: async (e) => {
    const a = S.actors[e.side];
    sfx('buzz', { volume: 0.5 });
    floatText(S.layers.fx, t('room.stage.skipped'), a.container.x, a.container.y - ACTOR_H - 10, { size: 18, color: COLORS.muted });
    await gsap.to(a.body, { rotation: 0.12, duration: sec(120), yoyo: true, repeat: 1 });
  },

  // Een byte die voorbij 255 opgelapt wordt: de balk loopt vol, bevriest, en klapt om
  ValueOverflowed: async (e) => {
    const a = S.actors[e.side];
    sfx('heal');
    await roll(a.hp, 255, (v) => drawHp(a, Math.min(v, a.maxHp)), { maxMs: 500 });
    sfx('freeze');
    shake(a.body, juice.shakePx.medium);
    floatText(S.layers.fx, t('room.stage.overflow'), a.container.x, a.container.y - ACTOR_H - 10, { size: 22 });
    await hitPause(juice.overflow.freezeMs);
    sfx('glitch');
    drawHp(a, e.after);
    floatText(S.layers.fx, String(e.after), a.container.x, a.container.y + 4, { size: 30 });
    await wait(500);
  },

  Braced: async (e) => {
    const a = S.actors[e.side];
    sfx('shield', { volume: 0.6 });
    a.blockBox.visible = true;
    a.blockText.text = String(e.total);
    a.blockBox.scale.set(0.3);
    floatText(S.layers.fx, t('room.stage.block', { amount: e.amount }), a.container.x, a.container.y - ACTOR_H - 10, { size: 20 });
    await gsap.to(a.blockBox.scale, { x: 1, y: 1, duration: sec(220), ease: 'back.out(3)' });
  },

  Repaired: async (e) => {
    const a = S.actors[e.side];
    sfx('heal');
    floatText(S.layers.fx, `+${e.amount}`, a.container.x, a.container.y - ACTOR_H - 10, { size: 26 });
    await roll(a.hp, e.hpAfter, (v) => drawHp(a, v));
    await wait(200);
  },

  RepairEmpty: async (e) => {
    const a = S.actors[e.side];
    sfx('buzz', { volume: 0.6 });
    floatText(S.layers.fx, t('room.stage.repair-empty'), a.container.x, a.container.y - ACTOR_H - 10, { size: 20, color: COLORS.muted });
    await shake(a.body, juice.shakePx.small);
  },

  WoundUp: async (e) => {
    const a = S.actors[e.side];
    if (e.wasCharged) {
      sfx('tick', { volume: 0.5 });
      floatText(S.layers.fx, t('room.stage.already-wound'), a.container.x, a.container.y - ACTOR_H - 10, { size: 16, color: COLORS.muted });
      await wait(350);
      return;
    }
    sfx('click');
    // Indrukken als een veer, dan terugveren
    await gsap.to(a.body.scale, { x: 1.12, y: 0.82, duration: sec(140), ease: 'power2.in' });
    a.charge.visible = true;
    a.charge.scale.set(0.3);
    gsap.to(a.charge.scale, { x: 1, y: 1, duration: sec(200), ease: 'back.out(3)' });
    await gsap.to(a.body.scale, { x: 1, y: 1, duration: sec(220), ease: 'elastic.out(1, 0.4)' });
  },

  DuelEnded: async (e) => {
    const won = e.outcome === 'PlayerWon';
    const loser = e.outcome === 'PlayerWon' ? S.actors.Enemy : e.outcome === 'EnemyWon' ? S.actors.Player : null;
    if (loser) {
      const dir = loser.side === 'Player' ? -1 : 1;
      gsap.to(loser.body, { rotation: dir * 1.2, alpha: 0.35, duration: sec(500), ease: 'power2.in' });
    }
    sfx(won ? 'win' : 'lose');
    const key = e.outcome === 'PlayerWon' ? 'room.stage.won' : e.outcome === 'EnemyWon' ? 'room.stage.lost' : 'room.stage.shift-over';
    banner(t(key), won);
    await wait(500);
  },
};

function banner(label, won) {
  clearBanner();
  const box = new PIXI.Container();
  const words = text(label, { size: 34 });
  const w = words.width + 50;
  const h = 64;
  const plate = new PIXI.Graphics().rect(-w / 2, -h / 2, w, h).fill(COLORS.white).stroke({ width: 3, color: COLORS.ink });
  box.addChild(plate, words);
  box.position.set(W / 2, H / 2 - 70);
  box.scale.set(1, 0);
  S.layers.top.addChild(box);
  S.banner = box;
  gsap.to(box.scale, { y: 1, duration: sec(300), ease: 'back.out(2)' });
  stamp(box, w / 2 - 10, -h / 2 + 10, won ? 'check' : 'cross', { radius: 22 });
}
