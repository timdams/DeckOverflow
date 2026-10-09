# Todo: de wereld

Wat bewust is uitgesteld voor de wereld (plattegrond, backend, Codex, ✗-register), met genoeg context om het later op te pakken. Nieuwste bovenaan. Wat af is, gaat eruit (de geschiedenis staat in git).

## Plattegrond en uit de doos

De plattegrond is sinds 4 oktober 2026 één tekening, met dozen en het moment "uit de doos" (zie [README](README.md#één-tekening-de-band-en-de-dozen) en [de beeldtaal](beeldtaal.md)). Wat nog open staat:

- **Uit de doos bekijken in een zichtbaar venster.** Gebouwd en getest met stilgezette CSS-animaties (`?unbox=conveyor-belt` als superuser), maar nog niet op gevoel afgesteld: het tempo, de stralen, de explosietekening. Geen geluid; als dat erbij komt, via `wwwroot/audio/ui.js`.
- **Het eerste echte "uit de doos"** is de Controlekamer, sinds ze op 7 oktober 2026 vrijgegeven is. Nakijken met een speler die de laatste baas van de Card Hall verslaat: komt het moment na de onthulling, en voelt het als een beloning?
- **Een nieuwe afdeling krijgt een plaats op de vloer.** `Spots` in `WorldMap.razor` zet elke afdeling op de tekening; een afdeling zonder plaats belandt onderaan op een rij.
- **Het figuurtje** loopt nu naar de afdeling die je kiest. Bij de afdeling waar je het laatst speelde, zou ook kunnen; daarvoor moet de voortgang dat bijhouden.
- **Rechtop op een smal scherm** staan de vloer en het paneel onder elkaar en wordt de tekening klein. Nakijken op een telefoon.

## Taal

Het spel is Nederlands sinds 4 oktober 2026, met Engels als optie (zie [architectuur.md](../architectuur.md) en de [woordenlijst](woordenlijst.md)).

- **Kaart- en vijandnamen laten nalezen.** De woordspelingen ("Mep", "Erin wringen", "Restjeszakje", "De Caster") zijn een eerste versie; nalezen met iemand die de cursus geeft, en met studenten.
- **Tekeningen met Engelse tekst erin.** Nog niet nagekeken; het titelscherm heeft er geen. Een tekening met tekst moet ofwel zonder tekst, ofwel per taal, en hoort dan in [art/todo.md](../../art/todo.md).
- **Losse Engelse tekst in Razor of JS** vangt `StringsTests` niet; die controleert alleen sleutels. Een snelle zoektocht op 4 oktober 2026 vond niets, maar een test die zichtbare tekst zonder `S.T(` opspoort, ontbreekt.
- **Een taalwissel bouwt de schermen opnieuw op** (`@key` op `lang-scope` in `FactoryPage.razor` en `CardHallRun.razor`; de run zelf blijft): een open Codex-pagina of een half gekozen kaart in een venster springt terug naar het begin. Wisselen is zeldzaam; pas aanpakken als het stoort.
- **Het accountpaneel** en latere afdelingen: nakijken of al hun tekst via `Strings` loopt. De Controlekamer doet dat sinds 4 oktober 2026.

## Opties

- **Een wachtende run overleeft geen herlaadbeurt.** "Main menu" houdt de run in het geheugen; herladen of de tab sluiten (op een telefoon gebeurt dat vaak vanzelf) en ze is weg. Kan met `Run.Replay`: de shell bewaart seed, startact en de lijst commands (die zijn al polymorf serialiseerbaar) en speelt ze bij het laden opnieuw af. Let op `?fight=`-runs (een eigen map) en op `DebugWin` in de lijst.
- **Snelle modus in het optiescherm.** Bestaat nu alleen als toets F.
- **Escape opent de opties** op een laptop.

## Mobiel

Liggend spelen op een telefoon kwam erbij op 4 oktober 2026 (zie [architectuur.md](../architectuur.md#platform-en-techniek)). Getest in een desktopbrowser op 844×390 met nagebootste touch, nog niet op een echt toestel.

- **Testen op een echte telefoon**, Android en iPhone: volledig scherm, de notch, slepen met de vinger, de toetsenbordpopup in het accountpaneel.
- **Tekst in het gevecht is klein op een telefoon.** De stage schaalt 960×540 naar ongeveer 0,7: de uitleg op een kaart (11px) en het log worden zo'n 8px. Vasthouden vergroot een kaart, maar het log en de intents niet. Mogelijk: grotere letters in de stage zodra het scherm kort is, of het log achter een knop.
- **Snelle modus bestaat alleen als toets F.** Geluid staat nu in het optiescherm; snel nog niet (zie Opties).
- **Tooltips (`title`) bestaan niet op een aanraakscherm.** Wat ertoe doet, staat al in beeld (relics openen een overzicht, een uitgeschakelde keuze toont waarom), maar de map-knopen en de Scrap-knop leunen op een tooltip.

## Backend

- **Feedback op de Lopende Band.** `World/FeedbackHeart.razor` staat in de Card Hall en sinds 4 oktober 2026 in de uitslag van de Controlekamer (onderwerp `level:<key>`); de Lopende Band heeft het nog niet.
- **Testrijen in `feedback`.** Twee rijen van 4 oktober 2026 (`test:claude` en `enemy:slime`) zijn tests, geen echte spelers. Weggooien voor je telt: `delete from feedback where created_at < '2026-10-05';`
- **De docent kan alleen nog een klascode maken** (beslist op 3 oktober 2026, zie [backend.md](backend.md)). Wat nu meer doet en eruit moet: afdelingen vrijgeven (`release_department` en `Account.ReleaseAsync`, de knoppen in `AccountPanel.razor`, de kolom `classes.released_departments`, `unlocks.how = teacher`) en de voortgang van de klas lezen (policies op `progress` en `unlocks`). Daarna [backend.md](backend.md) en [supabase.md](supabase.md) bijwerken. Een migratie op het echte project, dus eerst afstemmen met de sessie die de backend bouwde.
- **Oude gastaccounts opruimen.** Elke nieuwe browser (of gewiste opslag) maakt een gast; wie stopt, laat er een achter. Supabase raadt aan om oude anonieme accounts geregeld te verwijderen. Kan mee in de nachtelijke Action van de scorecontrole, met de service-sleutel: anonieme accounts zonder activiteit sinds bv. 90 dagen. Er staat er nu al een: "Bright Spanner 74", van een test op 3 oktober 2026.
- **Klassen beheren.** Een docent kan nu alleen afdelingen vrijgeven, niet terugtrekken, en geen leerling uit de klas halen (de database laat dat wel toe, het scherm nog niet). Een dashboard met de voortgang van de klas komt na de MVP.

- **De service-sleutel als GitHub-secret.** De Action `score-check` (gebouwd op 7 oktober 2026) heeft `SUPABASE_SERVICE_KEY` nodig in Settings → Secrets → Actions: de secret key (`sb_secret_…`) of de oude service_role-sleutel van het project. Zonder faalt ze elke nacht, en pauzeert het gratis project na een week zonder activiteit. Daarna één keer met de hand starten (Run workflow) en nakijken.
- **Een testrij in `scores`.** Rij 1 (7 oktober 2026, score 0, lege commandolijst) kwam van een test van de Prikklok. De eerste nachtelijke controle wijst ze af; daarna mag ze weg: `delete from scores where id = 1;`
- **De Prikklok voor de andere afdelingen.** Alleen de Card Hall heeft een dagelijkse run. De Controlekamer en de Lopende Band meten een bord (regels, beurten, machines, tikken); daarvoor moeten ze een dagelijkse puzzel krijgen en moet de scorecontrole hun motor kennen. Eerst voorleggen.
- **De Prikklok opent op de afdeling waar je het laatst speelde** (README). Pas nodig als er een tweede afdeling met een dagelijkse run is.
- **Het klassement van gisteren.** Het venster toont alleen vandaag. Wie wil zien hoe de klas het gisteren deed, nadat alles nagekeken is, kan dat nog niet.
- **Leesstand, ✗-panelen en het zakje met onderdelen synchroniseren.** Nu blijven ze in de browser (`PlayerProgress.CodexRead`, `XPanels`, `Trinkets`); een tweede toestel toont 0 panelen en een leeg zakje. Kan als kolommen op `profiles` of een eigen tabel.
- **Een tweede Supabase-project voor ontwikkeling.** Nu praat ook `localhost` met het echte project. Het gratis plan laat twee actieve projecten toe en `stadsrally` neemt er al een.

## Code modulair maken

Gedaan op 3 oktober 2026: `DeckOverflow.Core` en `DeckOverflow.CardHall` als aparte projecten met eigen tests, de shell met `World/` en `Features/CardHall/`, de stage in `wwwroot/card-hall/stage/`, en het ✗-register met `IXPanelSource` (`World/XPanels.cs`). Wat nog rest:

Gedaan op 4 oktober 2026: de wereld (`World/FactoryPage`, `World/TitleScreen`) en de run van de Kaartenhal (`Features/CardHall/CardHallRun`) zijn gesplitst; de stages delen `wwwroot/shared/` en `wwwroot/audio/`; het ✗-register heeft een pagina per afdeling; `tests/DeckOverflow.Web.Tests` test de shell. De Codex blijft één catalogus in Core, want ze is het boek en dezelfde pagina gaat in verschillende afdelingen open; wanneer een afdeling een pagina mag openen, beslist ze zelf (`Acts.ActOfChapter` in de Kaartenhal). Wat nog rest:

- **De Controlekamer en de Lopende Band laden hun eigen voortgang** (`Store.LoadAsync` in hun pagina) en tonen hun eigen ✗-popups en meldingen. Een gedeelde dienst voor voortgang, popups en meldingen zou dat één keer doen, zoals `FactoryPage` het nu voor de Kaartenhal doet.
