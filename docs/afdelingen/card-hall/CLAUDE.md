# The Card Hall: regels voor wie eraan werkt

Geldt bovenop de [CLAUDE.md op de root](../../../CLAUDE.md). Het ontwerp staat in deze map: [README.md](README.md) (core loop, kernsysteem), [act-1.md](act-1.md), [act-2.md](act-2.md), [act-3.md](act-3.md), [kaarten.md](kaarten.md), [events.md](events.md), [ideeen.md](ideeen.md), [todo.md](todo.md).

## Waar de code staat

- **Motor:** `src/DeckOverflow.CardHall` (verwijst alleen naar `DeckOverflow.Core`): `Combat/` (gevecht, `Bestiary`, intents), `Cards/`, `Relics/`, `Maps/`, `Runs/` (run, acts, score), `Events/`, `Commands/`, `Achievements/XRegister.cs` (haar ✗-panelen en hoe ze die herkent).
- **Gedeeld, in Core:** `Values/` (getypeerde waarden, `IntRules`, `ByteRules`, `CastRules`), `Random/`, `Text/`, `Codex/` (de catalogus van alle pagina's), `Achievements/` (`XPanel`, `IXPanelSource`). Wijzig je daar iets, dan raakt het elke afdeling.
- **Shell:** `Features/CardHall/` (kaarten, map, deckkeuze). `Pages/RunPage.razor(.cs)` speelt de run, maar toont ook titelscherm en plattegrond: die splitsing staat in [de todo van de wereld](../../wereld/todo.md#code-modulair-maken).
- **Stage:** `wwwroot/card-hall/stage/` (PixiJS): `timeline.js` speelt events af, `log.js` schrijft het log, `juice.js` heeft de getallen voor de feel.
- **Tests:** `tests/DeckOverflow.CardHall.Tests`.

## Ontwerpregels die alleen hier gelden

- **Geen code in beeld in act 1.** Kaarten tonen getallen, letters en woorden. Echte C# verschijnt pas in de Codex.
- **In het spel heet casting "Omgieten"** (Force Fit); het woord "cast" verschijnt pas in de Codex, vanaf act 3.
- **Rekenen is gereedschap, nooit de taak.** Intents tonen standaard het totaal; alleen de Reckoner verbergt het.
- **De getypeerde aanval volgt echte C#:** tekst raakt geen getal (de kaart weigert), een crash (in beeld nooit "exception", zie [README](README.md#exceptions-en-de-call-stack)) beëindigt je beurt. Modifiers wachten over je beurt heen; Scrap veegt ze weg.
- **Een Codex-pagina heeft een minimale act** (`MinAct`): je voelt de regel vroeger dan je hem benoemd ziet.

## Een vijand, kaart of relic toevoegen

1. Motor: `Bestiary` of `CardCatalog` of een `Relic`-klasse, en in de juiste act (`Acts.cs`). Een kaart heeft een verbeterde versie nodig.
2. Teksten in `nl.json` en `en.json` (`enemy.*`, `card.*`, `effect.*`, `relic.*`, en `bug.*` voor een elite). Een tekening in `art.json`, of een regel in [art/todo.md](../../../art/todo.md).
3. Een nieuw event: in `GameEvent.cs`, in [events.md](events.md), en in `timeline.js` en `log.js`.
4. Tests in `tests/DeckOverflow.CardHall.Tests`.
5. De Codex- en ✗-toets uit de root-CLAUDE.md, en de docs in deze map bijwerken.
