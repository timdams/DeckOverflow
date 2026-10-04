# Ontbrekende art

Tekeningen die het spel nog mist, over alle afdelingen heen, zodat ze samen in één tekenvel (een batch) gegenereerd kunnen worden. Wie iets toevoegt zonder tekening, zet het hier; na het uitsnijden (`tools/cut_sheets.py`) en koppelen in `art.json` gaat het eruit.

**Een batch maken.** Kies tekeningen van dezelfde soort (zelfde vorm en grootte bij het uitsnijden), zet ze in een raster op één vel met het manualvel of een recent vel als stijlreferentie, en voeg het vel toe aan `SHEETS_SPEC` in `tools/cut_sheets.py`. Vijanden kijken naar links (of komen in `FLIP`). De imagen-skill vraagt altijd eerst bevestiging van de kosten.

Open sinds 4 oktober 2026: de vijanden van de Controlekamer gebruiken reserves, en de machines van de Lopende Band zijn getekend met vormen in Pixi. De producten op de band zijn vijanden uit de Card Hall en hebben hun tekening al.

| Sleutel | Soort (map in `art.json`) | Afdeling | Wat erop staat | Nu in het spel |
| --- | --- | --- | --- | --- |
| `press` | `actors` | Control Room | Een hydraulische pers die druk opbouwt: een zuiger, een manometer met de naald in het rood | `colossus` (oude tekening) |
| `metronome` | `actors` | Control Room | Een metronoom op pootjes met een schild dat elke derde tik omhoog klapt | `golem` (oude tekening) |
| `mender` | `actors` | Control Room | Een automaat met een lasbrander en pleisters, een tas met twee herstelsets | `foreman` |
| `goto-fail` | `actors` | Control Room | Elite, echte bug: een slot met twee identieke hangsloten, waarvan één open hangt | tekening uit spike 8 |
| `sentry` | `actors` | Control Room | Een wachter met twee schakelaars die allebei hetzelfde alarm aanzetten (OF) | `and-levers` |
| `contrarian` | `actors` | Control Room | Een automaat met een pijl op zijn borst die altijd de andere kant op wijst (NIET) | `plate-stack` |
| `cutter` | `actors` | Control Room | Een snoeischaar op wieltjes die elk getal na de komma afknipt; snippers op de grond | `crate-stack` |
| `overload` | `actors` | Control Room | Een zware automaat met een kraan vol gewichten, een wijzer die tot 255 loopt | `level-256` (de byte-elite uit de Card Hall, als knipoog) |
| `telex` | `actors` | Control Room | Een telexmachine waar een eindeloze papierstrook met cijfers uit rolt | `null-ghost` |
| `estimator` | `actors` | Control Room | Een automaat met een weegschaal en een stempel "≈", die elk getal afrondt | `nesting-robot` |
| `day-248` | `actors` | Control Room | Elite, echte bug: een vliegtuig met een tellerdisplay op de romp dat van 255 naar 0 springt, de lichten gaan uit | `mars-orbiter` |
| `giant` | `actors` | Control Room | Een reus uit opgestapelde kisten, met een meetlat tot 600 die bij 256 een rode streep heeft | `blueprint-twins` |
| `titan` | `actors` | Control Room | Een titaan met een veer op zijn rug die elke derde tel opgespannen wordt | `flight-501` (de Convert-elite uit de Card Hall, als knipoog) |
| `knight-capital` | `actors` | Control Room | Elite, echte bug: een tickerbord dat op hol slaat, met een stoffige oude hendel 'Power Peg' bovenaan | `changelog` |
| `belt-machine` | nieuw (tegels) | Conveyor Belt | Een werkbank boven een stuk band, met een pers of lasser; plaats voor het label (`+4`, `×2`) | vorm in Pixi |
| `belt-saw` | nieuw (tegels) | Conveyor Belt | Een zaag die een product doormidden deelt, zaagsel ernaast (deling) | vorm in Pixi |
| `belt-gate` | nieuw (tegels) | Conveyor Belt | Een wissel met een keurder en twee uitgangen | vorm in Pixi |
| `belt-counter` | nieuw (tegels) | Conveyor Belt | Een mechanisch telwerk met rolletjes boven de band | vorm in Pixi |
| `belt-source`, `belt-output` | nieuw (tegels) | Conveyor Belt | Een trechter waar producten uit vallen; een verzendpoort met een stempel | vorm in Pixi |
| `front` | nieuw (achtergrond) | Conveyor Belt | Een brede, vage strook: het front in de verte, mist, rookwolkjes. Zie de todo van de afdeling | niets |

## Liggen klaar, nog niet gebruikt

Uit het vel `extras` (`art/sheets/extras-*.png`), uitgesneden in `wwwroot/art/actors/`: `heartbleed`, `mars-orbiter`, `loop-snake`, `crate-stack`, `null-ghost`, `foreman` (voor latere afdelingen), `hero-cheer`, `hero-down`, `hero-reading`, `punch-clock`, `pouch` (voor eindscherm, Codex, Prikklok en zakje).

Uit het vel `future` (`art/sheets/future-*.png`), uitgesneden in `wwwroot/art/actors/`: `nesting-robot` (recursie, een stack overflow), `fencepost` (off-by-one), `hamster-wheel` (een oneindige lus), `lockers` (een array begint bij 0), `shelf-overrun` (voorbij het einde van een array), `stamp-press` (nu The Stamper in de Controlekamer; een methode die overal hetzelfde stempelt), `junction-box` (`switch`), `safety-net` (`catch`), `plate-stack` (de call stack), `and-levers` (`&&`: twee hendels tegelijk), `changelog` (de tease "Controlekamer: regels bijgewerkt"), `blueprint-twins` (twee objecten uit één klasse).

Uit het vel `items2`, in `wwwroot/art/items/`: `catch` (een kaart met een catch), `relic-undo` (de relic Undo, als Scrap vaak nodig blijkt) en `relic-brackets` (een relic Haakjes).
