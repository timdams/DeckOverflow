// ui.js - de geluiden van de shell: knoppen, de afdelingen op de plattegrond, de Codex en het ✗-register.
// Luistert op het hele document, zodat geen enkel Razor-component er iets voor hoeft te doen.
// Wil een knop een ander geluid, dan zet hij data-sfx="naam" (of data-sfx="none" voor stilte).

import { initAudio, sfx } from './audio.js';

const BOOKS = '.codex-book, .xregister';
const HOVER = '.department, .play-big';

initAudio();

document.addEventListener('click', (e) => {
  const el = e.target.closest?.('button, a[href], [role="button"]');
  if (!el || el.disabled) return;
  const named = el.closest('[data-sfx]')?.dataset.sfx;
  if (named === 'none') return;
  // In de Codex of het ✗-register blader je: dat klinkt als papier
  sfx(named ?? (el.closest(BOOKS) ? 'uiBook' : 'uiClick'), { volume: 0.5 });
}, true);

let hovered = null;
document.addEventListener('pointerover', (e) => {
  if (e.pointerType !== 'mouse') return;
  const el = e.target.closest?.(HOVER);
  if (el === hovered) return;
  hovered = el;
  if (el && !el.disabled) sfx('uiHover', { volume: 0.25 });
});

// Een boek dat opengaat
new MutationObserver((records) => {
  for (const r of records) {
    for (const n of r.addedNodes) {
      if (n.nodeType === 1 && (n.matches(BOOKS) || n.querySelector(BOOKS))) return sfx('uiBook', { volume: 0.6 });
    }
  }
}).observe(document.body, { childList: true, subtree: true });
