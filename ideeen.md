# Ideeën en open vragen

Richting, geen belofte, per afdeling. Hier bouwt niemand aan tot de product owner erom vraagt of het als beslissing in [DECISIONS.md](DECISIONS.md) staat (zie [Rolverdeling](CLAUDE.md#rolverdeling)). Wat beslist is, verhuist naar het document van zijn module; wat technisch is uitgesteld, staat in de `todo.md` van de module.

Een nieuw idee komt onder zijn afdeling, of onder De wereld als het over de plattegrond, de Codex, het ✗-register, de backend of over meerdere afdelingen gaat. Een nieuwe afdeling krijgt hier een eigen sectie.

## De wereld

- (nog leeg)

## The Card Hall

### De eerste playtest

De grootste onzekerheid blijft of studenten de regels in de game herkennen wanneer ze later echte code lezen.

- [x] **Klaarmaken.** De playtests lopen goed (oktober 2026). De sneltoets W blijft erin (beslist op 4 oktober 2026), alleen voor de superuser.
- [ ] **Wie.** 5 studenten en 2 collega's, zoals eerder beslist. Liefst ook één leerling uit het middelbaar, voor de ondergrens.
- [ ] **Wat we meten.**
  - Begrijpen ze zonder uitleg wat een intent met `block` of `cards` doet?
  - Merken ze de teases op (paginanummer, deur, zakje, de Stray), en worden ze er nieuwsgierig van?
  - Lezen ze de Codex, en hoe ver (de teller `1/4` toont het)?
  - Waar zitten ze vast? De Ink-vastloper werd pas gevonden door te spelen; er zijn er vast meer.
- [ ] **Transfer meten.** Na de playtest dezelfde studenten 5 korte C#-expressies laten voorspellen, bv. `7 / 2`, `"1" + 2`, `(byte)300`, `(int)2.9`, `'A' + 1`.

### Balans van de Card Hall

- [ ] **Run-lengte.** Drie acts van 6 rijen: halen we 30 tot 45 minuten, of wordt het langer? Meten in de playtest.
- [ ] **De 613-combo.** Ink, Spare Screw, Read en Whack blijven over beurten heen werken. Scrap maakt dat niet sterker, maar test of de combo te vaak valt.
- [ ] **Scrap.** 1 energie is een gok. Te duur als het vaak nodig is, te goedkoop als het een gratis "oeps" wordt. Als de playtest toont dat Scrap vaak nodig is, komt er een relic **Undo** (Ctrl+Z: de eerste Scrap per gevecht is gratis).
- [ ] **Y2K.** Zonder Floating-kaart of Count Letters moet je drie beurten wachten. Is dat spannend (de klok tikt naar middernacht) of saai?
- [ ] **Score.** De gewichten (100 per verdieping, 10 per HP, 5 per bespaarde beurt onder 150) zijn een eerste voorstel. Kijken naar de verdeling zodra de Prikklok scores verzamelt; het histogram moet spreiding tonen, geen muur bij 2100.
- [ ] **Zeldzaamheden en pity timer** afstellen. Er is nu geen pity timer.

### Inhoud die nog wacht

Uit het oorspronkelijke ontwerp, nog niet gebouwd. Elk punt moet eerst als gevecht leuk zijn (de ontwerptoets).

- [x] **Bool-schim** (`bool`) en **Ritmeschildpad** (`%`), gebouwd, zie [act-1.md](docs/afdelingen/card-hall/act-1.md).
- [x] **Tweelingschutters, De Naamloze, de patch met `const` en Haakjes**: gebouwd op 4 oktober 2026, zie [act-1.md](docs/afdelingen/card-hall/act-1.md). De Etiketkamer is geschrapt: een etiket kiezen om een vat te openen is een vraag als poort.
- [ ] **The Nameless testen op fun.** Identifiers blijft het zwakste concept als mechaniek. Is een vaste kring van namen (raak, blok, raak, blok) spannend, of een sleur? En een echte bug voor hem zoeken, zoals de andere elites hebben.
- [ ] **De Tighten-kaarten afstellen.** Tighten, Then Hit+ (`++count * 3`) na drie Hit, Then Tighten slaat 21 voor 1 energie. Te sterk?
- [ ] **Operatorvoorrang in je eigen aanval.** Modifiers werken nog van links naar rechts, met haakjes in beeld: `(6 + 3) × 2`. Echte voorrang (`6 + 3 × 2` is 12) verandert elke combo; eerst beslissen of dat de bedoeling is. Move the Brackets doet het nu alleen op de aanval van een vijand.
- [ ] **Exceptions met een catch**: de beurt als call stack en de blueprints. Een eigen act, of een afdeling na H10.
- [ ] **Een derde elite voor act 3?** Nu Flight 501 en The Index. The Counter kan ook in act 3 terugkomen; Mars Climate Orbiter (verwisselde eenheden) heeft al een tekening.

### Sfeer en art

- [ ] **Tekeningen die klaarliggen** (het vel `extras`): de held die juicht en die onderuitgaat, voor het eindscherm; de held die leest, voor de Codex; de Prikklok en het zakje, voor hun knoppen; de lusslang, de kratten, het doosspook, de schakelaar en de voorman voor latere afdelingen.
- [ ] **Geluid**: een eerste set van tien kerngeluiden maken en testen. Geen prioriteit voor de playtest.
- [ ] **Een changelog** als tease ("Controlekamer: regels bijgewerkt"), nog niet gebouwd.

## The Control Room

### Concepten uit vorige hoofdstukken laten terugkomen

Gevraagd op 4 oktober 2026: casting, parsing, datatypes in de puzzels. C# is natuurkunde in elke afdeling, dus de types gedragen zich hier zoals in de Card Hall.

- **Gebouwd op 4 oktober 2026** als gevecht 9 en 10, anders dan eerst gedacht: de `byte` is jóúw automaat (wie te gretig oplapt, loopt over), en het kommagetal is jóúw Mep (opwinden redt de halve punt). Zo zit de keuze bij de speler.
- **Een `byte`-automaat als vijand.** Zijn HP is een `byte`; zijn regel "mijn HP < 20 → Oplappen" lapt hem op voorbij 255, en dan loopt hij over naar een klein getal. Wie zijn regels leest, laat hem zichzelf kapot oplappen. Een terugkeer van Level 256.
- **Kommagetallen tegen een `int`.** Een automaat slaat voor 6.5; jouw HP is een `int` en kapt af. Een voorwaarde `me.Hp < 20` rekent met het afgekapte getal, en dat verschuift het moment om op te lappen.
- **`char` en `int.Parse`: gebouwd op 4 oktober 2026** als De Typograaf (12) en De Kassabon (16), zie [README](docs/afdelingen/control-room/README.md#de-gevechten-drie-lagen). De oorspronkelijke gedachte: Een `char` is een getal (`'A'` is 65) en tekst wordt pas een getal na `int.Parse`. In de Card Hall zijn dat kaarten die je op een vijand speelt; in een regelduel zou het een zet worden als "Lezen" (parse de tekst van De Telex: `"405.5"` crasht met een `FormatException`, `"40"` wordt 40) of een vijand met een `char` als HP die door een klap van `'A'` naar `'7'` springt. Kan, maar het wordt snel een rekenoefening; eerst kijken hoe 11 tot 15 spelen.
- **Casting in een voorwaarde**, spaarzaam: `(int)(foe.Hp / 10) == 2`. Risico: het wordt een rekenoefening in plaats van een keuze.
- **Tekst als voorwaarde** (H3): een automaat die een bord leest, `label == "STOP"`, en een hoofdletter die het verschil maakt.

### Meer uit H5

- **`switch` als tabel van gevallen.** Een automaat die per beurtnummer % 4 iets anders doet, als tabel in plaats van een keten.
- **Kortsluiten.** Bij `&&` wordt de rechterkant niet bekeken als de linkerkant al `false` is; met een voorwaarde die iets kost (een teller die oploopt bij elke keer bekijken), wordt dat voelbaar.
- **De wetten van De Morgan**: `!(a && b)` is `!a || !b`. Een gevecht met twee borden die hetzelfde doen, waarvan één met minder tegels.

### Gambits als intents in de deckbuilder

Wie hier de regels van een vijand leest, kan dat ook in een kaartgevecht: de Stray Automaton in de Card Hall heeft al een regel als intent. Meer vijanden met een zichtbaar regelbord zou de twee afdelingen verbinden.

## The Conveyor Belt

- **Meer uit H6:** `break` (een noodstop op de band die het product meteen naar de uitgang stuurt), een `for` die omhoog en omlaag telt, en loops over tekst (een product per letter).
- **Meer terugblik:** casting (een machine `(byte)` die een te grote kist laat overlopen, tegenover `Convert`, dat crasht), poorten met EN en OF (H5), een `double` die na `+0.1` tien keer geen `1.0` is.
- **Meerdere producten tegelijk** op de band, die op elkaar wachten.
- **De Gereedschapsmuur (H7)** bouwt hierop: dezelfde band, met blueprints. Een stuk band één keer bouwen en overal stempelen, met instelknoppen (parameters).
- **Een klassement per bestelling:** het histogram van machines en tikken over de klas, zoals Opus Magnum.
