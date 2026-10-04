"""Zet de gekozen Kenney-geluiden uit assetsin/ in het spel (wwwroot/audio/sfx/ + sfx.json).

Elke logische naam (die de stages met sfx('hit') aanroepen) krijgt een of meer varianten; de stage kiest er
telkens een, zodat een reeks klappen niet als een machinegeweer klinkt. Elk bestand gaat mee als .ogg (zoals
Kenney het levert) en als .mp3 (voor oudere Safari). Het script meet ook hoe luid elk bestand is en zet een
gain in het manifest, zodat de pakketten even hard klinken; volume per aanroep blijft in de stage.

Draai opnieuw na een wijziging in SOUNDS:
    pip install soundfile lameenc numpy
    python tools/copy_sfx.py
"""
import json, pathlib, shutil
import numpy as np
import soundfile as sf
import lameenc

ROOT = pathlib.Path(__file__).resolve().parent.parent
SRC = ROOT / "assetsin"
OUT = ROOT / "src/DeckOverflow.Web/wwwroot/audio"

DIGITAL = "kenney_digital-audio/Audio"
IMPACT = "kenney_impact-sounds/Audio"
IFACE = "kenney_interface-sounds/Audio"
RPG = "kenney_rpg-audio/Audio"
SCIFI = "kenney_sci-fi-sounds/Audio"
UI = "kenney_ui-audio/Audio"

def five(pack, stem): return [f"{pack}/{stem}_{i:03}" for i in range(5)]

# Logische naam -> bestanden (zonder .ogg). Een mengeling: fabriek (metaal, stempels) en digitaal (piepjes).
SOUNDS = {
    # Gevechten en duels
    "hit":      five(IMPACT, "impactPunch_medium"),
    "hitBig":   five(IMPACT, "impactPunch_heavy"),
    "bass":     [f"{SCIFI}/lowFrequency_explosion_001"],            # iemand valt om
    "shield":   five(IMPACT, "impactPlate_medium"),                   # blok, schrap zetten, relic
    "shatter":  five(IMPACT, "impactGlass_heavy"),                    # blok kapot
    "whoosh":   [f"{RPG}/cloth{i}" for i in (1, 2, 3, 4)],            # kaart gespeeld, aanval, beurt voorbij
    "deal":     [f"{IFACE}/scratch_00{i}" for i in (1, 2, 3)],        # een kaart van de stapel
    "heal":     [f"{DIGITAL}/powerUp{i}" for i in (2, 5, 6, 7)],      # herstel, een waarde die groeit
    "win":      [f"{DIGITAL}/threeTone1"],
    "lose":     [f"{DIGITAL}/lowThreeTone"],
    # Typeregels
    "tick":     [f"{IFACE}/tick_00{i}" for i in (1, 2, 4)],           # tellen, plakken, afronden
    "click":    [f"{IFACE}/click_001", f"{UI}/click1", f"{UI}/click3"],
    "tinkle":   [f"{RPG}/handleCoins2", f"{RPG}/handleCoins"],        # wat na de komma stond, valt eraf
    "freeze":   [f"{DIGITAL}/lowDown"],                               # het stilvallen vlak voor een overloop
    "glitch":   [f"{DIGITAL}/lowRandom", f"{DIGITAL}/zapTwoTone"],    # overloop, crash, exception
    "buzz":     [f"{IFACE}/error_00{i}" for i in (1, 2, 4, 8)],       # een zet die weigert
    # De Lopende Band
    "machine":  five(IMPACT, "impactMetal_medium"),
    "latch":    [f"{RPG}/metalLatch", f"{RPG}/metalClick"],           # een wissel klakt om
    "stamp":    five(IMPACT, "impactWood_heavy"),
    "approve":  [f"{IFACE}/confirmation_001", f"{IFACE}/confirmation_003"],
    "bin":      five(IMPACT, "impactTin_medium"),
    "fall":     five(IMPACT, "impactSoft_heavy"),
    "overheat": [f"{SCIFI}/explosionCrunch_000"],
    "alarm":    [f"{DIGITAL}/twoTone1", f"{DIGITAL}/twoTone2"],
    # De wereld en de knoppen van de shell
    "uiClick":  [f"{UI}/click{i}" for i in (1, 2, 3)],
    "uiHover":  [f"{UI}/rollover{i}" for i in (2, 3, 4)],
    "uiBook":   [f"{RPG}/bookOpen", f"{RPG}/bookFlip3"],              # Codex en ✗-register
}

# Hoe luid een bestand klinkt: de RMS van het luidste stukje van 50 ms. Elk bestand wordt naar TARGET
# gebracht, maar nooit versterkt (Howler kan niet boven 1).
TARGET = 0.15

def loudness(data, rate):
    mono = data.mean(axis=1) if data.ndim > 1 else data
    win = max(1, int(rate * 0.05))
    if len(mono) <= win: return float(np.sqrt(np.mean(mono ** 2)))
    sq = np.convolve(mono ** 2, np.ones(win) / win, "valid")
    return float(np.sqrt(sq.max()))

def to_mp3(data, rate, path):
    channels = 1 if data.ndim == 1 else data.shape[1]
    pcm = (np.clip(data, -1, 1) * 32767).astype(np.int16)
    enc = lameenc.Encoder()
    enc.set_bit_rate(96 if channels == 1 else 128)
    enc.set_in_sample_rate(rate)
    enc.set_channels(channels)
    enc.set_quality(2)
    path.write_bytes(enc.encode(pcm.tobytes()) + enc.flush())

def main():
    dest = OUT / "sfx"
    if dest.exists(): shutil.rmtree(dest)
    dest.mkdir(parents=True)
    manifest = {}
    for name, files in SOUNDS.items():
        variants = []
        for i, rel in enumerate(files, 1):
            src = SRC / f"{rel}.ogg"
            data, rate = sf.read(src, dtype="float32")
            stem = f"{name}-{i}"
            shutil.copyfile(src, dest / f"{stem}.ogg")
            to_mp3(data, rate, dest / f"{stem}.mp3")
            variants.append({"file": stem, "gain": round(min(1.0, TARGET / max(loudness(data, rate), 1e-4)), 2)})
        manifest[name] = variants
    (OUT / "sfx.json").write_text(json.dumps(manifest, indent=1) + "\n", encoding="utf-8")
    size = sum(p.stat().st_size for p in dest.iterdir())
    for name, v in manifest.items(): print(f"{name:9} {' '.join(str(x['gain']) for x in v)}")
    print(f"{sum(len(v) for v in manifest.values())} geluiden, {size / 1024:.0f} kB (ogg + mp3)")

if __name__ == "__main__":
    main()
