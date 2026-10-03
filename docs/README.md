# Ontwerpdocumenten

Deze repo is de enige bron voor het ontwerp. Wijzigingen gebeuren hier, in markdown, en gaan mee in git.

| Document | Waarover |
| --- | --- |
| [Game Design Document](game-design-document.md) | Visie, pijlers, kernsysteem (types en Omgieten), loot, afdelingen met elk een eigen spelvorm, de fabrieksplattegrond, de onthulling, Act 1: De Vatenvallei, encounters, Codex, antipatronen |
| [Spike Design Doc](spike-design-doc.md) | Architectuur motor/shell/stage, eventcontract, interop, Byte-Golem-scenario, succescriteria, de weg naar de MVP, hosting, accounts en data |
| [Architectuurschema](architectuur.svg) | De motor beslist, de stage speelt af |
| [Product sheet](product-sheet/product-sheet.html) | A4-pitch voor instellingen en financiers, met [sfeerbeeld](product-sheet/sfeerbeeld.jpg) |

Tot 3 oktober 2026 stonden deze documenten in Claude Docs en een design-canvas ([GDD](https://claude.ai/artifact/5Zz4VdoN9QFaeYZEm1hsa7), [Spike Design Doc](https://claude.ai/artifact/TFfgyB6S5NvBcdx6tExyrj), [product sheet](https://claude.ai/artifact/95YGcMNq95KyYYNkNjmVJu)). Die versies worden niet meer bijgewerkt en lopen achter op de repo.

## Stand van zaken tegenover spike 1

- **Eventcontract:** de 15 events uit het Spike Design Doc staan zo in [GameEvent.cs](../spikes/01-byte-golem/src/DeckOverflow.Engine/Events/GameEvent.cs).
- **Solution-structuur:** het doc noemt nog `TypedValue` en `Turn`. De spike kwam zonder uit; de werkelijke structuur staat in de [README van spike 1](../spikes/01-byte-golem/README.md#structuur).
- **Spike 2** is gebouwd in [spikes/02-omgieten](../spikes/02-omgieten/). In plaats van een aparte `DoubleRules` beslist het type van het doelwit in de motor zelf, en `CastRules` doet de conversies. `TypeChanged` kreeg extra velden; de keuzes staan in de [README van spike 2](../spikes/02-omgieten/README.md#keuzes-tijdens-spike-2).
- **Techniekkeuze:** het GDD laat Blazor tegenover een JavaScript-engine nog open. De spike koos een combinatie: de motor in Blazor WebAssembly, de stage in PixiJS.

## Product sheet

Open `product-sheet.html` in een browser. Afdrukken naar pdf geeft één A4-pagina. Het lettertype komt van Google Fonts; offline valt het terug op systeemfonts.

Nog in te vullen: `[DATUM]` (prototype en roadmap), `[EMAIL]`, `[BEDRAG]`, `[PRIJS per instelling / jaar]`, `[MARKTOMVANG]` en het cijfer over uitval of slaagpercentage in het eerste jaar. Het sfeerbeeld is met AI gegenereerd en geen definitieve art.
