"""Genereert placeholder-geluiden als Howler-sprite (wwwroot/audio/sfx.wav + sprite-map).

Puur synthese, geen externe assets. Draai opnieuw na aanpassingen:
    python tools/make_sfx.py
"""
import json, wave, pathlib
import numpy as np

SR = 22050
rng = np.random.default_rng(255)

def t(dur): return np.linspace(0, dur, int(SR * dur), endpoint=False)
def env(n, attack=0.005, decay=None):
    e = np.ones(n)
    a = max(1, int(SR * attack)); e[:a] = np.linspace(0, 1, a)
    k = np.linspace(0, 1, n)
    return e * (1 - k) ** (decay if decay else 2)
def square(f, x): return np.sign(np.sin(2 * np.pi * f * x))
def sweep(f0, f1, x): return np.sin(2 * np.pi * np.cumsum(np.linspace(f0, f1, len(x))) / SR)
def noise(n): return rng.uniform(-1, 1, n)

def hit():
    x = t(0.16); return 0.7 * noise(len(x)) * env(len(x), decay=4) + 0.5 * sweep(220, 60, x) * env(len(x), decay=3)
def hit_big():
    x = t(0.35); return 0.6 * noise(len(x)) * env(len(x), decay=3) + 0.8 * sweep(160, 35, x) * env(len(x), decay=2)
def tick():
    x = t(0.04); return 0.5 * square(1200, x) * env(len(x), decay=6)
def shield():
    x = t(0.25); return 0.4 * (np.sin(2*np.pi*880*x) + 0.6*np.sin(2*np.pi*1320*x) + 0.3*np.sin(2*np.pi*2210*x)) * env(len(x), decay=5)
def shatter():
    x = t(0.3); return 0.5 * noise(len(x)) * env(len(x), decay=3) * (0.5 + 0.5*square(37, x))
def tinkle():
    x = t(0.6); s = np.zeros(len(x))
    for i, f in enumerate([2093, 2637, 3136, 2349]):
        o = int(SR * 0.09 * i); n = len(x) - o
        s[o:] += 0.3 * np.sin(2*np.pi*f*x[:n]) * env(n, decay=8)
    return s
def heal():
    x = t(0.5); return 0.35 * (sweep(440, 880, x) + 0.5*sweep(660, 1320, x)) * env(len(x), attack=0.05, decay=2)
def click():
    x = t(0.03); return 0.9 * noise(len(x)) * env(len(x), decay=10)
def glitch():
    x = t(0.45); s = square(110, x) * (rng.uniform(0, 1, len(x)) > 0.3)
    return 0.4 * s * env(len(x), decay=1.5) + 0.2 * noise(len(x)) * env(len(x), decay=2)
def bass():
    x = t(0.9); return 0.9 * sweep(120, 30, x) * env(len(x), attack=0.002, decay=1.5)
def whoosh():
    x = t(0.25); n = noise(len(x)); n = np.convolve(n, np.ones(12)/12, 'same')
    return 1.6 * n * np.sin(np.pi * np.linspace(0, 1, len(x)))
def deal():
    x = t(0.06); n = np.convolve(noise(len(x)), np.ones(4)/4, 'same'); return 0.6 * n * env(len(x), decay=4)
def buzz():
    x = t(0.22); return 0.35 * (square(98, x) + square(103, x)) * env(len(x), attack=0.01, decay=1)
def win():
    x = t(0.9); s = np.zeros(len(x))
    for i, f in enumerate([523, 659, 784, 1047]):
        o = int(SR * 0.11 * i); n = len(x) - o
        s[o:] += 0.3 * square(f, x[:n]) * env(n, decay=3)
    return s
def lose():
    x = t(0.9); return 0.4 * np.sign(sweep(330, 80, x)) * env(len(x), decay=1.2)
def freeze():
    x = t(0.5); return 0.25 * np.sin(2*np.pi*55*x) * np.sin(np.pi*np.linspace(0, 1, len(x)))

SOUNDS = dict(hit=hit, hitBig=hit_big, tick=tick, shield=shield, shatter=shatter, tinkle=tinkle,
              heal=heal, click=click, glitch=glitch, bass=bass, whoosh=whoosh, deal=deal,
              buzz=buzz, win=win, lose=lose, freeze=freeze)

gap = np.zeros(int(SR * 0.05))
chunks, sprite, pos = [], {}, 0
for name, fn in SOUNDS.items():
    s = np.clip(fn(), -1, 1)
    sprite[name] = [round(pos * 1000 / SR), round(len(s) * 1000 / SR)]
    chunks += [s, gap]; pos += len(s) + len(gap)

data = (np.concatenate(chunks) * 0.8 * 32767).astype(np.int16)
root = pathlib.Path(__file__).resolve().parent.parent / "src/DeckOverflow.Web/wwwroot/audio"
root.mkdir(parents=True, exist_ok=True)
with wave.open(str(root / "sfx.wav"), "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
(root / "sfx.json").write_text(json.dumps(sprite, indent=1))
print(json.dumps(sprite))
