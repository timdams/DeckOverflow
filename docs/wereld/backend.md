# Backend: hosting, accounts en data

Voor de hele wereld, niet voor één afdeling. Het werkplan met de stand van de migraties staat in [supabase.md](supabase.md); wat nog moet, in [todo.md](todo.md).

# Hosting, accounts en data

Beslist op 3 oktober 2026. Iedereen mag Deck Overflow spelen: studenten, maar ook leerlingen uit het middelbaar en wie het toevallig vindt. Er spelen dus minderjarigen mee, en dat stuurt elke keuze hieronder: zo weinig gegevens als het spel nodig heeft, en niets wat publiek naar een persoon wijst.

## Hosting: GitHub Pages

De game is een statische site (Blazor WebAssembly), dus een server is niet nodig.

- **Eén Pages-site voor alles.** Eén workflow publiceert het spel op de root en elke spike die we willen testen in een submap (`/spike-8/`). Zo heeft elke playtest een eigen link zonder iets te installeren.
- **`.nojekyll` in de output.** Zonder dat bestand negeert Pages de map `_framework/` van Blazor, en laadt er niets.
- **`<base href>` per map.** De site staat onder `/DeckOverflow/`, niet op `/`. De workflow zet de juiste base bij het publiceren, tenzij er later een eigen domein komt.
- **Geen deep links nodig.** Het spel werkt met querystrings op `/` (`?seed=…`, `?level=…`), dus de gebruikelijke 404-omweg voor SPA's is overbodig.
- **De repo moet publiek zijn** voor gratis Pages.
- **`<script type="importmap"></script>` in `index.html`.** .NET 10 zet een fingerprint in de naam van `dotnet.js`; de import map vertaalt die. Lokaal doet de devserver dat, op Pages laadt zonder die regel niets.
- Pages serveert de Brotli-bestanden van Blazor niet, dus de eerste keer laden is enkele MB zwaarder. Op te lossen met een eigen loader als de laadtijd een probleem blijkt.

## Accounts: eerst gast, later bewaren

