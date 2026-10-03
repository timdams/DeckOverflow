# Ontbrekende art

Tekeningen die het spel nog mist, over alle afdelingen heen, zodat ze samen in één tekenvel (een batch) gegenereerd kunnen worden. Wie iets toevoegt zonder tekening, zet het hier; na het uitsnijden (`tools/cut_sheets.py`) en koppelen in `art.json` gaat het eruit.

**Een batch maken.** Kies tekeningen van dezelfde soort (zelfde vorm en grootte bij het uitsnijden), zet ze in een raster op één vel met het manualvel of een recent vel als stijlreferentie, en voeg het vel toe aan `SHEETS_SPEC` in `tools/cut_sheets.py`. Vijanden kijken naar links (of komen in `FLIP`). De imagen-skill vraagt altijd eerst bevestiging van de kosten.

| Sleutel | Soort (map in `art.json`) | Afdeling | Wat erop staat | Nu in het spel |
| --- | --- | --- | --- | --- |
| `rhythm-turtle` | vijand (`actors`) | Card Hall, act 1 | Een schildpad met een schild dat in drie segmenten opengaat, één segment open; een metronoom of ritmeteken als accent | de tekening van Level 256 (reserve) |
| `flip` | kaart (`cards`) | Card Hall | Een hand die een tweestandenschakelaar omzet | geen tekening |
| `remainder` | kaart (`cards`) | Card Hall | Een deling waarbij een klein restje uit de machine valt | geen tekening |
| `after-midnight` | ✗-paneel (`panels`) | Card Hall | Het figuurtje dat wacht bij een klok die middernacht slaat, een kalender die van 99 naar 100 springt | geen tekening |

## Liggen klaar, nog niet gebruikt

Uit het vel `extras` (`art/sheets/extras-*.png`), uitgesneden in `wwwroot/art/actors/`: `zune`, `heartbleed`, `mars-orbiter`, `loop-snake`, `crate-stack`, `null-ghost`, `foreman` (voor latere afdelingen), `hero-cheer`, `hero-down`, `hero-reading`, `punch-clock`, `pouch` (voor eindscherm, Codex, Prikklok en zakje).
