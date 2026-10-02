// log.js - gevechtslog: één regel per gebeurtenis, zodat speler en tester kunnen nalezen
// wat een kaart of aanval deed. Kent geen regels, beschrijft alleen events.

import { num, typeOf, COLORS, FONT_BODY } from './juice.js';

const LINE_GAP = 1;

const L = {
  panel: null,
  width: 0,
  maxHeight: 0,
  lines: [],
  cardNames: new Map(),
};

export function buildLog(layer, x, y, width, maxHeight) {
  L.panel = new PIXI.Container();
  L.panel.position.set(x, y);
  L.width = width;
  L.maxHeight = maxHeight;
  layer.addChild(L.panel);
}

export function clearLog() {
  L.lines = [];
  render();
}

/** Kaartnamen uit de snapshot, want CardPlayed draagt alleen een id. */
export function rememberCards(views) {
  for (const v of views) L.cardNames.set(v.id, v.name);
}

export function logEvent(S, e) {
  const line = describe(S, e);
  if (!line) return;
  L.lines.push(line);
  render();
}

function render() {
  if (!L.panel) return;
  L.panel.removeChildren().forEach((c) => c.destroy());

  // Nieuwste onderaan; oudste regels vallen weg zodra het paneel vol is
  const texts = L.lines.map(([value, color]) => new PIXI.Text({
    text: value,
    style: { fontFamily: FONT_BODY, fontSize: 16, fill: color, wordWrap: true, wordWrapWidth: L.width, lineHeight: 15 },
  }));
  let height = texts.reduce((h, t) => h + t.height + LINE_GAP, 0);
  while (height > L.maxHeight && texts.length > 1) {
    const dropped = texts.shift();
    L.lines.shift();
    height -= dropped.height + LINE_GAP;
    dropped.destroy();
  }

  let y = 0;
  texts.forEach((t, i) => {
    t.position.set(0, y);
    t.alpha = 0.45 + 0.55 * ((i + 1) / texts.length);   // oudere regels vervagen
    y += t.height + LINE_GAP;
    L.panel.addChild(t);
  });
}

function describe(S, e) {
  const who = (id) => shortName(S.actor(id)?.data?.name);
  switch (e.type) {
    case 'CardPlayed':
      return [`> ${L.cardNames.get(e.cardId) ?? e.cardId} op ${who(e.targetId)}`, COLORS.white];
    case 'AttackLaunched':
      return [`> ${who(e.sourceId)} valt aan: ${e.expression} = ${num(e.value)}`, COLORS.orangeLight];
    case 'IntentAssigned':
      return [`  ${who(e.enemyId)}: aanval ${e.expressionBefore} wordt ${num(e.value)}`, COLORS.cyan];
    case 'ModifierQueued':
      return [`  volgende kaart: ${e.pending}`, COLORS.orangeLight];
    case 'ModifiersApplied':
      return [`  ${e.expression} = ${num(e.after)}`, COLORS.orangeLight];
    case 'RelicTriggered':
      return [`* ${S.relicNames?.get(e.relicId) ?? e.relicId}`, COLORS.orangeLight];
    case 'DamageDealt':
      return [`  ${who(e.targetId)}: -${num(e.amount)} HP (${num(e.hpBefore)} → ${num(e.hpAfter)})`, COLORS.red];
    case 'BlockAbsorbed':
      return [`  ${who(e.targetId)}: schild vangt ${num(e.absorbed)} op, ${num(e.remaining)} over`, COLORS.cyan];
    case 'ValueTruncated': {
      const what = { Damage: 'schade', Block: 'schild', Hp: 'HP' }[e.subject] ?? 'waarde';
      return [`  ${who(e.targetId)}: ${what} ${num(e.before)} afgekapt tot ${e.after}`, COLORS.orangeLight];
    }
    case 'BlockGained':
      return [`${who(e.targetId)}: +${num(e.amount)} schild`, COLORS.cyan];
    case 'BlockExpired':
      return [`${who(e.targetId)}: schild (${num(e.amount)}) vervalt`, COLORS.muted];
    case 'Healed':
      return [`  ${who(e.targetId)}: +${num(e.amount)} HP`, COLORS.green];
    case 'ValueOverflowed':
      return [`  ${who(e.targetId)}: ${e.before} + ${e.added} past niet in byte → ${e.after}`, COLORS.orange];
    case 'TypeChanged':
      return [`  ${who(e.targetId)}: ${e.from.toLowerCase()} → ${e.to.toLowerCase()}, HP ${num(e.hpBefore)} → ${num(e.hpAfter)}`, typeOf(e.to).color];
    case 'CombatantDied':
      return [`${who(e.targetId)} ${S.actor(e.targetId)?.isEnemy ? 'is verslagen' : 'crasht'}`, COLORS.white];
    case 'TurnStarted':
      return [`-- beurt ${e.turn} --`, COLORS.muted];
    case 'PlayRejected':
      return [`x ${e.reason}`, COLORS.muted];
    default:
      return null;
  }
}

/** "Vlottende Geest" wordt "Geest": genoeg om te herkennen, kort genoeg voor het paneel. */
function shortName(name) {
  if (!name) return '?';
  const parts = name.split(' ');
  return parts[parts.length - 1];
}
