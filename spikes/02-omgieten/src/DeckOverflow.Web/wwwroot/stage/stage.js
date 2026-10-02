// stage.js - init, scene graph, hand en input. Kent geen spelregels:
// speelt events af (timeline.js) en neemt de snapshot als waarheid (sync).

import { juice, sec, wait, roll, text, burst, num, typeOf, typeBadge, COLORS, FONT_BODY } from './juice.js';
import { initAudio, sfx, toggleMute } from './audio.js';
import { runQueue } from './timeline.js';
import { pixelSprite, icon, ART } from './sprites.js';

const W = 960;
const H = 540;
const GROUND_Y = 335;
const HAND_Y = 462;
const CARD_W = 108;
const CARD_H = 150;
const PLAY_LINE_Y = 385;   // kaart boven deze lijn loslaten = spelen (voor Self-kaarten)
const POSITIONS = { player: { x: 255, y: GROUND_Y }, enemy: { x: 690, y: GROUND_Y } };

// Placeholder-art per vijand. De motor geeft alleen een sleutel mee.
const LOOKS = {
  jij: { art: ART.hero, px: 9 },
  geest: { art: ART.geest, px: 10, float: true },
  kolos: { art: ART.kolos, px: 10 },
  golem: { art: ART.golem, px: 10 },
};

const CARD_ART = { slag: 'sword', 'vlottende-slag': 'vlot', schild: 'shield', herstel: 'heart', 'giet-int': 'kroes', 'giet-byte': 'kroes' };

const S = {
  app: null,
  root: null,      // geschaalde wereld van W x H
  shaker: null,    // wordt geschud, bevat alle spel-lagen
  layers: {},
  dotnet: null,
  actors: new Map(),
  hand: [],
  pending: null,
  busy: false,
  turn: 0,
  snapshot: null,
  hud: null,
  metrics: { loadMs: null, t0: null, reactMs: null, engineMs: null, fps: 0, minFps: null, playing: false },
};

// ---------------------------------------------------------------- publieke API

export async function init(host, dotnetRef, loadMs) {
  S.dotnet = dotnetRef;
  S.metrics.loadMs = loadMs;

  await Promise.all([
    document.fonts.load('16px "Press Start 2P"'),
    document.fonts.load('16px "VT323"'),
    initAudio(),
  ]).catch(() => {});

  const app = new PIXI.Application();
  await app.init({
    resizeTo: host,
    background: COLORS.bg,
    antialias: false,
    roundPixels: true,
    resolution: window.devicePixelRatio || 1,
    autoDensity: true,
  });
  host.appendChild(app.canvas);
  S.app = app;

  S.root = new PIXI.Container();
  S.shaker = new PIXI.Container();
  app.stage.addChild(S.root);
  S.root.addChild(S.shaker);

  for (const name of ['bg', 'actors', 'hand', 'fx', 'ui']) {
    S.layers[name] = new PIXI.Container();
    S.shaker.addChild(S.layers[name]);
  }
  S.layers.top = new PIXI.Container();   // niet geschud: flits, banners
  S.root.addChild(S.layers.top);

  buildBackground();
  buildUi();
  buildHud();
  setupInput();

  app.ticker.add(onTick);
  fit();
}

export async function play(events, meta = {}) {
  S.metrics.engineMs = meta.engineMs ?? null;
  S.metrics.playing = true;
  S.metrics.minFps = null;

  if (S.metrics.t0 !== null) {
    const t0 = S.metrics.t0;
    S.metrics.t0 = null;
    requestAnimationFrame(() => { S.metrics.reactMs = performance.now() - t0; });
  }

  try {
    await runQueue(S, events);
  } finally {
    S.metrics.playing = false;
  }
}

export function sync(snapshot) {
  const newTurn = snapshot.turn !== S.turn;
  S.snapshot = snapshot;

  for (const c of snapshot.combatants) {
    const a = S.actors.get(c.id) ?? createActor(c);
    a.data = c;
    a.setKind(c.kind, c.maxHp);
    a.setHp(c.hp);
    a.setBlock(c.block);
    if (c.intent && !a.intentShown) a.typeIntent(c.intent.expression, c.intent.value);
    if (!c.intent) a.setIntent(null);
  }

  S.ui.energy.text = `${snapshot.energy}/${snapshot.maxEnergy}`;
  S.ui.piles.text = `trek ${snapshot.drawPile} · aflegstapel ${snapshot.discardPile}`;

  if (newTurn) {
    S.turn = snapshot.turn;
    dealHand(snapshot.hand);
  } else {
    matchHand(snapshot.hand);
  }
}

