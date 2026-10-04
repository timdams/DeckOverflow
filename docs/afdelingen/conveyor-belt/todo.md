# Todo: The Conveyor Belt

Wat bewust is uitgesteld. Nieuwste bovenaan. Wat af is, gaat eruit.

## Na de werkvloer (4 oktober 2026)

Het scherm volgt sinds 4 oktober 2026 [de beeldtaal](../../wereld/beeldtaal.md) (zie [Het scherm](README.md#het-scherm-de-werkvloer)). Wat nog open staat:

- **Rechtop op een telefoon.** Beslist, nog niet gebouwd. De stage tekent de vloer dan een kwartslag gedraaid (de `root`-container 90° draaien in `fit()` als de host hoger is dan breed, en tekst en het product terugdraaien zodat ze rechtop blijven); de bestelbon wordt een strook bovenaan die als blad openschuift; de onderdelen een rij onder de duim; de grote knop rechtsonder. Daarvoor moet de vraag om te draaien (`ui.rotate`) op deze pagina weg. Zie de mockups op het [design-canvas](https://claude.ai/artifact/C2vUobKdxoLhtyMRDCo6Au).
- **Het licht uit nagekeken in een zichtbaar venster.** Getest met stilgezette animaties in een verborgen tabblad: het donker, het oranje rondje, de pijlen en het telwerk kloppen, maar het tempo van de steeds snellere rondjes (`JUICE.loop` bovenaan `stage.js`) is nog niet op gevoel afgesteld.
- **De rollen tekenen elke frame opnieuw** (`drawRollers`). Vlot genoeg op een laptop; op een trage schoolcomputer nakijken, en anders alleen hertekenen als de band in beeld beweegt.

## Na de overname uit spike 9 (4 oktober 2026)

- **Ontgrendelen.** Een speler kan de Lopende Band nog niet openen: eerst wordt de Controlekamer afgewerkt (beslist op 4 oktober 2026). Ze staat op *binnenkort* (`Availability.Soon`). Daarna, zoals de Controlekamer na de Card Hall: de sleutel geven als de laatste baas van de Controlekamer valt (`PlayerProgress.TryUnlock(Departments.ConveyorBelt, ...)`, met de melding `ui.toast.key`), `ui.world.conveyor-belt.needs` aanpassen, en later `Availability.Released`.
- **Het front in de verte, met een strakke animatie.** Bovenaan de stage, vaag en mistig, de strijd waar de goedgekeurde producten naartoe wandelen. Een eerste versie (heuvels, stokfiguurtjes, flitsen, rook in Pixi-tekenwerk) was te stom en ging eruit; later strakker, waarschijnlijk met echte tekeningen. De stage houdt er een strook voor vrij (`SKY` in `stage.js`).
- **Eigen tekeningen voor de machines** (pers, lasser, zaag, etiketprinter, wissel, telwerk, trechter, verzendpoort) in plaats van getekende vormen; zie [art/todo.md](../../../art/todo.md).
- **Onder de motorkap:** een bord omzetten naar C# (`while`, `for`, `do while`). Bij een vrij rooster lastiger dan bij een regelbord.
- **Voortgang synchroniseren:** `PlayerProgress.ConveyorBelt` blijft in de browser.
- **Een histogram,** zoals in de Controlekamer. Er is nog geen oplosser voor een vrij rooster; tegenover andere spelers kan pas met het klassement.
- **Testen met spelers:** de vragen uit [spike 9](../../../spikes/09-lopende-band/README.md#wat-spike-9-moet-aantonen).
- **Geluid:** machines, wissels, telwerken en elke afloop klinken (4 oktober 2026). Nog open: de band zoemt niet (een engine-loop uit het sci-fi-pakket kan), en er is geen geluidsknop op de pagina; aan of uit volgt wat de speler in de Card Hall of de Controlekamer koos.
