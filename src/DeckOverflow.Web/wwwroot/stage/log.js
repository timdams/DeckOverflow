// log.js - gevechtslog: één regel per gebeurtenis, zodat speler en tester kunnen nalezen
// wat een kaart of aanval deed. Kent geen regels, beschrijft alleen events.
// Zwart op papier; alleen een omgieten krijgt de kleur van zijn type.

import { num, typeOf, COLORS, FONT } from './juice.js';
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
    style: { fontFamily: FONT, fontSize: 11, fill: color, wordWrap: true, wordWrapWidth: L.width, lineHeight: 13 },
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
      return [t('stage.log.played', { card: cardName(e.cardId), target: who(e.targetId) }), COLORS.ink];
    case 'AttackLaunched':
      return [t('stage.log.attack', { source: who(e.sourceId), expression: e.expression, value: num(e.value) }), COLORS.ink];
    case 'IntentAssigned':
      return [t('stage.log.assigned', { enemy: who(e.enemyId), before: e.expressionBefore, value: num(e.value) }), COLORS.ink];
    case 'ModifierQueued':
      return [t('stage.log.next-card', { pending: e.pending }), COLORS.ink];
    case 'SolidFlipped':
      return [t('stage.log.solid', { target: who(e.targetId), value: e.solid ? 'true' : 'false' }), COLORS.ink];
    case 'HitPassedThrough':
      return [t('stage.log.passed-through', { target: who(e.targetId) }), COLORS.ink];
    case 'HitBounced':
      return [t('stage.log.bounced', { target: who(e.targetId), expression: e.expression, value: e.value }), COLORS.ink];
    case 'ModifiersScrapped':
      return [t('stage.log.scrapped', { pending: e.pending }), COLORS.ink];
    case 'ModifiersApplied':
      return [`  ${e.expression} = ${num(e.after)}`, COLORS.ink];
    case 'RelicTriggered':
      return [`* ${relicName(e.relicId)}`, COLORS.ink];
    case 'DamageDealt':
      return [t('stage.log.damage', { target: who(e.targetId), amount: num(e.amount), before: num(e.hpBefore), after: num(e.hpAfter) }), COLORS.ink];
    case 'BlockAbsorbed':
      return [t('stage.log.absorbed', { target: who(e.targetId), amount: num(e.absorbed), remaining: num(e.remaining) }), COLORS.ink];
    case 'ValueTruncated': {
      const key = { Damage: 'stage.log.truncated.damage', Block: 'stage.log.truncated.block', Hp: 'stage.log.truncated.hp' }[e.subject];
      return key ? [t(key, { target: who(e.targetId), before: num(e.before), after: e.after }), COLORS.ink] : null;
    }
    case 'BlockGained':
      return [t('stage.log.block', { target: who(e.targetId), amount: num(e.amount) }), COLORS.ink];
    case 'BlockExpired':
      return [t('stage.log.block-expired', { target: who(e.targetId), amount: num(e.amount) }), COLORS.muted];
    case 'Healed':
      return [t('stage.log.healed', { target: who(e.targetId), amount: num(e.amount) }), COLORS.ink];
    case 'ValueOverflowed':
      return [t('stage.log.overflow', { target: who(e.targetId), before: e.before, added: e.added, after: e.after }), COLORS.ink];
    case 'TypeChanged':
      return [t('stage.log.cast', { target: who(e.targetId), from: e.from.toLowerCase(), to: e.to.toLowerCase(), before: num(e.hpBefore), after: num(e.hpAfter) }), typeOf(e.to).color];
    case 'ValueRounded':
      return [t('stage.log.rounded', { target: who(e.targetId), before: num(e.before), after: num(e.after) }), COLORS.ink];
    case 'ValueGrew':
      return [t('stage.log.grew', { target: who(e.targetId), before: num(e.before), factor: num(e.factor), raw: num(e.raw), after: num(e.after) }), COLORS.ink];
    case 'ConversionCrashed':
      return [t('stage.log.crashed', { target: who(e.targetId), value: num(e.value) }), COLORS.ink];
    case 'TextAppended':
      return [t('stage.log.appended', { target: who(e.targetId), before: e.before, added: e.added, after: e.after }), COLORS.ink];
    case 'TextParsed':
      return [t('stage.log.parsed', { target: who(e.targetId), method: e.method, text: e.text, value: num(e.value) }), COLORS.ink];
    case 'TextCounted':
      return [t('stage.log.text-counted', { target: who(e.targetId), text: e.text, length: e.length }), COLORS.ink];
    case 'TextReset':
      return [t('stage.log.text-reset', { target: who(e.targetId), text: e.text }), COLORS.ink];
    case 'TextCrashed':
      return [t('stage.log.text-crashed', { target: who(e.targetId), length: e.length }), COLORS.ink];
    case 'ExceptionThrown':
      return [t('stage.log.exception', { exception: e.exception, expression: e.expression }), COLORS.ink];
    case 'HealingStopped':
      return [t('stage.log.healing-stopped', { target: who(e.targetId) }), COLORS.muted];
    case 'AttackSkipped':
      return [t('stage.log.skipped', { source: who(e.enemyId) }), COLORS.muted];
    case 'CombatantDied':
      return [t(S.actor(e.targetId)?.isEnemy ? 'stage.log.enemy-died' : 'stage.log.player-died', { target: who(e.targetId) }), COLORS.ink];
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
