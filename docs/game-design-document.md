# Deck Overflow: Game Design Document

1 oktober 2026 · Tim Dams

## Visie

Deck Overflow is een roguelike deckbuilder in de browser waarin de wereld gehoorzaamt aan C#. Wie de regels doorheeft, wint. Het spel moet verslavend zijn in de goede zin: je wil nog één run spelen, en elke run maakt je beter in C#.

- **Doelgroep:** eerstejaars programmeren, met de zwakkere studenten als ontwerpmaat. Wie Slay the Spire of Balatro kan spelen, moet dit kunnen spelen.
- **Bron:** de leerlijn van Zie Scherp Scherper. Elke act volgt een blok hoofdstukken.
- **Wat we bewust niet maken:** geen gamification (punten en badges op oefeningen), geen visuele programmeeromgeving. Een eerste versie met kaarten als codestatements is verworpen: dat was Scratch met een zwaard, een dunne laag over code schrijven.
- **De kern:** C#-semantiek zijn de natuurwetten. Types, operatoren, overflow en scope bepalen hoe gevechten verlopen. Begrijpen is de sterkste strategie.

## Pijlers

Deck Overflow is eerst een goede deckbuilder en pas daarna een leermiddel. Wat zonder leerdoel niet leuk is, gaat eruit.

1. **Echte keuzes, echt risico.** Runs, een map met paden, deckbouw-afwegingen en een run die kan mislukken. Geen veilige oefenmodus als standaard.
2. **C# is natuurkunde, geen leerstof.** De regels zijn altijd consistent en worden nooit vooraf uitgelegd. Je botst erop, net als op zwaartekracht.
3. **Begrijpen is de sterkste strategie.** Wie het concept doorheeft, wint efficiënter. Brute kracht mag, maar kost HP, goud of kaarten.
4. **Geen code in beeld in Act 1.** Kaarten tonen getallen, letters en woorden. Code verschijnt pas, optioneel, in de Codex.
5. **Eerst ervaren, dan benoemen.** De naam van een concept komt na het moment waarop het je een gevecht won of kostte.

**Ontwerptoets voor elke encounter:** zou iemand zonder enige interesse in programmeren dit gevecht nog willen spelen? Nee betekent herwerken.

**Tweede toets:** rekent de speler omdat het moet, of omdat hij iets wil bereiken? Alleen het tweede mag.

## Core loop

Eén act duurt 25 tot 35 minuten, zodat een run in een lesblok past. Meta-progressie is kennis van de speler, niet extra kracht.

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

### Omgieten

Omgieten verandert het type van een vijand. Het veralgemeent wat de golem leuk maakte: niet één puzzelvijand, maar een gereedschap voor elk gevecht.

- **Naar `byte`:** daarna laat helen elke vijand omklappen, niet alleen de golem.
- **Naar `int`:** een vijand met een decimaal schild verliest het restje bij elke treffer.

Dit is de C#-versie van "kwetsbaar maken en dan hard slaan", maar de speler ontdekt zelf welke vorm bij welke vijand past.

**Het spel loopt voor op het boek.** Casting komt in Zie Scherp Scherper pas in hoofdstuk 4. In de game heet het daarom Omgieten, een wereldnaam zonder het woord "cast". De Codex-pagina met de echte naam en de link naar het boek opent pas wanneer hoofdstuk 4 aan bod komt. Dat is de pijler "eerst ervaren, dan benoemen" in zijn zuiverste vorm.

### Later, als de basis staat

- **Statuseffecten met C#-betekenis**, één voor één: Memory leak (HP-verlies dat elke beurt groeit met `++`) eerst, daarna Readonly, Unchecked en Null.
- **De beurt als expressie**: de volgorde van je kaarten vormt een expressie met operatorvoorrang. Veelbelovend, maar het grootste risico op een Scratch-gevoel. Pas testen als types en omgieten bewezen zijn.

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

**De run die breekt.** Ergens in act 2 of 3 vallen twee relics en een kaart samen tot een combo die alles platwalst. Dat moment moet in elk archetype bestaan, en het moet alleen zichtbaar zijn voor wie het concept snapt.

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
2. **Grootste knal voor regel-exploits.** De Byte-Golem tikt naar 255, alles bevriest, het getal rolt naar 0, bas-drop. Een gewone grote slag voelt daarnaast bescheiden.
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

## Act 1: De Vatenvallei

Act 1 dekt variabelen, datatypes, identifiers, operatoren en expressies. Het thema maakt die concepten tastbaar: in de Vatenvallei is alles een waarde in een vat, en elk vat heeft een vorm (type) en een etiket (naam).

- Een `int`-vat heeft geen plaats voor een komma. Wat erin gegoten wordt, verliest zijn decimalen.
- Een `bool`-vat heeft maar twee standen.
- Een `string`-vat bewaart tekst, ook als die tekst toevallig op een getal lijkt.
- Een etiket moet geldig zijn, anders vindt niemand het vat terug.

Structuur: ongeveer 15 knopen op drie paden, 2 elites per pad, 1 baas.

