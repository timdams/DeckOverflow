// log.js - gevechtslog: één regel per gebeurtenis, zodat speler en tester kunnen nalezen
// wat een kaart of aanval deed. Kent geen regels, beschrijft alleen events.

import { num, typeOf, COLORS, FONT_BODY } from './juice.js';
import { t, cardName, relicName, enemyName } from './strings.js';

const LINE_GAP = 1;

const L = {
  panel: null,
  width: 0,
  maxHeight: 0,
  lines: [],
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
  const who = (id) => shortName(enemyName(S.actor(id)?.data?.key ?? '?'));
  switch (e.type) {
    case 'CardPlayed':
      return [t('stage.log.played', { card: cardName(e.cardId), target: who(e.targetId) }), COLORS.white];
    case 'AttackLaunched':
      return [t('stage.log.attack', { source: who(e.sourceId), expression: e.expression, value: num(e.value) }), COLORS.orangeLight];
    case 'IntentAssigned':
      return [t('stage.log.assigned', { enemy: who(e.enemyId), before: e.expressionBefore, value: num(e.value) }), COLORS.cyan];
    case 'ModifierQueued':
      return [t('stage.log.next-card', { pending: e.pending }), COLORS.orangeLight];
    case 'ModifiersApplied':
      return [`  ${e.expression} = ${num(e.after)}`, COLORS.orangeLight];
    case 'RelicTriggered':
      return [`* ${relicName(e.relicId)}`, COLORS.orangeLight];
    case 'DamageDealt':
      return [t('stage.log.damage', { target: who(e.targetId), amount: num(e.amount), before: num(e.hpBefore), after: num(e.hpAfter) }), COLORS.red];
    case 'BlockAbsorbed':
      return [t('stage.log.absorbed', { target: who(e.targetId), amount: num(e.absorbed), remaining: num(e.remaining) }), COLORS.cyan];
    case 'ValueTruncated': {
      const key = { Damage: 'stage.log.truncated.damage', Block: 'stage.log.truncated.block', Hp: 'stage.log.truncated.hp' }[e.subject];
      return key ? [t(key, { target: who(e.targetId), before: num(e.before), after: e.after }), COLORS.orangeLight] : null;
    }
    case 'BlockGained':
      return [t('stage.log.block', { target: who(e.targetId), amount: num(e.amount) }), COLORS.cyan];
    case 'BlockExpired':
      return [t('stage.log.block-expired', { target: who(e.targetId), amount: num(e.amount) }), COLORS.muted];
    case 'Healed':
      return [t('stage.log.healed', { target: who(e.targetId), amount: num(e.amount) }), COLORS.green];
    case 'ValueOverflowed':
      return [t('stage.log.overflow', { target: who(e.targetId), before: e.before, added: e.added, after: e.after }), COLORS.orange];
    case 'TypeChanged':
      return [t('stage.log.cast', { target: who(e.targetId), from: e.from.toLowerCase(), to: e.to.toLowerCase(), before: num(e.hpBefore), after: num(e.hpAfter) }), typeOf(e.to).color];
    case 'CombatantDied':
      return [t(S.actor(e.targetId)?.isEnemy ? 'stage.log.enemy-died' : 'stage.log.player-died', { target: who(e.targetId) }), COLORS.white];
    case 'TurnStarted':
      return [t('stage.log.turn', { turn: e.turn }), COLORS.muted];
    case 'PlayRejected':
      return [`x ${t(e.reason)}`, COLORS.muted];
    default:
      return null;
  }
}

/** "Floating Ghost" wordt "Ghost": genoeg om te herkennen, kort genoeg voor het paneel. */
function shortName(name) {
  if (!name) return '?';
  const parts = name.split(' ');
  return parts[parts.length - 1];
}
