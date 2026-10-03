# Todo

Wat bewust is uitgesteld, met genoeg context om het later op te pakken. Nieuwste bovenaan binnen elke sectie. Wat af is, gaat eruit (de geschiedenis staat in git).

## Backend

- **Scores controleren (stap 7 van [supabaseplan.md](supabaseplan.md)).** Consoleproject `tools/DeckOverflow.ScoreCheck`: haalt scores met `verified = pending`, speelt ze opnieuw af met `Run.Replay`, zet `ok` of `rejected`. Een GitHub Action draait het elke nacht, met de service-sleutel als GitHub-secret. Houdt meteen het gratis project wakker (pauzeert na een week zonder activiteit). Kan pas als de motor een score kent, zie de Prikklok.
- **De Prikklok.** Dagelijkse run met `DailySeed.For(datum)`, klasklassement met bijnamen, histogram voor iedereen (`score_histogram`). Eerst beslissen wat de score van een run is; die bestaat nog niet in de motor. Daarna een test dat `Run.Replay` dezelfde score geeft. De knop staat al grijs op de plattegrond (`ui.world.punch-clock`).
- **Leesstand en ✗-panelen synchroniseren.** Nu blijven ze in de browser; een tweede toestel toont 0 panelen. Kan als kolommen op `profiles` of een eigen tabel.
- **Een tweede Supabase-project voor ontwikkeling.** Nu praat ook `localhost` met het echte project. Het gratis plan laat twee actieve projecten toe en `stadsrally` neemt er al een.
