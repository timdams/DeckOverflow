# Beeldtaal: elke afdeling is een plek

Hoe een scherm eruitziet, in elke afdeling. Het thema (een montagehandleiding, het figuurtje zonder gezicht, kleur voor types) staat in [de visie](../visie.md#thema-niet-volgens-de-handleiding); hier staat hoe je er een scherm mee bouwt. Beslist op 4 oktober 2026, na mockups van elk scherm op een [design-canvas](https://claude.ai/artifact/C2vUobKdxoLhtyMRDCo6Au). Het canvas is een schets; deze tekst is de bron.

## Waarom

Het titelscherm werkt: één grote tekening met een verhaal, een figuur die iets doet, een titel op grote schaal, en oranje alleen waar een ✗ staat. De afdelingen erna vielen plat. De Controlekamer en de Lopende Band waren webformulieren in drie kolommen, met een klein prentje in een kader, en de plattegrond was een rooster van kaartjes. Wie het titelscherm ziet, verwacht een fabriek en krijgt een instellingenpagina.

## Vijf regels

1. **Elke afdeling is een plek.** In het midden staat één groot ding waar het gebeurt: het gevecht, de testcel, de werkvloer. De rest staat eromheen, zoals in die ruimte.
2. **Wat je bedient, is een ding uit die plek.** Regels zijn platen in een schakelkast, de gereedschapsbak is de onderdelenlijst van een handleiding, een bestelling hangt op een klembord, de tijdlijn is een ponsband. Een knop blijft een echte knop (toetsenbord, schermlezer), maar ziet eruit als een onderdeel.
3. **Eén zware letter, op schaal.** Koppen in het gewicht van het titelscherm: dezelfde systeemletter in 900, strak gespatieerd (`.heavy`). Geen webfont: Arial in 900 is op Windows al Arial Black, en een schoolnetwerk blokkeert geen systeemletters. Getallen die tellen (beurten, tikken, rondjes) staan in een telwerk: witte cijfers op inkt.
4. **Kleur blijft voor types.** Inkt, papier, en metaal (`--shade`) voor kasten en bakken. Oranje alleen voor ✗, echte bugs, het boek en de ene grote knop waarmee je begint. Gelukt is een zwarte stempel, mislukt een oranje ✗: geen groen of rood meer (dat was een uitzondering uit spike 9).
5. **Grote momenten veranderen de ruimte.** Een regel die vuurt, laat zijn lamp branden. Een oneindige loop doet het licht uit, behalve op het rondje dat blijft draaien. Een afdeling die opengaat, klapt uit haar doos. De getallen staan in de stage, bij de andere juice.

## De bouwstenen

Gedeeld in `wwwroot/css/app.css`, zodat elke afdeling uit dezelfde handleiding komt. De ponsband staat in `control-room.css` en de doos in `run.css`, zolang maar één scherm ze gebruikt.

| Bouwsteen | Hoe het eruitziet | Waar |
| --- | --- | --- |
| Stapnummer | Zwarte cirkel met een wit cijfer. Gestippeld en grijs: nog niet. Oranje met ✗: een echte bug | afdeling, gevecht, bestelling, regel |
| Kast | Metalen paneel met een dikke rand, een binnenrand en vier schroeven | schakelkast, de verzegelde kast van de vijand, het luik van de motorkap |
| Lamp | Een rondje per regel: wacht (metaal), vuurt (inkt, met stralen), nooit aan (grijs) | een regel in de Controlekamer |
| Onderdelenlijst | Per onderdeel een tekening in een vakje en een groot aantal (`∞`, `1×`); wat op is, wordt grijs | de gereedschapsbak |
| Bestelbon | Papier op een zwart klembord met een metalen klem, licht scheef | een bestelling van het front |
| Stempel | GOEDGEKEURD in inkt, ✗ in oranje, NOOIT BEREIKT in grijs. Een stempel valt met een klap | een uitslag |
| Telwerk | Witte cijfers op inkt, elk in een eigen vakje | beurten, tikken, regels, rondjes |
| Ponsband | Een papieren strook met een gaatje per beurt, met het nummer van de regel die vuurde | de tijdlijn van een duel |
| Gesloten doos | Een kartonnen doos met plakband en pijltjes "deze kant boven" | een afdeling die er nog niet is |

## Per scherm

- **De plattegrond:** de fabriek als één tekening, verbonden door de band van het titelscherm, met dozen voor wat er nog niet is. Zie [de wereld](README.md#één-tekening-de-band-en-de-dozen).
- **De Card Hall** had al het meeste. Het gevecht krijgt de tekening van de act als decor, groter type voor intent en HP, en de hand als echte waaier (zie [haar todo](../afdelingen/card-hall/todo.md)).
- **De Controlekamer:** twee schakelkasten en een testcel. Zie [haar README](../afdelingen/control-room/README.md#het-scherm-twee-kasten-en-een-testcel).
- **De Lopende Band:** een werkvloer met een trechter, een laadperron naar het front en een klembord. Zie [haar README](../afdelingen/conveyor-belt/README.md#het-scherm-de-werkvloer).

## Gebouwd

Op 4 oktober 2026, op dezelfde dag als de beslissing:

- **De bouwstenen** in `wwwroot/css/app.css` (`.heavy`, `.stencil`, `.step-badge`, `.bug-tag`, `.cabinet`, `.lamp`, `.rubber-stamp`, `.digits`, `.clipboard`, `.parts-list`, `button.big-round`), met drie kleine componenten in `World/`: `Digits` (een telwerk), `MachineIcon` (het teken op de grote knop) en `CardboardBox` (de doos).
- **De plattegrond** met de band en de dozen, en **uit de doos** (zie [de wereld](README.md#één-tekening-de-band-en-de-dozen)).
- **De Controlekamer** en **de Lopende Band**, elk volgens haar README.
- **De Card Hall** kreeg de tekening van de act als decor achter het gevecht, zacht en vervaagd (`setBackdrop` in de stage, de getallen in `juice.backdrop`). De platen hebben zelf een figuurtje; decorplaten zonder figuur staan op de [art-todo](../../art/todo.md). Sinds 4 oktober 2026 ook een grotere intent en HP-balk, de hand als waaier en een stapkader linksboven (zie [haar todo](../afdelingen/card-hall/todo.md)).

## Op een telefoon

Liggend blijft de standaard (zie [architectuur](../architectuur.md#platform-en-techniek)). De Lopende Band speelt ook rechtop: haar rooster is hoog genoeg als je het een kwartslag draait. De motor verandert niet; alleen de stage tekent de vloer gedraaid, zodat de band van boven naar onder loopt en de vakjes groot genoeg blijven om aan te tikken. Nog niet gebouwd (zie [haar todo](../afdelingen/conveyor-belt/todo.md)).
