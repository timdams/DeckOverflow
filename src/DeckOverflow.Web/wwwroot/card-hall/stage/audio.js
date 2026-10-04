// audio.js - Howler met een sprite van placeholder-geluiden (tools/make_sfx.py).
// Toonhoogte via rate: elke opeenvolgende trigger in een combo klinkt hoger.

import { juice } from './juice.js';

let howl = null;
// Geluid aan of uit is een keuze van dit toestel, over runs heen. Opslag kan falen (privévenster): dan staat het aan.
const MUTED_KEY = 'deckoverflow.muted';
let muted = (() => { try { return localStorage.getItem(MUTED_KEY) === '1'; } catch { return false; } })();

export async function initAudio(base = './audio/') {
  try {
    const sprite = await (await fetch(`${base}sfx.json`)).json();
    howl = new Howl({ src: [`${base}sfx.wav`], sprite, volume: 1 });
    Howler.volume(0.7);
    Howler.mute(muted);
    await new Promise((resolve) => {
      if (howl.state() === 'loaded') return resolve();
      howl.once('load', resolve);
      howl.once('loaderror', resolve);
    });
  } catch (err) {
    console.warn('Audio niet geladen, het spel speelt stil verder.', err);
    howl = null;
  }
}

export function sfx(name, { combo = 0, rate = 1, volume = 0.8 } = {}) {
  if (!howl || muted) return;
  const id = howl.play(name);
  howl.rate(Math.min(2.5, rate * (1 + combo * juice.comboPitchStep)), id);
  howl.volume(volume, id);
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
