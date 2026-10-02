"""Snijdt de spritevellen in art/sheets/ in losse PNG's voor wwwroot/art/{stijl}/.

Elk vel is een raster van 5x4 vakjes in een vaste volgorde (zie NAMES). De effen
achtergrond wordt transparant: alles wat op de achtergrondkleur lijkt en de rand van
het vel raakt. Daarna zoeken we de losse stukken op het hele vel en geven we elk stuk
aan het vakje waar zijn midden ligt. Zo blijft een ton die over de grens van haar
vakje steekt heel, en komt er geen randje van de buurman mee.

Teletekst wordt niet getekend maar berekend uit een andere stijl.

Gebruik, vanuit de spikemap:  python tools/cut_sheets.py
"""

import colorsys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SHEETS = ROOT / "art" / "sheets"
OUT = ROOT / "src" / "DeckOverflow.Web" / "wwwroot" / "art"

COLS, ROWS = 5, 4
NAMES = [
    "strike", "floating-strike", "heavy-strike", "floating-rain", "molded-strike",
    "shield", "thick-shield", "mend", "add", "double-up",
    "remold", "set-to-1", "byte-trap", "treasure", "rest",
    "relic-counter", "relic-floating-point", "relic-scrap-pouch", "relic-anchor-barrel", "relic-great-pot",
]

# Per stijl: tot welke afstand (grootste kanaalverschil) een pixel achtergrond is, en de
# zachte rand daarboven. "paper": zwarte lijnen op wit verdwijnen op een donkere kaart,
# dus het papier blijft staan als een kaartje met ronde hoeken. "pixel": terug naar
# zoveel echte pixels.
STYLES = {
    "pixel": {"fill": 10, "soft": (10, 40), "pixel": 64},
    "notebook": {"fill": 8, "soft": (8, 30), "paper": True},
    "sticker": {"fill": 10, "soft": (10, 34)},
    "clay": {"fill": 34, "soft": (34, 70)},
    "tin": {"fill": 9, "soft": (9, 30)},
    "patent": {"fill": 8, "soft": (8, 30), "paper": True},
    "manual": {"fill": 8, "soft": (8, 30), "paper": True},
    "cabinet": {"fill": 16, "soft": (16, 44)},
    "marginalia": {"fill": 9, "soft": (9, 30)},
    "cross-stitch": {"fill": 9, "soft": (9, 30)},
}

TELETEXT_SOURCE = "sticker"
TELETEXT_CHARS = (22, 15)  # tekens breed en hoog; elk teken is 2x3 blokjes

MARGIN = 0.06  # lege rand rond de tekening, als fractie van de zijde
SIZE = 192     # eindformaat voor de gladde stijlen


def background(rgb: np.ndarray) -> np.ndarray:
    corners = np.array([rgb[1, 1], rgb[1, -2], rgb[-2, 1], rgb[-2, -2]], dtype=int)
    return np.median(corners, axis=0)


