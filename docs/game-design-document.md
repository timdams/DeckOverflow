# Deck Overflow: Game Design Document

1 oktober 2026 · Tim Dams

## Visie

Deck Overflow is een spel in de browser waarin de wereld gehoorzaamt aan C#. Wie de regels doorheeft, wint. Het spel moet verslavend zijn in de goede zin: je wil nog één run spelen, en elke run maakt je beter in C#.

Het hart is een roguelike deckbuilder. Rond dat hart ligt een fabriek met afdelingen, één per blok hoofdstukken, en elke afdeling heeft de spelvorm die het best past bij wat ze aanleert. Een deckbuilder voor types en expressies, een regelsysteem voor `if`, een lopende band voor loops. Alles hangt samen op één plattegrond waarop je ziet hoe ver je fabriek af is.

- **Doelgroep:** eerstejaars programmeren, met de zwakkere studenten als ontwerpmaat. Iedereen mag spelen, ook leerlingen uit het middelbaar. Wie Slay the Spire of Balatro kan spelen, moet dit kunnen spelen.
- **Bron:** de leerlijn van Zie Scherp Scherper. Elke afdeling volgt een blok hoofdstukken.
- **Wat we bewust niet maken:** geen gamification (punten en badges op oefeningen), geen visuele programmeeromgeving. Een eerste versie met kaarten als codestatements is verworpen: dat was Scratch met een zwaard, een dunne laag over code schrijven. Ook de afdelingen waarin je regels of machines bouwt, blijven aan de goede kant van die lijn: zie [Afdelingen](#afdelingen-elk-hoofdstuk-zijn-eigen-spelvorm).
- **De kern:** C#-semantiek zijn de natuurwetten. Types, operatoren, overflow en scope bepalen hoe gevechten verlopen. Begrijpen is de sterkste strategie.

## Pijlers

Deck Overflow is eerst een goede deckbuilder en pas daarna een leermiddel. Wat zonder leerdoel niet leuk is, gaat eruit.

1. **Echte keuzes, echt risico.** Runs, een map met paden, deckbouw-afwegingen en een run die kan mislukken. Geen veilige oefenmodus als standaard. In de andere afdelingen geldt hetzelfde: een oplossing kan mislukken, en er zijn er altijd meerdere.
2. **C# is natuurkunde, geen leerstof.** De regels zijn altijd consistent en worden nooit vooraf uitgelegd. Je botst erop, net als op zwaartekracht.
3. **Begrijpen is de sterkste strategie.** Wie het concept doorheeft, wint efficiënter. Brute kracht mag, maar kost HP, goud of kaarten.
4. **Geen code in beeld in Act 1.** Kaarten tonen getallen, letters en woorden. Code verschijnt pas, optioneel, in de Codex. Over de hele game loopt een helling: eerst waarden ervaren (de deckbuilder), dan regels in symbolen bouwen (Controlekamer, Lopende Band, met een optionele C#-weergave), en pas bij OOP echte code als volwaardige laag.
5. **Eerst ervaren, dan benoemen.** De naam van een concept komt na het moment waarop het je een gevecht won of kostte.

**Ontwerptoets voor elke encounter:** zou iemand zonder enige interesse in programmeren dit gevecht nog willen spelen? Nee betekent herwerken.

**Tweede toets:** rekent de speler omdat het moet, of omdat hij iets wil bereiken? Alleen het tweede mag.

## Thema: niet volgens de handleiding

De Vatenvallei is een fabriek die alles bouwt volgens de specificatie. De speler is het figuurtje uit de montagehandleiding, en die doet precies wat de ✗-panelen verbieden. Wat de handleiding verbiedt maar C# toelaat, werkt: C# is natuurkunde, de handleiding is maar papier.

**Waarom.** In spike 6 was alleen het rekenwerk C#. Kaarten heetten Strike en Block, iconen waren zwaarden en schilden: een middeleeuwse Slay the Spire. Het thema moet zelf uit software komen, niet alleen de getallen.

- **De held** is het figuurtje zonder gezicht uit een handleiding. Geen krijger, maar iemand die de verboden stappen leest als tips.
- **De fabriek** is de tegenspeler. Gewone vijanden zijn producten van de lopende band. Wie een truc te vaak gebruikt, krijgt een nieuwe revisie van de handleiding (zie De wereld patcht zich).
- **Elites zijn echte bugs** uit de geschiedenis. Na het gevecht vertelt de Codex het echte verhaal. Bazen blijven figuren van de fabriek, zoals de inspecteur.
- **Toon:** droog en deadpan, zoals een testfaciliteit die alles volgens protocol doet. Grappig voor volwassenen, nooit schattig.
- **Beeldtaal:** zwarte lijnen op papier, één schreefloze letter, stapnummers. Kleur is voorbehouden aan types: zie je kleur, dan zie je een type. Een verslagen vijand valt uiteen in een explosietekening.

### Kaartnamen

Elke kaartnaam is een instructie of een overtreding, nooit een wapen. "Floating" blijft het woord voor `double`, omdat het later naar `float` en `double` verwijst. De spelnamen zijn Engels; de werktitels in dit document blijven Nederlands.

| Werktitel | Spelnaam | Wat je ziet |
| --- | --- | --- |
| Slag | Whack | het figuurtje klopt met de vlakke hand op een machine |
| Zware Slag | Hammer It In | een pin die te groot is voor het gat |
| Schild | Hold Firmly | het figuurtje houdt een kantelende kast tegen |
| Dik Schild | Two-Person Lift | twee figuurtjes tillen samen |
| Herstel | Fill Past the Line | een vat met een maxstreep, de vloeistof erboven |
| Voeg toe | Spare Screw | het schroefje dat altijd overblijft |
| Dubbel | Second Pair of Hands | het figuurtje met vier armen |
| Giet om | Force Fit | een ronde pin in een vierkant gat |
| Zet op 1 | Wrong Label | een nieuw etiket over het oude |
| Vlottende Slag | Floating Bolts | drie bouten die zweven |

### Elites: echte bugs

Gewone vijanden zijn producten van de fabriek, bazen zijn figuren van de fabriek. Elites zijn beroemde softwarefouten, elk bij een concept uit hun act.

| Elite | Act | Echte bug | Concept |
| --- | --- | --- | --- |
| Level 256 | 1 | Pac-Man (1980): het levelnummer is een `byte`. Op level 256 loopt het over en wordt de rechterhelft van het doolhof rommel. | `byte`-overflow |
| Effective Power | 1 | iPhone (2015): één bericht met een reeks Arabische Unicode-tekens liet het toestel crashen zodra het die tekst probeerde te tonen. | tekst, Unicode |
| Flight 501 | 2 | Ariane 5 (1996): een `double` werd omgezet naar een 16-bits geheel getal, dat liep over. De raket vernietigde zichzelf na 37 seconden. | casting |
| The Index | 2 | Vancouver Stock Exchange (1982): de index werd na elke berekening afgekapt in plaats van afgerond en zakte in 22 maanden tot ongeveer de helft van zijn echte waarde. | afronden |
| Zune | 4 | Zune (2008): alle spelers van één model bevroren op 31 december, de 366e dag van een schrikkeljaar, in een `while`-lus die nooit stopte. | loops |
| Heartbleed | 6 | OpenSSL (2014): een server las voorbij het einde van een buffer en stuurde geheime data mee terug. | arrays |
| The Counter | later | Gangnam Style (2014): YouTube zette zijn weergaventeller van 32 naar 64 bits omdat de maximumwaarde in zicht kwam. | `int`-overflow |

We gebruiken geen bugs met doden (zoals de Patriot-raket in 1991). "Nuclear Gandhi" is een mythe; die kan hoogstens als Codex-grap: deze bug heeft nooit bestaan. Geen merknamen of logo's in beeld: een speelhalkast, geen Pac-Man.

## Core loop

Dit is de loop van de deckbuilder, de afdelingen Vatenvallei en Gieterij. De andere afdelingen krijgen elk een eigen loop, die eerst als spike moet bewijzen dat hij leuk is.

Een run begint in Act 1, en je probeert zo ver mogelijk te geraken. Eén act duurt 10 tot 15 minuten. De deckbuilder heeft voorlopig twee acts (H2-H3 en H4), dus een volledige run past ruim in een lesblok. Elke act die je in een run bereikt, wordt een startpunt: een volgende run mag daar beginnen, met een deck dat je eerst draft uit de kaarten van de vorige acts. De docent kan een act ook voor de hele klas vrijgeven. Meta-progressie is kennis van de speler en startpunten, nooit extra kracht.

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
- **Gebouwd op 3 oktober 2026:** Split, Floating Point, Ink en Read. Modifiers blijven wachten tot je volgende kaart, ook over je beurt heen. Read is zeldzaam en kost 2, want Ink, Spare Screw en Read maken van één Whack `int.Parse(6 + "1" + 3)`, 613 schade. Letterkaarten (`'A' + 1`) wachten nog: een `char` is meteen 65 of meer, en dat moet eerst gebalanceerd worden.
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

## Verslavingsmotor: de trucs van de foor

We gebruiken bewust alles wat roguelikes, slotmachines en mobiele games zo kleverig maakt, maar elke truc wijst naar spelen en begrijpen, nooit naar geld of schuldgevoel.

| Truc | Hoe het werkt | In Deck Overflow |
| --- | --- | --- |
| Nog één run | Korte sessies, nul drempel om opnieuw te beginnen | Run van 25 tot 35 minuten, herstart in één klik en onder 3 seconden |
| Bijna-gewonnen | Een nipt verlies trekt harder dan een zwaar verlies | Doodscherm toont hoeveel HP de vijand nog had en welke kaart het verschil had gemaakt |
| Variabele beloning | Onvoorspelbare beloningen houden de aandacht vast | Zeldzaamheden gewoon, ongewoon, zeldzaam, legendarisch, elk met eigen glans en geluid |
| Anticipatie | De opbouw voor de onthulling is het lekkerste deel | Kist trilt, kleur lekt door de kieren voor hij opengaat; kaartbeloningen draaien één voor één om |
| Pity timer | Pech mag niet te lang duren | Na een reeks kisten zonder zeldzame vondst is de volgende gegarandeerd zeldzaam |
| Altijd vooruitgang | Ook verlies moet iets opleveren | Na elke run vult een balk die nieuwe kaarten, relics of een tweede personage ontgrendelt |
| Verzamelalbum | Lege vakjes willen gevuld worden | Codex en kaartcollectie tonen silhouetten van wat je nog niet ontdekte |
| Moeilijkheidstreden | Na winst wacht een nieuwe berg | Ascensie-niveaus die telkens één regel strenger maken |
| Dagelijkse run | Een vaste reden om terug te komen | Zelfde seed voor iedereen, klassement per klas; docent kan een klas-seed zetten |
| Zachte streak | Ritme opbouwen | Teller voor opeenvolgende dagelijkse runs, zonder straf als je een dag mist |
| Geheimen | Geruchten verspreiden zich op de speelplaats | Verborgen vijanden en routes, zoals een geest die alleen verschijnt op een zeldzaam pad |
| Deelbare momenten | Opscheppen is brandstof | Seed en eindscherm van een gebroken run delen met één klik |
| Prestaties | Kleine doelen naast het grote | Grappige achievements voor spelprestaties, zoals een elite verslaan met één kaart |

**Grens.** Geen echt geld, geen loot boxes, geen FOMO-timers die studietijd verdringen, geen straf voor afwezigheid. Een docent kan een maximale speelduur per dag instellen. Verslavend is het doel, schadelijk niet.

## Game feel

De juice volgt het begrip: hoe beter je een C#-regel uitbuit, hoe harder het spel reageert. Zie [analyse van Balatro](https://blakecrosley.com/guides/design/balatro) en [hit pause](https://bugnet.io/blog/how-to-use-hit-pause-to-make-impacts-feel-powerful).

1. **Intents rekenen zichtbaar uit.** Bij `3 + 2 × 4` licht eerst `2 × 4` op met een tik, dan de `+`. Elke stap een hogere toon: operatorvoorrang als ritme.
2. **Grootste knal voor regel-exploits.** Level 256 tikt naar 255, alles bevriest, het getal rolt naar 0, bas-drop. Een gewone grote slag voelt daarnaast bescheiden.
3. **Regels altijd hoor- en zichtbaar.** Bij `int`-afkappen breekt de decimaal af en valt rinkelend weg; bij integer deling rolt de rest als munt in de Restzak.
4. **Fouten grappig, niet bestraffend.** De "5" stempelt achter de "20" en de Papieren Golem zwelt op tot "205".
5. **Types hebben een vaste kleur en vorm**, zodat je een `double` of `string` in één blik leest, ook bij kleurenblindheid.
6. **Combo's als kettingreactie.** Kaarten en relics vuren na elkaar af met een stuiter en stijgende toon. Schudden en hit pause schalen met de impact. Snelle modus voor ervaren spelers.

Grens: juice beloont altijd een inzicht of slimme zet, nooit puur toeval.

## Exceptions en de call stack

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
- **Klas-seed:** een docent kan tijdelijk een archetype uitsluiten.

Het roguelike-format remt al vanzelf: een combo vraagt specifieke kaarten en relics, en die zijn nooit gegarandeerd.

## Afdelingen: elk hoofdstuk zijn eigen spelvorm

De game volgt Zie Scherp Scherper, en elk blok hoofdstukken is een afdeling van de fabriek. Een deckbuilder is perfect voor types en expressies, omdat een kaart een waarde is: je stelt waarden samen en de game doet de control flow. Voor `if` en loops wringt dat: daar moet de speler zelf regels opstellen die de game uitvoert. Daarom kiest elke afdeling de spelvorm die dezelfde vorm heeft als het concept.

Drie regels houden het één spel:

1. **De vorm volgt het concept.** Een genre komt er alleen in als zijn kernmechaniek het concept *is*: in een gambit-systeem wint de eerste regel die klopt, en dat is een `else if`-keten.
2. **De natuurwetten blijven gelden.** In elke afdeling kapt `int` nog altijd af en loopt `byte` over. Wat je in de Vatenvallei leerde, werkt overal.
3. **De afdelingen voeden elkaar.** Wat je in de ene afdeling bouwt, duikt op in de andere: gambits uit de Controlekamer worden intents van vijanden in de deckbuilder, een blueprint uit de Gereedschapsmuur wordt een kaart. Wie `if` snapt, leest vijanden beter.

| Afdeling | Hoofdstuk | Spelvorm | Waarom deze vorm | Echte bug |
| --- | --- | --- | --- | --- |
| De Vatenvallei (act 1) | H2 basis, H3 tekst | Roguelike deckbuilder | Kaarten zijn waarden en expressies: types, afkappen, overflow, tekst die plakt; Omgieten als ervaring | Level 256, Effective Power |
| De Gieterij (act 2) | H4 werken met data | Roguelike deckbuilder | Expliciet omzetten: cast kapt af, Convert rondt af, Parse kan crashen, Math als gereedschap | Flight 501, The Index |
| De Controlekamer | H5 beslissingen | Gambits: je stelt de regels van een automaat op ("als een vijand onder 10 HP staat: aanvallen") en kijkt dan hoe hij vecht | De eerste regel die klopt wint: volgorde, `else if`, logische operatoren. `switch` is een tabel van gevallen | nog te kiezen |
| De Lopende Band | H6 loops | Automatiseringspuzzel: banden en machines die herhalen tot een voorwaarde waar is | Herhaling met een stopvoorwaarde; een band die nooit stopt, is de oneindige lus | Zune (2008): een `while`-lus die op 31 december van een schrikkeljaar nooit stopte |
| De Gereedschapsmuur | H7 methoden | Dezelfde band, met blueprints: een machine één keer bouwen en overal stempelen, met instelknoppen | Hergebruik en parameters; een blueprint wordt ook een kaart in de deckbuilder | nog te kiezen |
| Het Magazijn | H8 arrays | Grid en inventaris: vakken met een nummer, effecten op de buren | Index, lengte, off-by-one dat je meteen ziet | Heartbleed (2014): lezen voorbij het einde van een array |
| Nog te benoemen | H9 en verder: OOP | Tower defense of eigen kaarten ontwerpen: een torentype is een klasse, elke geplaatste toren een object | Klasse en object, overerving, polymorfisme (elke toren doet `Aanval()` op zijn manier) | uit te werken |
| Terug in de deckbuilder | Exceptions, call stack | Roguelike deckbuilder | De beurt is al een call stack, try/catch-kaarten | uit te werken |

**De lijn met visuele programmeeromgevingen.** Een regel in de Controlekamer of een machine op de band is een strategische keuze over wat er moet gebeuren, geen statement dat je regel per regel uitschrijft. Het voorbeeld is Opus Magnum, niet Scratch. Het grootste risico zit in de Lopende Band: die mag niet afglijden naar een opdrachtenlijstje. Dezelfde ontwerptoets geldt: zou iemand zonder interesse in programmeren dit willen spelen?

Alleen de Vatenvallei wordt nu gebouwd. De tabel is een richting, geen belofte: elke afdeling moet eerst als spel leuk zijn. De volgende spike is een klein gambit-prototype voor de Controlekamer, omdat dat genre het verst van de deckbuilder ligt, goedkoop te bouwen is en meteen test of gambits als intents in de deckbuilder werken.

## De wereld: de fabrieksplattegrond

De plattegrond verschijnt pas na [de onthulling](#de-onthulling). Tussen de afdelingen zit geen wereld om rond te lopen, maar één scherm: de plattegrond van de fabriek, getekend als het eerste blad van een montagehandleiding. Elke afdeling is een genummerde stap, en elk nummer is een hoofdstuk van het boek. Vier eisen: overzichtelijk, afdelingen ontgrendelen, mastery per afdeling tonen, en toegang tot klassement en achievements.

### Eén scherm

- In het midden de plattegrond, met de afdelingen in de volgorde van het boek. Aan de rand drie vaste knoppen: de **Codex**, de **Prikklok** (klassement) en het **✗-register** (achievements).
- Klik je op een afdeling, dan schuift één paneel open: naam, hoofdstuk, spelvorm, de onderdelenlijst, en twee knoppen: **Spelen** en **Dagelijkse run**.
- Van het openen van de game tot in een spel: hooguit twee klikken. De plattegrond is een keuzescherm, geen menu in een menu.

### Ontgrendelen

- Een gesloten afdeling staat gestippeld getekend, met de onderdelen nog in de zak. Een open afdeling is uitgetekend.
- Een afdeling gaat open als je de baas of eindpuzzel van de vorige verslaat, of als de docent ze vrijgeeft met de klascode, zodat de klas de lesplanning kan volgen.
- Open blijft open. Een sterke student gaat vooruit, een zwakkere keert terug naar een eerdere afdeling. Dat terugkeren is nooit een straf: elke afdeling blijft even leuk om opnieuw te spelen.
- De startpunten binnen de deckbuilder (een run in act 2 beginnen) blijven bestaan, binnen de Vatenvallei en de Gieterij.

### Mastery: de onderdelenlijst

Elke montagehandleiding begint met een onderdelenlijst. Bij ons zijn de onderdelen de concepten van dat hoofdstuk, uit de concepttabel van de afdeling. Elk onderdeel heeft drie toestanden:

1. **In de zak.** Nog niet tegengekomen. Je ziet een silhouet.
2. **Uitgepakt.** De regel heeft een gevecht of puzzel beslist; de Codex-pagina is open.
3. **Gemonteerd.** Je hebt de regel bewust in je voordeel gebruikt, in meerdere runs. Bijvoorbeeld: een vijand laten overlopen in drie verschillende runs, of de elite van dat concept verslaan.

De afdeling op de plattegrond groeit mee: hoe meer onderdelen gemonteerd, hoe meer van de tekening ingekt en ingekleurd. Een volledige afdeling komt tot leven: de schouw rookt, de band draait. Mastery is dus geen cijfer of percentage, maar een fabriek die af raakt.

- **Zwaktes zien.** Een open afdeling met losse onderdelen krijgt een sticker "hapert". Zo ziet een student meteen waar er nog te oefenen valt: "vandaag de Lopende Band".
- **Alleen gedrag telt.** Een onderdeel monteer je door te spelen, nooit door een Codex-pagina te lezen of een vraag te beantwoorden.
- **Geen slijtage.** Gemonteerd blijft gemonteerd. Een fabriek die roest als je een week niet speelt, is een straf voor afwezigheid.
- Het docentdashboard leest later dezelfde onderdelenlijst, per student en per klas.

### Prikklok: het klassement

- Per afdeling een dagelijkse seed, met een klasklassement onder anonieme bijnamen. De Prikklok opent op de afdeling waar je het laatst speelde.
- Elke spelvorm meet iets eigens: in de deckbuilder winst en resterende HP, in de Controlekamer het aantal regels, op de band het aantal machines en cycli.
- **Histogram in plaats van ranglijst**, zoals bij Opus Magnum: je ziet waar je oplossing valt tegenover de klas, niet dat je 27ste van 28 bent. Een top 10 van bijnamen kan ernaast, maar het histogram is de standaard.

### ✗-register: de achievements

- De achievements zijn ✗-panelen uit de handleiding: dingen die de handleiding verbiedt en die jij toch deed. Elke afdeling heeft een eigen pagina.
- Voorbeelden: een baas laten overlopen tot hij sterft, de Controlekamer winnen met één regel, de band duizend keer laten draaien zonder vast te lopen.
- Alleen spelprestaties, nooit leerprestaties: geen "lees tien Codex-pagina's" (zie [Antipatronen](#antipatronen)). Een deel is verborgen, zodat geruchten zich op de speelplaats verspreiden.

## De onthulling

Een student begint Deck Overflow en denkt dat het een gewone deckbuilder is. Pas later ontdekt die dat er een hele fabriek achter de muur ligt, zoals bij Inscryption. In een klas blijft dat geen week geheim, dus we ontwerpen het als een mysterie, niet als een verrassing. Een verrassing is weg zodra iemand ze verklapt. Een mysterie wordt sterker als een klasgenoot zegt: "er zit iets achter die deur."

### Wanneer de muur opengaat

De onthulling mag niet alleen afhangen van winnen, anders zien net de zwakkere studenten de wereld nooit. De eerste van drie voorwaarden die vervuld is, opent de muur:

1. **De baas van act 2 verslaan.** Het grote moment: de muur van de Gieterij scheurt open.
2. **Het vangnet.** Na een aantal runs of een bepaalde speeltijd, ook zonder ooit te winnen, merkt de fabriek je op: er verschijnt een barst in de muur, en je kan er zelf doorheen.
3. **De docent.** Komt de les bij H5, dan moet iedereen de Controlekamer in. De docent opent de muur voor de hele klas tegelijk, als klasmoment, niet als spoiler.

### De teaseladder

Elke tease roept een vraag op en legt niets uit. Wie hem mist, mist niets.

| Wanneer | Tease | Waarom het werkt |
| --- | --- | --- |
| Vanaf run 1 | Een paginanummer in de hoek, zoals in een handleiding: *p. 2 / 21*. Op de achtergrond van de map een gestippelde deur met een ⑤ | Waarom 21 pagina's? |
| Runs 1 tot 3 | Af en toe een onderdeel dat nergens voor dient, zoals een tandwiel met "hoort bij stap 6", in een zakje dat je kan openen | Een leeg verzamelvak wil gevuld worden |
| Runs 2 tot 4 | Een vreemde vijand uit een andere afdeling, met een intent die geen getal is maar een regel: "als jij een schild hebt: dubbele schade" | Een eerste smaak van de Controlekamer, in de deckbuilder |
| Na een nederlaag | Soms een geluid achter de deur, een ratelende band, of een briefje dat onder de deur door schuift | Het doodscherm wordt gelezen, je wacht er toch even |
| Codex | Tabbladen die niet opengaan, alleen met een nummer | Silhouetten, zoals in het verzamelalbum |
| Changelog | "Controlekamer: regels bijgewerkt" | Een administratieve hint, grappig voor wie hem opmerkt |

### Je was al aan het bouwen

De onderdelenlijst wordt vanaf run 1 in stilte bijgehouden. Bij de onthulling zoomt het beeld uit naar de plattegrond, en de Vatenvallei is al half ingekt met de onderdelen die je onbewust monteerde. De tandwielen uit je zakje vallen op hun plaats. De boodschap: je bouwde hier al de hele tijd aan. De wereld is een beloning, geen nieuwe taak.

### Voor en na

- **Voor de onthulling** is er geen plattegrond. De game opent meteen in de deckbuilder, met als titelscherm de kaft van de handleiding. Klassement en ✗-register bestaan, maar alleen voor de deckbuilder, als tabbladen in het spel.
- **Na de onthulling** vervangt de plattegrond het titelscherm, en verhuizen Prikklok en ✗-register naar de plattegrond.
- **Wie het al weet, verliest niets.** De teases lezen dan als voorpret in plaats van als raadsel.
- **Naar buiten toe** mogen product sheet en presentaties voor docenten en instellingen de wereld verklappen. Tegenover studenten zwijgen we erover.

## Act 1: De Vatenvallei

Act 1 dekt hoofdstuk 2 en 3 van Zie Scherp Scherper: variabelen, datatypes, identifiers, operatoren, expressies, constanten en tekst. Het thema maakt die concepten tastbaar: in de Vatenvallei is alles een waarde in een vat, en elk vat heeft een vorm (type) en een etiket (naam).

- Een `int`-vat heeft geen plaats voor een komma. Wat erin gegoten wordt, verliest zijn decimalen.
- Een `bool`-vat heeft maar twee standen.
- Een `string`-vat bewaart tekst, ook als die tekst toevallig op een getal lijkt.
- Een etiket moet geldig zijn, anders vindt niemand het vat terug.

Structuur: een map van 6 rijen, 1 elite per pad, 1 baas. Samen 10 tot 15 minuten.

| Concept | Waar het in de game zit | Zie Scherp Scherper |
| --- | --- | --- |
| Toekenning (`=` vs `+=`) | Zet- en Voeg-toe-kaarten | H2 |
| `int` en afkappen | Tinnen Ridder, schild-mechaniek | H2 |
| `double` | Vlottende kaarten, Floating Point maakt je aanval `double`, relic Vlottende Komma | H2 |
| Type van een expressie | `int` met `double` geeft `double`: de getypeerde aanval | H2 |
| `bool` als datatype | Bool-schim (logische operatoren pas in Act 3, H5) | H2 |
| Identifiers | Elite De Naamloze, event De Etiketkamer | H2 |
| Rekenkundige operatoren | Intents van alle vijanden | H2 |
| Integer deling | Split (`7 / 2` is 3), intents als `20 / x`, Splitsbende | H2 |
| Modulo `%` | Remainder, Ritmeschildpad, relic Restzak | H2 |
| Operatorvoorrang | Baas De Rekenmeester, relic Haakjes | H2 |
| `++` en `--` | Tweelingschutters | H2 |
| Constanten (`const`) | De patch: een revisie maakt een waarde `const` | H2 |
| Overflow | Elite Level 256 (vroeger De Byte-Golem), voorlopig de enige elite van act 1 | H2 |
| `string` en `+` | Ink-kaarten (`"1" + "2"` is `"12"`), Read zet tekst om naar een getal, Papieren Golem | H3 |
| `char` is een getal | Letterkaarten: `'A' + 1` is 66 | H3 |
| Unicode | Elite Effective Power | H3 |
| Casting | Omgieten-kaarten (kernsysteem), event De Smeltkroes; Codex pas in Act 2 | H4 |
| Parse | Read; Codex pas in Act 2 | H4 |

## Encounters

Elke gewone vijand draait rond één regel. De valkuil is wat een beginner intuïtief doet. De slimme zet is wat iemand doet die de regel doorheeft.

| Vijand | Concept | Valkuil | Slimme zet | Codex |
| --- | --- | --- | --- | --- |
| Slijmklodder (tutorial) | Toekenning: `=` vs `+=` | "Zet schild op 5" spelen met 12 schild: je gooit 7 weg | Zet-kaarten gebruiken op de vijand: zijn aanval op 1 zetten | Toekenning |
| Tinnen Ridder | `int` kapt af | Kaarten met 2,9 schade: doen er 2 | Hele getallen stapelen, of één Vlottende kaart als breekijzer | Integer types |
| Bool-schim | `bool` | Zware aanval: één treffer, staat terug recht na de tweede | Tellen: een oneven aantal treffers, ongeacht de grootte | Booleans |
| Papieren Golem | `string` + `int` plakt | 5 schade op "20" HP wordt "205" en hij groeit | Eerst de Inkt-kaart (omzetten naar getal), dan pas slaan | Strings en concatenatie |
| Splitsbende (2 tot 4 kobolden) | Integer deling | Splitsslag 7 over 2 doelen: 3 en 3, de laatste kobold overleeft met 1 HP | Even getallen spelen, of eerst een kobold uitschakelen | Integer deling |
| Ritmeschildpad | Modulo `%` | Aanvallen als het schild dicht is | Zijn intent `beurt % 3 == 0` lezen en je zware kaarten sparen voor die beurt | Modulo |
| Tweelingschutters | `i++` vs `++i` | Denken dat Snelle Steek meteen met de verhoogde waarde slaat | Postfix vroeg in de beurt, prefix als finisher | Increment en decrement |

**Event De Smeltkroes (casting).** Giet een Vlottende kaart om naar een gewone: hij kost voortaan 0 energie, maar verliest zijn decimalen. Soms een slimme ruil, soms een vergissing.

**Event De Etiketkamer (identifiers).** Je vindt drie vaten met kostbare inhoud. Je mag er één openen door het juiste etiket te kiezen. Etiketten als `2goud`, `goud zak` of `class` zijn ongeldig en het vat blijft dicht.

## Elites en baas

Elites combineren een regel met druk. De baas test of je expressies kan lezen terwijl alles tegelijk gebeurt.

### Elite: Level 256 (overflow)

250 van 255 HP, onkwetsbaar voor schade, heelt zichzelf elke beurt met 2. Na een paar beurten zit hij op 255 en zijn volgende heling laat hem klappen. Wie het doorheeft, speelt een Herstel-kaart op hem en wint in één beurt. Wie het niet doorheeft, kan hem ook uitzitten, maar verliest onderweg veel HP.

Vroeger De Byte-Golem. Hij is nu een speelhalkast waarvan de rechterhelft van het scherm in brokken uiteenvalt: de echte bug van Pac-Man, waar het levelnummer een `byte` is en level 256 het doolhof breekt. De Codex vertelt dat verhaal na het gevecht.

### Elite: Effective Power (tekst en Unicode)

Een bericht dat ontploft als je het leest. Zijn HP is een `string`: wie er schade bij telt, plakt cijfers aan zijn tekst en maakt hem langer. Wie ziet dat elk teken een getal is (`char`), kan hem teken per teken afbreken. Het is de echte bug van 2015, waarbij één reeks Unicode-tekens iPhones liet crashen.

### Elite: Flight 501 (casting, Act 2)

Een raket met 506 HP als `int`, te sterk om met schade te verslaan. Wie hem naar `byte` omgiet, ziet 506 terugspringen naar 250, en dan is hij te doen. Het is de echte bug van Ariane 5 (1996): een getal dat niet in zijn nieuwe type paste. In spike 4 tot 6 heette hij de Tinnen Kolos.

### Elite: De Naamloze (identifiers)

Een schaduw die zich verbergt achter drie etiketten: `Schaduw`, `schaduw` en `_schaduw`. Alleen het echte etiket is raakbaar, en het wisselt elke beurt. Zijn intent verraadt welk etiket hij bedoelt. Hoofdlettergevoeligheid is hier letterlijk een kwestie van leven en dood.

### Baas: De Rekenmeester (expressies en operatorvoorrang)

Een oude rekenaar met een stenen tablet. Zijn intents zijn steeds langere expressies, en als enige vijand toont hij het totaal niet: hier is rekenen bewust de kern van het gevecht.

1. **Fase 1:** `3 + 2 × 4` en vergelijkbaar. Wie van links naar rechts rekent, blokt 20 terwijl er 11 komt, en verspilt kaarten.
2. **Fase 2:** integer deling en modulo komen erbij: `17 / 5 + 17 % 5`. Hij splitst zijn aanval over beurten.
3. **Fase 3:** hij schrijft zijn expressie om elke beurt. Met de relic of kaart Haakjes kan je zijn tablet bijsturen: `(3 + 2) × 4` wordt dan jouw wapen in plaats van het zijne.

Wie de Rekenmeester verslaat, heeft zonder het te merken tientallen C#-expressies juist geëvalueerd onder tijdsdruk.

## Kaarten en relics

Het starterdeck is bewust saai, zodat elke beloning een echte keuze wordt.

**Starterdeck (10 kaarten):** 5× Slag (6 schade), 4× Schild (5 blok), 1× Snelle Steek (postfix: slaat met huidige kracht, kracht +1 daarna).

| Kaart | Kost | Effect | Concept |
| --- | --- | --- | --- |
| Zet op 1 | 1 | Zet de aanval van een vijand op 1 voor deze beurt | Toekenning |
| Voeg toe | 0 | +3 aan je volgende kaart | `+=` |
| Vlottende Slag | 2 | 4,5 schade; exact op een `double`, een `int`-doelwit kapt af tot 4 | `double` |
| Floating Point | 0 | `× 1.0`: je volgende aanval wordt een `double` (verbeterd: `× 1.5`) | Type van een expressie |
| Ink | 1 | `+ "1"`: tekst plakt aan je volgende aanval; tekst raakt geen getal | `string` en `+` |
| Read (zeldzaam) | 2 | `int.Parse(…)` op je volgende aanval. Ongeldige tekst geeft een `FormatException` en kost je de beurt | Parsen, ervaren in act 1 |
| Flip | 0 | Draai een `bool`-toestand om | `bool` |
| Split | 0 | `/ 2`, maar je volgende kaart slaat twee keer: `9 / 2` is twee keer 4 (verbeterd: `/ 2.0`, twee keer 4.5) | Integer deling |
| Voorsprong | 1 | Kracht +1, dan slaan | `++i` |
| Herstel | 1 | +6 HP op een doelwit naar keuze | Overflow (als je het doorhebt) |

| Relic | Effect | Concept |
| --- | --- | --- |
| Haakjes | Eén keer per gevecht: bepaal welk deel van een expressie eerst gerekend wordt | Operatorvoorrang |
| Restzak | Bewaart de rest van elke deling; bij 5 vuurt hij 5 schade af | Modulo |
| Vlottende Komma | Je eerste Vlottende kaart per gevecht kost 0 | `double` |
| Teller | Elke derde kaart die je speelt, kost 0 | Modulo en tellen |
| Etiketmaker | De Naamloze toont zijn echte etiket één beurt vooraf | Identifiers |

## Act 2: De Gieterij

Act 2 dekt hoofdstuk 4: werken met data. Act 1 liet types en tekst voelen; de Gieterij gaat over **expliciet omzetten**. Een gieterij giet gesmolten metaal in mallen, en dat is casting, letterlijk. Hier krijgt Omgieten zijn echte naam. In het Engelstalige spel heet de act The Mold Works, omdat het openingsevent van act 1 al The Foundry heet.

| Concept | Waar het in de game zit |
| --- | --- |
| Expliciete cast kapt af | Force Fit, Flight 501, Codex "Casting" na de baas |
| Impliciete omzetting (klein naar groot) | The Ingot |
| `Convert` rondt af en is checked: een `OverflowException` als het niet past | Measure Twice, Flight 501 |
| `Math.Round`, bankiersafronding | The Rounder |
| `int.Parse` en een ongeldige tekst | Read the Label, The Label |
| Afkappen tegenover afronden | Elite The Index |

### Gewone vijanden

| Vijand | Mechaniek | Wat je ontdekt | Andere manier om te winnen |
| --- | --- | --- | --- |
| The Label | Zijn HP is tekst: `"40"`. Schade plakt eraan vast (`"40" + 6` is `"406"`), en zijn aanval is een tiende van zijn HP. | Tekst is geen getal: eerst parsen. Omgieten weigert, want `(int)"40"` compileert niet. | Wrong Label maakt er `"1"` van; dan nog parsen, maar van 1 HP |
| The Rounder | `double`-HP. Hij rondt elke inkomende treffer af met `Math.Round`: 1.5 wordt 2, maar 2.5 ook, en 3.5 wordt 4. Welke meervoudige kaart je speelt, maakt het verschil. | Bankiersafronding, tegenover het afkappen van een `int` | Met Force Fit naar `int` omgieten: dan kapt hij af en rondt hij nooit meer af |
| Raw Ingot | Een eenvoudige `int` die na elke aanval wat blok opbouwt | Niets nieuws: een adempauze tussen de puzzels | Gewoon slaan |

### Elites

- **Flight 501** (Ariane 5, 1996): 506 HP als `int`, te sterk om plat te slaan. Omgieten naar `byte` maakt er 250 van. Measure Twice (`Convert.ToByte`) klapt niet om maar gooit een `OverflowException`, zoals bij de echte raket: hij crasht en slaat zijn aanval over. Zonder echte cast naar `byte` in je deck krijg je The Index in zijn plaats. Flight 501 zat eerst in act 1, maar hoort bij casting.
- **The Index** (Vancouver Stock Exchange, 1982): zijn HP groeit elke beurt met `(int)(HP * 1.05)`. Hij kapt af in plaats van af te ronden, dus onder 20 HP eet het afkappen de groei op: `(int)(19 * 1.05)` is `(int)19.95` is 19. Wie hem onder de 20 krijgt, ziet zijn groei stilvallen, net als de echte index die maanden zakte.

### Baas: The Caster

Een figuur van de fabriek die zichzelf na elke aanval in een andere mal giet: van `int` naar `double` naar `byte`, en weer van voren af. Als `int` kapt hij je decimalen af, als `double` neemt hij alles exact, als `byte` loopt hij over als je hem heelt. Hij begint met 300 HP, en dat past niet in een `byte`: wie hem boven 255 houdt tot hij een `byte` wordt, ziet hem omklappen (`(byte)300` is 44). Het slotgevecht vat act 1 en act 2 samen. Daarna opent de Codex-pagina "Casting" met de echte naam.

### Nieuwe kaarten

| Kaart | Wat ze doet | Concept |
| --- | --- | --- |
| Measure Twice | Zet een vijand om naar `byte` met `Convert.ToByte`: afronden in plaats van afkappen. Past het niet, dan een `OverflowException`: de vijand crasht en slaat zijn aanval over. | Convert tegenover cast: checked tegenover unchecked |
| Read the Label (nog te bouwen) | Tekst-HP wordt een getal (`int.Parse`). Op ongeldige tekst crasht ze, en je beurt eindigt. | Parse |

Round Off (`Math.Round` op een vijand) en Square Root (`Math.Sqrt` op zijn HP) zijn voorlopig geschrapt. Round Off verschuift hoogstens een halve HP en doet dus te weinig; Square Root maakt van elke baas een gevecht van één kaart.

## De Codex

De Codex is de brug van spel naar cursus, en hij is nooit verplicht. Een pagina opent pas als een regel een gevecht echt heeft beslist, niet bij de eerste ontmoeting.

Een Codex-pagina heeft vier lagen, die de speler zelf openklikt:

1. **Wat er gebeurde.** Een korte replay van het moment: "Je heelde de golem met 6 en zijn HP klapte naar 0."
2. **De naam.** "Dit heet integer overflow."
3. **Onder de motorkap.** Hier verschijnt voor het eerst echte C#: drie tot vijf regels die precies dat moment nabootsen.
4. **Verder lezen.** Link naar het hoofdstuk in Zie Scherp Scherper.

Wie een pagina leest, krijgt niets extra. De beloning zit in de volgende run: je weet nu iets wat je vijand niet verwacht. Docenten zien in hun dashboard welke pagina's per student ontgrendeld zijn, en dus welke regels al eens gevoeld zijn.

## Platform en techniek

Deck Overflow wordt een website: spelen zonder installatie, op laptop en tablet.

- **Game-engine:** Blazor WebAssembly voor de regelmotor en de shell, PixiJS voor de stage. Gekozen na zes spikes.
- **Audio:** Web Audio, zodat we toonhoogte per trigger kunnen laten stijgen en geluiden laag op laag kunnen stapelen.
- **Regelmotor:** één centrale motor die C#-semantiek naspeelt (types, afkappen, overflow, voorrang). De game-feel-laag luistert naar zijn events, zodat elke regel automatisch zijn eigen animatie en geluid krijgt.
- **Spelers en accounts:** iedereen mag spelen, ook leerlingen uit het middelbaar. Je speelt meteen als gast en kan later een account met gebruikersnaam en wachtwoord maken om je fabriek te bewaren; e-mail is optioneel. In klassementen staat altijd een gegenereerde bijnaam, nooit wat de speler zelf typte. Geen analytics of tracking. De klascode, waarmee een docent een afdeling vrijgeeft, is optioneel. Details in het [Spike Design Doc](spike-design-doc.md#hosting-accounts-en-data).
- **Docentdashboard:** pas na de MVP.
- **LMS-koppeling:** met Moodle-omgevingen zoals Digitap, als latere stap.

**Taal.** Het spel is Engels. Alle spelteksten staan in één bestand per taal, zodat een Nederlandse versie één extra bestand is.

## Antipatronen

Zodra een van deze erin sluipt, zijn we terug bij gamification. Ze zijn verboden, ook als een pilot erom vraagt.

- **Vragen als poort.** Nooit "beantwoord juist om aan te vallen". De game stelt geen vragen, de vijand stelt problemen.
- **Uitleg vooraf.** Geen tutorialschermen die een regel uitleggen voor je hem tegenkomt. Hooguit één zin bij de eerste kaart van een nieuw type.
- **Regels die buigen voor de les.** Als `int` afkapt, dan kapt `int` altijd af, ook als het de speler toevallig helpt.
- **Punten of cijfers.** Geen score per juist antwoord, geen badges voor gelezen Codex-pagina's. Winnen is de enige score.
- **Code typen in Act 1.** Pas in latere afdelingen, en dan nog als optionele laag.
- **Eén juiste oplossing.** Elk gevecht moet op meerdere manieren te winnen zijn. Begrijpen maakt het efficiënter, niet verplicht.

## Open vragen en volgende stappen

De grootste onzekerheid is of studenten de regels in de game herkennen wanneer ze later echte code lezen. Een kleine playtest moet dat vroeg uitwijzen.

- [x] Hoofdstuknummers uit Zie Scherp Scherper invullen in de concepttabel
- [x] `char` een eigen vijand of mechaniek geven, of bewust naar een latere act schuiven
- [ ] Identifiers zijn het zwakste concept als mechaniek: De Naamloze testen op fun, niet alleen op leerwaarde
- [x] Runlengte afstemmen op een lesblok
- [x] Techniekkeuze: Blazor WebAssembly of een JavaScript-game-engine
- [ ] Papieren prototype van 3 encounters en de Rekenmeester, playtest met 5 studenten
- [ ] Transfer meten: na de playtest dezelfde studenten 5 korte C#-expressies laten voorspellen

- [ ] Per archetype de jackpot-combo uitwerken en testen of hij echt alleen zichtbaar is voor wie het concept snapt
- [ ] Zeldzaamheidskansen en pity timer afstellen
- [ ] Dagelijkse run: seeds en klasklassement, met of zonder server
- [ ] Standaard speelduurlimiet bepalen samen met collega's
- [ ] Geluidsontwerp: een eerste set van tien kerngeluiden maken en testen

- [ ] Typetabel uitwerken: welk aanvalstype werkt hoe op welk doelwittype
- [x] Bepalen hoe de Codex-pagina Casting opengaat: via het docentendashboard of gekoppeld aan de lesplanning

Beslist op 2 oktober 2026: Act 1 is H2 en H3, een act duurt 10 tot 15 minuten, de Codex opent na het gevecht met echte C# en een link naar het hoofdstuk, `char` hoort bij Act 1. Nog open:

- [x] Art: de AI-gegenereerde art in de handleidingstijl is definitief (beslist op 2 oktober 2026). Nieuwe tekeningen komen van hetzelfde model met het manualvel als referentiebeeld, zodat de stijl gelijk blijft.
- [x] Startpunten: starterdeck plus vijf keer 1 uit 3, 1 relic uit 3, volle HP, 100 goud (beslist op 3 oktober 2026)
- [ ] Echte bugs kiezen voor de Controlekamer (beslissingen) en de Gereedschapsmuur (methoden)
- [x] Acts 7 en verder (H9 tot H21) uitwerken: vervangen door afdelingen met een eigen spelvorm (3 oktober 2026)
- [ ] Playtest pas na Act 1: 5 studenten en 2 collega's

Beslist op 3 oktober 2026: elke afdeling krijgt de spelvorm die past bij haar hoofdstuk, rond één fabrieksplattegrond. Nog open:

- [x] Spike 8: gambit-prototype voor de Controlekamer gebouwd ([spikes/08-controlekamer](../spikes/08-controlekamer/)), nog niet met spelers getest
- [ ] Gambits als intents in de deckbuilder: pas proberen als spike 8 toont dat spelers de regels van de vijand lezen
- [ ] Per afdeling vastleggen wanneer een onderdeel "gemonteerd" is
- [ ] Spelvorm voor OOP kiezen: tower defense of eigen kaarten ontwerpen
- [ ] Wat ontgrendelt een afdeling precies: de baas van de vorige, of al een bepaald aantal gemonteerde onderdelen?
- [ ] Plattegrond als papieren mock testen: begrijpt een student zonder uitleg waar er te oefenen valt?
- [ ] Vangnet voor de onthulling afstellen: na hoeveel runs of hoeveel speeltijd gaat de muur vanzelf open?
- [ ] Teases testen: merken studenten het paginanummer, de deur en de onderdelen op, en worden ze er nieuwsgierig van?
