# Deck Overflow: visie en principes

Wat voor elke afdeling geldt: waarom het spel bestaat, de pijlers, het thema, de verslavingsmotor, de game feel en wat we nooit doen. Een afdeling volgt deze regels; wat alleen voor één afdeling geldt, staat in haar eigen map onder [afdelingen/](afdelingen/).

## Visie

Deck Overflow is een spel in de browser waarin de wereld gehoorzaamt aan C#. Wie de regels doorheeft, wint. Het spel moet verslavend zijn in de goede zin: je wil nog één run spelen, en elke run maakt je beter in C#.

Het hart is een roguelike deckbuilder. Rond dat hart ligt een fabriek met afdelingen, één per blok hoofdstukken, en elke afdeling heeft de spelvorm die het best past bij wat ze aanleert. Een deckbuilder voor types en expressies, een regelsysteem voor `if`, een lopende band voor loops. Alles hangt samen op één plattegrond waarop je ziet hoe ver je fabriek af is.

- **Doelgroep:** eerstejaars programmeren, met de zwakkere studenten als ontwerpmaat. Iedereen mag spelen, ook leerlingen uit het middelbaar. Wie Slay the Spire of Balatro kan spelen, moet dit kunnen spelen.
- **Bron:** de leerlijn van Zie Scherp Scherper. Elke afdeling volgt een blok hoofdstukken.
- **Wat we bewust niet maken:** geen gamification (punten en badges op oefeningen), geen visuele programmeeromgeving. Een eerste versie met kaarten als codestatements is verworpen: dat was Scratch met een zwaard, een dunne laag over code schrijven. Ook de afdelingen waarin je regels of machines bouwt, blijven aan de goede kant van die lijn: zie [Afdelingen](wereld/README.md#afdelingen-elk-hoofdstuk-zijn-eigen-spelvorm).
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
- **Beeldtaal:** zwarte lijnen op papier, één schreefloze letter, stapnummers. Kleur is voorbehouden aan types: zie je kleur, dan zie je een type. Een verslagen vijand valt uiteen in een explosietekening. Elke afdeling is een plek, en wat je bedient is een onderdeel uit die plek: een schakelkast, een onderdelenlijst, een klembord. Zie [de beeldtaal](wereld/beeldtaal.md).

### Elites: echte bugs

Gewone vijanden zijn producten van de fabriek, bazen zijn figuren van de fabriek. Elites zijn beroemde softwarefouten, elk bij een concept uit hun act. De kolom Act geeft de act van de Card Hall, of de afdeling voor bugs die nog niet gebouwd zijn. Voor Zune en Heartbleed staan al tekeningen klaar, net als voor Mars Climate Orbiter (1999: een verwisseling van eenheden, een kandidaat rond types).

| Elite | Act | Echte bug | Concept |
| --- | --- | --- | --- |
| Level 256 | 1 | Pac-Man (1980): het levelnummer is een `byte`. Op level 256 loopt het over en wordt de rechterhelft van het doolhof rommel. | `byte`-overflow |
| The Counter | 1 | Gangnam Style (2014): YouTube zette zijn weergaventeller van 32 naar 64 bits omdat de maximumwaarde in zicht kwam. | `int`-overflow |
| Effective Power | 2 | iPhone (2015): één bericht met een reeks Arabische Unicode-tekens liet het toestel crashen zodra het die tekst probeerde te tonen. | tekst, Unicode |
| Y2K | 2 | De millenniumbug (1999): websites schreven het jaar als `"19" + (jaar - 1900)`. Op 1 januari 2000 stond er 19100. | string-concatenatie |
| Flight 501 | 3 | Ariane 5 (1996): een `double` werd omgezet naar een 16-bits geheel getal, dat liep over. De raket vernietigde zichzelf na 37 seconden. | casting |
| The Index | 3 | Vancouver Stock Exchange (1982): de index werd na elke berekening afgekapt in plaats van afgerond en zakte in 22 maanden tot ongeveer de helft van zijn echte waarde. | afronden |
| Zune | Lopende Band | Zune (2008): alle spelers van één model bevroren op 31 december, de 366e dag van een schrikkeljaar, in een `while`-lus die nooit stopte. | loops |
| Heartbleed | Magazijn | OpenSSL (2014): een server las voorbij het einde van een buffer en stuurde geheime data mee terug. | arrays |

We gebruiken geen bugs met doden (zoals de Patriot-raket in 1991). "Nuclear Gandhi" is een mythe; die kan hoogstens als Codex-grap: deze bug heeft nooit bestaan. Geen merknamen of logo's in beeld: een speelhalkast, geen Pac-Man.

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
| Dagelijkse run | Een vaste reden om terug te komen | Zelfde seed voor iedereen, klassement per klas |
| Zachte streak | Ritme opbouwen | Teller voor opeenvolgende dagelijkse runs, zonder straf als je een dag mist |
| Geheimen | Geruchten verspreiden zich op de speelplaats | Verborgen vijanden en routes, zoals een geest die alleen verschijnt op een zeldzaam pad |
| Deelbare momenten | Opscheppen is brandstof | Seed en eindscherm van een gebroken run delen met één klik |
| Prestaties | Kleine doelen naast het grote | Grappige achievements voor spelprestaties, zoals een elite verslaan met één kaart |

**Grens.** Geen echt geld, geen loot boxes, geen FOMO-timers die studietijd verdringen, geen straf voor afwezigheid. Verslavend is het doel, schadelijk niet.

## Game feel

De juice volgt het begrip: hoe beter je een C#-regel uitbuit, hoe harder het spel reageert. Zie [analyse van Balatro](https://blakecrosley.com/guides/design/balatro) en [hit pause](https://bugnet.io/blog/how-to-use-hit-pause-to-make-impacts-feel-powerful).

1. **Intents rekenen zichtbaar uit.** Bij `3 + 2 × 4` licht eerst `2 × 4` op met een tik, dan de `+`. Elke stap een hogere toon: operatorvoorrang als ritme.
2. **Grootste knal voor regel-exploits.** Level 256 tikt naar 255, alles bevriest, het getal rolt naar 0, bas-drop. Een gewone grote slag voelt daarnaast bescheiden.
3. **Regels altijd hoor- en zichtbaar.** Bij `int`-afkappen breekt de decimaal af en valt rinkelend weg; bij integer deling rolt de rest als munt in de Restzak.
4. **Fouten grappig, niet bestraffend.** De "5" stempelt achter de "20" en de Papieren Golem zwelt op tot "205".
5. **Types hebben een vaste kleur en vorm**, zodat je een `double` of `string` in één blik leest, ook bij kleurenblindheid.
6. **Combo's als kettingreactie.** Kaarten en relics vuren na elkaar af met een stuiter en stijgende toon. Schudden en hit pause schalen met de impact. Snelle modus voor ervaren spelers.

Grens: juice beloont altijd een inzicht of slimme zet, nooit puur toeval.

## Antipatronen

Zodra een van deze erin sluipt, zijn we terug bij gamification. Ze zijn verboden, ook als een pilot erom vraagt.

- **Vragen als poort.** Nooit "beantwoord juist om aan te vallen". De game stelt geen vragen, de vijand stelt problemen.
- **Uitleg vooraf.** Geen tutorialschermen die een regel uitleggen voor je hem tegenkomt. Hooguit één zin bij de eerste kaart van een nieuw type.
- **Regels die buigen voor de les.** Als `int` afkapt, dan kapt `int` altijd af, ook als het de speler toevallig helpt.
- **Punten of cijfers.** Geen score per juist antwoord, geen badges voor gelezen Codex-pagina's. Winnen is de enige score.
- **Code typen in Act 1.** Pas in latere afdelingen, en dan nog als optionele laag.
- **Eén juiste oplossing.** Elk gevecht moet op meerdere manieren te winnen zijn. Begrijpen maakt het efficiënter, niet verplicht.
