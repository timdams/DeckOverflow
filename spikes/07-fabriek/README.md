# Spike 7: niet volgens de handleiding

Spike 6 zette het spel in de taal van een montagehandleiding, maar het bleef Slay the Spire met een middeleeuws thema: Strike, Block, zwaarden en schilden. Alleen het rekenwerk was C#. Spike 7 geeft het spel een thema dat zelf uit software komt.

**De wereld is een fabriek die alles volgens de specificatie bouwt. Jij bent het figuurtje uit de handleiding, en je doet precies wat de ✗-panelen verbieden.** Wat de handleiding verbiedt maar C# toelaat, werkt. De grote vijanden zijn echte, beroemde bugs.

Het ontwerp staat in het [GDD](../../docs/game-design-document.md), sectie "Thema: niet volgens de handleiding". Vertrekpunt was een kopie van [spike 6](../06-handleiding/), die ongewijzigd blijft. De motor is niet veranderd: alleen namen, tekeningen en de shell.

## Wat spike 7 moet aantonen

- Voelt het spel nog als een middeleeuws vechtspel, of als iets eigens?
- Lezen testers de kaartnamen als grap of als verwarring? Weten ze wat "Spare Screw" doet zonder de tekst te lezen?
- Werkt het verhaal van een echte bug na het gevecht als beloning, of slaan testers het over?

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

De seed staat in de URL (`/?seed=255`). Om een bug snel te proberen: `/?fight=golem` (Level 256) of `/?fight=colossus` (Flight 501).

## Wat er veranderde

### Kaarten: overtredingen in plaats van wapens

| Id | Was | Is nu |
| --- | --- | --- |
| `strike` | Strike | Whack |
| `heavy-strike` | Heavy Strike | Hammer It In |
| `floating-strike` | Floating Strike | Floating Bolts |
| `floating-rain` | Floating Rain | Floating Parts |
| `molded-strike` / `molded-rain` | Molded Strike / Rain | Molded Bolts / Parts |
| `shield` | Shield | Hold Firmly |
| `thick-shield` | Thick Shield | Two-Person Lift |
| `mend` | Mend | Fill Past the Line |
| `add` | Add | Spare Screw |
| `double-up` | Double Up | Second Pair of Hands |
| `remold-int` / `remold-byte` | Remold | Force Fit |
| `set-to-1` | Set to 1 | Wrong Label |
| `byte-trap` | Byte Trap | Squeeze In |

De ids in de motor blijven; alleen `en.json` en de tekeningen veranderden. Elke tekening toont het figuurtje dat iets doet wat niet mag.

### Vijanden: echte bugs

| Motorsleutel | Naam | Tekening | Echte bug |
| --- | --- | --- | --- |
| `golem` | Level 256 | speelhalkast waarvan het scherm in blokjes uiteenvalt | Pac-Man (1980), het levelnummer is een `byte` |
| `colossus` | Flight 501 | raket die panelen verliest | Ariane 5 (1996), een te groot getal omgezet naar een te klein type |
| `reckoner` | The Reckoner | de inspecteur met een klembord | (geen bug: de fabriek zelf) |

Na een gevecht tegen een bug toont het beloningsscherm een **Codex**-kader met het echte verhaal (`bug.<sleutel>.title` en `.story` in `en.json`). Gewone vijanden hebben geen verhaal en tonen niets. De shell onthoudt welke vijand je bevocht uit de snapshot van het gevecht; de motor weet er niets van.

The Index (Vancouver Stock Exchange, afkappen) heeft al een tekening (`actors/the-index.png`), maar nog geen gevecht: dat is motorwerk.

### Beeld

- Map: een gevecht is de Whack-tekening, een elite het doorstreepte instructieblad, de baas de inspecteur. Geen zwaarden, helm of kroon meer.
- De intentballon toont de Whack-tekening in plaats van gekruiste zwaarden.
- Blok is een vastgeschroefde, gearceerde plaat in plaats van een schild.
- De held draagt een moersleutel in plaats van een zwaard. De andere drie varianten staan in `art/actors/hero-*.png`; wisselen is één regel in `art.json`.
- De bugs kijken op hun vel naar rechts; het snijscript spiegelt ze, zodat ze de held aankijken en de brokken naar hem toe vliegen.

## Art

Twee nieuwe vellen (GPT-5.4 Image 2, met het manualvel als referentie):

| Vel | Raster | Inhoud |
| --- | --- | --- |
| `violations` | 5×3 | de dertien kaarten als overtredingen, plus een doorstreept en een afgestempeld instructieblad voor de revisies |
| `bugs` | 2×2 | Level 256, Flight 501, The Index, de inspecteur |
| `heroes` | 2×2 | de held zonder zwaard: met moersleutel (gekozen), schroevendraaier, opgestroopte mouwen, gereedschapskist |

```bash
python tools/cut_sheets.py
```

## Open punten

- **De revisies** (`rev-crossed`, `rev-stamped`) zijn getekend maar nog niet gebruikt. Ze horen bij "de wereld patcht zich", en dat bestaat nog niet in de motor.
- **De iconen van intents** voor blok en heal zijn nog een schild en een flesje.
- **Het Codex-kader** is gebouwd en getest met de build en de tests, maar nog niet in de browser gezien na een gewonnen elite.
