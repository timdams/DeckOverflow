// audio.js - de geluiden van het hele spel, gedeeld door de stages en de shell (tools/copy_sfx.py).
// Elke logische naam heeft een of meer varianten; sfx() kiest er telkens een, nooit twee keer na elkaar dezelfde.
// Elk bestand bestaat als .ogg en .mp3: Howler neemt wat de browser kan afspelen.

const BASE = new URL('./', import.meta.url).href;

let sounds = null;          // naam -> [{ howl, gain }]
let loading = null;
const last = new Map();     // naam -> index van de vorige variant

// Geluid aan of uit is een keuze van dit toestel, over runs en afdelingen heen. Opslag kan falen (privévenster): dan staat het aan.
const MUTED_KEY = 'deckoverflow.muted';
let muted = (() => { try { return localStorage.getItem(MUTED_KEY) === '1'; } catch { return false; } })();

/** Laadt alle geluiden één keer; elke volgende oproep wacht op dezelfde lading. Faalt het, dan speelt het spel stil verder. */
export function initAudio() {
  loading ??= load();
  return loading;
}

async function load() {
  try {
    const manifest = await (await fetch(`${BASE}sfx.json`)).json();
    Howler.volume(0.7);
    Howler.mute(muted);
    const waits = [];
    sounds = {};
    for (const [name, variants] of Object.entries(manifest)) {
      sounds[name] = variants.map(({ file, gain }) => {
        const howl = new Howl({ src: [`${BASE}sfx/${file}.ogg`, `${BASE}sfx/${file}.mp3`], volume: 1 });
        waits.push(new Promise((resolve) => {
          if (howl.state() === 'loaded') return resolve();
          howl.once('load', resolve);
          howl.once('loaderror', resolve);
        }));
        return { howl, gain };
      });
    }
    await Promise.all(waits);
  } catch (err) {
    console.warn('Audio niet geladen, het spel speelt stil verder.', err);
    sounds = null;
  }
}

export function sfx(name, { rate = 1, volume = 0.8 } = {}) {
  const variants = sounds?.[name];
  if (!variants || muted) return;
  let i = Math.floor(Math.random() * variants.length);
  if (variants.length > 1 && i === last.get(name)) i = (i + 1) % variants.length;
  last.set(name, i);
  const { howl, gain } = variants[i];
  const id = howl.play();
  howl.rate(Math.min(2.5, rate), id);
  howl.volume(Math.min(1, volume * gain), id);
}

export function toggleMute() {
  return setMuted(!muted);
}

export function setMuted(value) {
  muted = value;
  if (typeof Howler !== 'undefined') Howler.mute(muted);
  try { localStorage.setItem(MUTED_KEY, muted ? '1' : '0'); } catch { }
  return muted;
}

export const isMuted = () => muted;
