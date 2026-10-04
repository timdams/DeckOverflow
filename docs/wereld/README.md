# De wereld

Alles rond de afdelingen: welke afdelingen er zijn, de fabrieksplattegrond, ontgrendelen, mastery, de Prikklok en de onthulling. Aparte documenten: de [Codex](codex.md), het [✗-register](x-register.md), de [backend](backend.md) (accounts, klascode, data, hosting) en de [woordenlijst](woordenlijst.md) (de termen en de toon van *Zie Scherp Scherper* voor de Nederlandse spelteksten).

## Afdelingen: elk hoofdstuk zijn eigen spelvorm

De game volgt Zie Scherp Scherper, en elk blok hoofdstukken is een afdeling van de fabriek. Een deckbuilder is perfect voor types en expressies, omdat een kaart een waarde is: je stelt waarden samen en de game doet de control flow. Voor `if` en loops wringt dat: daar moet de speler zelf regels opstellen die de game uitvoert. Daarom kiest elke afdeling de spelvorm die dezelfde vorm heeft als het concept.

Vier regels houden het één spel:

1. **De vorm volgt het concept.** Een genre komt er alleen in als zijn kernmechaniek het concept *is*: in een gambit-systeem wint de eerste regel die klopt, en dat is een `else if`-keten.
2. **De natuurwetten blijven gelden.** In elke afdeling kapt `int` nog altijd af en loopt `byte` over. Wat je in de Vatenvallei leerde, werkt overal.
3. **De afdelingen voeden elkaar.** Wat je in de ene afdeling bouwt, duikt op in de andere: gambits uit de Controlekamer worden intents van vijanden in de deckbuilder, een blueprint uit de Gereedschapsmuur wordt een kaart. Wie `if` snapt, leest vijanden beter.
4. **Elke afdeling bouwt op wat ervoor kwam** (beslist op 4 oktober 2026). Ze begint met de basis van haar eigen hoofdstuk en wordt stelselmatig complexer, tot puzzels die ook kennis uit vorige hoofdstukken vragen: in de Controlekamer beslissen afkappen en overflow uit H2 welke regels werken. Zo herhaalt de stof zich zonder ooit een herhalingsoefening te zijn.

| Afdeling | Hoofdstuk | Spelvorm | Waarom deze vorm | Echte bug |
| --- | --- | --- | --- | --- |
| The Card Hall, act 1: De Vatenvallei | H2 basis | Roguelike deckbuilder | Getallen: types, afkappen, overflow, deling, voorrang; Omgieten als ervaring | Level 256, The Counter |
| The Card Hall, act 2: De Drukkerij | H3 tekst | Roguelike deckbuilder | Tekst: `string` plakt, `char` is een getal, `Length`, Unicode | Effective Power, Y2K |
| The Card Hall, act 3: De Gieterij | H4 werken met data | Roguelike deckbuilder | Expliciet omzetten: cast kapt af, Convert rondt af, Parse kan crashen, Math als gereedschap | Flight 501, The Index |
| De Controlekamer | H5 beslissingen | Gambits: je stelt de regels van een automaat op ("als een vijand onder 10 HP staat: aanvallen") en kijkt dan hoe hij vecht | De eerste regel die klopt wint: volgorde, `else if`, logische operatoren. `switch` is een tabel van gevallen | goto fail (Apple, 2014), Knight Capital (2012) |
| De Lopende Band | H6 loops | Automatiseringspuzzel: banden en machines die herhalen tot een voorwaarde waar is | Herhaling met een stopvoorwaarde; een band die nooit stopt, is de oneindige lus | Zune (2008): een `while`-lus die op 31 december van een schrikkeljaar nooit stopte |
| De Gereedschapsmuur | H7 methoden | Dezelfde band, met blueprints: een machine één keer bouwen en overal stempelen, met instelknoppen | Hergebruik en parameters; een blueprint wordt ook een kaart in de deckbuilder | nog te kiezen |
| Het Magazijn | H8 arrays | Grid en inventaris: vakken met een nummer, effecten op de buren | Index, lengte, off-by-one dat je meteen ziet | Heartbleed (2014): lezen voorbij het einde van een array |
| Nog te benoemen | H9 en verder: OOP | Tower defense of eigen kaarten ontwerpen: een torentype is een klasse, elke geplaatste toren een object | Klasse en object, overerving, polymorfisme (elke toren doet `Aanval()` op zijn manier) | uit te werken |
| Terug in de deckbuilder | Exceptions, call stack | Roguelike deckbuilder | De beurt is al een call stack, try/catch-kaarten | uit te werken |