1. **Je speelt meteen als gast.** Geen loginscherm vooraf: het spel moet aanvoelen als gewoon een deckbuilder, zie [de onthulling](README.md#de-onthulling). Technisch een anoniem account in Supabase, gebonden aan je browser.
2. **"Bewaar je fabriek"** verschijnt op een natuurlijk moment, na je eerste gewonnen gevecht of bij de onthulling. Je kiest een gebruikersnaam en een wachtwoord; je gastvoortgang gaat mee (Supabase "link identity").
3. **E-mail is optioneel**, alleen om je wachtwoord te herstellen. Zonder e-mail ben je je fabriek kwijt als je je wachtwoord vergeet, en dat zeggen we bij het registreren. Supabase logt in met e-mail, dus een account zonder e-mail krijgt achter de schermen een adres als `gebruikersnaam@users.deckoverflow.invalid`.
4. **De klascode is optioneel.** Wie alleen speelt, ontgrendelt afdelingen door te spelen. Een docent maakt een klas (daarvoor is een geregistreerd account nodig), deelt de code, geeft afdelingen vrij en ziet de voortgang van de klas.

**Privacy.** Een account om voortgang te bewaren steunt op de uitvoering van de dienst, niet op toestemming. Daarom is er geen toestemming van ouders nodig, ook niet onder de 13 jaar (artikel 8 AVG geldt alleen bij verwerking op basis van toestemming). Wat we daarvoor wel beloven:

- We vragen nooit naar een echte naam, leeftijd, school of woonplaats.
- **Gebruikersnamen zijn nooit publiek.** In klassementen staat een gegenereerde bijnaam uit woordenlijsten ("Rusty Bolt 42"). Vrije tekst die anderen zien, bestaat niet.
- **Geen analytics, geen tracking, geen cookies van derden.** Dan is er ook nergens toestemming voor nodig.
- Een privacyverklaring in taal die een kind van 12 begrijpt.
- Je account en alles wat eraan hangt, verwijder je in het spel met één knop.

Bij een brede uitrol laten we dit nalezen door iemand van privacy bij de instelling. Een koppeling met Moodle (LTI) is een latere stap, als instellingen dat vragen.

## Feedback in het spel

Gebouwd op 4 oktober 2026. Een speler kan de maker laten weten wat hij leuk vond, altijd optioneel en zonder beloning: een hartje of "meh" na een gewonnen gevecht (onderwerp `enemy:<key>`), en op het eindscherm van een run een hartje plus een kort bericht (onderwerp `run`, hooguit 500 tekens, met de vraag er geen naam of school in te zetten). De component `World/FeedbackHeart.razor` kan elke afdeling gebruiken; zonder backend toont ze niets.

- **Alleen schrijven.** De tabel `feedback` heeft geen leesregel: spelers, klasgenoten en docenten lezen niets, ook hun eigen rijen niet. Tim leest in het dashboard of met de service-sleutel.
- **Geen tracking.** Er wordt niets ongevraagd bijgehouden: alleen wat de speler zelf aanklikt of typt. De disclaimer op het hoofdscherm ("Over dit spel") zegt dat zo.
- **Lezen**, bv. per vijand: `select subject, count(*) filter (where verdict = 'like') as fun, count(*) filter (where verdict = 'dislike') as meh, count(distinct user_id) as players from feedback group by subject order by subject;` en de berichten met `select created_at, subject, comment from feedback where comment is not null order by created_at desc;`.

## Superuser

Beslist op 4 oktober 2026. De maker speelt als **superuser**, de enige die een gevecht wint met de sneltoets W. Voor de superuser staat alles open wat gebouwd is: ook afdelingen op *binnenkort*, elk level, elke act als startpunt en de plattegrond voor de onthulling. Ook de testroutes `?seed=`, `?fight=` en `?level=` werken alleen voor de superuser; bij een speler doen ze niets. `?all` en `?world` bestaan niet meer.

- **Wie het is, staat alleen in Supabase**, nooit in de (publieke) code: `app_metadata.role = 'superuser'`. Een speler kan zijn `app_metadata` niet zelf aanpassen. Iemand superuser maken: `update auth.users set raw_app_meta_data = raw_app_meta_data || '{"role":"superuser"}' where email = '…';`. De rol komt mee bij de volgende inlog of vernieuwing van de sessie (binnen het uur).
- **Lokaal** (`localhost`) is iedereen superuser, om te ontwikkelen zonder in te loggen.
- **Niets in de voortgang.** Open is niet hetzelfde als verdiend: er komen geen sleutels, Codex-pagina's of panelen bij. Wat je speelt, telt wel gewoon mee.
- **Een controle in de browser** (`Backend/Superuser.cs`, gelezen uit de bewaarde sessie). Wie de app patcht, komt erlangs; daar staat niets tegenover dat dat de moeite waard maakt.

## Data: Supabase

Postgres in de EU-regio (Frankfurt), met de beveiliging per rij (row level security) en databasefuncties van Supabase. Er is geen eigen API-server: de shell praat met `HttpClient` tegen de REST-API. `DeckOverflow.Api` uit de MVP-tabel vervalt daarmee.

| Tabel | Inhoud | Wie leest |
| --- | --- | --- |
| `profiles` | user id, gegenereerde bijnaam, aangemaakt op | jezelf; je bijnaam ook je klas |
| `progress` | user id, onderdeel (bv. `h2.overflow`), toestand (in de zak, uitgepakt, gemonteerd), bijgewerkt op | jezelf, de docent van je klas |
| `unlocks` | user id, afdeling, hoe (baas, vangnet, docent), wanneer | jezelf, de docent van je klas |
| `classes` | code, naam die de docent kiest, eigenaar, vrijgegeven afdelingen | leden en eigenaar |
| `class_members` | klas, user id | leden en eigenaar |
| `scores` | user id, afdeling, dagelijkse seed, commandolijst, geclaimde score, gecontroleerd (ja, nee, nog niet) | je klas (met bijnaam); iedereen alleen als histogram |

- **Offline eerst.** Het spel houdt voortgang ook lokaal bij en synchroniseert als er verbinding is. Een haperend schoolnetwerk mag een run nooit breken.
- **Scores worden opnieuw gespeeld.** De motor is deterministisch, dus een score bestaat uit de seed en de commandolijst. Een nachtelijke GitHub Action speelt elke nieuwe score opnieuw af met de echte C#-motor en zet `gecontroleerd`. Een klassement van vandaag wordt definitief na controle. Zo hergebruiken we de motor zonder C#-server; Supabase Edge Functions draaien TypeScript en kunnen dat niet.
- **Kosten.** Het gratis plan volstaat voor de pilot, maar pauzeert een project na een week zonder activiteit. Voor echt gebruik (ook tijdens vakanties) is het Pro-plan nodig, ongeveer 25 dollar per maand.
