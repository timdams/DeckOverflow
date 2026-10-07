# Act 1: De Vatenvallei

Beslist op 3 oktober 2026: de deckbuilder, voortaan **The Card Hall**, heeft drie acts, één per hoofdstuk. Act 1 dekt hoofdstuk 2, act 2 (De Drukkerij) hoofdstuk 3, act 3 (De Gieterij) hoofdstuk 4. De run-lengte blijft: acts van 6 rijen. In het Engelse spel heet act 1 **The Vat Valley**.

**Gebouwd:**

| Rol | Vijand | Mechaniek |
| --- | --- | --- |
| Eerste rijen | Slime Blob, Tin Knight | Een eenvoudige vijand om in te komen; een `int` met een `int`-schild en intent `24 / (cards + 1)` |
| Gewoon | Floating Ghost | Een `double` met een decimaal schild; naar `int` omgegoten verliest hij de restjes |
| Gewoon | Dripper | Een `double` die halve schade uitdeelt, intent `energy * 4 + 2.5` |
| Gewoon | The Splitter | Intent `30 / (block + 1)`: blokken is delen |
| Gewoon | The Stray Automaton | Een tease uit de Controlekamer, intent `block > 0 ? 16 : 8` |
| Gewoon | Bool Ghost | Een `bool` die elke treffer omdraait; intent `isSolid ? 6 : 14` |
| Gewoon | Rhythm Turtle | Schild open als `cards % 3 == 0`; intent `turn % 2 == 0 ? 16 : 4` |
| Gewoon | Twin Shooters | Eén teller, twee schutters: intent `shots++ + ++shots`, elke beurt 4 harder |
| Vroeg event | Bottomless Jug | Een `byte` die zich heelt tot hij omklapt: het wondermoment van act 1 |
| Elite | Level 256 | Heelt zichzelf tot hij omklapt |
| Elite | The Counter | 30 onder `int.MaxValue`, telt op tot hij unchecked omklapt |
| Elite | The Nameless | Alleen te raken als zijn naam klopt: `shadow` wel, `Shadow`, `2shadow` en `sha-dow` niet |
| Baas | The Reckoner | Toont zijn totaal niet: rekenen is hier de kern |

De concepttabel hieronder is het oorspronkelijke ontwerp. De rijen voor H3 en H4 zijn verhuisd naar act 2 en 3. Intussen is alles uit die tabel gebouwd (zie hieronder), behalve de Etiketkamer: die is geschrapt, want een etiket kiezen om een vat te openen is een vraag als poort.

Act 1 dekt hoofdstuk 2 van Zie Scherp Scherper: variabelen, datatypes, identifiers, operatoren, expressies en constanten. Het thema maakt die concepten tastbaar: in de Vatenvallei is alles een waarde in een vat, en elk vat heeft een vorm (type) en een etiket (naam).

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
| Overflow | Elites Level 256 en The Counter, de Bottomless Jug | H2 |
| `string` en `+` | Ink (ervaren in act 1); verder in act 2 | H3 |
| Casting | Omgieten (Force Fit), event De Smeltkroes; Codex pas in act 3 | H4 |
| Parse | Read; Codex pas in act 3 | H4 |

## Encounters

Ontwerp, deels gebouwd: de Tinnen Ridder (Tin Knight) zit in het spel, de Splitsbende als één vijand (The Splitter), de Slijmklodder als eenvoudige vijand zonder `=`-tegenover-`+=`-puzzel. De rest wacht.

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

### Gebouwd: de Bool-schim (act 1)

Gebouwd op 3 oktober 2026, in het spel **Bool Ghost**, 30 HP, met de tekening van de schakelaar. Een geest met gewone `int`-HP en één `bool`: `isSolid`. Hij is een gewone vijand in act 1, met een bewuste intent.

