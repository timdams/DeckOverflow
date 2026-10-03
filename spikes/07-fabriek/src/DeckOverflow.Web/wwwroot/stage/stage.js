// stage.js - init, scene graph, hand en input. Kent geen spelregels:
// speelt events af (timeline.js) en neemt de snapshot als waarheid (sync).
// Spike 6: alles is een bladzijde uit een montagehandleiding.

import { juice, sec, wait, roll, text, burst, num, typeOf, typeBadge, circled, stamp, dashed, COLORS, FONT } from './juice.js';
import { initAudio, sfx, toggleMute } from './audio.js';
import { runQueue } from './timeline.js';
import { buildLog, clearLog } from './log.js';
import { loadStrings, t, cardName, relicName } from './strings.js';
import { loadArt, cardTexture, relicTexture, actorTexture, iconTexture } from './art.js';

const W = 960;
const H = 540;
const GROUND_Y = 335;
const HAND_Y = 458;
const CARD_W = 108;
const CARD_H = 150;
const ART_SIZE = 58;      // plaatje op een kaart
const ICON_SIZE = 26;
const PLAY_LINE_Y = 380;   // kaart boven deze lijn loslaten = spelen (voor Self-kaarten)
const POSITIONS = { player: { x: 255, y: GROUND_Y }, enemy: { x: 690, y: GROUND_Y } };

