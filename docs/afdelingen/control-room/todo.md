# Todo: The Control Room

Wat bewust is uitgesteld, met genoeg context om het later op te pakken. Nieuwste bovenaan. Wat af is, gaat eruit.


## Na de schakelkasten (4 oktober 2026)

Het scherm volgt sinds 4 oktober 2026 [de beeldtaal](../../wereld/beeldtaal.md) (zie [Het scherm](README.md#het-scherm-twee-kasten-en-een-testcel)). Afgewerkt op 4 oktober 2026: het dossier bij een elite, de ponsband die meeschuift, de testcel zonder dubbele rand, een klikje per bekeken regel, het feedbackhartje en slepen met een vinger. Wat nog open staat:

- **Het klikje per regel op gevoel afstellen** (`juice.ruleTick` in `wwwroot/shared/juice.js`), in een zichtbaar venster met geluid aan: is het een keten die je hoort, of geratel bij vijf regels?
- **Slepen met een vinger op een echt toestel.** `wwwroot/shared/touch-drag.js` is getest met nagebootste touch-events in een desktopbrowser.

## Vrijgeven (4 oktober 2026)

- **De Controlekamer staat op binnenkort.** Spelers verdienen de sleutel al met de laatste baas van de Card Hall, maar de deur blijft dicht. Als ze af is: `Availability.Released` in `World/Departments.cs`; wie de sleutel heeft, kan dan meteen binnen. Daarna de Lopende Band zijn sleutel geven (zie haar [todo](../conveyor-belt/todo.md)).

## Na de bouw van 4 oktober 2026

- **Testen met spelers.** De vragen van spike 8 staan nog open: is regels opstellen en toekijken leuk, ontdekken spelers zonder uitleg dat de volgorde telt, lezen ze de regels van de vijand, zegt "onder de motorkap" iets?
- **De Typograaf en De Kassabon met spelers testen** (12 en 16, gebouwd op 4 oktober 2026). Bij de Typograaf: wordt duidelijk waarom hij op `'\'` vastloopt, of lijkt het een bug? Bij de Kassabon: lezen ze `"205.5"` als tekst en niet als een getal met een komma? Hun tekeningen zijn nog reserves (zie [art/todo.md](../../../art/todo.md)).
- **De terugblik met spelers testen** (9 tot 17), en of de tegengestelde lessen (opwinden loont tegen een `int`, niet tegen tekst) verwarren of net doen nadenken. Specifiek voor 9 en 10: valt het op dat 2.5 een 2 wordt, en lezen ze "220 + 40 → 4" als een les en niet als een bug van het spel?
- **Voortgang synchroniseren.** `PlayerProgress.ControlRoom` (beste scores, je borden) blijft in de browser; `SyncedProgressStore` stuurt alleen Codex, onderdelen en ontgrendelingen naar Supabase.
- **Het histogram van andere spelers**, zoals Opus Magnum het echt doet. Nu toont het alle winnende borden die de oplosser vindt (tot 3 regels, zonder EN/OF/NIET). Kan via de Prikklok-tabellen zodra die bestaan.
- **Testen op een echte telefoon.** Getest in een desktopbrowser en in een kader van 844×390, niet op een toestel. De tekst in de stage is liggend op een telefoon klein, zoals in de Card Hall.