export function reset() {
  for (const a of S.actors.values()) a.container.destroy({ children: true });
  S.actors.clear();
  for (const c of S.hand) c.destroy({ children: true });
  S.hand = [];
  S.layers.fx.removeChildren().forEach((c) => c.destroy({ children: true }));
  S.layers.top.removeChildren().forEach((c) => c.destroy({ children: true }));
  S.turn = 0;
  S.pending = null;
  S.busy = false;
}

export function dispose() {
  window.removeEventListener('keydown', onKey);
  S.app?.destroy(true, { children: true });
  S.app = null;
  S.actors.clear();
  S.hand = [];
}

// ---------------------------------------------------------------- helpers voor timeline.js

S.actor = (id) => S.actors.get(id);

S.takePendingCard = ({ keep = false } = {}) => {
  const p = S.pending;
  if (!p) return null;
  if (!keep) {
    S.hand = S.hand.filter((c) => c !== p.card);
    S.pending = null;
  }
  return p.card;
};

S.layoutHand = () => layoutHand();

S.discardHand = async () => {
  const cards = S.hand;
  S.hand = [];
  await Promise.all(cards.map((c, i) =>
    gsap.to(c, {
      y: H + CARD_H, x: c.x + 260, rotation: 0.6,
      delay: sec(i * 40), duration: sec(300), ease: 'power2.in',
      onComplete: () => c.destroy({ children: true }),
    })));
};

S.dimExcept = (actor) => {
  const others = [S.layers.bg, S.layers.hand, S.layers.ui, ...[...S.actors.values()].filter((a) => a !== actor).map((a) => a.container)];
  gsap.to(others, { alpha: 0.25, duration: sec(150) });
  return { restore: () => gsap.to(others, { alpha: 1, duration: sec(300) }) };
};

S.flashScreen = (strength = 0.8) => {
  const f = new PIXI.Graphics().rect(0, 0, W, H).fill(COLORS.white);
  f.alpha = strength;
  S.layers.top.addChild(f);
  gsap.to(f, { alpha: 0, duration: sec(260), ease: 'power2.out', onComplete: () => f.destroy() });
};

S.announce = async (label) => {
  const t = text(label, { size: 22, color: COLORS.orange });
  t.position.set(-200, H / 2 - 40);
  S.layers.top.addChild(t);
  await gsap.timeline({ onComplete: () => t.destroy() })
    .to(t, { x: W / 2, duration: sec(220), ease: 'power3.out' })
    .to(t, { x: W + 200, duration: sec(220), ease: 'power3.in' }, `+=${sec(300)}`);
};

S.banner = async (label, color) => {
  const plate = new PIXI.Graphics().rect(-W / 2, -48, W, 96).fill({ color: COLORS.ink, alpha: 0.85 });
  const t = text(label, { size: 48, color });
  const banner = new PIXI.Container();
  banner.addChild(plate, t);
  banner.position.set(W / 2, H / 2 - 40);
  banner.scale.set(1, 0);
  S.layers.top.addChild(banner);
  await gsap.to(banner.scale, { y: 1, duration: sec(300), ease: 'back.out(2)' });
  burst(S.layers.top, W / 2, H / 2 - 40, { color, count: 40, speed: 320, ms: 900 });
};

// ---------------------------------------------------------------- scene

function buildBackground() {
  const bg = S.layers.bg;
  const grid = new PIXI.Graphics();
  for (let x = 0; x <= W; x += 24) grid.moveTo(x, 0).lineTo(x, H);
  for (let y = 0; y <= H; y += 24) grid.moveTo(0, y).lineTo(W, y);
  grid.stroke({ width: 1, color: COLORS.grid, alpha: 0.05 });
  bg.addChild(grid);

  // Geheugenadressen die traag voorbij schuiven: we zitten in een draaiend programma
  S.hexColumns = [];
  for (let i = 0; i < 9; i++) {
    const lines = Array.from({ length: 30 }, () => `0x${Math.floor(Math.random() * 0xffff).toString(16).padStart(4, '0')}`).join('\n');
    const col = new PIXI.Text({ text: lines, style: { fontFamily: FONT_BODY, fontSize: 18, fill: COLORS.orange, lineHeight: 20 } });
    col.alpha = 0.035;
    col.position.set(20 + i * 110, -Math.random() * 300);
    col.speed = 4 + Math.random() * 6;
    bg.addChild(col);
    S.hexColumns.push(col);
  }

  const floor = new PIXI.Graphics()
    .rect(0, GROUND_Y + 4, W, 3).fill({ color: COLORS.orange, alpha: 0.25 })
    .rect(0, GROUND_Y + 10, W, 1).fill({ color: COLORS.orange, alpha: 0.1 });
  bg.addChild(floor);
}

