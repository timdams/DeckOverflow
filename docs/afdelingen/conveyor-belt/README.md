# The Conveyor Belt: de Lopende Band

Afdeling ⑥ op de plattegrond, hoofdstuk 6 van Zie Scherp Scherper (herhalingen). Je legt band en machines op een rooster en kijkt hoe de producten rijden. Een band die terugkomt bij zichzelf, is letterlijk een loop. Zo heet het nergens in beeld: `while`, `do while` en `for` staan in de Codex. Het voorbeeld is Opus Magnum, niet Scratch. Verder: [events](events.md), [ideeën](../../../ideeen.md#the-conveyor-belt), [todo](todo.md).

**Gebouwd op 4 oktober 2026** als afdeling in het spel, overgenomen uit [spike 9](../../../spikes/09-lopende-band/README.md). **Een speler kan ze nog niet ontgrendelen:** eerst wordt de Controlekamer afgewerkt. Op de plattegrond staat ze op *binnenkort*, met haar uitleg als tease; de [superuser](../../wereld/backend.md#superuser) kan er al in, en springt met `/conveyor-belt?level=zune` naar één bestelling.

## Het verhaal: bestelbonnen van het front

Elke puzzel is een bestelling van het front: "Slijmklodders, elk met minstens 30 HP". De testgevallen zijn de bestelde stuks. Op de band rijdt geen getal maar een vijand uit de Card Hall, met zijn HP als label in de kleur van zijn type; hij groeit naarmate hij de bestelling nadert. Goedgekeurd krijgt hij een stempel en wandelt hij van de band, naar het front; afgekeurd valt hij in de bak. Zo voeden de afdelingen elkaar: wat je in de Kaartenhal bevecht, komt van deze band.

## De loop: een reeks puzzels

1. **Lees de bestelbon:** wat er binnenkomt en wat het moet worden.
2. **Leg de band.** Kies iets uit de gereedschapsbak en klik op een vakje; een nieuwe band gaat verder in de richting van de vorige, nog eens klikken draait hem. Een gelegd stuk stel je in via een paneel (richtingen, getal, voorwaarde). Werkt ook met een vinger.
3. **Start en kijk.** De producten rijden tik per tik. De uitslag van een stuk verschijnt pas als het aankomt: vuurwerk of een rode stempel. Pauzeren, één tik, sneller (tot 8×), terugspoelen, en opnieuw afspelen, ook na een overwinning.
4. **Verbeter.** Twee scores, elk apart: machines en tikken, zoals Opus Magnum.

## Het scherm: de werkvloer

Beslist op 4 oktober 2026, met de [beeldtaal](../../wereld/beeldtaal.md). Geen rooster met pijltjes, maar een werkvloer. **Gebouwd op 4 oktober 2026**, behalve de telefoon rechtop (zie [todo](todo.md)). Wat de stage tekent, staat in [events.md](events.md).

- **In het midden de werkvloer.** Betonnen tegels; de bron is een trechter, de uitgang een laadperron *naar het front*, gearceerd. Band zijn rollen die echt draaien. Een machine is een kast met een meter, een poort een rond ventiel met een *ja*- en een *nee*-uitgang, een teller een telwerk. Het product rijdt mee, met zijn waarde in de kleur van zijn type. Bovenaan de vloer een telwerk met de tikken.
- **Links de onderdelenlijst**, zoals op de eerste pagina van een handleiding: per stuk een tekening en een groot aantal (`∞` band, `1×` poort). Wat op is, wordt grijs. Het paneel van een gelegd stuk (richtingen, getal, voorwaarde) staat eronder.
- **Rechts de bestelbon op een klembord**, met per testgeval een stempel: GOEDGEKEURD in inkt, of een oranje ✗ met wat er misliep. Geen groen of rood.
- **Onder de vloer** de grote ronde startknop, één tik, de snelheden en de tijdlijn.
- **Een oneindige loop doet het licht uit.** De vloer wordt donker, alleen het rondje brandt in oranje, het product rijdt rondjes die steeds sneller gaan, en een telwerk telt ze tot de band zichzelf stopt. Daarna verschijnt het ✗-paneel. Welke vakjes het rondje zijn, weet de motor: `CaseRun.LoopFrom` is de frame waar de herhaalde toestand voor het eerst voorkwam.
- **Op een telefoon rechtop** draait de vloer een kwartslag: de band loopt van boven naar onder, met vakjes die groot genoeg blijven om aan te tikken. De bestelbon is een strook bovenaan die als blad openschuift, de onderdelen liggen onder je duim, en de grote knop staat rechtsonder. Het rooster in de motor blijft hetzelfde; alleen de stage tekent het gedraaid.

## De natuurwetten

- **Het rooster:** een bron (in), een uitgang (verzending), en lege vakjes. Band is onbeperkt; machines, poorten en tellers zitten in een beperkte gereedschapsbak.
- **Een machine** bewerkt de inhoud (`+4`, `×2`, `/2`, `−366`, `+"-"`). Met echt .NET-gedrag, zoals in de Card Hall: een `byte` loopt over (`ByteRules` uit Core), een deling van ints kapt af, tekst plakt.
- **Een poort** kijkt naar een voorwaarde (`< 30`, `% 5 == 0`, `.Length < 6`) en heeft twee uitgangen. Een band die terugloopt naar de poort, is een `while`; staat de poort achter het werk, een `do while`.
- **Een teller** stuurt het product n keer de loop in en dan naar buiten, en begint daarna weer bij 0: een `for`.
- **Een product op een vakje zonder band valt eraf.** Komt dezelfde toestand terug (vakje, richting, inhoud en tellers), dan is het bewezen een oneindige loop en stopt de band. Wat aankomt met iets anders dan besteld, wordt afgekeurd.
- **Deterministisch, zonder RNG.**

## De tien bestellingen

Drie lagen, zoals in de Controlekamer (zie [de wereld](../../wereld/README.md), regel 4): eerst de basis van H6, dan terugblik op vorige hoofdstukken, dan een echte bug. `LevelTests` bevat voor elke bestelling een bord dat werkt.

| # | Bestelling | Product | Testgevallen | Wat je ontdekt |
| --- | --- | --- | --- | --- |
| 1 | Eerste band | Blikken ridder | 0 → 5, 10 → 15 | Band leggen, een machine onderweg. Nog geen loop |
| 2 | Verdubbelen | Slijmklodder | 1 → 32, 3 → 48, 5 → 40 | Hoe vaak, hangt af van de invoer: een poort en een band terug (`while`) |
| 3 | Drie keer | Bodemloze kruik | 0 → 12, 7 → 19, 20 → 32 | Een vast aantal rondjes: een teller (`for`). Drie keer +4 of twee keer +6 |
| 4 | Minstens één keer | Druppelaar | 2 → 11, 50 → 53, 9 → 12 | Een poort vóór het werk laat 50 onaangeroerd door: zet ze erachter (`do while`) |
| 5 | Loop in een loop | Zwevend spook | 0 → 24, 5 → 29 | +24 met alleen +1 en tellers tot 6: een teller in een teller |
| 6 | Naar boven afronden (H2, H5) | Ritmeschildpad | 3 → 5, 10 → 10, 12 → 15, 21 → 25 | `while (v % 5 != 0) v++` |
| 7 | Rondjes in een byte (H2) | Level 256 | 250 → 14, 240 → 4, 200 → 4 | `while (v > 100) v += 20` stopt alleen omdat de byte overloopt; een poort `< 300` stopt nooit |
| 8 | Halveren (H2) | Splijter | 100 → 3, 37 → 4, 64 → 4 | Een int delen door een int kapt af: 25 / 2 is 12 |
| 9 | Aanvullen (H3) | Het Etiket | "a" → "a-----", … | `while (v.Length < 6) v += "-"`; wat al lang genoeg is, blijft |
| 10 | Zune (elite, echte bug) | Zune 30 | 400 → 34, 1000 → 268, 366 → 366 | De loop van de Zune ligt al vast en werkt, behalve op dag 366: die draait eeuwig. Geef die dag een uitweg |

**Zune (2008):** op 31 december 2008 bevroren alle Zune 30-spelers bij het opstarten: in een schrikkeljaar trok de while-loop alleen een jaar af als er meer dan 366 dagen over waren, dus op dag 366 gebeurde er niets. Geen doden, zoals [de visie](../../visie.md) vraagt; Zune stond daar al voor de Lopende Band.

## Wat ze met de wereld deelt

- **Codex (H6):** *while en do while* (een poort ziet hetzelfde product terug), *for* (een teller is klaar), *Geneste loops* (een teller is klaar terwijl een andere nog telt), *Oneindige loop* (dezelfde toestand kwam terug). Ook een band die niet lukt, opent pagina's: een oneindige loop voel je het best als hij misloopt.
- **Codex uit de Card Hall:** *Modulo*, *Overflow* (het doelwit is het product, `enemy.<key>`), *Een int delen door een int* en *De lengte van een string* gaan ook hier open, met dezelfde teksten.
- **✗-register:** *Laat de band nooit eeuwig draaien* (een oneindige loop), *Niet stoppen bij hoofdstuk 6* (de laatste bestelling), en verborgen *Niets van de band laten vallen*.
- **Voortgang:** per bestelling je beste machines en tikken, en je bord (`PlayerProgress.ConveyorBelt`). Nog alleen in de browser.
- **Ontgrendelen:** nog niet. Komt later, waarschijnlijk na de laatste baas van de Controlekamer (zie [todo](todo.md)).
