# Spike 3: de Vatenvallei, een hele act

Spike 2 testte één gevecht. Spike 3 test de core game loop eromheen: een map met paden kiezen, vechten, een beloning kiezen, en dat herhalen tot de baas. HP, goud, deck en relics lopen door van knoop naar knoop. De vraag is of je na een run meteen nog een wil spelen. De scope staat in het [Spike Design Doc](../../docs/spike-design-doc.md#spike-3-de-hele-act).

Vertrekpunt was een kopie van [spike 2](../02-omgieten/). Die blijft ongewijzigd.

## Wat spike 3 moet aantonen

- Testers kiezen hun pad bewust: ze wegen een elite tegen een rustvuur, een winkel tegen een gevecht. Laat ze hardop denken op de map.
- Beloningen voelen als een keuze, niet als "neem de beste". Wordt Overslaan ooit gekozen?
- Een run duurt 10 tot 15 minuten, en na een verloren run klikt de tester zelf op "Nieuwe run".
- De types en Omgieten uit spike 2 blijven bruikbaar over een hele run, niet alleen in één gevecht.

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

De seed staat in de URL (`/?seed=255`): dezelfde seed geeft dezelfde map, dezelfde gevechten en dezelfde beloningen bij dezelfde keuzes. Zonder seed kiest het spel er een. Om één vijand snel te proberen: `/?gevecht=rekenmeester` (of `slijm`, `ridder`, `geest`, `druppel`, `kolos`, `golem`) geeft een run van één gevecht plus een rustvuur.

Bediening in het gevecht zoals in spike 2: sleep een kaart op een doelwit, **Einde beurt** rechtsonder, **F** snelle modus, **M** geluid. Op de map klik je een knoop die oplicht.

## De act

Een map van 8 rijen plus de baas, met 4 paden die elkaar raken maar nooit kruisen. Vaste rijen geven ritme: onderaan altijd een gevecht, halverwege een schat, bovenaan een rustvuur voor de baas. Elke map heeft minstens 2 elites, een winkel en een event. Elites komen pas vanaf rij 4, en een elite, winkel of rustvuur volgt nooit op dezelfde soort.

| Knoop | Wat er gebeurt |
| --- | --- |
| Gevecht | Rij 1 en 2: Slijmklodder of Tinnen Ridder. Verderop: Ridder, Vlottende Geest of Druppelaar. Daarna 12 tot 20 goud en 1 kaart uit 3 |
| Elite | Tinnen Kolos of Byte-Golem. 28 tot 40 goud, een relic, en meer kans op zeldzame kaarten |
| Rustvuur | Herstel 30% van je max HP, of verbeter een kaart (je ziet de verbeterde versie vooraf) |
| Onbekend | De Smeltkroes of Het Lekkende Vat |
| Winkel | 2 gewone, 2 ongewone en 1 zeldzame kaart, een relic, en één kaart laten verwijderen voor 75 goud |
| Schat | Een kist met 20 tot 30 goud en een relic |
| Baas | De Rekenmeester: hij toont zijn totaal niet |

### Vijanden

| Vijand | Type | HP | Aanvallen, om beurten | Eigenheid |
| --- | --- | --- | --- | --- |
| Slijmklodder | `int` | 20 | `2 * 3`, `4 + 4` | Om in te komen |
| Tinnen Ridder | `int` | 30 | `5 + 3`, `10 / 3` | Schild van 5.5 dat zijn `int` afkapt tot 5. Vlottende kaarten verliezen hier hun decimalen |
| Vlottende Geest | `double` | 24.5 | `9 / 2.0`, `13 / 2.0` | Uit spike 2: een decimaal schild |
| Druppelaar | `double` | 19.5 | `5 * 1.5`, `2.5 + 2.5` | Halve schade die jouw `int`-HP afkapt |
| Tinnen Kolos (elite) | `int` | 506 | `12 + 3 * 2` | Uit spike 2: Omgieten naar `byte` en dan helen |
| Byte-Golem (elite) | `byte` | 250/255 | `15 / 2.0` | Uit spike 1: heelt +2 per beurt tot hij omklapt |
| De Rekenmeester (baas) | `int` | 90 | `3 + 2 * 4`, `(3 + 2) * 4`, `17 / 5 + 17 % 5`, `2 * 3 + 4 * 2` | Het totaal blijft verborgen: `= ?` |

### Kaarten

Het starterdeck is dat van spike 2. In de beloningen, de winkel en de Smeltkroes zitten nieuwe kaarten, elk rond één regel. Aan het rustvuur krijgt elke kaart één verbetering, met een `+` achter de naam.

| Kaart | Kost | Effect | Concept |
| --- | --- | --- | --- |
| Zware Slag | 2 | 14 schade (`int`) | |
| Vlottende Regen | 1 | 4× 1.5 schade (`double`) | Op een `int` blijft er 4× 1 over |
| Dik Schild | 2 | 13 blok | |
| Zet op 1 | 1 | De aanval van een vijand wordt deze beurt 1 | Toekenning `=` |
| Voeg toe | 0 | Je volgende kaart: +3 | `+=` |
| Verdubbel | 1 | Je volgende kaart: ×2 | `*=`, en de volgorde telt |
| Byteval (zeldzaam) | 2 | Giet een vijand om naar `byte` en herstel hem 6 HP | Overflow in één kaart |
| Gegoten Slag/Regen | 0 | Uit de Smeltkroes: een Vlottende kaart als `int` | Casting: gratis, maar zonder decimalen |

### Relics

| Relic | Effect | Concept |
| --- | --- | --- |
| Teller | Elke derde kaart in een gevecht kost 0 | Modulo en tellen |
| Vlottende Komma | Je eerste Vlottende kaart per gevecht kost 0 | `double` |
| Restzak | Bewaart wat er van jouw schade afgekapt wordt; bij 3 vuurt hij 3 schade af | Afkappen als bron |
| Ankervat | Begin elk gevecht met 6 blok | |
| Grote Pot | +8 max HP | |

## Keuzes tijdens spike 3

- **De run is een tweede motor naast het gevecht**, met hetzelfde patroon: `Run.Handle(ICommand)` geeft events terug, `Run.Snapshot()` is de waarheid. Gevechtscommands gaan door naar het lopende `Combat`. De events van het gevecht en van de run delen één volgnummer.
- **De map volgt uit de seed.** Loot gebruikt een aparte RNG-stroom en elk gevecht een eigen seed, afgeleid van de run-seed en de knoop. Zo verandert de map niet als er een beloning bijkomt. Een test pint de regels van de map vast over 200 seeds.
- **Intents wisselen om beurten.** Een vijand heeft nu een patroon in plaats van één aanval. Zo kan de Rekenmeester zijn expressies afwisselen, en wisselt ook een gewone vijand zijn aanval.
- **Modifiers werken op het getal van de volgende kaart, in volgorde:** eerst +3 en dan ×2 geeft `(6 + 3) × 2 = 18`, omgekeerd `6 × 2 + 3 = 15`. De stage toont de som en rekent ze uit. Een Vlottende Slag krijgt de modifier op elke treffer, en daarna kapt een `int`-doelwit gewoon af. Omgieten en Zet op 1 verbruiken geen modifier.
- **Zet op 1 is toekenning, geen aftrekking:** wat de vijand van plan was, doet er niet meer toe. Het geldt tot hij aanvalt.
- **Byteval op een vijand die al een `byte` is**, slaat het omgieten over en heelt gewoon. Een losse Giet om naar hetzelfde type blijft geweigerd, zoals in spike 2.
- **HP buiten een gevecht zakt nooit onder 1.** Een event kost HP maar doodt je niet; alleen een gevecht kan de run beëindigen.
- **Een verbeterde kaart blijft verbeterd na omgieten:** Vlottende Slag+ (3× 3.5) wordt Gegoten Slag+ (3× 3).
- **De schermen buiten het gevecht zijn HTML in de shell**, boven de stage die altijd klaarstaat. Ze tonen alleen wat de snapshot zegt. Placeholders volstaan: emoji op de map, CSS-kaarten met de typekleuren van de stage.
- **Nieuwe events in het contract.** Gevecht: `IntentAssigned`, `ModifierQueued`, `ModifiersApplied`, `RelicTriggered`, en `IntentRevealed.value` is leeg als het totaal verborgen is. Run: `NodeEntered`, `GoldChanged`, `RunHpChanged`, `CardAdded`, `CardRemoved`, `CardTransformed`, `RelicGained`, `RunRejected`, `RunEnded`. De stage negeert de run-events; de shell maakt er meldingen van.
- **De getallen zijn eerste gokken**: HP en aanvallen in `Bestiary.cs`, prijzen en goud in `Run.cs`, kaarten in `CardCatalog.cs`, de vorm van de map in `MapGenerator.cs`.

## Open

- Is de act te lang of te kort voor een lesblok? Meten tijdens de playtest.
- Is de Rekenmeester met 90 HP haalbaar met een deck na 7 knopen, zonder dat hij een formaliteit wordt?
- Zijn Voeg toe en Verdubbel te sterk op Vlottende kaarten (de modifier telt per treffer)?
- De Codex ontbreekt nog: na een gevecht opent er nog geen pagina.
- Het event-scherm en de schat zijn in de motor getest, maar nog niet in de browser doorgeklikt.

Art en geluid blijven placeholders: eerst moet de core game loop leuk zijn.