- **Elke treffer draait `isSolid` om**, raak of niet. Is hij solid, dan neemt hij de schade; is hij het niet, dan gaat de treffer erdoor. De eerste treffer raakt dus, de tweede niet, de derde weer.
- **Meervoudige kaarten worden een telspel.** Floating Bolts (3 treffers) raakt twee keer, Floating Parts (4) ook maar twee. Split (twee treffers) raakt één keer. Eén grote Hammer It In is hier sterk.
- **Zijn intent is `isSolid ? 6 : 14`.** Wie zijn beurt eindigt terwijl hij doorzichtig is, krijgt het hard. Je wil eindigen op een oneven aantal treffers.
- **Een nieuwe kaart Flip** (1 energie, verbeterd 0): `isSolid = !isSolid`, zonder treffer. Een gereedschap om de telling recht te zetten. Op een vijand zonder `bool` weigert ze: `!hp` compileert niet.
- **Wat je ontdekt:** een `bool` kent twee standen, en `!` draait om. Het tellen is pariteit, zonder dat het spel het zo noemt. Logische operatoren (`&&`, `||`) wachten op de Controlekamer.
- **Codex:** een pagina **Booleans** (H2), met jouw moment: `!true` werd `false`. Onder zijn balk staat altijd `isSolid = true` of `false`.
- **Andere manier om te winnen:** gewoon slaan en de 14 opvangen met blok.

### Gebouwd: de Ritmeschildpad (act 1)

Gebouwd op 3 oktober 2026, in het spel **Rhythm Turtle**, 30 HP. Een schildpad met een schild dat alleen open is in het ritme van jouw beurt. De voorwaarde staat op zijn schild, zoals een intent: `cards % 3 == 0`.

- **Zijn schild is dicht, behalve bij elke derde kaart** die je deze beurt speelt. Een treffer op een dicht schild ketst af (0 schade). Je derde en zesde kaart raken.
- **Dus speel je eerst twee goedkope kaarten** (Spare Screw, Split, Floating Point: die kosten 0) en dan je zware slag. De Counter-relic (elke derde kaart kost 0) wordt hier ineens goud waard.
- **Zijn intent is `turn % 2 == 0 ? 16 : 4`**: om de andere beurt slaat hij hard. Je ziet het ritme in de ballon.
- **Een nieuwe kaart Remainder** (1 energie, verbeterd `% 3`): `% 5` op de aanval van een vijand. Een aanval van 16 wordt 1, 23 wordt 3, maar 25 wordt 0. De rest is altijd kleiner dan 5: modulo als verdediging. Werkt op elke vijand, dus ook buiten dit gevecht nuttig.
- **Wat je ontdekt:** `%` geeft de rest, en "elke derde" is `% 3 == 0`.
- **Codex:** een pagina **Modulo** (H2), met jouw moment: een treffer op zijn schild (`2 % 3` is 2) of Remainder (`16 % 5` is 1). Onder zijn balk staat `cards % 3 == 0`.
- **Andere manier om te winnen:** Force Fit naar `byte` en helen, zoals bij Level 256; zijn schild houdt schade tegen, geen heling.

### Gebouwd: de Tweelingschutters (act 1)

Gebouwd op 4 oktober 2026, in het spel **Twin Shooters**, 34 HP. Twee schutters met één teller, `shots`, die onder hun balk staat.

- **Hun intent is `shots++ + ++shots`**, echt uitgerekend door C#. De linkse slaat met de oude waarde, de rechtse telt eerst op. Met `shots = 1` is dat `1 + 3` = 4, en daarna is `shots` 3. Elke beurt slaan ze dus 4 harder: 4, 8, 12, 16.
- **Wrong Label of Remainder** overschrijft hun aanval, en dan telt hun teller die beurt niet mee: de expressie liep niet.
- **Twee nieuwe kaarten met een gedeelde teller** (`count`, begint elk gevecht op 3): **Hit, Then Tighten** (0 energie, `count++`: slaat 3 en draait de teller verder) en **Tighten, Then Hit** (1 energie, `++count * 2`: eerst verder, dan slaan met de nieuwe waarde, dus 8). De kaart toont altijd wat ze nu zou slaan. Het ontwerp "postfix vroeg in de beurt, prefix als afmaker" volgt vanzelf: elke goedkope Hit, Then Tighten maakt de afmaker 2 zwaarder. Op de kaarten staat geen code, alleen woorden; het log toont `count++`.
- **Codex:** de pagina **++ and --** (H2). Ze gaat ook open bij de Tally Counter, die na elke winst `++count` heelt.
- **Andere manier om te winnen:** snel slaan voor ze op toeren komen, of blokken en Remainder op hun zware beurten.