def alpha_mask(rgb: np.ndarray, bg: np.ndarray, fill: int, soft: tuple[int, int]) -> np.ndarray:
    """Alfa per pixel: 0 voor achtergrond die de rand van het vel raakt, zacht aan de rand."""
    dist = np.abs(rgb.astype(int) - bg).max(axis=2)
    labels, _ = ndimage.label(dist <= fill)
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    bg_mask = np.isin(labels, edge[edge > 0])

    lo, hi = soft
    ramp = np.clip((dist - lo) * 255 // max(1, hi - lo), 0, 255)
    border = ndimage.binary_dilation(bg_mask, iterations=2) & ~bg_mask
    alpha = np.full(dist.shape, 255, dtype=np.uint8)
    alpha[border] = ramp[border]
    alpha[bg_mask] = 0
    return alpha


def pieces_per_cell(alpha: np.ndarray, cell_w: int, cell_h: int) -> list[np.ndarray]:
    """Een masker per vakje met alle stukken waarvan het midden in dat vakje ligt."""
    labels, _ = ndimage.label(alpha > 24, structure=np.ones((3, 3)))
    masks = [np.zeros(alpha.shape, dtype=bool) for _ in NAMES]
    for i, sl in enumerate(ndimage.find_objects(labels), start=1):
        part = labels[sl] == i
        if part.sum() < 6:
            continue  # stofje
        cy = (sl[0].start + sl[0].stop) / 2
        cx = (sl[1].start + sl[1].stop) / 2
        col = min(int(cx // cell_w), COLS - 1)
        row = min(int(cy // cell_h), ROWS - 1)
        masks[row * COLS + col][sl] |= part
    return masks


def square_box(mask: np.ndarray) -> tuple[int, int, int]:
    """Links, boven en zijde van het vierkant rond het masker, met wat marge."""
    ys, xs = np.nonzero(mask)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    side = round(max(x1 - x0, y1 - y0) * (1 + 2 * MARGIN))
    return round((x0 + x1) / 2 - side / 2), round((y0 + y1) / 2 - side / 2), side


def cutout(rgb: np.ndarray, alpha: np.ndarray, mask: np.ndarray) -> Image.Image:
    left, top, side = square_box(mask)
    img = Image.fromarray(np.dstack([rgb, np.where(mask, alpha, 0)]).astype(np.uint8), "RGBA")
    return img.crop((left, top, left + side, top + side))  # buiten het vel wordt transparant


def paper(rgb: np.ndarray, mask: np.ndarray) -> Image.Image:
    """Het vierkant rond de tekening uit het vel, met het papier erbij en ronde hoeken."""
    h, w = mask.shape
    left, top, side = square_box(mask)
    left, top = min(max(left, 0), w - side), min(max(top, 0), h - side)
    img = Image.fromarray(rgb[top:top + side, left:left + side]).convert("RGBA")
    corners = Image.new("L", img.size, 0)
    ImageDraw.Draw(corners).rounded_rectangle((0, 0, side - 1, side - 1), radius=side // 10, fill=255)
    img.putalpha(corners)
    return img


def pixelate(img: Image.Image, size: int) -> Image.Image:
    small = img.resize((size, size), Image.Resampling.BOX)
    alpha = small.getchannel("A").point(lambda a: 255 if a >= 110 else 0)
    rgb = small.convert("RGB").quantize(colors=32, method=Image.Quantize.MEDIANCUT).convert("RGB")
    rgb.putalpha(alpha)
    return rgb


# De zeven kleuren van teletekst; zwart is "uit".
TELETEXT = {
    "red": (255, 0, 0), "green": (0, 255, 0), "yellow": (255, 255, 0), "blue": (0, 0, 255),
    "magenta": (255, 0, 255), "cyan": (0, 255, 255), "white": (255, 255, 255),
}


def teletext_colour(rgb) -> str | None:
    """Via tint en helderheid, niet via afstand: hout wordt rood, staal wit, zoals op pagina 888."""
    h, l, s = colorsys.rgb_to_hls(*(c / 255 for c in rgb))
    if l < 0.12:
        return None
    if s < 0.25:
        return "white" if l > 0.38 else None
    deg = h * 360
    if deg < 45 or deg >= 330:
        return "yellow" if l > 0.55 else "red"
    for limit, name in ((70, "yellow"), (165, "green"), (200, "cyan"), (265, "blue"), (330, "magenta")):
        if deg < limit:
            return name
    return "red"


def teletext(src: Image.Image) -> Image.Image:
    """Mozaïek van 2x3 blokjes per teken, met één voorgrondkleur per teken, zoals op tv."""
    cols, rows = TELETEXT_CHARS
    w, h = cols * 2, rows * 3
    small = src.resize((w, h), Image.Resampling.BOX)
    px = small.load()
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dst = out.load()
    for cy in range(rows):
        for cx in range(cols):
            on = [(x, y, teletext_colour(px[x, y][:3]))
                  for y in range(cy * 3, cy * 3 + 3) for x in range(cx * 2, cx * 2 + 2) if px[x, y][3] >= 128]
            names = [n for _, _, n in on if n]
            if not names:
                continue
            fg = max(set(names), key=names.count)
            for x, y, n in on:
                if n:
                    dst[x, y] = (*TELETEXT[fg], 255)
    return out  # een blokje is bijna vierkant (een teken is ongeveer 12x20 beeldpunten)


def main() -> None:
    for style, cfg in STYLES.items():
        sheet_path = max(SHEETS.glob(f"{style}-2*.png"))  # nieuwste vel van deze stijl
        rgb = np.asarray(Image.open(sheet_path).convert("RGB"))
        bg = background(rgb)
        alpha = alpha_mask(rgb, bg, cfg["fill"], cfg["soft"])
        masks = pieces_per_cell(alpha, rgb.shape[1] // COLS, rgb.shape[0] // ROWS)

        target = OUT / style
        target.mkdir(parents=True, exist_ok=True)
        for name, mask in zip(NAMES, masks):
            if not mask.any():
                raise ValueError(f"{style}: vakje {name} is leeg")
            sprite = paper(rgb, mask) if cfg.get("paper") else cutout(rgb, alpha, mask)
            if cfg.get("pixel"):
                sprite = pixelate(sprite, cfg["pixel"])
            else:
                sprite = sprite.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
            sprite.save(target / f"{name}.png", optimize=True)
        print(f"{style}: {len(NAMES)} sprites uit {sheet_path.name} (achtergrond {bg.astype(int).tolist()})")

    target = OUT / "teletext"
    target.mkdir(parents=True, exist_ok=True)
    for name in NAMES:
        src = Image.open(OUT / TELETEXT_SOURCE / f"{name}.png").convert("RGBA")
        teletext(src).save(target / f"{name}.png", optimize=True)
    print(f"teletext: {len(NAMES)} sprites berekend uit {TELETEXT_SOURCE}")


if __name__ == "__main__":
    main()