**De lijn met visuele programmeeromgevingen.** Een regel in de Controlekamer of een machine op de band is een strategische keuze over wat er moet gebeuren, geen statement dat je regel per regel uitschrijft. Het voorbeeld is Opus Magnum, niet Scratch. Het grootste risico zit in de Lopende Band: die mag niet afglijden naar een opdrachtenlijstje. Dezelfde ontwerptoets geldt: zou iemand zonder interesse in programmeren dit willen spelen?

De Card Hall, de Controlekamer (op 4 oktober 2026, zie [haar map](../afdelingen/control-room/README.md)) en de Lopende Band (op 4 oktober 2026 overgenomen uit [spike 9](../../spikes/09-lopende-band/README.md), zie [haar map](../afdelingen/conveyor-belt/README.md)) zijn gebouwd. Alleen de Card Hall is open voor spelers: de andere twee staan op binnenkort (zie [Ontgrendelen](#ontgrendelen)) tot de Controlekamer af is. De tabel is een richting, geen belofte: elke afdeling moet eerst als spel leuk zijn. De volgende spike is een klein gambit-prototype voor de Controlekamer, omdat dat genre het verst van de deckbuilder ligt, goedkoop te bouwen is en meteen test of gambits als intents in de deckbuilder werken.

## De wereld: de fabrieksplattegrond

De plattegrond verschijnt pas na [de onthulling](#de-onthulling). Tussen de afdelingen zit geen wereld om rond te lopen, maar één scherm: de plattegrond van de fabriek, getekend als het eerste blad van een montagehandleiding. Elke afdeling is een genummerde stap, en elk nummer is een hoofdstuk van het boek. Vier eisen: overzichtelijk, afdelingen ontgrendelen, mastery per afdeling tonen, en toegang tot klassement en achievements.

**Gebouwd op 3 oktober 2026.** De plattegrond, de onthulling en een deel van de teases staan in het spel:

- De afdelingen zijn genummerd naar het eerste hoofdstuk: ② The Card Hall (H2 tot H4, open), ⑤ The Control Room (H5) en ⑥ The Conveyor Belt (H6), allebei binnenkort, ⑦ The Tool Wall, ⑧ The Warehouse en ⑨ The Blueprint Office (H9 en verder), die laatste drie ooit: nog in de doos.
- De onderdelen van een afdeling zijn de Codex-pagina's van haar hoofdstukken. De tekening van een afdeling wordt grijs-naar-ingekt naarmate er meer onderdelen uitgepakt zijn; onder de helft krijgt ze de sticker *loose parts*.
- **Drie soorten dicht** (beslist op 4 oktober 2026, `Availability` in `World/Departments.cs`). *Vrijgegeven:* je verdient de sleutel en de deur gaat open. *Binnenkort:* gebouwd maar nog dicht; de plattegrond toont al naam, spelvorm, uitleg en onderdelen (de tease), en zegt waar de sleutel ligt. *Ooit:* gestippeld, "zit nog in de doos". Een afdeling vrijgeven is één regel in `Departments.All`.
- **De sleutel van de Controlekamer** verdien je met de laatste baas van de Card Hall, ook nu ze op binnenkort staat: je krijgt een melding "Sleutel gevonden", een sticker op de plattegrond, en de dichte pagina zegt dat je de sleutel al hebt. De sleutel blijft bewaard (`PlayerProgress.Unlocks`), dus zodra de Controlekamer vrijkomt, kan wie hem heeft meteen binnen. Wie de onthulling via het vangnet kreeg, heeft hem nog niet.
- De Lopende Band heeft nog geen sleutel: waarschijnlijk de laatste baas van de Controlekamer, zoals de Controlekamer na de Card Hall.
- De Prikklok staat er al, uitgeschakeld tot er een backend is.
- Onthulling: de laatste baas van de Card Hall verslaan, of het vangnet na **5 gestarte runs** (een barst op het titelscherm die je zelf aanklikt).
- Teases die er al zijn: het paginanummer op de map (`p. 2 / 18`), een gestippelde deur met een 5, een zin op het doodscherm (*Behind the door marked 5, something rattles.*) en de dichtgeniete Codex-tabbladen.
- **Onderdelen die nergens voor dienen:** een kist bevat in de helft van de gevallen een onderdeel (een kaart met een regel, een schakel van een lopende band, een haak voor een gereedschapsbord, een etiket voor een bak), elk met "Belongs to step N". Ze gaan in een zakje dat je in de bovenbalk opent, en blijven over runs heen. Na de onthulling staan ze bij hun afdeling op de plattegrond.
- **De vreemde vijand:** *The Stray Automaton*, ontsnapt uit de Controlekamer, zit in de gewone gevechten van act 1 en 2. Zijn intent is een regel, `block > 0 ? 16 : 8`: wie blokt, krijgt het dubbel.
- De [superuser](backend.md#superuser) ziet de plattegrond altijd, ook voor de onthulling.

### Eén scherm

- In het midden de plattegrond, met de afdelingen in de volgorde van het boek. Aan de rand drie vaste knoppen: de **Codex**, de **Prikklok** (klassement) en het **✗-register** (achievements).
- Klik je op een afdeling, dan schuift één paneel open: naam, hoofdstuk, spelvorm, de onderdelenlijst, en twee knoppen: **Spelen** en **Dagelijkse run**.
- Van het openen van de game tot in een spel: hooguit twee klikken. De plattegrond is een keuzescherm, geen menu in een menu.

### Ontgrendelen

- Een gesloten afdeling staat gestippeld getekend, met de onderdelen nog in de zak. Een open afdeling is uitgetekend.
- **Je ontgrendelt een afdeling zelf** (beslist op 3 oktober 2026): ze gaat open als je de baas of eindpuzzel van de vorige verslaat. Geen drempel van gemonteerde onderdelen, en niemand anders kan een afdeling voor je openzetten, ook een docent niet.
- Open blijft open. Een sterke student gaat vooruit, een zwakkere keert terug naar een eerdere afdeling. Dat terugkeren is nooit een straf: elke afdeling blijft even leuk om opnieuw te spelen.
- De startpunten binnen de deckbuilder (een run in act 2 of 3 beginnen) blijven bestaan, binnen de Card Hall.

### Mastery: de onderdelenlijst

Elke montagehandleiding begint met een onderdelenlijst. Bij ons zijn de onderdelen de concepten van dat hoofdstuk, uit de concepttabel van de afdeling. Elk onderdeel heeft drie toestanden:

1. **In de zak.** Nog niet tegengekomen. Je ziet een silhouet.
2. **Uitgepakt.** De regel heeft een gevecht of puzzel beslist; de Codex-pagina is open.
3. **Gemonteerd.** Je hebt de regel in je voordeel gebruikt: zijn Codex-moment gebeurde in een gewonnen gevecht, in drie verschillende runs. Of je versloeg de elite van dat concept (Level 256 voor overflow, Y2K voor concatenatie). Beslist op 3 oktober 2026.

De afdeling op de plattegrond groeit mee: hoe meer onderdelen gemonteerd, hoe meer van de tekening ingekt en ingekleurd. Een volledige afdeling komt tot leven: de schouw rookt, de band draait. Mastery is dus geen cijfer of percentage, maar een fabriek die af raakt.

- **Zwaktes zien.** Een open afdeling met losse onderdelen krijgt een sticker "hapert". Zo ziet een student meteen waar er nog te oefenen valt: "vandaag de Lopende Band".
- **Alleen gedrag telt.** Een onderdeel monteer je door te spelen, nooit door een Codex-pagina te lezen of een vraag te beantwoorden.
- **Geen slijtage.** Gemonteerd blijft gemonteerd. Een fabriek die roest als je een week niet speelt, is een straf voor afwezigheid.

### Prikklok: het klassement

- Per afdeling een dagelijkse seed, met een klasklassement onder anonieme bijnamen. De Prikklok opent op de afdeling waar je het laatst speelde.
- Elke spelvorm meet iets eigens: in de deckbuilder winst en resterende HP, in de Controlekamer het aantal regels, op de band het aantal machines en cycli.
- **De score van een run** (beslist op 3 oktober 2026, `RunScore` in de motor): 100 per verdieping die je voorbij bent, over alle acts heen (een volledige run is 21 verdiepingen). Wie uitspeelt, krijgt er 10 per resterende HP bij, en 5 per beurt onder de 150 (alle gevechten samen). Verliezen geeft geen bonus, dus snel sterven loont nooit, en een gewonnen run scoort altijd meer dan een verloren run. Het eindscherm toont de som, geen geheime formule. De server rekent hem na met `Run.Replay`.
- **Histogram in plaats van ranglijst**, zoals bij Opus Magnum: je ziet waar je oplossing valt tegenover de klas, niet dat je 27ste van 28 bent. Een top 10 van bijnamen kan ernaast, maar het histogram is de standaard.

### Opties: dezelfde knop in elke afdeling

Elke afdeling heeft linksboven dezelfde knop (☰) naar hetzelfde optiescherm, de component `World/OptionsMenu.razor` (sinds 4 oktober 2026). Op een scherm staat de knop vooraan in de bovenbalk, in een gevecht of puzzel in de lege hoek linksboven. Het scherm biedt:

- **Main menu:** terug naar het titelscherm of de plattegrond, zonder de run af te breken. Daar staat dan "Continue run", met waar je was. Een nieuwe run starten gooit de wachtende weg.
- **Restart run:** dezelfde run opnieuw, met dezelfde seed en startpunt. Vraagt eerst bevestiging.
- **Abandon run:** de run stopt en telt nergens mee (geen score, geen verlies). Vraagt eerst bevestiging.
- **Sound:** aan of uit, onthouden op dit toestel (`deckoverflow.muted`). De toets M doet hetzelfde.

Zolang een animatie loopt, kan je het geluid omzetten, maar de run niet verlaten. Een nieuwe afdeling gebruikt dezelfde component en vult de callbacks in voor haar eigen run of puzzel.

## De onthulling

Een student begint Deck Overflow en denkt dat het een gewone deckbuilder is. Pas later ontdekt die dat er een hele fabriek achter de muur ligt, zoals bij Inscryption. In een klas blijft dat geen week geheim, dus we ontwerpen het als een mysterie, niet als een verrassing. Een verrassing is weg zodra iemand ze verklapt. Een mysterie wordt sterker als een klasgenoot zegt: "er zit iets achter die deur."

### Wanneer de muur opengaat

De onthulling mag niet alleen afhangen van winnen, anders zien net de zwakkere studenten de wereld nooit. De eerste van twee voorwaarden die vervuld is, opent de muur:

1. **De laatste baas van de Card Hall verslaan** (act 3, de Caster). Het grote moment: de muur van de Gieterij scheurt open.
2. **Het vangnet.** Na een aantal runs of een bepaalde speeltijd, ook zonder ooit te winnen, merkt de fabriek je op: er verschijnt een barst in de muur, en je kan er zelf doorheen.

### De teaseladder

Elke tease roept een vraag op en legt niets uit. Wie hem mist, mist niets.

| Wanneer | Tease | Waarom het werkt |
| --- | --- | --- |
| Vanaf run 1 | Een paginanummer in de hoek, zoals in een handleiding: *p. 2 / 21*. Op de achtergrond van de map een gestippelde deur met een ⑤ | Waarom 21 pagina's? |
| Runs 1 tot 3 | Af en toe een onderdeel dat nergens voor dient, zoals een tandwiel met "hoort bij stap 6", in een zakje dat je kan openen | Een leeg verzamelvak wil gevuld worden |
| Runs 2 tot 4 | Een vreemde vijand uit een andere afdeling, met een intent die geen getal is maar een regel: "als jij een schild hebt: dubbele schade" | Een eerste smaak van de Controlekamer, in de deckbuilder |
| Na een nederlaag | Soms een geluid achter de deur, een ratelende band, of een briefje dat onder de deur door schuift | Het doodscherm wordt gelezen, je wacht er toch even |
| Codex | Dichtgeniete tabbladen H5 tot H18, alleen met een nummer; bij H5 steekt een hoekje uit (`if … then …`) | Het boek is dikker dan wat je al zag |
| Changelog | "Controlekamer: regels bijgewerkt" | Een administratieve hint, grappig voor wie hem opmerkt |

### Je was al aan het bouwen

De onderdelenlijst wordt vanaf run 1 in stilte bijgehouden. Bij de onthulling zoomt het beeld uit naar de plattegrond, en de Vatenvallei is al half ingekt met de onderdelen die je onbewust monteerde. De tandwielen uit je zakje vallen op hun plaats. De boodschap: je bouwde hier al de hele tijd aan. De wereld is een beloning, geen nieuwe taak.

### Voor en na

- **Voor de onthulling** is er geen plattegrond. De game opent meteen in de deckbuilder, met als titelscherm de kaft van de handleiding. Klassement en ✗-register bestaan, maar alleen voor de deckbuilder, als tabbladen in het spel.
- **Na de onthulling** vervangt de plattegrond het titelscherm, en verhuizen Prikklok en ✗-register naar de plattegrond.
- **Wie het al weet, verliest niets.** De teases lezen dan als voorpret in plaats van als raadsel.
- **Naar buiten toe** mogen product sheet en presentaties voor docenten en instellingen de wereld verklappen. Tegenover studenten zwijgen we erover.

## Open vragen

## Mastery: gemonteerd

Beslist: een moment in een gewonnen gevecht in drie verschillende runs, of de elite van het concept verslaan (zie [Mastery](#mastery-de-onderdelenlijst)). De motor kent de momenten al per gevecht; de shell telt runs.

- [ ] Bouwen: per Codex-pagina de runs tellen, een stempel op de pagina, en de afdeling op de plattegrond die meer ingekt raakt.
- [ ] Nog te beslissen: telt een regel die je alleen *ondergaat* (de Dripper die afkapt) ook, of alleen momenten die jij veroorzaakte? Voorstel: voorlopig alles, en kijken of het te snel gaat.

## De wereld

- [ ] **De Controlekamer met spelers testen**: lezen ze de regels van een automaat, en ontdekken ze dat de volgorde telt? Gebouwd op 4 oktober 2026.
- [ ] **Gambits als intents** in de deckbuilder: de Stray is de eerste. Meer pas als spike 8 toont dat spelers ze lezen.
- [ ] **Spelvorm voor OOP**: tower defense of eigen kaarten ontwerpen.
- [ ] **De plattegrond** als papieren mock testen: ziet een student zonder uitleg waar er te oefenen valt?
- [ ] **Echte bugs** kiezen voor de Gereedschapsmuur (H7). De Controlekamer heeft goto fail en Knight Capital.

## De Prikklok

- **Dagelijkse run** (beslist): altijd vanaf act 1, met `DailySeed.For(datum)`. De eerste voltooide run van de dag telt, zodat er geen reden is om eindeloos opnieuw te starten.
- [ ] **Dagelijkse modifier**, zoals "vandaag geen Ink" of "alle vijanden `checked`".
- [ ] **Zachte streak** voor dagelijkse runs, zonder straf als je een dag mist.
