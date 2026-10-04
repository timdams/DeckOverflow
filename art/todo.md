# Ontbrekende art

Tekeningen die het spel nog mist, over alle afdelingen heen, zodat ze samen in één tekenvel (een batch) gegenereerd kunnen worden. Wie iets toevoegt zonder tekening, zet het hier; na het uitsnijden (`tools/cut_sheets.py`) en koppelen in `art.json` gaat het eruit.

**Een batch maken.** Kies tekeningen van dezelfde soort (zelfde vorm en grootte bij het uitsnijden), zet ze in een raster op één vel met het manualvel of een recent vel als stijlreferentie, en voeg het vel toe aan `SHEETS_SPEC` in `tools/cut_sheets.py`. Vijanden kijken naar links (of komen in `FLIP`). De imagen-skill vraagt altijd eerst bevestiging van de kosten.

Open sinds 4 oktober 2026: de machines van de Lopende Band zijn getekend met vormen in Pixi. De producten op de band zijn vijanden uit de Card Hall en hebben hun tekening al.

| Sleutel | Soort (map in `art.json`) | Afdeling | Wat erop staat | Nu in het spel |
| --- | --- | --- | --- | --- |
| `typographer` | `actors` | Control Room | Een typograaf-automaat met een letterkast op zijn buik en een grote loden letter in de hand; half kleine letter, half hoofdletter | `typesetter` (de zetter uit de Card Hall) |
| `receipt` | `actors` | Control Room | Een lange kassabon op pootjes met een getal bovenaan, waar steeds meer cijfers aan vastgeschreven worden | `changelog` |
| `belt-machine` | nieuw (tegels) | Conveyor Belt | Een werkbank boven een stuk band, met een pers of lasser; plaats voor het label (`+4`, `×2`) | vorm in Pixi |
| `belt-saw` | nieuw (tegels) | Conveyor Belt | Een zaag die een product doormidden deelt, zaagsel ernaast (deling) | vorm in Pixi |
| `belt-gate` | nieuw (tegels) | Conveyor Belt | Een wissel met een keurder en twee uitgangen | vorm in Pixi |
| `belt-counter` | nieuw (tegels) | Conveyor Belt | Een mechanisch telwerk met rolletjes boven de band | vorm in Pixi |
| `belt-source`, `belt-output` | nieuw (tegels) | Conveyor Belt | Een trechter waar producten uit vallen; een verzendpoort met een stempel | vorm in Pixi |
| `front` | nieuw (achtergrond) | Conveyor Belt | Een brede, vage strook: het front in de verte, mist, rookwolkjes. Zie de todo van de afdeling | niets |
| `backdrop-vat-valley`, `backdrop-print-shop`, `backdrop-mold-works` | `wide` | Card Hall | Decor achter het gevecht, één per act: dezelfde plek als de actplaat, maar zonder het figuurtje en met een lege, vlakke vloer in het midden (daar staan held en vijand), 1280×720 | de actplaat, zacht en vervaagd; haar figuurtje schemert door |

## Liggen klaar, nog niet gebruikt

Uit het vel `extras` (`art/sheets/extras-*.png`), uitgesneden in `wwwroot/art/actors/`: `heartbleed`, `mars-orbiter`, `loop-snake`, `crate-stack`, `null-ghost`, `foreman` (voor latere afdelingen; tot 4 oktober 2026 reserves in de Controlekamer, nu weer vrij), `hero-cheer`, `hero-down`, `hero-reading`, `punch-clock`, `pouch` (voor eindscherm, Codex, Prikklok en zakje).

Uit het vel `future` (`art/sheets/future-*.png`), uitgesneden in `wwwroot/art/actors/`: `nesting-robot` (recursie, een stack overflow), `fencepost` (off-by-one), `hamster-wheel` (een oneindige lus), `lockers` (een array begint bij 0), `shelf-overrun` (voorbij het einde van een array), `stamp-press` (nu The Stamper in de Controlekamer; een methode die overal hetzelfde stempelt), `junction-box` (`switch`), `safety-net` (`catch`), `plate-stack` (de call stack), `and-levers` (`&&`: twee hendels tegelijk), `changelog` (de tease "Controlekamer: regels bijgewerkt"), `blueprint-twins` (twee objecten uit één klasse).

Uit het vel `items2`, in `wwwroot/art/items/`: `catch` (een kaart met een catch), `relic-undo` (de relic Undo, als Scrap vaak nodig blijkt) en `relic-brackets` (een relic Haakjes).
