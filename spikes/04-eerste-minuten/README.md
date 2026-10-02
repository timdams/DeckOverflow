# Spike 4: de eerste minuten

Spike 3 bouwde een hele act, maar het begin speelde stroef. Het starterdeck had drie kaarten die in het eerste gevecht niets deden (twee keer Omgieten en Herstel), en het eerste aha-moment, een byte die omklapt, kwam pas bij een elite. Spike 4 werkt aan de eerste minuten, naar het voorbeeld van Slay the Spire: elke starterkaart werkt meteen, je kiest al iets voor het eerste gevecht, en het wonder gebeurt vroeg.

Daarnaast is het spel nu Engelstalig. We ontwikkelen en documenteren in het Nederlands; alleen wat de speler ziet, is Engels.

Vertrekpunt was een kopie van [spike 3](../03-vatenvallei/). Die blijft ongewijzigd.

## Wat spike 4 moet aantonen

- Na 2 minuten weet een tester wat elke kaart in zijn hand doet, zonder uitleg.
- De keuze in de Gieterij voelt als een eigen start. Kiezen testers verschillend, en kunnen ze zeggen waarom?
- De Bottomless Jug geeft een "wacht, wat?"-moment. Zeggen testers iets hardop als de kruik omklapt, en snappen ze daarna waarom?
- Een `?` op de map wordt aantrekkelijk na de kruik, niet verdacht.

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

De seed staat in de URL (`/?seed=255`). Om één vijand snel te proberen: `/?fight=jug` (of `slime`, `knight`, `ghost`, `dripper`, `colossus`, `golem`, `reckoner`). Dat slaat de Gieterij over.

## Wat er veranderde

### Het starterdeck

| Spike 3 | Spike 4 |
| --- | --- |
| 3× Slag, 2× Vlottende Slag, 2× Schild, Herstel, 2× Giet om | 4× Strike, 2× Floating Strike, 3× Shield, 1× Add |

Add (Voeg toe, +3 op je volgende kaart) is de "Bash" van dit deck: de enige kaart met een eigen regel, en die regel zie je al in je eerste beurt. Remold (Giet om) en Mend (Herstel) zijn nu gewone beloningen. Een test bewijst dat elke starterkaart iets doet tegen de vijanden van rij 1 en 2.

### De Gieterij

Voor de eerste knoop kies je één van drie dingen, zoals bij Neow:

| Keuze | Wat |
| --- | --- |
| A fresh mold | 1 kaart uit 3, ongewoon of zeldzaam |
| Take *relic* | Een relic die je vooraf ziet, met zijn tekst |
| A pouch of 100 gold | Voor de winkel of het verwijderen van een kaart |

De Gieterij is een gewoon event (`Adventures.Foundry`) dat bij de start openstaat. De kaartkeuze gebruikt het bestaande beloningsscherm.

### De Bottomless Jug

Een onbekende knoop in rij 2 of 3 is altijd de Bottomless Jug: een `byte` met 200/255 HP die na elke aanval 40 drinkt. Na twee beurten past 240 + 40 niet meer in een byte en blijft er 24 over. Hij heelt meer dan een starterdeck per beurt kan slaan, dus hij klapt altijd om, wat de speler ook doet. Daarna is hij met een paar slagen weg. Hij valt aan met `7 / 2` (3, integer deling) en `2 * 2`.

De kruik komt één keer per run; een tweede vroege `?` wordt een gewoon event. Na de kruik krijg je de beloning van een gewoon gevecht.

### Elk gevecht te winnen

De Tinnen Kolos (506 HP) is alleen te verslaan door hem naar `byte` om te gieten. Nu Omgieten niet meer in het starterdeck zit, krijg je in zijn plaats de Byte-Golem als je geen kaart hebt die naar byte giet. Het wordt de Kolos zodra je Remold (byte) of Byte Trap hebt.

## Keuzes tijdens spike 4

