# Spike 1: de Byte-Golem

> Afgesloten spike, bewaard als referentie. De stand bij het afsluiten staat ook in git onder de tag `spike-1`. Nieuwe spelcode komt niet hier, zie de [project-README](../../README.md).

Eén gevecht tegen de Byte-Golem. De spike moest drie vragen beantwoorden (zie het [Spike Design Doc](../../docs/spike-design-doc.md)):

1. Draait een C#-motor in de browser met een aanvaardbare laadtijd?
2. Stuurt die motor een PixiJS-stage aan zonder voelbare interop-vertraging?
3. Voelt één gevecht satisfying genoeg om er nog één te willen?

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

Een seed kies je via de URL: `/?seed=255`. Zonder seed geldt `ByteGolemScenario.DefaultSeed` (255), waarbij Herstel in de eerste hand zit.

## Bediening

- Sleep een kaart op een doelwit. Schild mag je gewoon omhoog slepen.
- **Einde beurt**: knop rechtsonder.
- **F**: snelle modus (alle duurtijden x0,5, behalve hit pause).
- **M**: geluid aan/uit.

## Structuur

```text
DeckOverflow.sln
├─ src/
│  ├─ DeckOverflow.Engine/     pure C#, geen dependencies
│  │  ├─ Values/               ValueKind, ByteRules, IntRules
│  │  ├─ Combat/               Combat, Combatant, Intent, setup, snapshot, ByteGolemScenario
│  │  ├─ Cards/                CardDefinition, Effect, Deck, CardCatalog
│  │  ├─ Commands/             PlayCard, EndTurn
│  │  ├─ Events/               GameEvent en subtypes (eventcontract)
│  │  └─ Random/               SeededRng (PCG32)
│  └─ DeckOverflow.Web/        Blazor WebAssembly shell
│     ├─ Pages/CombatPage      host voor de stage, knoppen, orkestratie
│     ├─ Interop/StageBridge   enige plek die met JS praat
│     └─ wwwroot/
│        ├─ stage/             stage.js, timeline.js, juice.js, audio.js, sprites.js
│        ├─ lib/               PixiJS 8, GSAP 3, Howler 2 (vendored)
│        ├─ audio/             placeholder-sprite, gegenereerd door tools/make_sfx.py
│        └─ fonts/             Press Start 2P, VT323 (OFL)
├─ tests/DeckOverflow.Engine.Tests/
└─ tools/make_sfx.py           synthetiseert de placeholder-geluiden (numpy)
```

## Meetpunten

Linksboven staat een HUD die de succescriteria rechtstreeks meet:

| HUD | Criterium |
| --- | --- |
| `speelbaar na` | Laadtijd tot de Speel-knop klaarstaat. Meet ook met devtools, koude cache. |
| `fps` en `min tijdens laatste animatie` | Vloeiendheid, vooral tijdens de overflow. |
| `klik→frame` | Reactietijd van loslaten tot het eerste animatieframe. |
| `motor` | Tijd die `Combat.Handle` nodig had. |

Downloadgrootte meet je op de publish-output:

```bash
dotnet publish src/DeckOverflow.Web -c Release
```

## Keuzes tijdens de spike

Deze punten wijken af van of vullen het design doc aan:

- **Extra events**: `BlockAbsorbed`, `BlockExpired`, `AttackLaunched`, `TurnStarted`, `PlayRejected` en `CombatEnded`. De stage negeert onbekende types, dus het contract blijft uitbreidbaar.
- **Schade stopt op 0.** Ook voor de `byte`-golem. Underflow (`2 - 6` wordt `252`) is een mooie mechaniek voor later, maar zou de variant uit het design doc (golem op 2, daarna een Slag) breken.
- **Herstel op een `int`-speler gaat niet boven max HP.** Dat is een spelregel. Bij `byte` geldt de typegrens, en die loopt over.
- **Blok is een `int`.** Een halve schade kost een hele blokpunt: 7,5 schade op 10 blok laat 2 blok over.
- **De golem heelt ook via `ByteRules`.** Staat hij op 254, dan doodt hij zichzelf met +2.
- **De stage weigert al ongeldige doelwitten** (een Slag op jezelf landt niet). De motor weigert alleen wat de stage niet kan weten, zoals te weinig energie.
- **Seed-determinisme** is vastgepind in `SeededRngTests.Reeks_is_vastgepind`. Faalt die test, dan veranderen alle bestaande seeds.

## Open bij het afsluiten

- Playtest met 5 studenten en 2 collega's; noteren wie pad B zelf vindt.
- Meten op een gewone schoollaptop.
- Licenties nakijken (PixiJS MIT, Howler MIT, GSAP standaardlicentie, fonts OFL).
