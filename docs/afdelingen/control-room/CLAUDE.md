# The Control Room: regels voor wie eraan werkt

Geldt bovenop de [CLAUDE.md op de root](../../../CLAUDE.md). Het ontwerp staat in deze map: [README.md](README.md) (loop, natuurwetten, de gevechten), [events.md](events.md), [todo.md](todo.md); ideeën staan in [ideeen.md op de root](../../../ideeen.md#the-control-room).

## Waar de code staat

- **Motor:** `src/DeckOverflow.ControlRoom` (verwijst alleen naar `DeckOverflow.Core`): `Gambits/` (regels, voorwaarden, `Duel`, events, `DuelRecording`), `Levels/` (`LevelCatalog`, `Solver`, `Histogram`), `Achievements/XRegister.cs` (haar ✗-panelen).
- **Shell:** `Features/ControlRoom/` (`ControlRoomPage` op `/control-room`, `RuleBoard` met de gereedschapsbak, `RuleText` voor zinnen en C#). `Interop/ControlRoomStage.cs` is de enige brug naar haar stage.
- **Stage:** `wwwroot/control-room/stage/stage.js` (PixiJS). Ze deelt de beeldtaal, de tekeningen en de teksten (`wwwroot/shared/`) en het geluid (`wwwroot/audio/`) met de andere stages.
- **Data:** `wwwroot/control-room/solutions.json`, het histogram van alle winnende borden, gemaakt door `HistogramTests`.
- **Tests:** `tests/DeckOverflow.ControlRoom.Tests`.

## Ontwerpregels die alleen hier gelden

- **Geen RNG.** Een duel is volledig bepaald door de twee regelborden. Dat maakt de oplosser, de tijdlijn en een eerlijk klassement mogelijk; voeg geen toeval toe zonder dat eerst voor te leggen.
- **Een regel die klopt, voert altijd uit**, ook als de zet niets doet. Daarop drijven de Hersteller en Knight Capital.
- **Elk gevecht: "alleen meppen" verliest nipt, en er zijn meerdere winnende borden.** Bijstellen gebeurt met de oplosser, nooit op gevoel. Faalt een test in `LevelTests` na een getal, kijk dan eerst wat het gevecht nu doet.
- **Kleur blijft voorbehouden aan types.** Een regel die vuurt, laat haar lamp branden in inkt. HP is een `int`, dus de balk is int-blauw.
- **Het scherm volgt [de beeldtaal](../../wereld/beeldtaal.md):** twee schakelkasten en een testcel (zie [Het scherm](README.md#het-scherm-twee-kasten-en-een-testcel)). Een nieuw stuk op het scherm bouw je uit de gedeelde bouwstenen, niet met een eigen stijl.
- **Eerst ervaren, dan benoemen.** Op het bord staan woorden ("als", "EN", "NIET"); `if`, `&&` en `!` verschijnen alleen onder de motorkap en in de Codex.

## Een gevecht toevoegen of bijstellen

1. `LevelCatalog.All`: vijand, zijn regels, de woordenschat (checks, zetten, EN/OF, NIET) en het aantal vakjes. Een elite krijgt `Bug: true`.
2. Teksten in `nl.json` en `en.json`: `room.enemy.<key>`, `room.flavor.<key>`, en voor een elite `bug.<key>.title` en `.story`. Een tekening onder `actors` in `art.json`, of een regel in [art/todo.md](../../../art/todo.md).
3. Tests in `LevelTests`; daarna het histogram vernieuwen: `DECKOVERFLOW_WRITE_SOLUTIONS=1 dotnet test tests/DeckOverflow.ControlRoom.Tests --filter HistogramTests`.
4. Een nieuw event: in `DuelEvent.cs`, in [events.md](events.md), in de handlers van `stage.js` en in het log van `ControlRoomPage`.
5. De Codex- en ✗-toets uit de root-CLAUDE.md, en de tabel in [README.md](README.md).
