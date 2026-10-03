# Todo

Wat bewust is uitgesteld, met genoeg context om het later op te pakken. Nieuwste bovenaan binnen elke sectie. Wat af is, gaat eruit (de geschiedenis staat in git).

## Backend

- **De docent kan alleen nog een klascode maken** (beslist op 3 oktober 2026, zie de GDD). Wat nu meer doet en eruit moet: afdelingen vrijgeven (`release_department` en `Account.ReleaseAsync`, de knoppen in `AccountPanel.razor`, de kolom `classes.released_departments`, `unlocks.how = teacher`) en de voortgang van de klas lezen (policies op `progress` en `unlocks`). Daarna het Spike Design Doc (hosting, accounts en data) en [supabaseplan.md](supabaseplan.md) bijwerken. Een migratie op het echte project, dus eerst afstemmen met de sessie die de backend bouwde.
- **Oude gastaccounts opruimen.** Elke nieuwe browser (of gewiste opslag) maakt een gast; wie stopt, laat er een achter. Supabase raadt aan om oude anonieme accounts geregeld te verwijderen. Kan mee in de nachtelijke Action van de scorecontrole, met de service-sleutel: anonieme accounts zonder activiteit sinds bv. 90 dagen. Er staat er nu al een: "Bright Spanner 74", van een test op 3 oktober 2026.
- **Klassen beheren.** Een docent kan nu alleen afdelingen vrijgeven, niet terugtrekken, en geen leerling uit de klas halen (de database laat dat wel toe, het scherm nog niet). Een dashboard met de voortgang van de klas komt na de MVP.

- **Scores controleren (stap 7 van [supabaseplan.md](supabaseplan.md)).** Consoleproject `tools/DeckOverflow.ScoreCheck`: haalt scores met `verified = pending`, speelt ze opnieuw af met `Run.Replay`, zet `ok` of `rejected`. Een GitHub Action draait het elke nacht, met de service-sleutel als GitHub-secret. Houdt meteen het gratis project wakker (pauzeert na een week zonder activiteit). Vergelijkt `Run.Replay(...).Score.Total` met `claimed_score`.
- **De Prikklok.** Dagelijkse run met `DailySeed.For(datum)`, klasklassement met bijnamen, histogram voor iedereen (`score_histogram`). De score bestaat (`Run.Score`, `RunScore`, zie de Prikklok in de GDD) en `Run.Replay` geeft dezelfde score (`ReplayTests`). Nog te doen: de dagelijkse run starten, de score insturen, het klassement tonen. De knop staat al grijs op de plattegrond (`ui.world.punch-clock`).
- **Leesstand, ✗-panelen en het zakje met onderdelen synchroniseren.** Nu blijven ze in de browser (`PlayerProgress.CodexRead`, `XPanels`, `Trinkets`); een tweede toestel toont 0 panelen en een leeg zakje. Kan als kolommen op `profiles` of een eigen tabel.
- **Een tweede Supabase-project voor ontwikkeling.** Nu praat ook `localhost` met het echte project. Het gratis plan laat twee actieve projecten toe en `stadsrally` neemt er al een.
