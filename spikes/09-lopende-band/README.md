# Spike 9: de Lopende Band

De derde afdeling (H6, herhalingen). Je legt band en machines op een rooster en kijkt hoe de kisten rijden. Een band die terugkomt bij zichzelf, is letterlijk een lus. Zo heet het nergens in beeld: de namen `while`, `do while` en `for` horen in de Codex. Het voorbeeld is Opus Magnum, niet Scratch: je bouwt een machine in de ruimte, je schrijft geen lijst opdrachten.

Het ontwerp staat in [de wereld](../../docs/wereld/README.md) ("De Lopende Band"). De spike begint vanaf nul en deelt geen code met het spel in `src/`; de natuurwetten (een `byte` loopt over, een deling van ints kapt af, tekst plakt) zijn wel dezelfde, met echt .NET-gedrag.

## Wat spike 9 moet aantonen

1. **Is een band leggen en laten draaien leuk**, ook voor iemand zonder interesse in programmeren? Of voelt het als een opdrachtenlijst?
2. **Ontdekken spelers zelf wat een lus is?** Er is geen machine "lus": een band die terugloopt naar een poort, is het. Zien ze het verschil tussen een poort vóór en ná het werk (`while` tegenover `do while`) zonder uitleg?
3. **Voelt een oneindige lus als een les, niet als een fout van het spel?** De band stopt met "alles staat weer net zoals een tik eerder". Begrijpen spelers waarom?
4. **Werkt de terugblik?** Herkennen spelers in de puzzels 6 tot 9 wat ze in de Card Hall en de Controlekamer leerden (modulo, een `byte` die overloopt, `/` op ints, `.Length`)?
5. **Werken testgevallen als drijfveer?** Elke puzzel heeft meerdere invoerwaarden, zodat je een echte lus bouwt en geen antwoord vast legt.

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

`/?all` zet alle puzzels open. Met `/?level=zune` ga je meteen naar één puzzel (dan staat ook alles open). Na een push naar `main` staat de spike op `/DeckOverflow/spike-9/`.

## Hoe het werkt

- **Het rooster:** een bron (in), een uitgang (uit), en lege vakjes. Band is onbeperkt; machines, poorten en tellers zitten in een beperkte gereedschapsbak.
- **Een kist** rijdt één vakje per tik, met een waarde met een type (`int`, `byte`, `string`). De kist kleurt naar haar type, zoals in het spel.
- **Een machine** bewerkt de inhoud (`+4`, `×2`, `/2`, `−366`, `+"-"`) en stuurt de kist verder.
- **Een poort** kijkt naar een voorwaarde (`< 30`, `% 5 == 0`, `.Length < 6`) en heeft twee uitgangen: klopt het, of niet. Een band die terugloopt naar de poort, is een `while`; staat de poort achter het werk, een `do while`.
- **Een teller** stuurt de kist n keer de lus in en dan naar buiten, en begint daarna weer bij 0: een `for`.
- **De natuurwetten:** een kist op een vakje zonder band valt eraf. Komt dezelfde toestand terug (vakje, richting, inhoud en tellers), dan is het bewezen een oneindige lus, en stopt de band. Een kist die aankomt met iets anders dan gevraagd, telt als fout.
- **Score:** het aantal machines (band telt niet) en het aantal tikken over alle testgevallen, zoals Opus Magnum. Je beste en je borden blijven in de browser bewaard.

## De tien puzzels

Drie lagen, zoals in de Controlekamer: eerst de basis van H6, dan terugblik op vorige hoofdstukken, dan een echte bug. De tests in `LevelTests` bevatten voor elke puzzel een bord dat werkt.

| # | Puzzel | Testgevallen | Wat je ontdekt |
| --- | --- | --- | --- |
| 1 | Eerste band | 0 → 5, 10 → 15 | Band leggen, een machine onderweg. Nog geen lus |
| 2 | Verdubbelen | 1 → 32, 3 → 48, 5 → 40 | Hoe vaak, hangt af van de invoer: een lus met een poort (`while (v < 30)`) |
| 3 | Drie keer | 0 → 12, 7 → 19, 20 → 32 | Een vast aantal rondjes: een teller. Drie keer +4 of twee keer +6 |
| 4 | Minstens één keer | 2 → 11, 50 → 53, 9 → 12 | Een poort vóór het werk laat 50 onaangeroerd door: zet ze erachter (`do while`) |
| 5 | Lus in een lus | 0 → 24, 5 → 29 | +24 met alleen +1 en tellers tot 6: een teller in een teller |
| 6 | Naar boven afronden (H2, H5) | 3 → 5, 10 → 10, 12 → 15, 21 → 25 | `while (v % 5 != 0) v++`: de poort stuurt de kist naar buiten als het klopt |
| 7 | Rondjes in een byte (H2) | 250 → 14, 240 → 4, 200 → 4 | `while (v > 100) v += 20` stopt alleen omdat de byte overloopt. Een poort `< 300` stopt nooit |
| 8 | Halveren (H2) | 100 → 3, 37 → 4, 64 → 4 | Een int delen door een int kapt af: 25 / 2 is 12 |
| 9 | Aanvullen (H3) | "a" → "a-----", "abc" → "abc---", "abcdef" → "abcdef" | `while (v.Length < 6) v += "-"`, en wat al lang genoeg is, blijft |
| 10 | Zune (elite, echte bug) | 400 → 34, 1000 → 268, 366 → 366 | De lus van de Zune 30 ligt al vast en werkt, behalve op dag 366: die blijft eeuwig rondjes draaien. Geef die dag een uitweg |

