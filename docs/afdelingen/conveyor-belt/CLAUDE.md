# The Conveyor Belt: regels voor wie eraan werkt

Geldt bovenop de [CLAUDE.md op de root](../../../CLAUDE.md). Het ontwerp staat in deze map: [README.md](README.md), [events.md](events.md), [todo.md](todo.md); ideeën staan in [ideeen.md op de root](../../../ideeen.md#the-conveyor-belt).

## Waar de code staat

- **Motor:** `src/DeckOverflow.ConveyorBelt` (verwijst alleen naar `DeckOverflow.Core`): `Belts/` (waarden en bewerkingen, stukken, `Level`, `LevelCatalog`, `Simulator` met de Codex-momenten), `Achievements/XRegister.cs` (haar ✗-panelen).
- **Shell:** `Features/ConveyorBelt/` (`ConveyorBeltPage` op `/conveyor-belt`, `PieceText` voor de tekens, `PieceJson` om een bord te bewaren). `Interop/ConveyorBeltStage.cs` is de enige brug naar haar stage.
- **Stage:** `wwwroot/conveyor-belt/stage/stage.js` (PixiJS): tekent het hele rooster en meldt klikken terug aan de shell (`CellClicked`). Woorden krijgt ze van de shell (`labels` in `setLevel`), nooit zelf.
- **CSS:** `wwwroot/css/conveyor-belt.css`, alles onder `.belt-room`; de kop en de stappen deelt ze met `control-room.css`.
- **Tests:** `tests/DeckOverflow.ConveyorBelt.Tests`.

## Ontwerpregels die alleen hier gelden

- **Geen machine "loop".** Een loop is een band die terugkomt. Een nieuwe zet of machine die zelf herhaalt, eerst voorleggen.
- **Elke bestelling heeft meerdere testgevallen,** zodat je een echte loop bouwt en geen antwoord vast legt.
- **Een oneindige loop wordt bewezen, niet geraden:** de motor bewaart elke toestand. Een vangnet van 5000 tikken mag nooit afgaan.
- **Geen RNG.**
- **Het product is een vijand uit de Card Hall** (`Level.Product` is een `enemy.<key>`): zijn tekening komt uit `art.json`, zijn naam uit `enemy.*`.
- **Geen groen of rood** (beslist op 4 oktober 2026): gelukt is een zwarte stempel, mislukt een oranje ✗, zoals de rest van het spel. Het scherm volgt [de beeldtaal](../../wereld/beeldtaal.md); zie [Het scherm](README.md#het-scherm-de-werkvloer).

## Een bestelling toevoegen of bijstellen

1. `LevelCatalog.All`: rooster, bron en uitgang, testgevallen, gereedschapsbak, eventueel vaste stukken (`Fixed`) en een startbord (`Start`), het product, en `Bug: true` voor een elite.
2. Een bord dat werkt in `LevelTests.Solutions()`, en een test die toont wat de bestelling leert.
3. Teksten in `nl.json` en `en.json`: `belt.level.<key>.name` en `.brief` (de bestelbon), voor een elite `bug.<key>.title` en `.story`; een nieuw product heeft `enemy.<key>` en een tekening onder `actors` in `art.json`.
4. Een nieuw event: in de stage en in [events.md](events.md).
5. De Codex- en ✗-toets uit de root-CLAUDE.md, en de tabel in [README.md](README.md).
