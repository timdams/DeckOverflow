# Spike 2: types en Omgieten

Playtest 1 toonde dat één regel per vijand niet schaalt: tegen een gewone vijand bleef het schade doen en blokken, zoals in Slay the Spire. Spike 2 test het kernsysteem uit het [Game Design Document](../../docs/game-design-document.md#kernsysteem-types-en-omgieten): elk ding heeft een type, en Omgieten verandert dat type. De scope staat in het [Spike Design Doc](../../docs/spike-design-doc.md#scope-spike-2).

Vertrekpunt was een kopie van [spike 1](../01-byte-golem/). Die blijft ongewijzigd.

## Wat spike 2 moet aantonen

- Testers kiezen hun aanvalstype bewust. Laat ze hardop denken.
- Testers vinden zonder hint dat Omgieten plus helen ook buiten de golem werkt.
- Na de test: "Voelde dit gewone gevecht als Slay the Spire?" Het antwoord moet nee zijn.

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

Kies een gevecht via de URL: `/?gevecht=geest` (standaard) of `/?gevecht=kolos`, eventueel met `&seed=255`. Na winst tegen de Geest leidt "Volgend gevecht" naar de Kolos.

Bediening zoals in spike 1: sleep een kaart op een doelwit, **Einde beurt** rechtsonder, **F** snelle modus, **M** geluid.

## De gevechten

Iedereen speelt met hetzelfde deck van 10: 3× Slag (`int`, 6), 2× Vlottende Slag (`double`, 3× 2.5), 2× Schild, Herstel, Giet om → `int`, Giet om → `byte`.

| | Vlottende Geest | Tinnen Kolos |
| --- | --- | --- |
| Type | `double` | `int` |
| HP | 24.5 | 506 |
| Valt aan met | `9 / 2.0` = 4.5 (wordt 4 op jouw `int`-HP) | `12 + 3 * 2` = 18 |
| Eigenheid | Mistschild: 12.5 blok na elke aanval, één beurt geldig | Te groot om plat te slaan |
| Wat hij test | Bewust een aanvalstype kiezen | Omgieten plus helen zelf vinden |

**De Geest.** Als `double` vangt zijn schild alles exact op: een Slag van 6 haalt 6 weg. Giet je hem om naar `int`, dan verliest hij de restjes: 24.5 HP wordt 24, het schild 12. Elke nieuwe Mistschild kapt af tot 12. Maar een schild van een geheel type kost ook een hele punt per halve treffer: Vlottende Slag haalt dan 9 schildpunten weg in plaats van 7.5. Op zijn HP kapt diezelfde Vlottende Slag wel af, van 2.5 naar 2. Wanneer welk type loont, is de puzzel.

**De Kolos.** 506 HP aan 6 per Slag, terwijl hij 18 per beurt terugslaat: dat haal je niet. `(byte)506` is 250, want een byte houdt alleen de rest na 256 over. Daarna doet Herstel wat het bij de Byte-Golem deed: 250 + 6 past niet en klapt om naar 0.

## Keuzes tijdens spike 2

- **Het type van het doelwit beslist**, zoals de typetabel in het GDD zegt. Een `int` of `byte` kapt elke treffer apart af, een `double` neemt alles exact.
- **Een `double`-aanval op een `int`-doelwit kapt af.** Vlottende Slag doet op de Kolos 3× 2, niet 7.5. Het GDD zei bij de kaart nog "negeert afkappen"; dat is aangepast, want een regel buigt nooit.
- **Blok volgt het type van zijn drager.** De speler is een `int`, dus zijn blok is dat ook (zoals in spike 1). Het schild van de Geest is een `double` zolang hij dat zelf is.
- **Omgieten volgt echte C#-conversies** (`CastRules`): `double` naar `int` kapt af, `int` naar `byte` klapt om zoals `unchecked((byte)x)`, en `double` naar `byte` loopt via `int`. Ook het blok verliest zijn restje.
- **Max HP hangt af van het type**: een `byte` heeft max 255, ook als de vijand als `int` 506 had. Giet je terug naar `int`, dan krijgt hij zijn oorspronkelijke max terug.
- **Omgieten naar hetzelfde type wordt geweigerd** zonder energie te kosten, net als een kaart op een verkeerd doelwit.
- **Omgieten kan alleen op een vijand.** Jezelf omgieten is een mechaniek voor later.
- **`TypeChanged` draagt meer dan het doc voorstelde**: HP voor en na, max HP en blok na, en `wrapped` als de waarde omklapte. Een verloren decimaal komt eerst als `ValueTruncated`, zoals elders.
- **HP en blok zijn in het eventcontract `double`**, omdat een `double`-doelwit decimalen houdt. Voor `int` en `byte` staat er nooit iets na de komma.
- **Elk type heeft een vaste kleur en vorm**: `int` blauw en hoekig, `double` paars en rond, `byte` geel met een dubbele rand. Een omgegoten vijand draagt voortaan de kleur van zijn nieuwe type.
- **Wie de Kolos eerst slaat, maakt de truc moeilijker, en dat blijft zo.** Na een Slag wordt 500 als byte 244, en dan brengt één Herstel hem op 250 in plaats van 0. Eerst denken, dan slaan: dat hoort bij de puzzel.
- **`ValueTruncated` zegt wat er afgekapt wordt** (`subject`: `Damage`, `Block` of `Hp`). Zo ziet afgekapte schade er niet meer uit als een afgekapt schild of als omgieten.
- **De getallen zijn eerste gokken**: HP, schild en aanval van beide vijanden staan in `Scenarios.cs` en moeten in de playtest afgesteld worden.

## Na de eerste playtest: leesbaarheid

Testers zagen niet altijd of iets schade kreeg, en de vijand leek soms schade te krijgen zonder aanval. Er was geen bug, maar drie verschillende dingen zagen eruit als schade: afgekapte HP bij omgieten, een afgekapt schild in de vijandbeurt, en de val van 506 naar 250. Daarom:

- **Een samenvatting per actie** boven elk doelwit: "-4 HP", "GEBLOKT · 6 op schild", "+12 schild", "int → byte". De vorige verdwijnt zodra er een nieuwe actie begint.
- **Een gevechtslog** links, één regel per gebeurtenis, bijvoorbeeld "Geest: schild vangt 6 op, 6.5 over".
- **Elke afgekapte waarde zegt wat ze is**: schade boven het hoofd, een schild in cyaan bij het schild, HP in het wit met "(int) HP".
- **Een getal bij elke schildtreffer**, en een grotere HP-balk waarop het verloren stuk even licht blijft staan.

## Open

- De Geest afstellen: is Omgieten naar `int` daar ooit de betere keuze, of alleen een valkuil?
- Playtest met studenten en collega's, met de drie vragen hierboven.

Art en geluid blijven placeholders: eerst moet de core game loop leuk zijn. Omgieten hergebruikt daarom gewoon de geluiden van spike 1.