### Gebouwd: De Naamloze (elite, act 1)

Gebouwd op 4 oktober 2026, in het spel **The Nameless**, 44 HP, nog zonder echte bug (de andere elites hebben er een). Zijn variabele heet `shadow`, maar elke beurt wordt hij anders aangesproken, in een vaste kring: `shadow`, `Shadow`, `shadow`, `2shadow`, `shadow`, `sha-dow`. De naam van deze beurt staat onder zijn balk.

- **Klopt de naam niet, dan weigert elke kaart die hem viseert**, met de echte reden: `Shadow` is een andere naam (hoofdletters tellen), `2shadow` begint met een cijfer, `sha-dow` heeft een teken dat niet mag. Een gereserveerd woord (`class`) zou ook weigeren. Dat is de compilefout uit de visie: een ongeldige zet weigert.
- **Op die beurten slaat hij hard** (14, 15, 16), op de andere licht (9, 9, 10). Je blokt dus op de verkeerde namen en zet modifiers klaar (Spare Screw, Second Pair of Hands): die wachten tot de naam weer klopt.
- **Codex:** de pagina **Identifiers** (H2), met jouw moment: welke naam je probeerde en hoe hij echt heet.
- **Geschrapt:** de Etiketmaker (een relic die zijn volgende naam toont; de naam staat al in beeld) en het event De Etiketkamer (een vraag als poort).

### Gebouwd: de patch met `const` (act 1 en verder)

Gebouwd op 4 oktober 2026. Overschrijf je de aanval van dezelfde soort vijand drie keer in een run (Wrong Label of Remainder), dan komt er tussen twee gevechten een revisie: de melding "Patch notes: …'s attack is const now". Vanaf dan staat `const attack` onder zijn balk, en weigeren Wrong Label en Remainder op hem: `attack = 1` compileert niet meer. De Codex-pagina **Constants** (H2) opent bij die eerste weigering. Wie op één truc leunt, moet een tweede vinden.

### Gebouwd: Move the Brackets (act 1)

Gebouwd op 4 oktober 2026, de kaart Haakjes uit het ontwerp. **Move the Brackets** (1 energie, verbeterd 0) verschuift de haakjes in de aanval van een vijand, voor deze beurt. Niet elke aanval heeft haakjes om te verschuiven; waar het kan, rekent C# de nieuwe groepering zelf uit:

| Vijand | Aanval | Met de haakjes elders |
| --- | --- | --- |
| The Reckoner | `(3 + 2) * 4` = 20 | `3 + 2 * 4` = 11 |
| The Reckoner | `17 / 5 + 17 % 5` = 5 | `17 / (5 + 17) % 5` = 0 |
| The Splitter | `30 / (block + 1)` | `30 / block + 1`: met 5 blok 7, zonder blok een `DivideByZeroException` en geen aanval |
| Dripper | `energy * 4 + 2.5` | `energy * (4 + 2.5)`: zonder energie 0, maar elke energie die je overhoudt kost 6.5 |

