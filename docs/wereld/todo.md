# Todo: de wereld

Wat bewust is uitgesteld voor de wereld (plattegrond, backend, Codex, ✗-register), met genoeg context om het later op te pakken. Nieuwste bovenaan. Wat af is, gaat eruit (de geschiedenis staat in git).

## Backend

- **De docent kan alleen nog een klascode maken** (beslist op 3 oktober 2026, zie [backend.md](backend.md)). Wat nu meer doet en eruit moet: afdelingen vrijgeven (`release_department` en `Account.ReleaseAsync`, de knoppen in `AccountPanel.razor`, de kolom `classes.released_departments`, `unlocks.how = teacher`) en de voortgang van de klas lezen (policies op `progress` en `unlocks`). Daarna [backend.md](backend.md) en [supabase.md](supabase.md) bijwerken. Een migratie op het echte project, dus eerst afstemmen met de sessie die de backend bouwde.
- **Oude gastaccounts opruimen.** Elke nieuwe browser (of gewiste opslag) maakt een gast; wie stopt, laat er een achter. Supabase raadt aan om oude anonieme accounts geregeld te verwijderen. Kan mee in de nachtelijke Action van de scorecontrole, met de service-sleutel: anonieme accounts zonder activiteit sinds bv. 90 dagen. Er staat er nu al een: "Bright Spanner 74", van een test op 3 oktober 2026.
- **Klassen beheren.** Een docent kan nu alleen afdelingen vrijgeven, niet terugtrekken, en geen leerling uit de klas halen (de database laat dat wel toe, het scherm nog niet). Een dashboard met de voortgang van de klas komt na de MVP.

- **Scores controleren (stap 7 van [supabase.md](supabase.md)).** Consoleproject `tools/DeckOverflow.ScoreCheck`: haalt scores met `verified = pending`, speelt ze opnieuw af met `Run.Replay`, zet `ok` of `rejected`. Een GitHub Action draait het elke nacht, met de service-sleutel als GitHub-secret. Houdt meteen het gratis project wakker (pauzeert na een week zonder activiteit). Vergelijkt `Run.Replay(...).Score.Total` met `claimed_score`.
- **De Prikklok.** Dagelijkse run met `DailySeed.For(datum)`, klasklassement met bijnamen, histogram voor iedereen (`score_histogram`). De score bestaat (`Run.Score`, `RunScore`, zie de Prikklok in [README.md](README.md#prikklok-het-klassement)) en `Run.Replay` geeft dezelfde score (`ReplayTests`). Nog te doen: de dagelijkse run starten, de score insturen, het klassement tonen. De knop staat al grijs op de plattegrond (`ui.world.punch-clock`).
- **Leesstand, ✗-panelen en het zakje met onderdelen synchroniseren.** Nu blijven ze in de browser (`PlayerProgress.CodexRead`, `XPanels`, `Trinkets`); een tweede toestel toont 0 panelen en een leeg zakje. Kan als kolommen op `profiles` of een eigen tabel.
- **Een tweede Supabase-project voor ontwikkeling.** Nu praat ook `localhost` met het echte project. Het gratis plan laat twee actieve projecten toe en `stadsrally` neemt er al een.

## Code modulair maken

Gedaan op 3 oktober 2026: `DeckOverflow.Core` en `DeckOverflow.CardHall` als aparte projecten met eigen tests, de shell met `World/` en `Features/CardHall/`, de stage in `wwwroot/card-hall/stage/`, en het ✗-register met `IXPanelSource` (`World/XPanels.cs`). Wat nog rest:

- **`RunPage` splitsen.** Eén pagina toont nu titelscherm, plattegrond, onthulling én de run van de deckbuilder. De wereld (titel, plattegrond, onthulling, Codex, register, account) hoort in een eigen pagina of component in `World/`; de run in `Features/CardHall/`. Pas nodig als een tweede afdeling in het spel komt.
- **De Codex per afdeling registreren.** De catalogus staat in Core, maar alle pagina's staan nog in één lijst, en `MinAct` is een begrip van de Card Hall. Bij een tweede afdeling: elke afdeling levert haar pagina's (zoals `IXPanelSource`), en de minimale act wordt een minimale stap van die afdeling. Zie [codex.md](codex.md).
- **Het register toont nog één lijst.** `XRegisterBook` loopt over `XPanels.All`; een pagina per afdeling (met `IXPanelSource.Department`) komt als er een tweede afdeling is.
- **Teksten van wereldpanelen testen.** `StringsTests` (in de Card Hall-tests) kijkt alleen de panelen van de Card Hall na; "Do not read the manual" staat in `World/XPanels.cs` en heeft geen test. Een klein testproject voor de shell, of de wereldpanelen naar Core.
