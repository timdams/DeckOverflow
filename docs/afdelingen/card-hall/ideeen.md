# The Card Hall: ideeën en open vragen

Wat beslist is, verhuist naar zijn document; wat technisch is uitgesteld, staat in [todo.md](todo.md).

## De eerste playtest

De grootste onzekerheid blijft of studenten de regels in de game herkennen wanneer ze later echte code lezen.

- [x] **Klaarmaken.** De playtests lopen goed (oktober 2026). De sneltoets W blijft erin (beslist op 4 oktober 2026), alleen voor de superuser.
- [ ] **Wie.** 5 studenten en 2 collega's, zoals eerder beslist. Liefst ook één leerling uit het middelbaar, voor de ondergrens.
- [ ] **Wat we meten.**
  - Begrijpen ze zonder uitleg wat een intent met `block` of `cards` doet?
  - Merken ze de teases op (paginanummer, deur, zakje, de Stray), en worden ze er nieuwsgierig van?
  - Lezen ze de Codex, en hoe ver (de teller `1/4` toont het)?
  - Waar zitten ze vast? De Ink-vastloper werd pas gevonden door te spelen; er zijn er vast meer.
- [ ] **Transfer meten.** Na de playtest dezelfde studenten 5 korte C#-expressies laten voorspellen, bv. `7 / 2`, `"1" + 2`, `(byte)300`, `(int)2.9`, `'A' + 1`.

## Balans van de Card Hall

- [ ] **Run-lengte.** Drie acts van 6 rijen: halen we 30 tot 45 minuten, of wordt het langer? Meten in de playtest.
- [ ] **De 613-combo.** Ink, Spare Screw, Read en Whack blijven over beurten heen werken. Scrap maakt dat niet sterker, maar test of de combo te vaak valt.
- [ ] **Scrap.** 1 energie is een gok. Te duur als het vaak nodig is, te goedkoop als het een gratis "oeps" wordt. Als de playtest toont dat Scrap vaak nodig is, komt er een relic **Undo** (Ctrl+Z: de eerste Scrap per gevecht is gratis).
- [ ] **Y2K.** Zonder Floating-kaart of Count Letters moet je drie beurten wachten. Is dat spannend (de klok tikt naar middernacht) of saai?
- [ ] **Score.** De gewichten (100 per verdieping, 10 per HP, 5 per bespaarde beurt onder 150) zijn een eerste voorstel. Kijken naar de verdeling zodra de Prikklok scores verzamelt; het histogram moet spreiding tonen, geen muur bij 2100.
- [ ] **Zeldzaamheden en pity timer** afstellen. Er is nu geen pity timer.

## Inhoud die nog wacht

Uit het oorspronkelijke ontwerp, nog niet gebouwd. Elk punt moet eerst als gevecht leuk zijn (de ontwerptoets).

- [x] **Bool-schim** (`bool`) en **Ritmeschildpad** (`%`), gebouwd, zie [act-1.md](act-1.md).
- [x] **Tweelingschutters, De Naamloze, de patch met `const` en Haakjes**: gebouwd op 4 oktober 2026, zie [act-1.md](act-1.md). De Etiketkamer is geschrapt: een etiket kiezen om een vat te openen is een vraag als poort.
- [ ] **The Nameless testen op fun.** Identifiers blijft het zwakste concept als mechaniek. Is een vaste kring van namen (raak, blok, raak, blok) spannend, of een sleur? En een echte bug voor hem zoeken, zoals de andere elites hebben.
- [ ] **De Tighten-kaarten afstellen.** Tighten, Then Hit+ (`++count * 3`) na drie Hit, Then Tighten slaat 21 voor 1 energie. Te sterk?
- [ ] **Operatorvoorrang in je eigen aanval.** Modifiers werken nog van links naar rechts, met haakjes in beeld: `(6 + 3) × 2`. Echte voorrang (`6 + 3 × 2` is 12) verandert elke combo; eerst beslissen of dat de bedoeling is. Move the Brackets doet het nu alleen op de aanval van een vijand.
- [ ] **Exceptions met een catch**: de beurt als call stack en de blueprints. Een eigen act, of een afdeling na H10.
- [ ] **Een derde elite voor act 3?** Nu Flight 501 en The Index. The Counter kan ook in act 3 terugkomen; Mars Climate Orbiter (verwisselde eenheden) heeft al een tekening.

## Sfeer en art

- [ ] **Tekeningen die klaarliggen** (het vel `extras`): de held die juicht en die onderuitgaat, voor het eindscherm; de held die leest, voor de Codex; de Prikklok en het zakje, voor hun knoppen; de lusslang, de kratten, het doosspook, de schakelaar en de voorman voor latere afdelingen.
- [ ] **Geluid**: een eerste set van tien kerngeluiden maken en testen. Geen prioriteit voor de playtest.
- [ ] **Een changelog** als tease ("Controlekamer: regels bijgewerkt"), nog niet gebouwd.
