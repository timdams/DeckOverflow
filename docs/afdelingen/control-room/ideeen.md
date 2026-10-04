# Ideeën en open vragen: The Control Room

Richting, geen belofte. Wat gebouwd wordt, verhuist naar [README.md](README.md).

## Concepten uit vorige hoofdstukken laten terugkomen

Gevraagd op 4 oktober 2026: casting, parsing, datatypes in de puzzels. C# is natuurkunde in elke afdeling, dus de types gedragen zich hier zoals in de Card Hall.

- **Gebouwd op 4 oktober 2026** als gevecht 9 en 10, anders dan eerst gedacht: de `byte` is jóúw automaat (wie te gretig oplapt, loopt over), en het kommagetal is jóúw Mep (opwinden redt de halve punt). Zo zit de keuze bij de speler.
- **Een `byte`-automaat als vijand.** Zijn HP is een `byte`; zijn regel "mijn HP < 20 → Oplappen" lapt hem op voorbij 255, en dan loopt hij over naar een klein getal. Wie zijn regels leest, laat hem zichzelf kapot oplappen. Een terugkeer van Level 256.
- **Kommagetallen tegen een `int`.** Een automaat slaat voor 6.5; jouw HP is een `int` en kapt af. Een voorwaarde `me.Hp < 20` rekent met het afgekapte getal, en dat verschuift het moment om op te lappen.
- **`char` en `int.Parse`: gebouwd op 4 oktober 2026** als De Typograaf (12) en De Kassabon (16), zie [README](README.md#de-gevechten-drie-lagen). De oorspronkelijke gedachte: Een `char` is een getal (`'A'` is 65) en tekst wordt pas een getal na `int.Parse`. In de Card Hall zijn dat kaarten die je op een vijand speelt; in een regelduel zou het een zet worden als "Lezen" (parse de tekst van De Telex: `"405.5"` crasht met een `FormatException`, `"40"` wordt 40) of een vijand met een `char` als HP die door een klap van `'A'` naar `'7'` springt. Kan, maar het wordt snel een rekenoefening; eerst kijken hoe 11 tot 15 spelen.
- **Casting in een voorwaarde**, spaarzaam: `(int)(foe.Hp / 10) == 2`. Risico: het wordt een rekenoefening in plaats van een keuze.
- **Tekst als voorwaarde** (H3): een automaat die een bord leest, `label == "STOP"`, en een hoofdletter die het verschil maakt.

## Meer uit H5

- **`switch` als tabel van gevallen.** Een automaat die per beurtnummer % 4 iets anders doet, als tabel in plaats van een keten.
- **Kortsluiten.** Bij `&&` wordt de rechterkant niet bekeken als de linkerkant al `false` is; met een voorwaarde die iets kost (een teller die oploopt bij elke keer bekijken), wordt dat voelbaar.
- **De wetten van De Morgan**: `!(a && b)` is `!a || !b`. Een gevecht met twee borden die hetzelfde doen, waarvan één met minder tegels.

## Gambits als intents in de deckbuilder

Wie hier de regels van een vijand leest, kan dat ook in een kaartgevecht: de Stray Automaton in de Card Hall heeft al een regel als intent. Meer vijanden met een zichtbaar regelbord zou de twee afdelingen verbinden.
