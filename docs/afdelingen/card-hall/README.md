# The Card Hall: de deckbuilder

Afdeling ② op de plattegrond, hoofdstuk 2 tot 4 van Zie Scherp Scherper. Een roguelike deckbuilder in drie acts. Per act: [act 1](act-1.md) (H2), [act 2](act-2.md) (H3), [act 3](act-3.md) (H4). Verder: [kaarten en relics](kaarten.md), [events van de motor](events.md), [ideeën en open vragen](../../../ideeen.md#the-card-hall).

## Core loop

Dit is de loop van de deckbuilder, The Card Hall. De andere afdelingen krijgen elk een eigen loop, die eerst als spike moet bewijzen dat hij leuk is.

Een run begint in Act 1, en je probeert zo ver mogelijk te geraken. Eén act duurt 10 tot 15 minuten. De deckbuilder heeft drie acts (H2, H3 en H4), dus een volledige run duurt 30 tot 45 minuten: één act past in een stukje les, een hele run in een lesblok. Elke act die je in een run bereikt, wordt een startpunt: een volgende run mag daar beginnen, met een deck dat je eerst draft uit de kaarten van de vorige acts. Meta-progressie is kennis van de speler en startpunten, nooit extra kracht.

- **Een act** is een map van 6 rijen met een baas erboven.
- **Na de baas** heel je volledig en kies je 1 baasrelic uit 3. Dan begint de volgende act.
- **Een start in een latere act** begint met het starterdeck, vijf keer 1 kaart uit 3 uit de pool van de vorige acts, 1 relic uit 3, volle HP en 100 goud. Ongeveer wat je had gehad als je had doorgespeeld.

1. **Map kiezen.** Een vertakte kaart met gevechten, elites, rustplekken, winkels en mysterie-events. Elk pad heeft een andere mix van risico en beloning.
2. **Vechten.** 3 energie, 5 kaarten per beurt. Elke vijand toont zijn intent voor de volgende beurt.
3. **Beloning kiezen.** 1 kaart uit 3, goud, soms een relic. Je deck wordt sterker of juist rommeliger.
4. **Codex ontgrendelen.** Was een C#-regel beslissend in dat gevecht, dan opent er een pagina.
5. **Herhalen tot de baas.** Sterf je, dan eindigt de run. De Codex blijft, en daarmee je inzicht.

### Intents: rekenen is gereedschap, nooit de taak

De C#-regels moeten beslissingen veranderen, niet elke beurt een som opleggen. Verplicht hoofdrekenen om te weten hoeveel je moet blokken, is een quiz met een zwaard erop.

- **Verdediging is leesbaar.** Een intent toont standaard het totaal, groot, zoals in Slay the Spire. De expressie staat er klein onder. Een dodelijk-icoon toont wanneer een intent je zou doden.
- **Gebouwd op 3 oktober 2026:** bewuste intents met de variabelen `block`, `cards` (deze beurt gespeeld), `energy` (over) en `hp`. The Splitter slaat `30 / (block + 1)`, de Tin Knight `24 / (cards + 1)`, de Dripper `energy * 4 + 2.5`. De ballon toont de formule met namen; het totaal springt mee terwijl je speelt. Bij de aanval telt wat je aan het eind van je beurt hebt.
- **Leren door te kijken.** Tijdens de vijandbeurt wordt de expressie zichtbaar stap voor stap uitgerekend. Spelers zien operatorvoorrang honderden keren gebeuren, zoals ze in Balatro het scoresysteem leren door te kijken.
- **Verdedigen is variabelen manipuleren.** De interessante intents bevatten variabelen die de speler beïnvloedt, zoals `20 / x` met `x` jouw aantal schilden. Het totaal past zich live aan terwijl je kaarten overweegt. De vraag is niet "hoeveel is dit?" maar "welke variabele verander ik het goedkoopst?"
- **In de aanval is hoger niet altijd beter.** Volgorde telt ("+3" en "×2" in de juiste volgorde), te veel kan fout zijn (overflow, de Bool-schim, de Ritmeschildpad) en types sturen keuzes (decimalen zijn verspild op een `int`-vijand). Hier zit de puzzel, en die is leuk omdat je iets wil maximaliseren.
- **Echt rekenen alleen als hoogtepunt.** De Rekenmeester is het enige gevecht waarin het totaal verborgen blijft. Eén keer per act voelt dat als uitdaging; elke beurt zou het voelen als huiswerk.

## Kernsysteem: types en omgieten

De eerste playtest leerde dat één regel per vijand niet schaalt. De Byte-Golem ontdekken was leuk, maar gewone gevechten voelden als Slay the Spire: schade doen en blokken. C# moet in elk gevecht zitten, niet in uitzonderingen. Daarom rust de game op twee systemen die overal terugkomen.

### Alles heeft een type

Er bestaan geen gewone monsters meer. Elke vijand heeft een type, en elke aanval ook. Elk gevecht stelt zo de vraag: welke kaart werkt op dit type?

| Type van het doelwit | Gedrag |
| --- | --- |
| `int` | Kapt decimalen van inkomende schade af |
| `double` | Neemt elke waarde exact, decimalen inbegrepen |
| `byte` | Klapt om boven 255 |
| `string` | Plakt getallen aan zijn waarde in plaats van ze op te tellen |
| `bool` | Kent maar twee toestanden: elke treffer draait hem om |

### Ook je aanval heeft een type

Een aanval is geen kaal getal: het is een waarde met een type, die door je modifiers stroomt. C# rekent elke stap uit volgens de echte regels, en pas daarna landt de waarde op het type van het doelwit. Zo zit H2 en H3 in elke beurt, niet alleen in het moment van aankomst.

| Je speelt | Expressie | Resultaat | Concept |
| --- | --- | --- | --- |
| Split (`/ 2`), daarna Whack 7 | `7 / 2` | 3 | deling van gehele getallen |
| Floating Point, Split, Whack 7 | `7.0 / 2` | 3.5 | één `double` maakt de expressie `double` |
| hetzelfde op een `int`-vijand | | 3 | decimalen sneuvelen bij aankomst |
| Remainder (`% 5`), daarna Whack 7 | `7 % 5` | 2 | modulo |
| Ink `"1"`, daarna Ink `"2"` | `"1" + "2"` | `"12"` | tekst plakt |
| Ink `"4"`, daarna Whack 2 | `"4" + 2` | `"42"` | een getal plakt mee als tekst |
| daarna Read | `int.Parse("42")` | 42 | tekst wordt een getal |
| Letter `'A'`, daarna Spare Screw | `'A' + 3` | 68 | een `char` is een getal |

- **Het totaal staat groot, de expressie klein.** Net als bij een intent past het totaal zich live aan terwijl je kaarten overweegt, en tijdens het spelen wordt de expressie stap voor stap uitgerekend. De speler hoeft niets zelf te rekenen.
- **Tekst raakt geen getal.** Een tekstaanval op een getal-vijand compileert niet (`hp - "42"`), dus de kaart weigert. Eerst Read spelen.
- **Begrijpen is de sterkste strategie.** Met getallen is 1 + 2 maar 3; als tekst wordt het 12. Wie tekst aan elkaar plakt en pas dan omzet, slaat het hardst (het archetype Schrijver). Op een `byte`-vijand loopt `"300"` dan weer over.
- **Gebouwd op 3 oktober 2026:** Split, Floating Point, Ink en Read. Modifiers blijven wachten tot je volgende kaart, ook over je beurt heen. Wie ze kwijt wil, veegt ze weg met **Scrap** voor 1 energie (beslist op 3 oktober 2026, na een playtest: Ink tegen een getal maakte van elke kaart tekst, ook Shield, en zonder Read zat je vast). Read is zeldzaam en kost 2, want Ink, Spare Screw en Read maken van één Whack `int.Parse(6 + "1" + 3)`, 613 schade. Letterkaarten (`'A' + 1`) wachten nog: een `char` is meteen 65 of meer, en dat moet eerst gebalanceerd worden.
- **Read is eigenlijk `int.Parse`**, dus H4. Net als Omgieten ervaar je het in act 1 onder een wereldnaam, en geeft de Codex het in act 2 zijn echte naam.
- **Geen Scratch-probleem.** De deckbuilder is één spelvorm binnen een grotere wereld, dus hij mag dicht tegen code aan zitten. Je bouwt wel altijd één aanval met hooguit een paar modifiers, geen programma. De vraag aan de speler blijft: hoe maak ik dit getal zo groot (of zo klein) mogelijk?

### Omgieten

Omgieten verandert het type van een vijand. Het veralgemeent wat de golem leuk maakte: niet één puzzelvijand, maar een gereedschap voor elk gevecht.

- **Naar `byte`:** daarna laat helen elke vijand omklappen, niet alleen de golem.
- **Naar `int`:** een vijand met een decimaal schild verliest het restje bij elke treffer.

Dit is de C#-versie van "kwetsbaar maken en dan hard slaan", maar de speler ontdekt zelf welke vorm bij welke vijand past.

**Het spel loopt voor op het boek.** Casting komt in Zie Scherp Scherper pas in hoofdstuk 4. In de game heet het daarom Omgieten, een wereldnaam zonder het woord "cast". De Codex-pagina met de echte naam en de link naar het boek opent pas wanneer hoofdstuk 4 aan bod komt. Dat is de pijler "eerst ervaren, dan benoemen" in zijn zuiverste vorm.

### Later, als de basis staat

- **Statuseffecten met C#-betekenis**, één voor één: Memory leak (HP-verlies dat elke beurt groeit met `++`) eerst, daarna Readonly, Unchecked en Null.
- **Operatorvoorrang in je eigen aanval**: nu werken modifiers van links naar rechts. Later kan een kaart als Haakjes de volgorde breken, zodat `3 + 2 * 4` en `(3 + 2) * 4` een keuze worden in je eigen beurt, niet alleen in de intents van de Rekenmeester.

## Run-variatie en loot

De juice maakt elke klik leuk, de loot maakt dat je nog een run wil. Elke run is een ander deck, en de beste runs ontstaan uit combinaties die je pas ziet als je de regel snapt.

- **Kiezen uit drie.** Na elk gevecht 1 kaart uit 3, of niets. Een slank deck is ook een strategie.
- **Risico op de map.** Elites geven betere loot dan gewone gevechten, rustvuren genezen. Spelers doseren zelf hun hebzucht.
- **Elites als leerrand.** Een elite rond een concept dat je nog niet beheerst is riskant, maar de beloning is groter. Zo zoeken spelers zelf hun grens op.
- **Codex voedt de pool.** Ontdek je een regel, dan komen er kaarten en relics in de loot-pool die erop voortbouwen. Inzicht wordt letterlijk meer speelgoed.
- **Legendarische vondsten veranderen je speelstijl, niet de regels.** Ze maken je machtig binnen C#, nooit erbuiten.

### Archetypes zijn concepten

Elk deck-archetype draait rond één C#-regel. Wie een archetype kiest, doorgrondt dat concept vanzelf, omdat de run ervan afhangt.

| Archetype | Concept | Kernstukken | Jackpot-combo |
| --- | --- | --- | --- |
| Ritme | Modulo `%` | Teller, Restzak, ritmekaarten | Restzak + Splitsslag op een oneven aantal vijanden: gratis schade elke beurt |
| Overloper | Overflow | Herstel-kaarten, Byte-relics | Vijanden helen tot ze klappen, zodat hoge HP een zwakte wordt |
| Smelter | Casting | Smeltkroes, Vlottende kaarten | Dure Vlottende kaarten omgieten tot gratis afgekapte kaarten |
| Schrijver | `string` en concatenatie | Inkt, Stempel | Tekstwaarden aan elkaar plakken tot één enorm getal en dan pas omzetten |
| Rekenmeester | Operatorvoorrang | Haakjes, Voeg toe | Volgorde van je combo herschikken zodat de vermenigvuldiging als laatste valt |

**De run die breekt.** Ergens in act 2 vallen twee relics en een kaart samen tot een combo die alles platwalst. Dat moment moet in elk archetype bestaan, en het moet alleen zichtbaar zijn voor wie het concept snapt.

## Exceptions en de call stack

**In beeld heet het crashen** (beslist op 4 oktober 2026). Het boek gebruikt in H2 tot H4 het woord *crashen*; *exception* komt pas in het hoofdstuk over exception handling (H10 in de Codex). Dus tonen stage, log en panelen "crasht", nooit `FormatException` of `OverflowException`, en een crash opent de Codex-pagina *Exceptions* niet. Het gedrag blijft hetzelfde: een crash beëindigt je beurt. De namen in de tabel hieronder zijn voor de ontwikkelaars, en voor later, als try/catch er komt.

Fouten zijn natuurwetten, geen straf van het spel: een exception treft wie ze veroorzaakt. De speler kan erin trappen, maar kan een vijand er ook in lokken.

### De beurt is een call stack

- Elke gespeelde kaart komt bovenop de stapel van de beurt.
- Gooit een kaart een exception, dan wikkelt die de stapel af en annuleert alles erboven, tot ze een catch tegenkomt.
- Niet opgevangen: de beurt crasht. Beurt voorbij, plus schade.
- Te hoog stapelen geeft een `StackOverflowException`. Dat is de natuurlijke rem op eindeloze combo's.
- **Compilefouten zijn geen exceptions.** Een kaart op een doelwit van het verkeerde type leggen kan gewoon niet: de kaart kleurt rood en weigert. Alleen runtime-fouten ontploffen. Zo voelen spelers het verschil tussen "mag niet" en "loopt mis tijdens het draaien".

| Exception | Wanneer | Voorbeeld | Act |
| --- | --- | --- | --- |
| `DivideByZeroException` | Een deling door nul | De Rekenmeester valt aan met `20 / x`, met `x` jouw aantal schilden. Maak `x` nul en hij ontploft in zijn eigen formule | 1 |
| `IndexOutOfRangeException` | Een positie buiten bereik | "Raak positie 3" bij posities 0 tot 2: de aanval raakt jou | 1 |
| `FormatException` | Tekst die geen getal is omzetten | Inkt op "abc" ontploft in je gezicht | 1 |
| `StackOverflowException` | De stapel wordt te hoog | Te veel kaarten in één beurt | 1 |
| `NullReferenceException` | Een kaart target iets dat `null` is | Een schimvijand is onraakbaar tot je hem met `new` concreet maakt | Later, rond objecten |

In Act 1 bestaan exceptions al, maar is er nog geen catch: een crash beëindigt gewoon je beurt. Try/catch-kaarten en blueprints verschijnen vanaf de act rond exceptions.

### Blueprints: patronen als standaard

Blueprints zijn een derde soort loot, naast kaarten en relics. Ze leggen een codepatroon rond elke beurt, zodat een speler niet telkens dezelfde lange stapel hoeft te bouwen. Er zijn maximaal 3 blueprint-slots.

**Eerst zelf, dan automatiseren.** Een blueprint valt pas als loot nadat je het patroon een aantal keer met losse kaarten gebouwd hebt, over runs heen. Een teller in de Codex toont hoe ver je staat, bijvoorbeeld Vangnet 3/5. Het aantal per blueprint is een balansknop. Net als bij een methode: wie hetzelfde stuk code telkens opnieuw schrijft, wil het op den duur hergebruiken.

| Blueprint | Patroon | Effect | Keerzijde |
| --- | --- | --- | --- |
| Vangnet | `try`/`catch` | Een catch ligt automatisch onderaan elke beurt en vangt de eerste exception | Vangt ook exceptions die je bewust wilde laten doorborrelen voor een combo |
| Altijd | `finally` | Na elke beurt, ook na een crash, 3 blok | Neemt een kostbaar slot in |
| Wachter | `if`-guard | Kaarten op een ongeldig doelwit worden overgeslagen in plaats van te crashen | De overgeslagen kaart is verspild |
| Terugval | `??` | Een `null`-doelwit wordt de dichtstbijzijnde vijand | Je kiest je doelwit niet meer zelf |
| Veilig Parsen | `TryParse` | Inkt crasht nooit; ongeldige tekst wordt 0 | 0 schade in plaats van een crash |

Elke blueprint heeft een echte keerzijde, zodat kiezen een afweging blijft. Blueprints buigen de regels niet: het zijn gewone C#-patronen die binnen de regels werken.

## Exploits, patches en versies

Een killercombo die je binnen een run vindt, is de jackpot en mag voelen als slim. Het probleem is pas dat dezelfde combo elke run werkt: dan stopt het plezier en oefen je nog maar één concept. Daarom reageert de wereld, zoals echte software.

### De wereld patcht zich

Elke nerf wordt zelf een nieuw C#-concept.

- **`checked`:** na een paar overwinningen met overflow verschijnen vijanden in een `checked`-context. Helen voorbij 255 gooit dan een `OverflowException`, die terugketst op jou.
- **`const` en `readonly`:** wie zwaar op Zet-kaarten leunt, krijgt vijanden met `const`-waarden. Toekennen is een compilefout: de kaart weigert.
- **Breder type:** de `long`-golem klapt pas om bij een getal dat je met helen nooit haalt.

Narratief brengen we het als patch notes: "De Vallei heeft een patch uitgerold." Studenten ervaren zo dat elk lek een oplossing heeft.

In de beeldtaal van de handleiding is een patch een nieuwe revisie. Het paneel dat je misbruikte, krijgt een stempel: "Rev. B: vul een byte-vat niet voorbij 255." De fabriek is daarmee de tegenspeler met een gezicht.

De patch zit in de MVP. In Act 1 gebruikt hij `const` (H2): na een paar keer Wrong Label op dezelfde soort vijand verschijnt een revisie die zijn aanval `const` maakt, en de kaart weigert.

### Patch-gebeurtenissen

- **De slechte patch.** Een act-variant op een buggy build, met glitch-effecten, een flikkerende HUD en kapotte regels vol exploits. De Codex zegt expliciet dat dit géén C# is: het is een kapotte wereld, geen kapotte taal. Pas laat in de game, als de echte regels ingesleten zijn.
- **De Franse patch.** Kaartteksten worden Frans, maar cijfers en symbolen kloppen nog, zodat spelers op symbolen leren lezen. De echte tand: Inkt leest "4,5" anders volgens de cultuur. Dat is de `CultureInfo`-valkuil, al spelend ontdekt.

| Patch-relic | Effect |
| --- | --- |
| Rollback-schijf | Draai de laatste patch één gevecht terug |
| Hotfix | Zet één vijand in `unchecked`, zodat overflow weer werkt |
| Changelog | Toont welke patch er volgende act aankomt |
| Bug Bounty | Elke nieuwe exploit die je vindt, levert goud op |
| Feature Flag | Zet één regel per gevecht aan of uit |

### C#-versies

Kaarten dragen een badge met de versie waarin hun feature verscheen, zoals "C# 6+". In een legacy-zone, of bij de baas De Legacy-koning, draait alles op een oude versie:

- Kaarten die op nieuwere features steunen, zoals string-interpolatie, `?.` of switch-expressies, worden grijs met een compilefout.
- Andere vallen terug op oud gedrag: een interpolatie-kaart wordt concatenatie met `+`, en meteen sluipt de string-valkuil weer binnen.
- Blueprints sneuvelen ook: Terugval werkt niet zonder `??`.

Het omgekeerde bestaat als zeldzame zone **Preview-features**: sterke experimentele kaarten die soms een exception gooien. Spelers voelen zo dat een taal een geschiedenis heeft.

### `[Obsolete]`: zeldzame slijtage

Een algemene slijtageteller op elke kaart wordt boekhouding. Daarom alleen dit: een kaart of combo die je over runs heen heel vaak gebruikt, krijgt eerst `[Obsolete]` als gele waarschuwing. Blijf je hem gebruiken, dan wordt het een rode fout en verzwakt hij.

Terugdraaien kan op drie manieren:

| Manier | Prijs |
| --- | --- |
| Refactor aan het rustvuur | Je geneest niet: een echte afweging, zoals helen tegenover upgraden |
| Winkel | Goud: sneller, maar duur |
| Laten afkoelen | Gratis: de markering verdwijnt na een paar runs zonder die kaart |

### Verleiden in plaats van straffen

- **Bonus voor variatie:** meer goud of snellere ontgrendelingen voor een archetype dat je weinig speelde.
- **Dagelijkse run met modifier:** "vandaag geen modulo" of "alle vijanden zijn `checked`".
- **Ascensie als compilerinstellingen:** elk niveau zet een vlag aan, zoals `checked` overal of strengere typecontrole. Moeilijker spelen is letterlijk strengere C#.

Het roguelike-format remt al vanzelf: een combo vraagt specifieke kaarten en relics, en die zijn nooit gegarandeerd.

## Inhoud voor de eerste playtest

Gebouwd op 3 oktober 2026, zodat een run gevarieerd genoeg is om te testen.

**Events.** Naast de Smeltkroes en het Lekkende Vat:

| Event | Act | Keuze | C#-knipoog |
| --- | --- | --- | --- |
| The Copy Machine | 1 en 2 | Kopieer een kaart, verlies 6 HP | De handleiding verbiedt kopiëren |
| The Scrap Bin | 1 en 2 | Verwijder gratis een kaart, of neem 40 goud | Deck slanker maken |
| The Rounding Desk | 2 | Laat je goud afronden op honderdtallen met `Math.Round` | Bankiersafronding: 150 wordt 200, 250 ook 200, 50 wordt 0. De keuze toont vooraf wat er gebeurt |

**Relics.** Naast de vijf van act 1:

| Relic | Effect | Concept |
| --- | --- | --- |
| Ink Well | Je eerste Ink of Read per gevecht kost 0 | `string` |
| Tally Counter | Na elk gewonnen gevecht heel je `++count`: 1, dan 2, dan 3 | `++` |
| Coin Mold | 50% meer goud uit gevechten, afgerond met `Math.Round` | Afronden |

**Sfeer en schermen.** Een titelscherm met een nieuwe sfeerplaat (imagen, GPT-5.4 Image 2, het scènevel als stijlreferentie) en het oranje van Zie Scherp Scherper (#e2790a) als enige accent; een intro van drie zinnen bij de allereerste run ("Not by the manual"); een actkaart met plaat, naam en één zin sfeer bij elke nieuwe act; en een banner van de actplaat boven de map. De platen staan in `art/wide/`, als lichte `.webp` in `wwwroot/art/wide/`.
