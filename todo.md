# Todo

Wat bewust is uitgesteld, met genoeg context om het later op te pakken. Nieuwste bovenaan binnen elke sectie. Wat af is, gaat eruit (de geschiedenis staat in git).

## Backend

- **Oude gastaccounts opruimen.** Elke nieuwe browser (of gewiste opslag) maakt een gast; wie stopt, laat er een achter. Supabase raadt aan om oude anonieme accounts geregeld te verwijderen. Kan mee in de nachtelijke Action van de scorecontrole, met de service-sleutel: anonieme accounts zonder activiteit sinds bv. 90 dagen. Er staat er nu al een: "Bright Spanner 74", van een test op 3 oktober 2026.
- **Klassen beheren.** Een docent kan nu alleen afdelingen vrijgeven, niet terugtrekken, en geen leerling uit de klas halen (de database laat dat wel toe, het scherm nog niet). Een dashboard met de voortgang van de klas komt na de MVP.

- **Scores controleren (stap 7 van [supabaseplan.md](supabaseplan.md)).** Consoleproject `tools/DeckOverflow.ScoreCheck`: haalt scores met `verified = pending`, speelt ze opnieuw af met `Run.Replay`, zet `ok` of `rejected`. Een GitHub Action draait het elke nacht, met de service-sleutel als GitHub-secret. Houdt meteen het gratis project wakker (pauzeert na een week zonder activiteit). Kan pas als de motor een score kent, zie de Prikklok.
- **De Prikklok.** Dagelijkse run met `DailySeed.For(datum)`, klasklassement met bijnamen, histogram voor iedereen (`score_histogram`). Eerst beslissen wat de score van een run is; die bestaat nog niet in de motor. Daarna een test dat `Run.Replay` dezelfde score geeft. De knop staat al grijs op de plattegrond (`ui.world.punch-clock`).
- **Leesstand en ✗-panelen synchroniseren.** Nu blijven ze in de browser; een tweede toestel toont 0 panelen. Kan als kolommen op `profiles` of een eigen tabel.
- **Een tweede Supabase-project voor ontwikkeling.** Nu praat ook `localhost` met het echte project. Het gratis plan laat twee actieve projecten toe en `stadsrally` neemt er al een.