Zo wordt de tablet van de Rekenmeester jouw wapen, zoals het ontwerp van fase 3 vroeg. De Codex-pagina Operator precedence opent ook bij een omgeschreven aanval; de Splitter door nul laten delen geeft het ✗-paneel *Do not divide by zero*. Operatorvoorrang in je eigen aanval (modifiers met voorrang in plaats van van links naar rechts) is niet gebouwd: zie [ideeen.md](../../../ideeen.md#the-card-hall).

## Elites en baas

Elites combineren een regel met druk. De baas test of je expressies kan lezen terwijl alles tegelijk gebeurt.

Dit is het oorspronkelijke ontwerp van act 1. Effective Power en Flight 501 zijn sindsdien verhuisd naar act 2 en act 3, De Naamloze is niet gebouwd, en de Rekenmeester heet in het spel The Reckoner. Wat nu in act 1 zit, staat in de tabel bij [Act 1](#act-1-de-vatenvallei).

### Elite: Level 256 (overflow)

250 van 255 HP, onkwetsbaar voor schade, heelt zichzelf elke beurt met 2. Na een paar beurten zit hij op 255 en zijn volgende heling laat hem klappen. Wie het doorheeft, speelt een Herstel-kaart op hem en wint in één beurt. Wie het niet doorheeft, kan hem ook uitzitten, maar verliest onderweg veel HP.

Vroeger De Byte-Golem. Hij is nu een speelhalkast waarvan de rechterhelft van het scherm in brokken uiteenvalt: de echte bug van Pac-Man, waar het levelnummer een `byte` is en level 256 het doolhof breekt. De Codex vertelt dat verhaal na het gevecht.

### Elite: Effective Power (tekst en Unicode)

Een bericht dat ontploft als je het leest. Zijn HP is een `string`, `"effective. Power"`: elke treffer plakt eraan vast, net als bij The Label. Maar hier is plakken de bedoeling: vanaf 32 tekens crasht het bericht, zoals de echte bug van 2015 waarbij één reeks Unicode-tekens iPhones liet crashen zodra ze de tekst probeerden te tonen. Een Whack plakt één teken (`"6"`), Floating Bolts drie keer `"2.5"`, samen negen. Wie snapt dat `"2.5"` drie tekens is, wint snel; wie gewoon hard slaat, niet. Ink werkt hier wel, want tekst op tekst is geldige C#. De HP-balk loopt vol naar de crash: 16/32. Gebouwd op 3 oktober 2026; de Codex-pagina String length opent na de crash. Het oorspronkelijke idee (teken per teken afbreken via `char`) wacht op de letterkaarten.

### Elite: Flight 501 (casting, act 3)

Een raket met 506 HP als `int`, te sterk om met schade te verslaan. Wie hem naar `byte` omgiet, ziet 506 terugspringen naar 250, en dan is hij te doen. Het is de echte bug van Ariane 5 (1996): een getal dat niet in zijn nieuwe type paste. In spike 4 tot 6 heette hij de Tinnen Kolos.

### Elite: De Naamloze (identifiers)

Een schaduw die zich verbergt achter drie etiketten: `Schaduw`, `schaduw` en `_schaduw`. Alleen het echte etiket is raakbaar, en het wisselt elke beurt. Zijn intent verraadt welk etiket hij bedoelt. Hoofdlettergevoeligheid is hier letterlijk een kwestie van leven en dood.

### Baas: De Rekenmeester (expressies en operatorvoorrang)

Een oude rekenaar met een stenen tablet. Zijn intents zijn steeds langere expressies, en als enige vijand toont hij het totaal niet: hier is rekenen bewust de kern van het gevecht.

1. **Fase 1:** `3 + 2 × 4` en vergelijkbaar. Wie van links naar rechts rekent, blokt 20 terwijl er 11 komt, en verspilt kaarten.
2. **Fase 2:** integer deling en modulo komen erbij: `17 / 5 + 17 % 5`. Hij splitst zijn aanval over beurten.
3. **Fase 3:** hij schrijft zijn expressie om elke beurt. Met de relic of kaart Haakjes kan je zijn tablet bijsturen: `(3 + 2) × 4` wordt dan jouw wapen in plaats van het zijne.

Wie de Rekenmeester verslaat, heeft zonder het te merken tientallen C#-expressies juist geëvalueerd onder tijdsdruk.