function buildUi() {
  const ui = S.layers.ui;

  const orb = new PIXI.Container();
  const ring = new PIXI.Graphics().circle(0, 0, 34).fill(COLORS.orange).circle(0, 0, 27).fill(COLORS.ink);
  const energy = text('3/3', { size: 16, color: COLORS.orangeLight });
  orb.addChild(ring, energy);
  orb.position.set(70, HAND_Y + 8);
  ui.addChild(orb);

  const piles = new PIXI.Text({ text: '', style: { fontFamily: FONT_BODY, fontSize: 18, fill: COLORS.muted } });
  piles.anchor.set(1, 0);
  piles.position.set(W - 12, 6);
  ui.addChild(piles);

  S.ui = { orb, energy, piles };
}

function buildHud() {
  const hud = new PIXI.Text({ text: '', style: { fontFamily: FONT_BODY, fontSize: 16, fill: COLORS.muted } });
  hud.position.set(10, 6);
  hud.alpha = 0.8;
  S.root.addChild(hud);
  S.hud = hud;
  S.hudTimer = 0;
}

function onTick(ticker) {
  fit();

  for (const col of S.hexColumns) {
    col.y += (col.speed * ticker.deltaMS) / 1000;
    if (col.y > 0) col.y = -300;
  }

  // Meetpunten voor de succescriteria
  const instant = 1000 / Math.max(1, ticker.deltaMS);
  if (S.metrics.playing) S.metrics.minFps = Math.min(S.metrics.minFps ?? instant, instant);
  S.hudTimer += ticker.deltaMS;
  if (S.hudTimer > 250) {
    S.hudTimer = 0;
    const m = S.metrics;
    const f = (v, unit = 'ms') => (v === null || v === undefined ? '-' : `${v.toFixed(v < 10 ? 1 : 0)} ${unit}`);
    S.hud.text = [
      `fps ${ticker.FPS.toFixed(0)}  min tijdens laatste animatie ${m.minFps === null ? '-' : m.minFps.toFixed(0)}`,
      `klik→frame ${f(m.reactMs)}  motor ${f(m.engineMs)}  speelbaar na ${f(m.loadMs)}`,
      `[F] snel ${juice.speed < 1 ? 'aan' : 'uit'}  [M] geluid`,
    ].join('\n');
  }
}

function fit() {
  const { width, height } = S.app.screen;
  const scale = Math.min(width / W, height / H);
  S.root.scale.set(scale);
  S.root.position.set((width - W * scale) / 2, (height - H * scale) / 2);
}

// ---------------------------------------------------------------- actors