// Hoe groot elke figuur op de bladzijde staat, en of hij zweeft. De motor geeft alleen een sleutel.
const LOOKS = {
  player: { h: 150 },   // spike 7: golem = Level 256 (speelhalkast), colossus = Flight 501, reckoner = de inspecteur
  slime: { h: 100 },
  knight: { h: 175 },
  ghost: { h: 165, float: true },
  dripper: { h: 175, float: true },
  jug: { h: 165 },
  colossus: { h: 215 },
  golem: { h: 175 },
  reckoner: { h: 215 },
};

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

  await Promise.all([loadStrings(), loadArt(), initAudio()]).catch(() => {});

  const app = new PIXI.Application();
  await app.init({
    resizeTo: host,
    background: COLORS.paper,
    antialias: true,
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
  S.layers.top = new PIXI.Container();   // niet geschud: banners
  S.root.addChild(S.layers.top);

  buildBackground();
  buildUi();
  buildHud();
  // Gevechtslog links, onder de HUD en naast de speler
  buildLog(S.layers.ui, 22, 84, 172, 220);
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

  S.setModifiers(snapshot.modifiers);
  S.relics = snapshot.relics;
  drawRelics();
  S.ui.setEnergy(snapshot.energy, snapshot.maxEnergy);
  S.ui.draw.text = String(snapshot.drawPile);
  S.ui.discard.text = String(snapshot.discardPile);

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
  clearLog();
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

/** Wat op je volgende kaart wacht, boven de batterij. */
S.setModifiers = (labels) => {
  S.ui.modifiers.text = labels && labels.length ? `${t('stage.next-card')}\n${labels.join(' ')}` : '';
};

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

/** Al de rest vervaagt tot een schets, de hoofdrolspeler blijft scherp. */
S.dimExcept = (actor) => {
  const others = [S.layers.bg, S.layers.hand, S.layers.ui, ...[...S.actors.values()].filter((a) => a !== actor).map((a) => a.container)];
  gsap.to(others, { alpha: 0.2, duration: sec(150) });
  return { restore: () => gsap.to(others, { alpha: 1, duration: sec(300) }) };
};

/** Geen witte flits op wit papier: het blad knippert even naar inkt. */
S.flashScreen = (strength = 0.8) => {
  const f = new PIXI.Graphics().rect(0, 0, W, H).fill(COLORS.ink);
  f.alpha = strength * 0.25;
  S.layers.top.addChild(f);
  gsap.to(f, { alpha: 0, duration: sec(200), ease: 'power2.out', onComplete: () => f.destroy() });
};

/** Een nieuwe beurt is een nieuwe stap in de handleiding: een groot omcirkeld cijfer. */
S.announce = async (turn) => {
  const group = new PIXI.Container();
  const step = circled(turn, { radius: 44, size: 46 });
  const label = text(t('stage.turn-label'), { size: 16, color: COLORS.muted });
  label.position.set(0, 64);
  group.addChild(step, label);
  group.position.set(W / 2, H / 2 - 60);
  group.scale.set(0.3);
  group.alpha = 0;
  S.layers.top.addChild(group);
  await gsap.timeline({ onComplete: () => group.destroy({ children: true }) })
    .to(group, { alpha: 1, duration: sec(100) })
    .to(group.scale, { x: 1, y: 1, duration: sec(220), ease: 'back.out(2.5)' }, 0)
    .to(group, { alpha: 0, y: group.y - 20, duration: sec(200) }, `+=${sec(350)}`);
};

/** Einde van het gevecht: een paneel met het figuurtje dat juicht of de klantendienst belt. */
S.banner = async (label, won) => {
  const panel = new PIXI.Container();
  const w = 360;
  const h = 210;
  panel.addChild(new PIXI.Graphics().rect(-w / 2, -h / 2, w, h).fill(COLORS.white).stroke({ width: 3, color: COLORS.ink }));
  const tex = iconTexture(won ? 'victory' : 'crash');
  if (tex) {
    const img = new PIXI.Sprite(tex);
    img.anchor.set(0.5);
    img.width = img.height = 120;
    img.position.set(0, -22);
    panel.addChild(img);
  }
  const title = text(label, { size: 30 });
  title.position.set(0, 72);
  panel.addChild(title);
  panel.position.set(W / 2, H / 2 - 50);
  panel.scale.set(1, 0);
  S.layers.top.addChild(panel);
  await gsap.to(panel.scale, { y: 1, duration: sec(300), ease: 'back.out(2)' });
  stamp(S.layers.top, W / 2 + w / 2 - 10, H / 2 - 50 - h / 2 + 10, won ? 'check' : 'cross', { radius: 26 });
};

// ---------------------------------------------------------------- scene

function buildBackground() {
  const bg = S.layers.bg;

  // Bladrand en snijtekens, zoals een gedrukte pagina
  const page = new PIXI.Graphics().rect(10, 10, W - 20, H - 20).stroke({ width: 1, color: COLORS.rule });
  for (const [x, y, dx, dy] of [[10, 10, 1, 1], [W - 10, 10, -1, 1], [10, H - 10, 1, -1], [W - 10, H - 10, -1, -1]]) {
    page.moveTo(x - dx * 8, y).lineTo(x + dx * 14, y).moveTo(x, y - dy * 8).lineTo(x, y + dy * 14);
  }
  page.stroke({ width: 1.5, color: COLORS.muted });
  bg.addChild(page);

  // De vloer: één lijn met arcering eronder, zoals in een technische tekening
  const floor = new PIXI.Graphics().moveTo(150, GROUND_Y + 6).lineTo(W - 120, GROUND_Y + 6).stroke({ width: 2, color: COLORS.ink });
  for (let x = 156; x < W - 120; x += 12) floor.moveTo(x, GROUND_Y + 7).lineTo(x - 8, GROUND_Y + 15);
  floor.stroke({ width: 1, color: COLORS.muted });
  bg.addChild(floor);

  // Een pijl van de held naar de vijand: zo hoort het te gaan
  const guide = new PIXI.Graphics();
  dashed(guide, POSITIONS.player.x + 90, GROUND_Y - 200, POSITIONS.enemy.x - 120, GROUND_Y - 200, { color: COLORS.rule, width: 2, dash: 10, gap: 8 });
  guide.poly([POSITIONS.enemy.x - 120, GROUND_Y - 206, POSITIONS.enemy.x - 108, GROUND_Y - 200, POSITIONS.enemy.x - 120, GROUND_Y - 194]).fill(COLORS.rule);
  bg.addChild(guide);
}

function iconSprite(name, size = ICON_SIZE) {
  const tex = iconTexture(name);
  const img = tex ? new PIXI.Sprite(tex) : new PIXI.Container();
  if (tex) {
    img.anchor.set(0.5);
    img.width = img.height = size;
  }
  return img;
}

function buildUi() {
  const ui = S.layers.ui;

  // Energie is een batterij: één cel per energie
  const battery = new PIXI.Container();
  const cells = new PIXI.Graphics();
  const energy = text('3/3', { size: 15 });
  energy.position.set(0, 42);
  battery.addChild(cells, energy);
  battery.position.set(70, HAND_Y - 6);
  ui.addChild(battery);

  const setEnergy = (value, max) => {
    const cw = 14;
    const ch = 22;
    const gap = 3;
    const w = max * cw + (max + 1) * gap;
    cells.clear()
      .roundRect(-w / 2, -ch / 2 - gap, w, ch + gap * 2, 3).fill(COLORS.white).stroke({ width: juice.line, color: COLORS.ink })
      .rect(w / 2, -6, 5, 12).fill(COLORS.ink);
    for (let i = 0; i < max; i++) {
      const x = -w / 2 + gap + i * (cw + gap);
      cells.rect(x, -ch / 2, cw, ch);
      if (i < value) cells.fill(COLORS.ink);
      else cells.stroke({ width: 1, color: COLORS.muted });
    }
    energy.text = `${value}/${max}`;
  };

  // Stapels rechtsboven: trekstapel en aflegbak, elk met een getal
  const pile = (name, x) => {
    const c = new PIXI.Container();
    const img = iconSprite(name, 34);
    const n = text('0', { size: 15, anchor: 0 });
    n.anchor.set(0, 0.5);
    n.position.set(22, 0);
    c.addChild(img, n);
    c.position.set(x, 32);
    ui.addChild(c);
    return n;
  };
  const draw = pile('draw', W - 150);
  const discard = pile('discard', W - 80);

  // Relics onder de stapels, met plaatje en de stand van relics die tellen
  const relics = new PIXI.Container();
  relics.position.set(W - 24, 64);
  ui.addChild(relics);

  const modifiers = text('', { size: 14 });
  modifiers.anchor.set(0.5, 1);
  modifiers.position.set(70, HAND_Y - 40);
  ui.addChild(modifiers);

  S.ui = { battery, energy, setEnergy, draw, discard, relics, modifiers };
}

function drawRelics() {
  const box = S.ui.relics;
  box.removeChildren().forEach((c) => c.destroy({ children: true }));
  (S.relics ?? []).forEach((r, i) => {
    const label = text(r.counter ? `${relicName(r.id)} ${r.counter}` : relicName(r.id), { size: 12, bold: false });
    label.anchor.set(1, 0.5);
    const y = i * (ICON_SIZE + 6) + ICON_SIZE / 2;
    const tex = relicTexture(r.id);
    if (tex) {
      const img = new PIXI.Sprite(tex);
      img.anchor.set(1, 0.5);
      img.width = img.height = ICON_SIZE;
      img.position.set(0, y);
      box.addChild(img);
    }
    label.position.set(tex ? -ICON_SIZE - 6 : 0, y);
    box.addChild(label);
  });
}

function buildHud() {
  const hud = new PIXI.Text({ text: '', style: { fontFamily: FONT, fontSize: 10, fill: COLORS.muted } });
  hud.position.set(22, 18);
  S.root.addChild(hud);
  S.hud = hud;
  S.hudTimer = 0;
}

function onTick(ticker) {
  fit();

  // Meetpunten voor de succescriteria
  const instant = 1000 / Math.max(1, ticker.deltaMS);
  if (S.metrics.playing) S.metrics.minFps = Math.min(S.metrics.minFps ?? instant, instant);
  S.hudTimer += ticker.deltaMS;
  if (S.hudTimer > 250) {
    S.hudTimer = 0;
    const m = S.metrics;
    const f = (v, unit = 'ms') => (v === null || v === undefined ? '-' : `${v.toFixed(v < 10 ? 1 : 0)} ${unit}`);
    S.hud.text = [
      `fps ${ticker.FPS.toFixed(0)}  min during last animation ${m.minFps === null ? '-' : m.minFps.toFixed(0)}`,
      `click→frame ${f(m.reactMs)}  engine ${f(m.engineMs)}  playable after ${f(m.loadMs)}`,
      t('stage.fast', { state: t(juice.speed < 1 ? 'stage.on' : 'stage.off') }),
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

/** Een lichte tint van een typekleur: alsof iemand het figuurtje met een markeerstift inkleurt. */
function marker(color) {
  const mix = (c) => Math.round(c + (255 - c) * 0.45);
  return (mix((color >> 16) & 0xff) << 16) | (mix((color >> 8) & 0xff) << 8) | mix(color & 0xff);
}

function createActor(c) {
  const isEnemy = c.isEnemy;
  const home = isEnemy ? POSITIONS.enemy : POSITIONS.player;
  const look = LOOKS[c.key] ?? (isEnemy ? LOOKS.golem : LOOKS.player);
  const tex = actorTexture(c.key) ?? actorTexture(isEnemy ? 'golem' : 'player');

  // De tekening staat met haar voeten op de vloer
  const img = new PIXI.Sprite(tex);
  img.anchor.set(0.5, 1);
  const scale = look.h / tex.height;
  img.scale.set(scale);
  const body = new PIXI.Container();   // wordt geplet, gekanteld en geschud
  body.addChild(img);
  const sprite = { container: body, img, w: tex.width * scale, h: look.h };

  const container = new PIXI.Container();
  container.position.set(home.x, home.y);
  container.addChild(body);

  // HP-balk met het C#-type ernaast: de belangrijkste hint in het hele scherm
  const barW = 150;
  const bar = new PIXI.Container();
  bar.position.set(-barW / 2, 17);
  const barBg = new PIXI.Graphics().rect(0, 0, barW, 18).fill(COLORS.white).stroke({ width: 2, color: COLORS.ink });
  // Wat er net af ging, blijft even gearceerd staan, zodat je ziet hoeveel een treffer kostte
  const barLag = new PIXI.Graphics().rect(1, 1, barW - 2, 16).fill(COLORS.shade);
  const barFill = new PIXI.Graphics().rect(1, 1, barW - 2, 16).fill(COLORS.white);   // getint in de kleur van het type
  const barLine = new PIXI.Graphics().rect(0, 0, barW, 18).stroke({ width: 2, color: COLORS.ink });
  const hpText = text('', { size: 12, halo: true });
  hpText.position.set(barW / 2, 9);
  bar.addChild(barBg, barLag, barFill, barLine, hpText);

  const badgeSlot = new PIXI.Container();
  badgeSlot.position.set(barW + 10, 9);
  bar.addChild(badgeSlot);

  // Schild links van de balk, met het getal erin
  const blockBox = new PIXI.Container();
  // Blok is een beschermplaat: vastgeschroefd, gearceerd, zoals een afschermkap in een handleiding
  const blockIcon = blockPlate(17);
  const blockText = text('', { size: 13, halo: true });
  blockText.position.set(0, 2);
  blockBox.addChild(blockIcon, blockText);
  blockBox.position.set(-barW / 2 - 26, 26);
  blockBox.visible = false;

  // Intent als tekstballon boven het hoofd, met een staartje naar de vijand
  const intent = new PIXI.Container();
  intent.position.set(0, -sprite.h - 44);
  const bubble = new PIXI.Graphics();
  // Spike 7: geen zwaarden meer. Een aanval is er gewoon op kloppen, zoals op de kaart Whack.
  const whackTex = cardTexture('strike');
  const intentIcon = whackTex ? new PIXI.Sprite(whackTex) : iconSprite('intent-attack', 30);
  if (whackTex) { intentIcon.anchor.set(0.5); intentIcon.width = intentIcon.height = 30; }
  const intentText = text('', { size: 15, anchor: 0 });
  intentText.anchor.set(0, 0.5);
  const intentValue = text('', { size: 15, color: COLORS.muted, anchor: 0 });
  intentValue.anchor.set(0, 0.5);
  intent.addChild(bubble, intentIcon, intentText, intentValue);

  const layoutBubble = () => {
    const gap = 6;
    const w = 30 + gap + intentText.width + (intentValue.text ? gap + intentValue.width : 0) + 20;
    const left = -w / 2;
    intentIcon.position.set(left + 10 + 15, 0);
    intentText.position.set(left + 10 + 30 + gap, 0);
    intentValue.position.set(intentText.x + intentText.width + gap, 0);
    bubble.clear()
      .roundRect(left, -21, w, 42, 8).fill(COLORS.white).stroke({ width: 2, color: COLORS.ink })
      .poly([-8, 20, 8, 20, 0, 33]).fill(COLORS.white)
      .moveTo(-8, 21).lineTo(0, 33).lineTo(8, 21).stroke({ width: 2, color: COLORS.ink, join: 'round' });
  };

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
    blockIcon,
    intentNode: intent,
    intentShown: false,
    hp: c.hp,
    kind: null,
    bornAs: c.kind,
    max: c.maxHp,
    badgeSlot,
    bar,

    center: () => ({ x: container.x, y: container.y - sprite.h / 2 }),
    head: () => ({ x: container.x, y: container.y - sprite.h - 10 }),

    setHp(value) {
      a.hp = value;
      hpText.text = `${num(value)}/${num(a.max)}`;
      const ratio = Math.max(0, Math.min(1, value / a.max));
      barFill.scale.x = ratio;
      if (ratio < barLag.scale.x) {
        gsap.to(barLag.scale, { x: ratio, delay: sec(450), duration: sec(450), ease: 'power2.in', overwrite: true });
      } else {
        gsap.killTweensOf(barLag.scale);
        barLag.scale.x = ratio;
      }
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
      // Omgegoten: het figuurtje is voortaan ingekleurd in zijn nieuwe type
      img.tint = kind === a.bornAs ? 0xffffff : marker(typeOf(kind).color);
    },

    /** Idle: een tekening die net niet stilstaat, of zweeft. */
    idle() {
      gsap.killTweensOf(body.scale);
      gsap.killTweensOf(body, 'y,rotation');
      body.scale.set(1);
      body.y = 0;
      body.rotation = 0;
      if (look.float) {
        gsap.to(body, { y: -8, duration: 1.4 + Math.random() * 0.3, yoyo: true, repeat: -1, ease: 'sine.inOut' });
      } else {
        gsap.to(body.scale, { y: 1.02, duration: 1.1 + Math.random() * 0.3, yoyo: true, repeat: -1, ease: 'sine.inOut' });
      }
    },

    setIntent(intentData) {
      intent.visible = !!intentData;
      a.intentShown = !!intentData;
      if (intentData) {
        intentText.text = intentData.expression;
        intentValue.text = intentLabel(intentData.value);
        layoutBubble();
      }
    },

    async typeIntent(expression, value) {
      a.intentShown = true;
      intent.visible = true;
      intentValue.text = '';
      for (let i = 1; i <= expression.length; i++) {
        intentText.text = expression.slice(0, i);
        layoutBubble();
        if (expression[i - 1] !== ' ') sfx('tick', { volume: 0.25, rate: 1.6 });
        await wait(35);
      }
      intentValue.text = intentLabel(value);
      layoutBubble();
      intentValue.alpha = 0;
      gsap.to(intentValue, { alpha: 1, duration: sec(200) });
    },

    /** Een treffer: wegkantelen en terugveren, als een karton dat een tik krijgt. */
    knock(dir, px) {
      gsap.fromTo(body, { x: dir * px, rotation: dir * 0.12 }, { x: 0, rotation: 0, duration: sec(320), ease: 'elastic.out(1, 0.4)' });
    },

    die() {
      gsap.killTweensOf(body.scale);
      gsap.killTweensOf(body, 'y,rotation');
      body.visible = false;
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

/** Een vastgeschroefde plaat met arcering: het blok van wie een klap opvangt. */
function blockPlate(r) {
  const g = new PIXI.Graphics().roundRect(-r, -r, r * 2, r * 2, 4).fill(COLORS.white).stroke({ width: juice.line, color: COLORS.ink });
  // Diagonalen x + y = d, afgekapt op een vierkant iets binnen de rand
  const a = r - 4;
  for (let d = -2 * a + 6; d < 2 * a; d += 7) {
    const x1 = Math.max(-a, d - a);
    const x2 = Math.min(a, d + a);
    g.moveTo(x1, d - x1).lineTo(x2, d - x2);
  }
  g.stroke({ width: 1, color: COLORS.muted });
  for (const [x, y] of [[-r + 4, -r + 4], [r - 4, -r + 4], [-r + 4, r - 4], [r - 4, r - 4]]) {
    g.circle(x, y, 1.8).stroke({ width: 1.2, color: COLORS.ink });
  }
  return g;
}

/** De Rekenmeester verbergt zijn totaal: dan staat er een vraagteken. */
function intentLabel(value) {
  return value === null || value === undefined ? '= ?' : `= ${num(value)}`;
}

// ---------------------------------------------------------------- hand

function createCard(view) {
  const card = new PIXI.Container();
  const kind = view.kind ? typeOf(view.kind) : null;

  // Wit kaartje met een zwarte rand; een kaart met een type krijgt een binnenrand in zijn kleur
  const body = new PIXI.Graphics()
    .roundRect(-CARD_W / 2, -CARD_H / 2, CARD_W, CARD_H, 6).fill(COLORS.white).stroke({ width: juice.line, color: COLORS.ink });
  if (kind) body.roundRect(-CARD_W / 2 + 5, -CARD_H / 2 + 5, CARD_W - 10, CARD_H - 10, 4).stroke({ width: 3, color: kind.color });

  // Kosten in een stapcirkel
  const cost = circled(view.cost, { radius: 12, size: 13, fill: COLORS.ink, color: COLORS.ink });
  cost.children[1].style.fill = COLORS.white;
  cost.position.set(-CARD_W / 2 + 6, -CARD_H / 2 + 6);

  const label = cardName(view.id);
  const name = text(label, { size: label.length > 13 ? 10 : 11, wrap: CARD_W - 20 });
  name.position.set(0, -CARD_H / 2 + 22);

  const art = cardArt(view);
  art.position.set(0, -CARD_H / 2 + 66);

  // Het type van de aanval, of bij Omgieten het type waarnaar je giet
  const extras = [];
  if (view.kind) {
    const badge = typeBadge(view.kind, { size: 9 });
    badge.position.set(CARD_W / 2 - 6 - badge.badgeWidth / 2, -CARD_H / 2 + 98);
    extras.push(badge);
  }
  if (view.castTo) {
    const arrow = text('→', { size: 14 });
    const badge = typeBadge(view.castTo, { size: 10 });
    arrow.position.set(-badge.badgeWidth / 2 - 8, 0);
    badge.position.set(8, 0);
    const group = new PIXI.Container();
    group.addChild(arrow, badge);
    group.position.set(0, -CARD_H / 2 + 98);
    extras.push(group);
  }

  const desc = text(t(view.text), { size: 11, bold: false, wrap: CARD_W - 14, anchor: 0 });
  desc.anchor.set(0.5, 0);
  desc.style.lineHeight = 13;
  desc.position.set(0, -CARD_H / 2 + 110);

  card.addChild(body, art, name, ...extras, desc, cost);
  card.view = view;
  card.art = art;
  card.eventMode = 'static';
  card.cursor = 'grab';
  card.on('pointerover', () => onCardHover(card, true));
  card.on('pointerout', () => onCardHover(card, false));
  card.on('pointerdown', (e) => onCardDown(card, e));
  S.layers.hand.addChild(card);
  return card;
}

/** Het plaatje van een kaart; zonder plaatje een leeg kadertje. */
function cardArt(view) {
  const tex = cardTexture(view.id);
  if (!tex) return new PIXI.Graphics().rect(-ART_SIZE / 2, -ART_SIZE / 2, ART_SIZE, ART_SIZE).stroke({ width: 1, color: COLORS.rule });
  const img = new PIXI.Sprite(tex);
  img.anchor.set(0.5);
  img.width = img.height = ART_SIZE;
  return img;
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
  S.hand.forEach((card, i) => { card.targetAlpha = views[i].playable ? 1 : 0.5; });
  layoutHand(false);
  S.hand.forEach((card, i) => {
    const home = { ...card.home };
    card.position.set(W / 2 + 40, H + 120);   // van onder het blad, niet dwars over de vijanden
    card.rotation = 0.5;
    card.scale.set(0.25);
    card.alpha = 0;
    gsap.to(card, {
      ...home, alpha: card.targetAlpha, delay: sec(i * juice.dealStaggerMs), duration: sec(300), ease: 'back.out(1.4)',
      onStart: () => sfx('deal', { combo: i, volume: 0.5 }),
    });
    gsap.to(card.scale, { x: 1, y: 1, delay: sec(i * juice.dealStaggerMs), duration: sec(300), ease: 'back.out(1.4)' });
  });
}

/** Na een gespeelde of geweigerde kaart: zelfde kaarten, nieuwe posities. Anders opnieuw delen. */
function matchHand(views) {
  const same = views.length === S.hand.length && views.every((v, i) => v.id === S.hand[i].view.id);
  if (!same) { dealHand(views); return; }
  S.hand.forEach((card, i) => { card.view = views[i]; card.alpha = views[i].playable ? 1 : 0.5; });
  layoutHand();
}

function onCardHover(card, over) {
  if (inputLocked() || S.drag) return;
  gsap.to(card, { y: card.home.y - (over ? 26 : 0), rotation: over ? 0 : card.home.rotation, duration: sec(120), ease: 'power2.out' });
  gsap.to(card.scale, { x: over ? 1.12 : 1, y: over ? 1.12 : 1, duration: sec(120), ease: 'power2.out' });
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
