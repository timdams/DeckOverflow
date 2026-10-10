# Beslissingen

Keuzes die bij de product owner liggen (zie [Rolverdeling](CLAUDE.md#rolverdeling)). De implementer zet een keuze hier met status OPEN en vraagt ze; de product owner vult **Beslissing** in en zet de status op BESLIST. Daarna krijgt ze één regel met datum onder [Beslist](docs/README.md#beslist), en de uitwerking in het document van haar module (D-001). Een nieuwe beslissing krijgt het volgende nummer; nummers worden niet hergebruikt.

```
## D-000 Titel
Status: OPEN | BESLIST

**Context:** waarom de keuze nu opduikt, en wat ervan afhangt.
**Opties:**
- A. ... Voor: ... Tegen: ...
- B. ...
**Aanbeveling:** de optie van de implementer, met de reden.
**Beslissing:** (vult de product owner in, met de datum)
```

## D-001 Verhouding tot "Beslist" in docs/README.md
Status: BESLIST

**Context:** [docs/README.md](docs/README.md#beslist) houdt al een lijst "Beslist" bij, kort en met datum, met de uitwerking in het document van het onderwerp. Met dit bestand komen er twee plekken waar beslissingen staan.
**Opties:**
- A. DECISIONS.md is de werkbank voor keuzes van de product owner. Wordt iets BESLIST, dan komt er ook één regel met datum in "Beslist" en de uitwerking in het document van de module. Voor: de docs blijven de enige bron voor het ontwerp, de modulaire opzet blijft. Tegen: een beslissing staat op twee plekken.
- B. Alles naar DECISIONS.md; "Beslist" in docs/README.md verdwijnt of verwijst hierheen. Voor: één plek. Tegen: het ontwerp leest niet meer op zichzelf, en DECISIONS.md groeit over alle afdelingen heen.
- C. Los van elkaar laten. Voor: geen werk. Tegen: ze lopen uit elkaar.
**Aanbeveling:** A. Het past bij "schrijf ontwerpwijzigingen rechtstreeks in het document van de module", en DECISIONS.md houdt ook de afgewogen opties bij, wat "Beslist" niet doet.
**Beslissing:** A (7 oktober 2026).

## D-002 Ideeën: hier of in de ideeen.md van een afdeling
Status: BESLIST

**Context:** De rolverdeling zegt dat ideeën onder "Ideeën" in DECISIONS.md komen. Elke afdeling heeft al een eigen `ideeen.md`, en de root-CLAUDE.md zegt: wat voor één afdeling geldt, schrijf je in haar map.
**Opties:**
- A. Alle ideeën hier. Voor: één lijst om door te nemen. Tegen: breekt de modulaire opzet; wie aan één afdeling werkt, leest ideeën van alle afdelingen.
- B. Een idee voor één afdeling in haar `ideeen.md`, een idee voor de wereld of over afdelingen heen hier. Voor: past bij de bestaande indeling. Tegen: de product owner moet op meer plekken kijken.
**Aanbeveling:** B. Tot er beslist is, volg ik A, zoals gevraagd.
**Beslissing:** geen van beide: één `ideeen.md` op de root, met een sectie per afdeling en een voor De wereld. De drie `ideeen.md`'s van de afdelingen zijn erin opgegaan (7 oktober 2026).

## D-003 Branches of rechtstreeks op main
Status: BESLIST

**Context:** De reviewer kijkt naar de diff tegenover main, maar tot nu toe gebeurt alle werk rechtstreeks op `main`. Daar is die diff leeg.
**Opties:**
- A. Op main blijven werken. De reviewer bekijkt dan wat nog niet naar `origin/main` gepusht is, plus wat nog niet gecommit is (zo staat hij nu ingesteld). Voor: niets verandert aan je werkwijze. Tegen: wie meteen pusht, heeft niets meer te reviewen; `/review HEAD~3` kan dan wel.
- B. Elke taak op een eigen branch, en mergen na de review. Voor: een duidelijke grens per taak, de diff is altijd de taak. Tegen: meer handelingen; GitHub Pages en CI draaien pas na de merge.
**Aanbeveling:** B voor taken die langer dan één sessie duren, A voor kleine dingen. De reviewer werkt al met beide.
**Beslissing:** A, alles op `main` (7 oktober 2026).


## D-004 De Controlekamer vrijgeven
Status: BESLIST

**Context:** De Controlekamer stond sinds 4 oktober 2026 op *binnenkort*: gebouwd, maar dicht tot ze af is. Het plan was eerst een speeltest met studenten, dan `Availability.Released`.
**Opties:**
- A. Nu vrijgeven. Voor: spelers komen zelf binnen, het eerste echte "uit de doos" gebeurt, en de feedback per gevecht begint te lopen. Tegen: de vragen van spike 8 (lezen ze de regels, ontdekken ze de volgorde) zijn nog niet getest.
- B. Eerst een speeltest. Voor: wat niet werkt, zien spelers niet. Tegen: wacht op een klas.
**Aanbeveling:** B, omdat de vrijgave en de Lopende Band ervan afhangen.
**Beslissing:** A, nu vrijgeven (7 oktober 2026). De sleutel van de Lopende Band is nog niet beslist.

## D-005 Hoe de Prikklok eruitziet
Status: BESLIST

**Context:** De Prikklok was een grijze knop op de plattegrond. De score, de dagelijkse seed en het schema in Supabase bestonden al; het scherm en de plek van de dagelijkse run niet.
**Opties:**
- A. Eén venster, op de plattegrond en (na je eerste run) op het titelscherm: "Prik in", je score van vandaag, het histogram van iedereen en de top 10 van je klas met bijnamen. Voor: alles op één plek, ook voor de onthulling. Tegen: een knop meer op het titelscherm.
- B. "Dagelijkse run" als knop in het paneel van de Card Hall, de Prikklok toont alleen de uitslag; voor de onthulling geen dagelijkse run. Voor: zoals de docs het schetsten. Tegen: twee plekken.
- C. Zoals A, maar alleen het histogram, zonder bijnamen.
**Aanbeveling:** A. Alleen voor de Card Hall: de andere afdelingen hebben nog geen score die de server kan nakijken.
**Beslissing:** A (7 oktober 2026).

## D-006 Opnieuw spelen na je dagelijkse run
Status: BESLIST

**Context:** Beslist op 3 oktober 2026: de eerste voltooide dagelijkse run telt. Open was of je daarna nog eens mag spelen.
**Opties:**
- A. Eén keer per dag, zoals een prikklok; daarna toont de knop je score tot morgen. Voor: duidelijk. Tegen: wie wil oefenen, moet wachten.
- B. Opnieuw spelen mag, maar telt niet; het scherm zegt dat. Voor: oefenen op dezelfde run. Tegen: wie de run al kent, kan een klasgenoot voorzeggen (dat kon al).
**Aanbeveling:** A.
**Beslissing:** B (7 oktober 2026). Opgeven telt niet als voltooid.

## D-007 Het histogram telt de eerste run
Status: BESLIST

**Context:** De databasefunctie `score_histogram` telde per speler de beste score, terwijl de eerste voltooide run telt (D-006). Een wijziging aan het Supabase-schema.
**Opties:**
- A. Een migratie: per speler de eerste score die niet afgewezen werd, meteen uitgevoerd op het project. Voor: de database volgt de regel, ook als een speler op twee toestellen speelt. Tegen: een migratie op het echte project.
- B. De migratie schrijven, de product owner voert ze uit.
- C. Niets: de client stuurt toch maar één score per dag in.
**Aanbeveling:** A. Alleen de functie verandert, geen tabel.
**Beslissing:** A (7 oktober 2026). Uitgevoerd als `20261007174912_punch_clock_first_run`.

## D-008 Met welke motor de nachtelijke controle nakijkt
Status: OPEN

**Context:** `tools/DeckOverflow.ScoreCheck` speelt elke ingestuurde run 's nachts opnieuw af met de motor van `main` op dat moment. Wie overdag een getal van een kaart, vijand of relic wijzigt en pusht, laat alle eerlijke runs van die dag afwijzen. Ook een leerling die nog een oude build in de cache heeft, wordt afgewezen. Gevonden door de reviewer.
**Opties:**
- A. Een motorversie (bv. de commit of een getal in de motor) meesturen in `scores`; de controle wijst een andere versie niet af maar laat ze pending of markeert ze apart. Voor: eerlijk. Tegen: een kolom erbij in Supabase, en een oude versie kan je 's nachts niet meer afspelen.
- B. Afspraak: spelwijzigingen die de Card Hall raken, pas na middernacht UTC uitrollen. Voor: geen code. Tegen: makkelijk vergeten; de gecachete build blijft een gat.
- C. Een afgewezen run van vóór een uitrol opnieuw nakijken met de motor van toen (de Action checkt die commit uit). Voor: niets aan het schema. Tegen: de Action moet per dag een commit kennen.
**Aanbeveling:** A, met een versiegetal in de motor dat omhooggaat bij elke wijziging aan de regels van de Card Hall. Tot dan B.
**Beslissing:**

## D-009 Ongecontroleerde scores in het klassement
Status: OPEN

**Context:** Het histogram en de top 10 van de klas tonen alle scores die niet afgewezen zijn, dus ook die van vandaag die nog pending zijn. Een vervalste score (via de REST-API) staat er dus tot de nacht. Een score boven 10.000 laat de shell sowieso weg (`PunchClock.MaxScore`).
**Opties:**
- A. Zo laten: je ziet de stand van vandaag meteen; de nacht ruimt op. Voor: meteen feedback. Tegen: een dag lang kan iemand bovenaan staan met een valse score.
- B. Alleen nagekeken scores (`ok`). Voor: wat je ziet, klopt. Tegen: de stand van vandaag zie je pas morgen; het histogram van vandaag is leeg.
- C. Zoals A, maar een pending score in de klaslijst gemarkeerd (bv. een stempel "nog niet nagekeken").
**Aanbeveling:** C.
**Beslissing:**

## D-010 Of je vandaag al prikte, op elk toestel
Status: OPEN

**Context:** De prikkaart (`PlayerProgress.Punch`) blijft in de browser. Op een tweede toestel of na gewiste opslag zegt het spel opnieuw "Prik in", en het eindscherm zegt "Geprikt". De server telt de eerste run (D-007), dus een tweede toestel telt niet. De tekst zegt nu "je eerste uitgespeelde run van vandaag op dit toestel", wat klopt maar niet uitlegt welke telt.
**Opties:**
- A. Bij het openen van de Prikklok en op het eindscherm in `scores` kijken of je vandaag al een rij hebt. Voor: klopt overal. Tegen: een netwerkvraag; zonder netwerk weet je het niet.
- B. `Punch` mee synchroniseren via `profiles` of een eigen kolom. Voor: één bron. Tegen: een schemawijziging.
- C. Zo laten. Voor: geen werk. Tegen: een leerling met twee toestellen snapt niet welke score telt.
**Aanbeveling:** A: de rij in `scores` is al de waarheid.
**Beslissing:**

## D-011 Details van de vrijgave en de Prikklok
Status: OPEN

**Context:** Keuzes die ik maakte bij D-004 en D-005 en die de speler ziet. Gevonden door de reviewer. Graag bevestigen of bijsturen.
**Opties (elk apart te bevestigen):**
- A. Een vrijgegeven afdeling zonder sleutel (wie de onthulling via het vangnet kreeg) toont de tease zoals bij binnenkort: tekening, spelvorm, uitleg, "Op slot" en "De sleutel ligt bij de laatste baas van de Kaartenhal." Anders: een dichte doos zoals bij ooit.
- B. Het ✗-register verbergt de panelen van een afdeling die voor jou nog dicht is (nu ook de vrijgegeven Controlekamer zonder sleutel), zodat het register voor de onthulling niets verklapt. Zo was het ook bij binnenkort.
- C. Het histogram heeft één emmer per verdieping (100 punten). De klaslijst verschijnt pas als er minstens twee spelers zijn; sta je niet in de top 10, dan sta je eronder.
- D. De Prikklok staat op het titelscherm pas na je eerste run, zodat de eerste minuten één knop hebben.
**Aanbeveling:** alle vier zo houden.
**Beslissing:**

## D-012 De ondertitel en de naam van het ✗-register
Status: BESLIST

**Context:** De ondertitel op het titelscherm ("Een deckbuilder waarin de wereld C# gehoorzaamt.") klopt niet meer nu de fabriek ook afdelingen zonder kaarten heeft. De naam "✗-register" is moeilijk uit te spreken en zegt niet wat erin staat.
**Opties (ondertitel):**
- A. "De handleiding zegt nee. C# zegt ja." Voor: sluit aan op de intro en de panelen, geldt voor elke afdeling. Tegen: zegt niet wat voor spel het is.
- B. "Een fabriek waarin alles C# gehoorzaamt." Voor: dicht bij de oude zin. Tegen: vlakker.
- C. "Een fabriek die C# gehoorzaamt, niet de handleiding." Een mengvorm.
**Opties (naam):**
- A. Strafblad / Rap sheet. Voor: elke leerling weet wat erop staat, past bij "Je deed het toch." Tegen: geen.
- B. Verboden panelen / Forbidden panels. Voor: letterlijk. Tegen: minder speels.
- C. Overtredingen / Violations. Voor: kort. Tegen: in spike 7 waren de kaarten de overtredingen.
**Aanbeveling:** A en A.
**Beslissing:** A en A (9 oktober 2026). In het spel heet het register Strafblad (Engels: Rap sheet); in code en docs blijft het intern ✗-register (`XRegister`, `xpanel.*`).

## D-013 Een beheerpaneel voor feedback en scores
Status: BESLIST

**Context:** Tim wil de feedback en de scores in het spel zelf lezen, niet alleen in het Supabase-dashboard. Feedback is nu met opzet voor niemand leesbaar via de API ([backend.md](docs/wereld/backend.md#feedback-in-het-spel)), en scores lees je alleen voor je eigen klas. Een paneel vraagt dus nieuwe databasefuncties die wél alles teruggeven, maar alleen aan de maker. De controle moet in de database gebeuren, niet in de browser: de bestaande superuser-controle in `Superuser.cs` is te patchen, en dat mag hier niet, want het gaat om berichten van leerlingen. Volgens de superuser-regel staat wie de maker is alleen in Supabase, nooit in de publieke code.
**Opties (wie mag het zien):**
- A. Wie `app_metadata.role = 'superuser'` heeft. De databasefuncties (security definer) controleren die rol in het JWT. Voor: volgt de bestaande regel; geen e-mailadres in de publieke repo; een speler kan zijn `app_metadata` niet zelf aanpassen. Tegen: wie je later superuser maakt om te testen, ziet ook de feedback.
- B. Een vast e-mailadres (dams.tim@telenet.be) in de databasefuncties. Voor: precies wat gevraagd is. Tegen: je adres staat in de publieke repo, en het gaat in tegen "wie het is, staat alleen in Supabase".
- C. Een aparte rol `admin` naast `superuser`. Voor: testers zien geen feedback. Tegen: een tweede rol om bij te houden.
**Opties (waar):** een knop "Beheer" in het accountpaneel, alleen zichtbaar voor wie het mag, die een paneel opent op de plattegrond (geen eigen route: een vernieuwing op `/admin` geeft een 404 op GitHub Pages).
**Aanbeveling:** A, of C als je de superuser-rol aan testers wilt geven. In beide gevallen zet je de rol één keer op je account met de SQL uit backend.md. Lokaal is iedereen superuser in de browser, maar de database geeft alleen data aan je echte account.
**Beslissing:** A, met feedback per onderwerp, de berichten en de scores, via een knop "Beheer" in het accountpaneel (10 oktober 2026).
