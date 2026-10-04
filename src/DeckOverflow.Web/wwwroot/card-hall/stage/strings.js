// strings.js - spelteksten uit text/<taal>.json, met dezelfde regels als Strings.cs in de shell:
// {amount} is de waarde zelf, {card:from} de naam van de kaart met die id, idem relic: en enemy:.
// en.json ligt eronder, zodat een tekst die nog niet vertaald is in het Engels verschijnt.
// Een ontbrekende sleutel toont zichzelf, zodat je hem meteen ziet staan.

let texts = {};

const fetchTexts = (lang) => fetch(`text/${lang}.json`).then((r) => (r.ok ? r.json() : {})).catch(() => ({}));

/** De taal komt van de shell (index.html), die ze ook bewaart. */
export async function loadStrings(lang = window.deckOverflow?.lang?.() ?? 'en') {
  const base = await fetchTexts('en');
  texts = lang === 'en' ? base : { ...base, ...(await fetchTexts(lang)) };
}

const raw = (key) => texts[key] ?? key;

export const cardName = (id) => raw(`card.${id.replace(/\+$/, '')}`) + (id.endsWith('+') ? '+' : '');
export const relicName = (id) => raw(`relic.${id}`);
export const enemyName = (key) => raw(`enemy.${key}`);

/** t('ui.floor', { floor: 3 }) of t({ key, args }) zoals de motor een TextRef stuurt. */
export function t(keyOrRef, args = {}) {
  const key = typeof keyOrRef === 'string' ? keyOrRef : keyOrRef.key;
  const values = typeof keyOrRef === 'string' ? args : (keyOrRef.args ?? {});
  return raw(key).replace(/\{(?:(card|relic|enemy):)?([a-zA-Z]+)\}/g, (match, kind, name) => {
    if (!(name in values)) return match;
    const value = String(values[name]);
    if (kind === 'card') return cardName(value);
    if (kind === 'relic') return relicName(value);
    if (kind === 'enemy') return enemyName(value);
    return value;
  });
}