function createActor(c) {
  const isEnemy = c.isEnemy;
  const home = isEnemy ? POSITIONS.enemy : POSITIONS.player;
  const look = LOOKS[c.key] ?? (isEnemy ? LOOKS.golem : LOOKS.jij);
  const sprite = pixelSprite(look.art, look.px);

  const container = new PIXI.Container();
  container.position.set(home.x, home.y);
  container.addChild(sprite.container);

  // HP-balk met het C#-type ernaast: de belangrijkste hint in het hele scherm
  const barW = 150;
  const bar = new PIXI.Container();
  bar.position.set(-barW / 2, 18);
  const barBg = new PIXI.Graphics().rect(0, 0, barW, 16).fill(COLORS.ink).rect(2, 2, barW - 4, 12).fill(0x2b2533);
  const barFill = new PIXI.Graphics().rect(2, 2, barW - 4, 12).fill(COLORS.white);   // getint in de kleur van het type
  const hpText = text('', { size: 10 });
  hpText.position.set(barW / 2, 8);
  bar.addChild(barBg, barFill, hpText);

  const badgeSlot = new PIXI.Container();
  badgeSlot.position.set(barW + 8, 8);
  bar.addChild(badgeSlot);

  const blockBox = new PIXI.Container();
  const shieldIcon = icon('shield', 3);
  const blockText = text('', { size: 10 });
  blockBox.addChild(shieldIcon, blockText);
  blockBox.position.set(-barW / 2 - 30, 26);
  blockBox.visible = false;

  const intent = new PIXI.Container();
  intent.position.set(0, -sprite.h - 34);
  const intentText = text('', { size: 14, color: COLORS.orangeLight });
  const intentValue = new PIXI.Text({ text: '', style: { fontFamily: FONT_BODY, fontSize: 18, fill: COLORS.muted } });
  intentValue.anchor.set(0.5);
  intentValue.position.set(0, 18);
  intent.addChild(intentText, intentValue);

  container.addChild(bar, blockBox, intent);
  S.layers.actors.addChild(container);

  const a = {
    id: c.id,
    isEnemy,
    home,
    data: c,
    container,
    sprite,
    blockBox,
    blockIcon: shieldIcon,
    intentNode: intent,
    intentShown: false,
    hp: c.hp,
    kind: null,
    bornAs: c.kind,
    max: c.maxHp,
    badgeSlot,

    center: () => ({ x: container.x, y: container.y - sprite.h / 2 }),
    head: () => ({ x: container.x, y: container.y - sprite.h - 10 }),

    setHp(value) {
      a.hp = value;
      hpText.text = `${num(value)}/${num(a.max)}`;
      barFill.scale.x = Math.max(0, Math.min(1, value / a.max));
    },

    rollHp(from, to) {
      return roll(from, to, (v) => a.setHp(v), { decimals: a.kind === 'Double' ? 1 : 0 });
    },

    setBlock(value) {
      blockBox.visible = value > 0;
      blockBox.alpha = 1;
      blockText.text = num(value);
    },

    /** Typebadge en kleur van de HP-balk volgen het C#-type. */
    setKind(kind, max) {
      a.max = max;
      if (kind === a.kind) return;
      a.kind = kind;
      badgeSlot.removeChildren().forEach((b) => b.destroy({ children: true }));
      const badge = typeBadge(kind);
      badge.x = badge.badgeWidth / 2;
      badgeSlot.addChild(badge);
      barFill.tint = typeOf(kind).color;
      // Omgegoten: het lijf draagt voortaan de kleur van zijn nieuwe type (ogen en omlijning niet)
      sprite.overlay.tint = typeOf(kind).color;
      sprite.overlay.alpha = kind === a.bornAs ? 0 : 0.75;
    },

    /** Idle: zachte ademhaling, of zweven voor een geest. */
    idle() {
      gsap.killTweensOf(sprite.container.scale);
      gsap.killTweensOf(sprite.container, 'y');
      sprite.container.scale.set(1);
      sprite.container.y = 0;
      if (look.float) {
        gsap.to(sprite.container, { y: -8, duration: 1.4 + Math.random() * 0.3, yoyo: true, repeat: -1, ease: 'sine.inOut' });
      } else {
        gsap.to(sprite.container.scale, { y: 1.03, duration: 1.1 + Math.random() * 0.3, yoyo: true, repeat: -1, ease: 'sine.inOut' });
      }
    },

    setIntent(intentData) {
      intent.visible = !!intentData;
      a.intentShown = !!intentData;
      if (intentData) {
        intentText.text = intentData.expression;
        intentValue.text = `= ${num(intentData.value)}`;
      }
    },

    async typeIntent(expression, value) {
      a.intentShown = true;
      intent.visible = true;
      intentValue.text = '';
      for (let i = 1; i <= expression.length; i++) {
        intentText.text = expression.slice(0, i);
        if (expression[i - 1] !== ' ') sfx('tick', { volume: 0.25, rate: 1.6 });
        await wait(35);
      }
      intentValue.text = `= ${num(value)}`;
      intentValue.alpha = 0;
      gsap.to(intentValue, { alpha: 1, duration: sec(200) });
    },

    knock(dir, px) {
      gsap.fromTo(sprite.container, { x: dir * px }, { x: 0, duration: sec(260), ease: 'elastic.out(1, 0.4)' });
    },

    die() {
      gsap.killTweensOf(sprite.container.scale);
      gsap.killTweensOf(sprite.container, 'y');
      sprite.container.visible = false;
      intent.visible = false;
      gsap.to(bar, { alpha: 0.3, duration: sec(300) });
    },
  };

  a.setKind(c.kind, c.maxHp);
  a.setHp(c.hp);
  a.setBlock(c.block);
  a.setIntent(null);
  a.idle();
  S.actors.set(c.id, a);
  return a;
}

// ---------------------------------------------------------------- hand

