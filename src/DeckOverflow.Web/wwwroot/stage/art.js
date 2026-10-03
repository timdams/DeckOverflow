// art.js - de tekeningen van de handleiding. De koppeling id -> bestand staat in
// art/art.json; de shell leest hetzelfde bestand. Iconen hebben een vaste naam.

const ICONS = [
  'fight', 'intent-attack', 'intent-block', 'intent-heal', 'intent-buff', 'intent-unknown',
  'hp', 'gold', 'draw', 'discard', 'energy', 'victory', 'crash', 'truncate', 'overflow', 'end-turn',
];

const A = {
  manifest: { cards: {}, relics: {}, actors: {} },
  textures: new Map(),   // pad -> PIXI.Texture
};

/** Laadt alles in één keer: het zijn er een vijftigtal en ze zijn klein. */
export async function loadArt() {
  try {
    A.manifest = await (await fetch('art/art.json')).json();
  } catch {
    return;   // geen art: de stage tekent dan omtrekken
  }
  const files = new Set([
    ...Object.values(A.manifest.cards),
    ...Object.values(A.manifest.relics),
    ...Object.values(A.manifest.actors),
    ...ICONS.map((n) => `icons/${n}`),
  ]);
  await Promise.all([...files].map(async (file) => {
    try {
      A.textures.set(file, await PIXI.Assets.load(`art/${file}.png`));
    } catch {
      console.warn(`Art ontbreekt: ${file}`);
    }
  }));
}

/** Texture voor een kaart ("strike+" deelt het plaatje van "strike"), of null. */
export const cardTexture = (id) => get(A.manifest.cards[id.replace(/\+$/, '')]);
export const relicTexture = (id) => get(A.manifest.relics[id]);
export const actorTexture = (key) => get(A.manifest.actors[key]);
export const iconTexture = (name) => get(`icons/${name}`);

function get(file) {
  return file ? A.textures.get(file) ?? null : null;
}
