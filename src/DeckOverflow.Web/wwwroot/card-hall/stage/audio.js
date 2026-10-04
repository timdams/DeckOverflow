// audio.js - de geluiden van de Card Hall, bovenop de gedeelde audio (wwwroot/audio/audio.js).
// Toonhoogte via rate: elke opeenvolgende trigger in een combo klinkt hoger.

import { juice } from '../../shared/juice.js';
import { sfx as play } from '../../audio/audio.js';

export { initAudio, toggleMute, setMuted, isMuted } from '../../audio/audio.js';

export function sfx(name, { combo = 0, rate = 1, volume = 0.8 } = {}) {
  play(name, { rate: rate * (1 + combo * juice.comboPitchStep), volume });
}