function createCard(view) {
  const card = new PIXI.Container();
  const edge = view.kind ? typeOf(view.kind).color : COLORS.orange;
  const body = new PIXI.Graphics()
    .rect(-CARD_W / 2, -CARD_H / 2, CARD_W, CARD_H).fill(COLORS.ink)
    .rect(-CARD_W / 2 + 3, -CARD_H / 2 + 3, CARD_W - 6, CARD_H - 6).fill(edge)
    .rect(-CARD_W / 2 + 6, -CARD_H / 2 + 6, CARD_W - 12, CARD_H - 12).fill(COLORS.panel);

  const cost = new PIXI.Container();
  cost.addChild(new PIXI.Graphics().rect(-12, -12, 24, 24).fill(COLORS.ink).rect(-10, -10, 20, 20).fill(COLORS.orange));
  const costText = text(view.cost, { size: 12, color: COLORS.ink, stroke: false });
  cost.addChild(costText);
  cost.position.set(-CARD_W / 2 + 8, -CARD_H / 2 + 8);

  // Press Start 2P is breed: lange namen als "Vlottende Slag" krijgen een kleiner corps
  const name = text(view.name, { size: view.name.length > 9 ? 7 : 10, color: COLORS.white });
  name.position.set(6, -CARD_H / 2 + 26);

  const art = icon(CARD_ART[view.id] ?? 'sword', 5);
  art.position.set(0, -16);

  // Het type van de aanval, of bij Omgieten het type waarnaar je giet
  const castTo = { 'giet-int': 'Int', 'giet-byte': 'Byte' }[view.id];
  const extras = [];
  if (view.kind) {
    const badge = typeBadge(view.kind, { size: 8 });
    badge.position.set(CARD_W / 2 - 8 - badge.badgeWidth / 2, -CARD_H / 2 + 14);
    extras.push(badge);
  }
  if (castTo) {
    const arrow = text('→', { size: 10, color: COLORS.white });
    const badge = typeBadge(castTo, { size: 10 });
    arrow.position.set(-badge.badgeWidth / 2 - 8, 6);
    badge.position.set(8, 6);
    const group = new PIXI.Container();
    group.addChild(arrow, badge);
    group.position.set(0, 0);
    extras.push(group);
    art.y = -26;
  }

  const desc = new PIXI.Text({
    text: view.text,
    style: { fontFamily: FONT_BODY, fontSize: 19, fill: COLORS.white, align: 'center', wordWrap: true, wordWrapWidth: CARD_W - 18, lineHeight: 17 },
  });
  desc.anchor.set(0.5, 0);
  desc.position.set(0, 20);

  card.addChild(body, cost, name, art, ...extras, desc);
  card.view = view;
  card.eventMode = 'static';
  card.cursor = 'grab';
  card.on('pointerover', () => onCardHover(card, true));
  card.on('pointerout', () => onCardHover(card, false));
  card.on('pointerdown', (e) => onCardDown(card, e));
  S.layers.hand.addChild(card);
  return card;
}

function slotOf(index, count) {
  const spread = Math.min(118, 560 / Math.max(1, count - 1));
  const offset = index - (count - 1) / 2;
  return { x: W / 2 + offset * spread, y: HAND_Y + Math.abs(offset) * 6, rotation: offset * 0.045 };
}

function layoutHand(animate = true) {
  S.hand.forEach((card, i) => {
    card.handIndex = i;
    card.home = slotOf(i, S.hand.length);
    card.zIndex = i;
    if (S.drag?.card === card) return;
    if (animate) gsap.to(card, { ...card.home, duration: sec(220), ease: 'power2.out' });
    else Object.assign(card, card.home);
  });
  S.layers.hand.sortableChildren = true;
}

function dealHand(views) {
  for (const c of S.hand) c.destroy({ children: true });
  S.hand = views.map(createCard);
  S.hand.forEach((card, i) => { card.targetAlpha = views[i].playable ? 1 : 0.55; });
  layoutHand(false);
  S.hand.forEach((card, i) => {
    const home = { ...card.home };
    card.position.set(60, HAND_Y + 40);
    card.rotation = -0.8;
    card.scale.set(0.3);
    card.alpha = 0;
    gsap.to(card, {
      ...home, alpha: card.targetAlpha, delay: sec(i * juice.dealStaggerMs), duration: sec(260), ease: 'back.out(1.6)',
      onStart: () => sfx('deal', { combo: i, volume: 0.5 }),
    });
    gsap.to(card.scale, { x: 1, y: 1, delay: sec(i * juice.dealStaggerMs), duration: sec(260), ease: 'back.out(1.6)' });
  });
}