## Keuzes tijdens spike 9

- **Bestelbonnen van het front** (na de eerste test, 4 oktober 2026: "oersaai, visueel en wat we doen"). Elke puzzel is een bestelling; de testgevallen zijn de bestelde stuks. In de kist zit een vijand uit de Card Hall (Slijmklodder, Blikken ridder, Level 256 ...), met zijn HP als label in de kleur van zijn type, en hij groeit naarmate hij de bestelling nadert. Zo voeden de afdelingen elkaar: wat je in de Kaartenhal bevecht, komt van deze band.
- **Een PixiJS-stage** tekent het rooster: band met meelopende pijltjes, machines die pompen en vonken, een wissel die omklakt, een telwerk, een trechter bij "in" en een verzendpoort bij "uit". Afkappen laat zaagsel vallen, een byte die overloopt schokt. Klikken op het rooster gaat naar de shell; het instelpaneel blijft HTML.
- **De afloop.** Een goedgekeurd product krijgt een stempel en wandelt van de band, naar rechts de stage uit; een afgekeurd valt in de bak; een oneindige lus geeft rook, een zwaailicht en "OVERHIT". De tekeningen komen uit het spel (`wwwroot/art/`).
- **Klikken in plaats van slepen.** Kies iets uit de bak en klik op een vakje; nog eens klikken op band draait hem. Een gelegd stuk stel je in via een paneel (richtingen, getal, voorwaarde). Dat werkt ook met een vinger.
- **Testgevallen in plaats van één kist.** Zonder meerdere invoerwaarden kan je de uitkomst "vast leggen" en is er geen lus nodig.
- **Een oneindige lus wordt bewezen, niet geraden.** De motor bewaart elke toestand; komt er een terug, dan stopt de band meteen. Een vangnet van 5000 tikken zou nooit mogen afgaan.
- **Spanning per testgeval** (na de eerste test, 4 oktober 2026). De uitslag van een kist verschijnt pas als ze in beeld aankomt: tot dan staat er "…". Gelukt: de rij trilt, er spat vuurwerk uit het vinkje en ze blijft groen. Mislukt: een dikke rode stempel "FOUT" met wat er misging. Alles juist: het resultaat springt op als een stempel. Tussen twee kisten pauzeert de band 1,1 seconde, op elke snelheid, zodat je de uitslag ziet.
- **Groen en rood voor gelukt en mislukt** zijn een bewuste uitzondering op "kleur is voorbehouden aan types". Voor het spel nog te beslissen: houden we dat, of doen inkt en vorm (vinkje, stempel) het werk?
- **Alleen Nederlands.** De spike is voor een playtest met studenten; in het spel komen de teksten in `nl.json` en `en.json`.
- **De motor is deterministisch en heeft geen RNG**, zoals de Controlekamer.

## Nog niet in de spike

- **Het front in de verte, met een strakke animatie.** Het idee: bovenaan, vaag en mistig, een strijd waar de goedgekeurde producten naartoe wandelen, zodat je ziet waarvoor de band werkt. Een eerste versie (heuvels, marcherende stokfiguurtjes, flitsen en rookwolken in Pixi-tekenwerk) was te stom en is er weer uit (4 oktober 2026). Later strakker, waarschijnlijk met echte tekeningen in plaats van getekende vormen.

- **Onder de motorkap:** een bord omzetten naar C# (`while`, `for`, `do while`) is bij een vrij rooster lastiger dan bij een regelbord. Eerst kijken of spelers erom vragen.
- **Meerdere kisten tegelijk** op de band, en een histogram tegenover andere spelers.
- **De Gereedschapsmuur** (H7): dezelfde band met blueprints, een machine één keer bouwen en overal stempelen.
- **Codex en ✗-panelen.** In het spel komen de pagina's *while*, *do while*, *for* en *geneste lussen* (H6), en panelen als "een band die nooit stopt".
