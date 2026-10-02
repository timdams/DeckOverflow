// sprites.js - placeholder pixelart, opgebouwd uit rechthoeken.
// Elke rij is even lang; '.' is leeg. Definitieve art komt pas na de spike.

const PALETTE = {
  k: 0x1b1622, // omlijning
  g: 0x6b6280, // steen
  d: 0x4a4358, // donkere steen
  o: 0xf39200, // oranje circuit
  e: 0xffd166, // gloeiende ogen
  w: 0x7cf6ff, // scherm
  s: 0x3d3a4b, // romp
  c: 0x4cc9f0, // schild
  r: 0xe5484d, // hart
  l: 0xcfd3dc, // lemmet
  n: 0x5fd068, // groen
  p: 0xb18cff, // geest, double-paars
  P: 0xe2d6ff, // geest, licht
  v: 0x5b3f99, // geest, donker
  t: 0xa7b0be, // tin
  T: 0x6d7480, // donker tin
  b: 0x4f8cff, // int-blauw
};

export const ART = {
  golem: [
    '.....kkkkkk.....',
    '....kggggggk....',
    '....kgeggegk....',
    '....kggggggk....',
    '..kkkkkookkkkk..',
    '.kggkggggggkggk.',
    'kgggkgoggogkgggk',
    'kgggkggooggkgggk',
    'kggdkgggoggkdggk',
    '.kkkkggoogggkkk.',
    '....kggooggk....',
    '....kggggggk....',
    '...kggk..kggk...',
    '...kggk..kggk...',
    '..kkddk..kddkk..',
    '..kkkkk..kkkkk..',
  ],
  hero: [
    '..kkkkkkkk..',
    '.koooooooook',
    '.kokkkkkkkok',
    '.kokwkkkwkok',
    '.kokkkkkkkok',
    '.kokkwwwkkok',
    '.koooooooook',
    '..kkkkkkkk..',
    '...kssssk...',
    '..kssssssk..',
    '..kssossssk.',
    '..kssssssk..',
    '...ksk.ksk..',
    '...kkk.kkk..',
  ],
  geest: [
    '....kkkkkk....',
    '..kkppppppkk..',
    '.kppPPppppppk.',
    '.kpPPpppppppk.',
    'kpppekppekpppk',
    'kpppkkppkkpppk',
    'kppppppppppppk',
    'kpppppvvpppppk',
    'kppppppppppppk',
    'kpPpppppppPppk',
    'kppppppppppppk',
    'kppkkppkkppkpk',
    'kpk..kk..kk.kk',
  ],
  kolos: [
    '....kkkkkkkk....',
    '...kttttttttk...',
    '...kttettettk...',
    '...kttttttttk...',
    '.kkkkkTTTTkkkkk.',
    'kttttkttttkttttk',
    'kttttkbbbbkttttk',
    'kttttkbttbkttttk',
    'kTttkkbbbbkkttTk',
    'kkkkkkttttkkkkkk',
    '....kttttttk....',
    '....kttkkttk....',
    '...kttk..kttk...',
    '...kTTk..kTTk...',
    '..kkkkk..kkkkk..',
  ],
  vlot: [
    '......kP',
    '.....kPP',
    '....kPP.',
    '.k.kPP..',
    '.kkPP...',
    '..kk....',
    '.kvkk...',
    'kv..k...',
  ],
  kroes: [
    '...oo...',
    '..oeeo..',
    'kkkkkkkk',
    'kgoooogk',
    'kggooggk',
    '.kggggk.',
    '..kkkk..',
  ],
  sword: [
    '......kl',
    '.....kll',
    '....kll.',
    '.k.kll..',
    '.kkll...',
    '..kk....',
    '.kokk...',
    'ko..k...',
  ],
  shield: [
    'kkkkkkk',
    'kccccck',
    'kcwccck',
    'kccccck',
    '.kccck.',
    '.kccck.',
    '..kck..',
    '...k...',
  ],
  heart: [
    '.kk.kk.',
    'krrkrrk',
    'krrrrrk',
    'krrrrrk',
    '.krrrk.',
    '..krk..',
    '...k...',
  ],
};

/**
 * Bouwt een pixelsprite. Geeft de container terug, een witte overlay voor flitsen,
 * de pixelposities (voor uiteenspatten) en de afmetingen.
 * De oorsprong ligt onderaan in het midden, zodat sprites op de grond staan.
 */
export function pixelSprite(art, px) {
  const rows = art.length;
  const cols = art[0].length;
  const w = cols * px;
  const h = rows * px;

  const body = new PIXI.Graphics();
  const flash = new PIXI.Graphics();
  const overlay = new PIXI.Graphics();   // blijvende kleurlaag, bv. na Omgieten
  const pixels = [];

  art.forEach((row, y) => {
    if (row.length !== cols) console.warn(`Pixelart: rij ${y} heeft lengte ${row.length}, verwacht ${cols}`);
    [...row].forEach((ch, x) => {
      const color = PALETTE[ch];
      if (color === undefined) return;
      const rx = x * px - w / 2;
      const ry = y * px - h;
      body.rect(rx, ry, px, px).fill(color);
      flash.rect(rx, ry, px, px).fill(0xffffff);
      if (ch !== 'k' && ch !== 'e') overlay.rect(rx, ry, px, px).fill(0xffffff);
      pixels.push({ x: rx + px / 2, y: ry + px / 2, color });
    });
  });

  flash.alpha = 0;
  overlay.alpha = 0;
  const container = new PIXI.Container();
  container.addChild(body, overlay, flash);
  return { container, flash, overlay, pixels, w, h, px };
}

/** Klein icoon, oorsprong in het midden. */
export function icon(name, px = 3) {
  const s = pixelSprite(ART[name], px);
  s.container.y = s.h / 2;
  const wrap = new PIXI.Container();
  wrap.addChild(s.container);
  return wrap;
}
