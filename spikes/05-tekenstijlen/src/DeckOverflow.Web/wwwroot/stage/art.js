// art.js - plaatjes voor kaarten en relics, in de gekozen tekenstijl.
// De koppeling id -> bestand staat in art/art.json; de shell leest hetzelfde bestand.

const STORE_KEY = 'deckoverflow.art-style';

const A = {
  manifest: { styles: [], pixelated: [], cards: {}, relics: {}, nodes: {} },
  style: null,
  textures: new Map(),   // url -> PIXI.Texture
};

/** Leest de manifest en de laatst gekozen stijl, en laadt die alvast. */
export async function loadArt() {
  try {
    A.manifest = await (await fetch('art/art.json')).json();
  } catch {
    return;   // geen art: de kaarten tonen gewoon geen plaatje
  }
  A.style = storedStyle() ?? A.manifest.styles[0];
  await preload(A.style);
}

export function currentStyle() {
  return A.style;
}

/** Wisselt van stijl en wacht tot alle plaatjes van die stijl geladen zijn. */
export async function useStyle(style) {
  if (!A.manifest.styles.includes(style)) return false;
  A.style = style;
  try { localStorage.setItem(STORE_KEY, style); } catch { /* privévenster: dan maar niet onthouden */ }
  await preload(style);
  return true;
}

/** Texture voor een kaart ("strike+" deelt het plaatje van "strike"), of null. */
export function cardTexture(id) {
  return texture(A.manifest.cards[id.replace(/\+$/, '')]);
}

export function relicTexture(id) {
  return texture(A.manifest.relics[id]);
}

function texture(file) {
  if (!file || !A.style) return null;
  return A.textures.get(url(A.style, file)) ?? null;
}

function url(style, file) {
  return `art/${style}/${file}.png`;
}

async function preload(style) {
  const files = new Set([...Object.values(A.manifest.cards), ...Object.values(A.manifest.relics)]);
  const pixelated = A.manifest.pixelated.includes(style);
  await Promise.all([...files].map(async (file) => {
    const u = url(style, file);
    if (A.textures.has(u)) return;
    try {
      const tex = await PIXI.Assets.load(u);
      // Pixelart scherp opschalen, de andere stijlen glad
      tex.source.scaleMode = pixelated ? 'nearest' : 'linear';
      A.textures.set(u, tex);
    } catch {
      console.warn(`Art ontbreekt: ${u}`);
    }
  }));
}

function storedStyle() {
  try {
    const s = localStorage.getItem(STORE_KEY);
    return A.manifest.styles.includes(s) ? s : null;
  } catch {
    return null;
  }
}
