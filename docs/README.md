# Ontwerpdocumenten

Deze repo is de enige bron voor het ontwerp. De docs zijn modulair: lees wat je nodig hebt, niet alles.

| Document | Waarover | Lees het als je werkt aan |
| --- | --- | --- |
| [visie.md](visie.md) | Visie, pijlers, thema, elites als echte bugs, verslavingsmotor, game feel, antipatronen | elk ontwerp |
| [architectuur.md](architectuur.md) | Platform, motor/shell/stage, determinisme, eventcontract, interop, technische risico's | code in elke afdeling |
| [wereld/](wereld/README.md) | Afdelingen, plattegrond, ontgrendelen, mastery, Prikklok, onthulling | de wereld rond de afdelingen |
| [wereld/beeldtaal.md](wereld/beeldtaal.md) | Hoe elk scherm eruitziet: elke afdeling een plek, de gedeelde bouwstenen | elk scherm, in elke afdeling |
| [wereld/codex.md](wereld/codex.md) | De Codex als het boek, over alle afdelingen | Codex-pagina's |
| [wereld/x-register.md](wereld/x-register.md) | Het ✗-register: achievements, en hoe een afdeling panelen levert | achievements |
| [wereld/backend.md](wereld/backend.md), [wereld/supabase.md](wereld/supabase.md) | Hosting, accounts, klascode, data; het werkplan van de backend | auth, opslag, klassement |
| [afdelingen/card-hall/](afdelingen/card-hall/README.md) | De deckbuilder: core loop, kernsysteem, drie acts, kaarten, events | de deckbuilder |
| [afdelingen/control-room/](afdelingen/control-room/README.md) | Regels voor een automaat (H5): acht gevechten, natuurwetten, events | de Controlekamer |
| [afdelingen/conveyor-belt/](afdelingen/conveyor-belt/README.md) | Band en machines op een rooster (H6): tien bestellingen, natuurwetten, wat de stage afspeelt | de Lopende Band |
| [geschiedenis/spikes.md](geschiedenis/spikes.md) | Archief: spike 1 tot 4, succescriteria, de weg naar de MVP | (zelden nodig) |
| [Architectuurschema](architectuur.svg) | De motor beslist, de stage speelt af | |
| [Product sheet](product-sheet/product-sheet.html) | A4-pitch voor instellingen en financiers | |

**Een nieuwe afdeling** krijgt een map `afdelingen/<naam>/` met een `README.md` (de spelvorm en haar loop), een `todo.md` en, als de code er is, een `events.md`; haar ideeën krijgen een sectie in [ideeen.md](../ideeen.md) op de root. Wat ze met de wereld deelt (Codex-pagina's, ✗-panelen), beschrijft ze in haar eigen map en registreert ze in de gedeelde systemen.

**Hoe je de docs leest.** Ze mengen wat gebouwd is met wat nog ontwerp is. Een sectie of regel met **Gebouwd** beschrijft het spel zoals het nu draait; de rest is richting.

## Stand van zaken

Op 3 oktober 2026 is **The Card Hall** speelbaar: de deckbuilder met drie acts, één per hoofdstuk (H2, H3, H4), van titelscherm tot eindscherm. Wat erin zit:

- **Drie acts** van 6 rijen plus een baas, elk met eigen vijanden, elites, baas en nieuwe kaarten. Een run kan ook in act 2 of 3 starten.
- **23 vijanden** (de Bottomless Jug meegeteld), waarvan 6 elites (echte bugs) en 3 bazen. **25 kaarten**, **13 relics**, **6 events**.
- **De getypeerde aanval**: je aanval is een waarde met een type die door modifiers stroomt, met echte C#-regels. Modifiers wachten over je beurt heen en zijn weg te vegen met Scrap.
- **Bewuste intents** die rekenen met jouw blok, kaarten, energie of HP.
- **De Codex**: 15 pagina's, geordend per hoofdstuk, met jouw moment als mini-animatie en een link naar het boek.
- **Het ✗-register**: 11 panelen.
- **De wereld**: teases, de onthulling en de fabrieksplattegrond met zes afdelingen. Alleen de Card Hall is open voor spelers. De Controlekamer (overgenomen uit spike 8) en de Lopende Band (tien bestellingen, overgenomen uit spike 9) zitten in het spel maar staan op **binnenkort** (beslist op 4 oktober 2026): de plattegrond toont al wat erin zit, en wie de laatste baas van de Card Hall verslaat, krijgt de sleutel van de Controlekamer, maar de deur blijft dicht tot de Controlekamer af is. De rest staat op **ooit**. De [superuser](wereld/backend.md#superuser) kan er al in.
- **Score van een run** en een dagelijkse seed in de motor; accounts en klascodes in Supabase. Het klassement zelf (de Prikklok) is nog niet aangesloten.
- **De sneltoets W** wint het lopende gevecht, alleen voor de [superuser](wereld/backend.md#superuser).

Nog niet met spelers getest. Eén bevinding van de ontwikkelaar zelf: Ink tegen een getal zette een run vast (opgelost met Scrap).

## Beslist

Kort, met de datum; de uitwerking staat in het document van het onderwerp.

- 2 oktober 2026: Blazor WebAssembly met PixiJS; de AI-art in handleidingstijl is definitief; runlengte afgestemd op een lesblok.
- 4 oktober 2026: afdelingen staan op vrijgegeven, binnenkort (gebouwd, dicht, met tease) of ooit (in de doos); de Controlekamer en de Lopende Band staan voorlopig op binnenkort.
- 4 oktober 2026: [de beeldtaal](wereld/beeldtaal.md). Elke afdeling is een plek met onderdelen uit een handleiding (schakelkast, werkvloer, klembord); de plattegrond is één tekening met dozen; een afdeling klapt uit haar doos; geen groen of rood; de Lopende Band speelt op een telefoon ook rechtop.
- 3 oktober 2026: elke afdeling krijgt haar eigen spelvorm rond één plattegrond; de Card Hall heeft drie acts, één per hoofdstuk; startpunten met vijf keer 1 uit 3; de Codex is het boek; het vangnet voor de onthulling na 5 gestarte runs; de score van een run; Scrap voor wachtende modifiers; een afdeling ontgrendel je zelf; een docent maakt alleen een klascode voor het klassement, niets anders; gemonteerd is drie gewonnen runs of de elite; de eerste voltooide dagelijkse run telt.
- 7 oktober 2026: [rolverdeling](../CLAUDE.md#rolverdeling) met product owner, implementer en reviewer; keuzes van de product owner in [DECISIONS.md](../DECISIONS.md) (D-001 tot D-003); ideeën samen in [ideeen.md](../ideeen.md) op de root, per afdeling; al het werk op `main`.

## Product sheet

Open `product-sheet.html` in een browser. Afdrukken naar pdf geeft één A4-pagina. Het lettertype komt van Google Fonts; offline valt het terug op systeemfonts.

Nog in te vullen: `[DATUM]` (prototype en roadmap), `[EMAIL]`, `[BEDRAG]`, `[PRIJS per instelling / jaar]`, `[MARKTOMVANG]` en het cijfer over uitval of slaagpercentage in het eerste jaar. Het sfeerbeeld is met AI gegenereerd en geen definitieve art.

Tot 3 oktober 2026 stonden de documenten in Claude Docs en een design-canvas. Die versies worden niet meer bijgewerkt.
