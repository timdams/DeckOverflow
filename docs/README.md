# Ontwerpdocumenten

| Document | Waarover | Bron |
| --- | --- | --- |
| [Game Design Document](game-design-document.md) | Visie, pijlers, kernsysteem (types en Omgieten), loot, Act 1: De Vatenvallei, encounters, Codex, antipatronen | [Claude Docs](https://claude.ai/artifact/5Zz4VdoN9QFaeYZEm1hsa7) |
| [Spike Design Doc](spike-design-doc.md) | Architectuur motor/shell/stage, eventcontract, interop, Byte-Golem-scenario, succescriteria, scope spike 2 | [Claude Docs](https://claude.ai/artifact/TFfgyB6S5NvBcdx6tExyrj) |
| [Architectuurschema](architectuur.svg) | De motor beslist, de stage speelt af | Uit het Spike Design Doc |
| [Product sheet](product-sheet/product-sheet.html) | A4-pitch voor instellingen en financiers, met [sfeerbeeld](product-sheet/sfeerbeeld.jpg) | [Design-canvas](https://claude.ai/artifact/95YGcMNq95KyYYNkNjmVJu) |

De markdown is overgenomen op 2 oktober 2026. De Claude Docs blijven de plek waar we samen schrijven en reageren; werk na een grote wijziging daar ook de kopie hier bij.

## Stand van zaken tegenover de code

- **Eventcontract:** de 15 events uit het Spike Design Doc staan zo in [GameEvent.cs](../src/DeckOverflow.Engine/Events/GameEvent.cs).
- **Solution-structuur:** het doc noemt nog `TypedValue` en `Turn`. De spike kwam zonder uit; de werkelijke structuur staat in de [README](../README.md#structuur).
- **Spike 2** (een `double`-vijand, getypeerde aanvallen, Omgieten, event `TypeChanged`, `DoubleRules`) is nog niet gebouwd.
- **Techniekkeuze:** het GDD laat Blazor tegenover een JavaScript-engine nog open. De spike koos een combinatie: de motor in Blazor WebAssembly, de stage in PixiJS.

## Product sheet

Open `product-sheet.html` in een browser. Afdrukken naar pdf geeft één A4-pagina. Het lettertype komt van Google Fonts; offline valt het terug op systeemfonts.

Nog in te vullen: `[DATUM]` (prototype en roadmap), `[EMAIL]`, `[BEDRAG]`, `[PRIJS per instelling / jaar]`, `[MARKTOMVANG]` en het cijfer over uitval of slaagpercentage in het eerste jaar. Het sfeerbeeld is met AI gegenereerd en geen definitieve art.
