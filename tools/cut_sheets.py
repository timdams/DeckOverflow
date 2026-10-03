"""Snijdt de vellen in art/sheets/ in losse PNG's voor wwwroot/art/.

Elk vel is een raster in een vaste volgorde (zie SHEETS). De witte achtergrond wordt
transparant: alles wat wit is en de rand van het vel raakt. Wit binnen een tekening
(het lijf van een figuurtje) blijft wit, zodat het op het papier van de stage niet
doorschijnt. Elk los stuk gaat naar het vakje waar zijn midden ligt, zodat een tekening
die over de grens van haar vakje steekt heel blijft.

Gebruik, vanuit de root van de repo:  python tools/cut_sheets.py
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SHEETS = ROOT / "art" / "sheets"
OUT = ROOT / "src" / "DeckOverflow.Web" / "wwwroot" / "art"

# Per vel: bestandsprefix, kolommen, rijen, map, vorm, eindformaat, lijndikte, namen in
# leesvolgorde. "square" past in een vierkant (kaarten, iconen); "tight" snijdt strak, zodat
# een figuur met de voeten op de grond kan staan. Lijndikte: hoeveel pixels de zwarte lijnen
# op het vel aandikken voor het verkleinen. Kleine iconen hebben dat nodig, anders
# verdwijnen de lijnen op de map en op een kaart.
SHEETS_SPEC = [
    ("manual", 5, 4, "items", "square", 192, 5, [
        "strike", "floating-strike", "heavy-strike", "floating-rain", "molded-strike",
        "shield", "thick-shield", "mend", "add", "double-up",
        "remold", "set-to-1", "byte-trap", "treasure", "rest",
        "relic-counter", "relic-floating-point", "relic-scrap-pouch", "relic-anchor-barrel", "relic-great-pot",
    ]),
    ("characters", 3, 3, "actors", "tight", 300, 3, [
        "hero", "slime", "knight",
        "ghost", "dripper", "jug",
        "colossus", "golem", "reckoner",
    ]),
    ("icons", 5, 4, "icons", "square", 160, 5, [
        "fight", "elite", "event", "shop", "boss",
        "intent-attack", "intent-block", "intent-heal", "intent-buff", "intent-unknown",
        "hp", "gold", "draw", "discard", "energy",
        "victory", "crash", "truncate", "overflow", "end-turn",
    ]),
    ("scenes", 3, 2, "scenes", "tight", 520, 3, [
        "foundry", "crucible", "leaking-barrel",
        "rest", "shop", "title",
    ]),
    # Kaarten als overtredingen van de handleiding
    ("violations", 5, 3, "items", "square", 192, 5, [
        "whack", "hammer-it-in", "floating-bolts", "floating-parts", "molded-bolts",
        "hold-firmly", "two-person-lift", "fill-past-the-line", "spare-screw", "second-pair-of-hands",
        "force-fit", "wrong-label", "squeeze-in", "rev-crossed", "rev-stamped",
    ]),
    # Elites als echte bugs, en de inspecteur als baas
    ("bugs", 2, 2, "actors", "tight", 300, 3, [
        "level-256", "flight-501",
        "the-index", "inspector",
    ]),
    # De held zonder zwaard, in vier varianten; art.json kiest er één
    ("heroes", 2, 2, "actors", "tight", 300, 3, [
        "hero-wrench", "hero-screwdriver",
        "hero-sleeves", "hero-toolbox",
    ]),
]

# Vellen waarop de figuren naar rechts kijken: gespiegeld, zodat ze de held aankijken.
FLIP = {"bugs"}

FILL = 12          # tot dit kanaalverschil met de achtergrond is een pixel achtergrond
SOFT = (12, 60)    # zachte rand daarboven
MARGIN = 0.05


def background(rgb: np.ndarray) -> np.ndarray:
    corners = np.array([rgb[1, 1], rgb[1, -2], rgb[-2, 1], rgb[-2, -2]], dtype=int)
    return np.median(corners, axis=0)


def alpha_mask(rgb: np.ndarray, bg: np.ndarray) -> np.ndarray:
    """Alfa per pixel: 0 voor achtergrond die de rand van het vel raakt, zacht aan de rand."""
    dist = np.abs(rgb.astype(int) - bg).max(axis=2)
    labels, _ = ndimage.label(dist <= FILL)
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    bg_mask = np.isin(labels, edge[edge > 0])

    lo, hi = SOFT
    ramp = np.clip((dist - lo) * 255 // (hi - lo), 0, 255)
    border = ndimage.binary_dilation(bg_mask, iterations=2) & ~bg_mask
    alpha = np.full(dist.shape, 255, dtype=np.uint8)
    alpha[border] = ramp[border]
    alpha[bg_mask] = 0
    return alpha


def pieces_per_cell(alpha: np.ndarray, cols: int, rows: int) -> list[np.ndarray]:
    """Een masker per vakje met alle stukken waarvan het midden in dat vakje ligt."""
    cell_w, cell_h = alpha.shape[1] / cols, alpha.shape[0] / rows
    labels, _ = ndimage.label(alpha > 24, structure=np.ones((3, 3)))
    masks = [np.zeros(alpha.shape, dtype=bool) for _ in range(cols * rows)]
    for i, sl in enumerate(ndimage.find_objects(labels), start=1):
        part = labels[sl] == i
        if part.sum() < 6:
            continue  # stofje
        cy = (sl[0].start + sl[0].stop) / 2
        cx = (sl[1].start + sl[1].stop) / 2
        col = min(int(cx // cell_w), cols - 1)
        row = min(int(cy // cell_h), rows - 1)
        masks[row * cols + col][sl] |= part
    return masks


def crop(rgb: np.ndarray, alpha: np.ndarray, mask: np.ndarray, shape: str) -> Image.Image:
    ys, xs = np.nonzero(mask)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    w, h = x1 - x0, y1 - y0
    if shape == "square":
        w = h = max(w, h)
    pad = round(max(w, h) * MARGIN)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    left, top = round(cx - w / 2) - pad, round(cy - h / 2) - pad
    img = Image.fromarray(np.dstack([rgb, np.where(mask, alpha, 0)]).astype(np.uint8), "RGBA")
    return img.crop((left, top, left + w + 2 * pad, top + h + 2 * pad))


def thicken(img: Image.Image, px: int) -> Image.Image:
    """Dikt zwarte lijnen aan: het donkerste in een venster wint, en de alfa groeit mee."""
    if px <= 1:
        return img
    rgb = img.convert("RGB").filter(ImageFilter.MinFilter(px))
    alpha = img.getchannel("A").filter(ImageFilter.MaxFilter(px))
    rgb.putalpha(alpha)
    return rgb


def fit(img: Image.Image, size: int) -> Image.Image:
    scale = size / max(img.size)
    return img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.Resampling.LANCZOS)


def main() -> None:
    for prefix, cols, rows, folder, shape, size, line, names in SHEETS_SPEC:
        sheet_path = max(SHEETS.glob(f"{prefix}-2*.png"))  # nieuwste vel
        rgb = np.asarray(Image.open(sheet_path).convert("RGB"))
        alpha = alpha_mask(rgb, background(rgb))
        masks = pieces_per_cell(alpha, cols, rows)

        target = OUT / folder
        target.mkdir(parents=True, exist_ok=True)
        for name, mask in zip(names, masks):
            if not mask.any():
                raise ValueError(f"{prefix}: vakje {name} is leeg")
            img = fit(thicken(crop(rgb, alpha, mask, shape), line), size)
            if prefix in FLIP:
                img = img.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            img.save(target / f"{name}.png", optimize=True)
        print(f"{folder}: {len(names)} uit {sheet_path.name}")


if __name__ == "__main__":
    main()