/** Na een gespeelde of geweigerde kaart: zelfde kaarten, nieuwe posities. Anders opnieuw delen. */
function matchHand(views) {
  const same = views.length === S.hand.length && views.every((v, i) => v.id === S.hand[i].view.id);
  if (!same) { dealHand(views); return; }
  S.hand.forEach((card, i) => { card.view = views[i]; card.alpha = views[i].playable ? 1 : 0.55; });
  layoutHand();
}

function onCardHover(card, over) {
  if (inputLocked() || S.drag) return;
  gsap.to(card, { y: card.home.y - (over ? 26 : 0), rotation: over ? 0 : card.home.rotation, duration: sec(120), ease: 'power2.out' });
  gsap.to(card.scale, { x: over ? 1.1 : 1, y: over ? 1.1 : 1, duration: sec(120), ease: 'power2.out' });
  card.zIndex = over ? 100 : card.handIndex;
  if (over) sfx('tick', { volume: 0.15, rate: 0.8 + card.handIndex * 0.1 });
}

// ---------------------------------------------------------------- input

function setupInput() {
  const stage = S.app.stage;
  stage.eventMode = 'static';
  stage.hitArea = S.app.screen;
  stage.on('pointermove', onDragMove);
  stage.on('pointerup', onDragEnd);
  stage.on('pointerupoutside', onDragEnd);
  window.addEventListener('keydown', onKey);
}

function onKey(e) {
  if (e.key === 'f' || e.key === 'F') juice.speed = juice.speed < 1 ? 1 : 0.5;
  if (e.key === 'm' || e.key === 'M') toggleMute();
}

function onCardDown(card, e) {
  if (inputLocked()) return;
  const p = e.getLocalPosition(S.layers.hand);
  S.drag = { card, dx: card.x - p.x, dy: card.y - p.y, hovered: null };
  card.zIndex = 200;
  card.cursor = 'grabbing';
  gsap.killTweensOf(card);
  gsap.to(card, { rotation: 0, duration: sec(80) });
  sfx('deal', { volume: 0.4, rate: 1.3 });
}

function onDragMove(e) {
  const d = S.drag;
  if (!d) return;
  const p = e.getLocalPosition(S.layers.hand);
  const nx = p.x + d.dx;
  const ny = p.y + d.dy;
  // Kantelen in de bewegingsrichting: kleine moeite, veel gevoel
  d.card.rotation = Math.max(-0.35, Math.min(0.35, (nx - d.card.x) * 0.02));
  d.card.position.set(nx, ny);

  const hovered = targetAt(p, d.card.view.target);
  if (hovered !== d.hovered) {
    if (d.hovered) gsap.to(d.hovered.container.scale, { x: 1, y: 1, duration: sec(100) });
    if (hovered) {
      gsap.to(hovered.container.scale, { x: 1.06, y: 1.06, duration: sec(100) });
      sfx('tick', { volume: 0.3, rate: 1.2 });
    }
    d.hovered = hovered;
  }
}

async function onDragEnd(e) {
  const d = S.drag;
  if (!d) return;
  S.drag = null;
  d.card.cursor = 'grab';
  if (d.hovered) gsap.to(d.hovered.container.scale, { x: 1, y: 1, duration: sec(100) });

  const p = e.getLocalPosition(S.layers.hand);
  let target = targetAt(p, d.card.view.target);
  if (!target && d.card.view.target === 'Self' && p.y < PLAY_LINE_Y) target = playerActor();

  if (!target) {
    layoutHand();
    return;
  }

  S.busy = true;
  S.pending = { card: d.card };
  S.metrics.t0 = performance.now();
  try {
    await S.dotnet.invokeMethodAsync('OnCardPlayed', d.card.handIndex, target.id);
  } catch (err) {
    console.error(err);
    layoutHand();
  } finally {
    S.pending = null;
    S.busy = false;
  }
}

/** Zolang de wachtrij speelt, is input geblokkeerd. */
function inputLocked() {
  return S.busy || S.metrics.playing;
}

function targetAt(p, mode) {
  for (const a of S.actors.values()) {
    if (!a.sprite.container.visible) continue;
    if (mode === 'Enemy' && !a.isEnemy) continue;
    if (mode === 'Self' && a.isEnemy) continue;
    const local = a.container.toLocal(p, S.layers.hand);
    const hw = a.sprite.w / 2 + 20;
    if (local.x > -hw && local.x < hw && local.y > -a.sprite.h - 20 && local.y < 50) return a;
  }
  return null;
}

function playerActor() {
  for (const a of S.actors.values()) if (!a.isEnemy) return a;
  return null;
}