| Concept | Waar het in de game zit | Zie Scherp Scherper |
| --- | --- | --- |
| Toekenning (`=` vs `+=`) | Zet- en Voeg-toe-kaarten | [hfst] |
| `int` en afkappen | Tinnen Ridder, schild-mechaniek | [hfst] |
| `double` | Vlottende kaarten, relic Vlottende Komma | [hfst] |
| `bool` | Bool-schim | [hfst] |
| `string` en `+` | Papieren Golem | [hfst] |
| `char` | Nog open (zie open vragen) | [hfst] |
| Identifiers | Elite De Naamloze, event De Etiketkamer | [hfst] |
| Rekenkundige operatoren | Intents van alle vijanden | [hfst] |
| Integer deling | Splitsbende | [hfst] |
| Modulo `%` | Ritmeschildpad, relic Restzak | [hfst] |
| Operatorvoorrang | Baas De Rekenmeester, relic Haakjes | [hfst] |
| `++` en `--` | Tweelingschutters | [hfst] |
| Casting | Omgieten-kaarten (kernsysteem), event De Smeltkroes; Codex pas bij hfst 4 | [hfst] |
| Overflow | Elite De Byte-Golem | [hfst] |

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

### Elite: De Byte-Golem (overflow)

250 van 255 HP, onkwetsbaar voor schade, heelt zichzelf elke beurt met 2. Na een paar beurten zit hij op 255 en zijn volgende heling laat hem klappen. Wie het doorheeft, speelt een Herstel-kaart op hem en wint in één beurt. Wie het niet doorheeft, kan hem ook uitzitten, maar verliest onderweg veel HP.

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
| Vlottende Slag | 2 | 4,5 schade, negeert afkappen | `double` |
| Inkt | 1 | Zet een tekstwaarde om naar een getal | Parsen |
| Flip | 0 | Draai een `bool`-toestand om | `bool` |
| Splitsslag | 2 | Verdeel 7 schade over alle vijanden | Integer deling |
| Voorsprong | 1 | Kracht +1, dan slaan | `++i` |
| Herstel | 1 | +6 HP op een doelwit naar keuze | Overflow (als je het doorhebt) |

| Relic | Effect | Concept |
| --- | --- | --- |
| Haakjes | Eén keer per gevecht: bepaal welk deel van een expressie eerst gerekend wordt | Operatorvoorrang |
| Restzak | Bewaart de rest van elke deling; bij 5 vuurt hij 5 schade af | Modulo |
| Vlottende Komma | Je eerste Vlottende kaart per gevecht kost 0 | `double` |
| Teller | Elke derde kaart die je speelt, kost 0 | Modulo en tellen |
| Etiketmaker | De Naamloze toont zijn echte etiket één beurt vooraf | Identifiers |

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

- **Game-engine:** Blazor WebAssembly of een JavaScript-game-engine met canvas. De keuze hangt af van hoe vlot animaties, partikels en hit pause lopen (zie open vragen).
- **Audio:** Web Audio, zodat we toonhoogte per trigger kunnen laten stijgen en geluiden laag op laag kunnen stapelen.
- **Regelmotor:** één centrale motor die C#-semantiek naspeelt (types, afkappen, overflow, voorrang). De game-feel-laag luistert naar zijn events, zodat elke regel automatisch zijn eigen animatie en geluid krijgt.
- **Docentdashboard:** per student welke Codex-pagina's ontgrendeld zijn, plus klas-seeds en speelduurlimiet.
- **LMS-koppeling:** met Moodle-omgevingen zoals Digitap, als latere stap.

## Antipatronen

Zodra een van deze erin sluipt, zijn we terug bij gamification. Ze zijn verboden, ook als een pilot erom vraagt.

- **Vragen als poort.** Nooit "beantwoord juist om aan te vallen". De game stelt geen vragen, de vijand stelt problemen.
- **Uitleg vooraf.** Geen tutorialschermen die een regel uitleggen voor je hem tegenkomt. Hooguit één zin bij de eerste kaart van een nieuw type.
- **Regels die buigen voor de les.** Als `int` afkapt, dan kapt `int` altijd af, ook als het de speler toevallig helpt.
- **Punten of cijfers.** Geen score per juist antwoord, geen badges voor gelezen Codex-pagina's. Winnen is de enige score.
- **Code typen in Act 1.** Pas in latere acts, en dan nog als optionele laag.
- **Eén juiste oplossing.** Elk gevecht moet op meerdere manieren te winnen zijn. Begrijpen maakt het efficiënter, niet verplicht.

## Open vragen en volgende stappen

De grootste onzekerheid is of studenten de regels in de game herkennen wanneer ze later echte code lezen. Een kleine playtest moet dat vroeg uitwijzen.

- [ ] Hoofdstuknummers uit Zie Scherp Scherper invullen in de concepttabel
- [ ] `char` een eigen vijand of mechaniek geven, of bewust naar een latere act schuiven
- [ ] Identifiers zijn het zwakste concept als mechaniek: De Naamloze testen op fun, niet alleen op leerwaarde
- [ ] Runlengte afstemmen op een lesblok
- [ ] Techniekkeuze: Blazor WebAssembly of een JavaScript-game-engine
- [ ] Papieren prototype van 3 encounters en de Rekenmeester, playtest met 5 studenten
- [ ] Transfer meten: na de playtest dezelfde studenten 5 korte C#-expressies laten voorspellen

- [ ] Per archetype de jackpot-combo uitwerken en testen of hij echt alleen zichtbaar is voor wie het concept snapt
- [ ] Zeldzaamheidskansen en pity timer afstellen
- [ ] Dagelijkse run: seeds en klasklassement, met of zonder server
- [ ] Standaard speelduurlimiet bepalen samen met collega's
- [ ] Geluidsontwerp: een eerste set van tien kerngeluiden maken en testen

- [ ] Typetabel uitwerken: welk aanvalstype werkt hoe op welk doelwittype
- [ ] Bepalen hoe de Codex-pagina Casting opengaat: via het docentendashboard of gekoppeld aan de lesplanning
