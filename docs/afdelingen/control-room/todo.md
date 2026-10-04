# Todo: The Control Room

Wat bewust is uitgesteld, met genoeg context om het later op te pakken. Nieuwste bovenaan. Wat af is, gaat eruit.


## Na de schakelkasten (4 oktober 2026)

Het scherm volgt sinds 4 oktober 2026 [de beeldtaal](../../wereld/beeldtaal.md) (zie [Het scherm](README.md#het-scherm-twee-kasten-en-een-testcel)). Wat nog open staat:

- **Een dossier bij een elite.** De mockup had onder de kast van de vijand een kaartje met de naam van de bug en het jaar ("goto fail; Apple, 2014"), en het verhaal na de overwinning. Nu staat alleen het oranje label op de kast, en het verhaal in de uitslag.
- **De ponsband schuift niet mee.** Bij een lang duel (tot 40 beurten) loopt de band uit beeld; ze zou naar de zet in beeld moeten schuiven.
- **De testcel en de stage.** De stage tekent nog haar eigen bladrand en het label van de beurt, binnen het raam van de testcel. Eén van de twee kan weg.

## Vrijgeven (4 oktober 2026)

- **De Controlekamer staat op binnenkort.** Spelers verdienen de sleutel al met de laatste baas van de Card Hall, maar de deur blijft dicht. Als ze af is: `Availability.Released` in `World/Departments.cs`; wie de sleutel heeft, kan dan meteen binnen. Daarna de Lopende Band zijn sleutel geven (zie haar [todo](../conveyor-belt/todo.md)).

## Na de bouw van 4 oktober 2026

- **Testen met spelers.** De vragen van spike 8 staan nog open: is regels opstellen en toekijken leuk, ontdekken spelers zonder uitleg dat de volgorde telt, lezen ze de regels van de vijand, zegt "onder de motorkap" iets?
- **De terugblik verder uitbouwen.** Gebouwd: afkappen, overflow, tekst, afronden, casting, Convert en de elite Dag 248. Nog niet: `char` als getal en `int.Parse`; zie [ideeen.md](ideeen.md) waarom die minder vanzelf in een regelduel passen.
- **De terugblik met spelers testen** (9 tot 13), en of de tegengestelde lessen (opwinden loont tegen een `int`, niet tegen tekst) verwarren of net doen nadenken. Specifiek voor 9 en 10: valt het op dat 2.5 een 2 wordt, en lezen ze "220 + 40 → 4" als een les en niet als een bug van het spel?
- **Voortgang synchroniseren.** `PlayerProgress.ControlRoom` (beste scores, je borden) blijft in de browser; `SyncedProgressStore` stuurt alleen Codex, onderdelen en ontgrendelingen naar Supabase.
- **Het histogram van andere spelers**, zoals Opus Magnum het echt doet. Nu toont het alle winnende borden die de oplosser vindt (tot 3 regels, zonder EN/OF/NIET). Kan via de Prikklok-tabellen zodra die bestaan.
- **Eigen tekeningen.** Alle vijanden zijn plaatshouders uit andere tekeningen (zie [art/todo.md](../../../art/todo.md)); goto fail is de tekening uit de spike.
- **Slepen op een aanraakscherm.** HTML5-slepen werkt niet met een vinger; daar tik je een tegel en dan een vakje. Echt slepen met de vinger kan met pointer-events, als tikken in een playtest stroef blijkt.
- **Testen op een echte telefoon.** Getest in een desktopbrowser en in een kader van 844×390, niet op een toestel. De tekst in de stage is liggend op een telefoon klein, zoals in de Card Hall.
- **Geen geluid voor een regel die oplicht.** De stage kent alleen het knikje; een klik per bekeken regel (van boven naar onder) zou de keten hoorbaar maken.