- **Spelteksten staan in `wwwroot/text/en.json`.** De motor kent geen spelteksten: hij geeft sleutels en getallen (`TextRef`), de shell (`Text/Strings.cs`) en de stage (`stage/strings.js`) maken er zinnen van. Een plaatshouder `{amount}` is de waarde zelf; `{card:from}`, `{relic:…}` en `{enemy:…}` zoeken de naam op bij een id. Een vertaling wordt later één extra bestand.
- **Ids zijn Engels**, want ze zijn de sleutels: `strike`, `floating-strike`, `remold-byte`, `jug`, `scrap-pouch`. Commentaar, XML-docs en testnamen blijven Nederlands.
- **Een kaart heeft geen naam of tekst meer in de code.** De naam hangt aan de id (`card.strike`, een `+` erachter voor de verbeterde versie), de tekst volgt uit het effect (`CardText`). Een verbeterde of omgegoten kaart heeft zo altijd het juiste getal op de kaart, zonder dat iemand het twee keer typt.
- **Relics zijn klassen met haken** (`Relic`): `MakesFree`, `OnDamageTruncated`, `TryFire`, `BlockAtCombatStart`, `MaxHpOnGain`. `Combat` en `Run` kennen geen enkele relic meer bij naam. Een gevecht krijgt verse exemplaren, dus een teller begint per gevecht opnieuw. Bij twee relics die dezelfde kaart gratis maken, wint de eerste in de catalogus.
- **Weigeringen zijn sleutels.** `PlayRejected.Reason` en `RunRejected.Reason` zijn nu bv. `reject.no-energy`. Het eventcontract blijft plat.
- **Een test bewaakt `en.json`.** Elke kaart, vijand, relic, knoop en elk event moet een tekst hebben, en elke plaatshouder moet een waarde krijgen. Een tweede test zoekt elke sleutel die letterlijk in motor, shell of stage staat en eist dat die in `en.json` bestaat.
- **Omgieten heet in het Engels "Remold"** en de gegoten kaarten "Molded Strike" en "Molded Rain". Het woord "cast" blijft voor de Codex, zoals het GDD vraagt.
- **Gevechtstests gebruiken een eigen testdeck** (`TestHelpers.TestDeck`, het starterdeck van spike 3), omdat ze Omgieten en Herstel nodig hebben.

## Namen

| Spike 3 | Spike 4 |
| --- | --- |
| Slag, Vlottende Slag, Zware Slag, Vlottende Regen | Strike, Floating Strike, Heavy Strike, Floating Rain |
| Schild, Dik Schild, Herstel | Shield, Thick Shield, Mend |
| Giet om, Byteval, Gegoten Slag | Remold, Byte Trap, Molded Strike |
| Zet op 1, Voeg toe, Verdubbel | Set to 1, Add, Double Up |
| Slijmklodder, Tinnen Ridder, Vlottende Geest, Druppelaar | Slime Blob, Tin Knight, Floating Ghost, Dripper |
| Tinnen Kolos, Byte-Golem, De Rekenmeester | Tin Colossus, Byte Golem, The Reckoner |
| Teller, Vlottende Komma, Restzak, Ankervat, Grote Pot | Counter, Floating Point, Scrap Pouch, Anchor Barrel, Great Pot |
| De Smeltkroes, Het Lekkende Vat | The Crucible, The Leaking Barrel |

Dit zijn eerste voorstellen; ze staan allemaal in `en.json` en zijn dus zonder code te veranderen.

## Open

- Is de kruik te makkelijk? Hij kan je niet echt pijn doen. Dat is bewust (het is een wonder, geen gevaar), maar misschien voelt het als een gratis gevecht.
- Ziet een tester die de kruik mist het wonder nog op tijd? Hij zit achter een `?` in rij 2 of 3, en niet elk pad heeft er een.
- Is 100 goud in de Gieterij te veel naast een relic?
- De kruik gebruikt voorlopig de sprite van de Druppelaar, groter.
- De Gieterij, de kruik en de Engelse teksten zijn in de browser doorgeklikt; de rest van de act nog niet opnieuw.

Art en geluid blijven placeholders.
