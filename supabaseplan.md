# Supabaseplan

Werkplan om de backend van Deck Overflow op te zetten. Het ontwerp staat in het [Spike Design Doc](docs/spike-design-doc.md#hosting-accounts-en-data); dit bestand is de uitvoering ervan, stap voor stap. Opgesteld op 3 oktober 2026.

## Beslist

- **Gratis plan** voor de pilot. Een gratis project pauzeert na een week zonder activiteit; de nachtelijke controle van scores (stap 7) houdt het project wakker. Pro (ongeveer 25 dollar per maand) pas bij echt gebruik.
- **Regio Frankfurt** (`eu-central-1`).
- **Geen eigen API-server.** De shell praat met `HttpClient` tegen de REST- en Auth-API van Supabase. Geen C#-library van Supabase.
- **De motor blijft buiten de backend.** Alles wat met Supabase praat, zit in de shell.
- **De repo blijft de enige bron.** Elke schemawijziging is een migratie in `supabase/migrations/`, ook als ze via de connector wordt uitgevoerd.
- **Woordenlijsten voor bijnamen:** Claude stelt ze op (zie onderaan).

## Stap 0: voorwerk zonder Supabase ✅

Af op 3 oktober 2026.

1. **Lokale voortgang.** `IProgressStore` in `src/DeckOverflow.Web/Progress/`, met `LocalProgressStore` op `localStorage` (één sleutel `deckoverflow.progress`, één JSON-blok `PlayerProgress`). De losse sleutels van vroeger worden bij de eerste start overgenomen. Bewaart: Codex-pagina's en leesstand, ✗-panelen, ontgrendelde afdelingen (met hoe en wanneer), gemonteerde onderdelen, startpunt, aantal runs, intro en onthulling. Een onderdeel is een Codex-pagina: uitgepakt = pagina open, dus geen aparte lijst; `Assembled` blijft leeg tot het GDD vastlegt wanneer iets gemonteerd is. `Departments.IsOpen` leest nu de ontgrendelingen, klaar voor `how = teacher`.
2. **Commando's serialiseerbaar.** `ICommand` heeft `[JsonDerivedType]` zoals `GameEvent`; `Run.Replay(seed, commands, setup)` speelt een opname opnieuw af. `ReplayTests`: een bot speelt een run, de commandolijst gaat door JSON, het opnieuw afspelen geeft exact dezelfde snapshot. Een score bestaat nog niet in de motor; die test komt erbij zodra de Prikklok een score definieert.
3. **Dagelijkse seed.** `DailySeed.For(DateOnly)` in de motor: FNV-1a 64 bit over `yyyy-MM-dd`. De shell geeft de UTC-datum mee, de motor kent geen klok. Vastgepind in `ReplayTests`.

Nog open voor de sync: de Codex-getallen (de waarden van het eerste moment) en de leesstand staan niet in het schema van stap 3. Op een tweede toestel krijg je de pagina's dus zonder je eigen getallen, tenzij `progress` een kolom `values jsonb` krijgt.

## Stap 1: project aanmaken (connector)

- Organisatie opvragen, project `deck-overflow` in `eu-central-1`, gratis plan. Kosten bevestigen met Tim.
- URL en publieke (anon/publishable) sleutel noteren. Die sleutel mag publiek; row level security beschermt de data.

**Gedaan op 3 oktober 2026.** Project `deck-overflow` (ref `bcrpupqzwmauqowtygae`), `eu-central-1`, gratis plan, Postgres 17.

- URL: `https://bcrpupqzwmauqowtygae.supabase.co`
- Publishable key: `sb_publishable_7O_mrhrwHyUrurdYaBsYug_yu99XI63` (de nieuwe vorm; de legacy anon-JWT bestaat ook maar gebruiken we niet)
- Het databasewachtwoord en de service-sleutel staan nergens in de repo.

## Stap 2: Auth instellen

- **Anonieme logins aan.**
- **E-mailbevestiging uit**, anders werken de adressen `gebruikersnaam@users.deckoverflow.invalid` niet.
- Site-URL `https://timdams.github.io/DeckOverflow/`, redirects ook voor `http://localhost:*`.
- Rate limits voor anonieme logins nakijken (open vraag captcha, zie onder).
- Wachtwoord minimum 8 tekens.

**Gedaan op 3 oktober 2026** in het dashboard. De limiet op anonieme logins stond op 30 per uur per IP-adres. Een school zit achter één IP, dus een tweede klas die in hetzelfde uur start, kreeg geen gastaccount meer. Voorstel: 150 per uur, nog geen captcha. Weigert Supabase toch, dan speelt de client lokaal verder en probeert later opnieuw (stap 5).

## Stap 3: schema (migraties)

Alle tabellen in `public`, row level security aan op elke tabel, `on delete cascade` vanaf `auth.users`.

| Tabel | Kolommen | Lezen | Schrijven |
| --- | --- | --- | --- |
| `profiles` | `user_id` pk → auth.users, `nickname` uniek, `created_at` | jezelf; leden van je klas | alleen via trigger |
| `progress` | `user_id`, `item` (bv. `h2.overflow`), `state` enum (`in_bag`, `unpacked`, `assembled`), `updated_at`; pk (`user_id`, `item`) | jezelf, docent van je klas | jezelf (upsert) |
| `unlocks` | `user_id`, `department`, `how` enum (`boss`, `safety_net`, `teacher`), `unlocked_at`; pk (`user_id`, `department`) | jezelf, docent van je klas | jezelf (insert) |
| `classes` | `id` uuid, `code` uniek, `name`, `owner_id`, `released_departments text[]`, `created_at` | leden en eigenaar | eigenaar (update van `name`, `released_departments`) |
| `class_members` | `class_id`, `user_id`; pk beide | leden van dezelfde klas, eigenaar | via `join_class`, verlaten mag zelf |
| `scores` | `id`, `user_id`, `department`, `seed_date date`, `commands jsonb`, `claimed_score int`, `verified` enum (`pending`, `ok`, `rejected`), `created_at` | je klas (met bijnaam) | jezelf (insert, `verified` altijd `pending`) |

Synchroniseren: `updated_at` wint, maar een toestand gaat nooit terug (`assembled` blijft `assembled`). Dat regelt een `check` of trigger in de database, niet de client.

### Functies (alle `security definer`, `search_path` vast)

- `handle_new_user()`: trigger op `auth.users`, maakt een profiel met een bijnaam: bijvoeglijk naamwoord + zelfstandig naamwoord + getal 10–99, opnieuw bij een botsing.
- `is_teacher_of(student uuid)` en `shares_class(other uuid)`: hulpfuncties voor de RLS-policies, zodat policies niet recursief over `class_members` lopen.
- `create_class(name text) returns code`: alleen voor geregistreerde accounts (`(auth.jwt()->>'is_anonymous')::bool = false`). Code van 6 tekens zonder verwarrende letters (geen 0/O, 1/I/L).
- `join_class(code text)`: voegt jezelf toe aan de klas.
- `release_department(class_id, department)`: alleen de eigenaar. De client leest `released_departments` en schrijft zelf een `unlocks`-rij met `how = teacher`.
- `score_histogram(department, seed_date)`: emmers met aantallen, voor iedereen leesbaar, zonder namen.
- `delete_my_account()`: verwijdert `auth.users` voor `auth.uid()`; de rest volgt via cascade.

Na de migraties: de security advisors van Supabase draaien en alles oplossen.

**Stand op 3 oktober 2026:** de migraties staan in `supabase/migrations/`, met dezelfde versienummers als op het project. `schema`, `nicknames` en `functions` zijn uitgevoerd via de connector. `delete_account` is met de hand uitgevoerd in de SQL-editor (de connector weigert een functie die uit `auth.users` verwijdert) en staat dus niet in de migratiegeschiedenis van Supabase. De security advisors melden twee soorten waarschuwingen die bij het ontwerp horen: security-definer-functies die aangemelde spelers mogen uitvoeren (dat is de API, elke functie controleert zelf wie belt) en policies die ook voor gasten gelden (gasten zijn de gewone spelers). Getest op een lokale Postgres 18 met een nagebootste `auth`: een gast kan geen klas maken, klasgenoten zien elkaars bijnaam en scores maar niet elkaars voortgang, de docent ziet de voortgang van de klas, een buitenstaander ziet niets, een toestand gaat nooit terug, `verified` en een seed van morgen kan de client niet schrijven, een unlock met `how = teacher` kan pas na de vrijgave, en `delete_my_account` ruimt alles op via cascade. Afwijkingen van de tabel hierboven:

- `progress` heeft een kolom `values jsonb`: de getallen van het eerste moment op de Codex-pagina. Ze blijven staan zoals ook de toestand nooit teruggaat.
- De hulpfuncties voor de policies en de trigger staan in een schema `private`, buiten de REST-API.
- Rechten per kolom: een eigenaar kan alleen `name` en `released_departments` van een klas wijzigen, een speler bij een score alleen `department`, `seed_date`, `commands` en `claimed_score` invullen.
- Een score kan alleen voor vandaag of gisteren (UTC), voor een run die over middernacht loopt.
- Zonder sessie (rol `anon`) kan je niets, ook het histogram niet: elke speler heeft minstens een gastsessie.

## Stap 4: inloggen met een gebruikersnaam of e-mail

Beslist op 3 oktober 2026: geen Edge Function, geen tabel `accounts`. Supabase logt in met een e-mailadres, en het spel heeft één veld "gebruikersnaam of e-mail":

- **Met een `@`:** een echt e-mailadres. Wie dat opgeeft, logt er ook mee in, en wachtwoordherstel werkt.
- **Zonder `@`:** een gebruikersnaam. De client maakt er `gebruikersnaam@users.deckoverflow.invalid` van, zowel bij het registreren als bij het inloggen. Kleine letters, cijfers, `-` en `_`, 3 tot 20 tekens. Een vergeten wachtwoord is dan niet te herstellen; dat zegt het registratiescherm.

Zo hoeft een gebruikersnaam nooit naar een e-mailadres opgezocht te worden, en blijft de backend zonder TypeScript. Uniek is de naam vanzelf: Supabase aanvaardt elk auth-adres maar één keer. Andere spelers zien de gebruikersnaam nooit, alleen de bijnaam.

Een gastaccount omzetten ("Bewaar je fabriek"): `PUT /auth/v1/user` met adres en wachtwoord op de anonieme sessie. Getest op het project: het gaat meteen door zonder bevestiging, de `user_id` en dus alle voortgang blijft, `is_anonymous` wordt onwaar, en daarna inloggen met adres en wachtwoord werkt. Supabase aanvaardt het domein `.invalid`.

## Stap 5: client in de shell

- `wwwroot/appsettings.json`: `Supabase:Url` en `Supabase:PublishableKey`.
- `SupabaseClient` (dun, `HttpClient`): anoniem aanmelden, sessie vernieuwen, upsert, select, rpc. De sessie in `localStorage`.
- `SyncedProgressStore`: schrijft altijd eerst lokaal, synchroniseert op de achtergrond, faalt stil zonder netwerk. Een haperend schoolnetwerk breekt nooit een run.
- Bij de eerste start: anoniem aanmelden op de achtergrond, nooit een scherm ervoor.

**Gedaan op 3 oktober 2026.** `Backend/SupabaseClient.cs` en `Progress/SyncedProgressStore.cs`. Zonder `Supabase`-sectie in `appsettings.json` valt de shell terug op `LocalProgressStore`.

- Gesynchroniseerd: Codex-pagina's met hun getallen (`progress`), gemonteerde onderdelen, ontgrendelde afdelingen (`unlocks`). Lokaal blijven: leesstand, ✗-panelen, startpunt, aantal runs, intro en onthulling.
- Binnenhalen gebeurt één keer per start en is een unie: niets gaat terug. Daarna stuurt elke keer bewaren alleen wat nog niet in de database staat.
- Een sessie die Supabase niet meer kent, wordt een nieuw gastaccount en de lokale voortgang gaat mee. Zonder netwerk blijft de oude sessie staan.
- In de browser getest tegen het project: gastaccount en bijnaam, push, binnenhalen na het wissen van de lokale voortgang, en `delete_my_account` (alles weg via cascade).
- Captchabescherming stond standaard aan op het project en blokkeerde de gastaccounts; uitgezet in Authentication → Attack Protection.
- Let op: ook `localhost` praat met het echte project. Een tweede project voor ontwikkeling kan later, als het gratis plan het toelaat.

## Stap 6: schermen

- "Bewaar je fabriek" na het eerste gewonnen gevecht of bij de onthulling: gebruikersnaam, wachtwoord, optioneel e-mail, met de waarschuwing dat je zonder e-mail je fabriek kwijt bent als je je wachtwoord vergeet.
- Inloggen op een ander toestel.
- Klascode invoeren; een klas maken (geregistreerd).
- De Prikklok aanzetten: dagelijkse seed en klasklassement met bijnamen, histogram voor iedereen.
- Account verwijderen met één knop.
- Alle teksten in `en.json`.

**Gedaan op 3 oktober 2026, behalve de Prikklok** (die wacht op een score in de motor, zie [todo.md](todo.md)). Eén paneel "Your factory" (`Components/AccountPanel.razor`, logica in `Backend/Account.cs`), te openen vanaf het titelscherm, de plattegrond en de onthulling.

- **Bewaren** verschijnt bij de onthulling, als knop naast "Look around", en blijft daarna op de plattegrond staan. Niet na het eerste gewonnen gevecht: dan is er nog weinig te verliezen, en het zou de eerste minuten onderbreken.
- **Inloggen** op een ander toestel: wat je daar als gast deed, gaat mee naar je fabriek, en dat gastaccount wordt opgeruimd.
- **Afmelden** vergeet alles op dit toestel. Zo erft wie na jou op een gedeelde schoolcomputer speelt, niets.
- **Klassen:** een code invoeren kan als gast; een klas maken alleen met een account. De eigenaar ziet de code en het aantal spelers, en geeft afdelingen vrij. Een leerling krijgt een vrijgegeven afdeling bij de volgende synchronisatie, en daarmee ook de plattegrond.
- **Verwijderen** in twee klikken, zonder browserdialoog.
- In de browser getest tegen het project: bewaren als `testdocent`, een klas maken en de Controlekamer vrijgeven, afmelden, als nieuwe gast aansluiten met de code in kleine letters (Controlekamer open, plattegrond zichtbaar), verwijderen, inloggen met een fout en een juist wachtwoord, en een gast die na het inloggen opgeruimd wordt.

## Stap 7: scores controleren

- Consoleproject `tools/DeckOverflow.ScoreCheck`: haalt scores met `verified = pending`, speelt ze opnieuw af met de motor, zet `ok` of `rejected`.
- GitHub Action, elke nacht. De service-sleutel als GitHub-secret, nooit in de repo.
- Houdt meteen het gratis project wakker.

## Stap 8: docs bijwerken

- Spike Design Doc: tabellen, functies, en inloggen met gebruikersnaam of e-mail zonder Edge Function.
- GDD: de Prikklok.
- Privacyverklaring in eenvoudige taal.

## Open vragen

- **Klasnaam is vrije tekst die leerlingen zien.** Dat botst met "vrije tekst die anderen zien, bestaat niet". Voorstel: de docent typt ze, alleen leden zien ze, en dat is aanvaardbaar omdat een docent geregistreerd is. Of: de naam ook genereren.
- **Captcha** tegen misbruik van gastaccounts: voorlopig niet. De limiet staat per IP (standaard 30 per uur, voorstel 150); opnieuw bekijken als er misbruik opduikt.
- **Mag een speler in meer dan één klas?** Het schema laat het toe; de schermen hoeven het niet.

## Woordenlijsten voor bijnamen

Thema fabriek en handleiding, Engels, neutraal. Vormen als "Rusty Bolt 42". 50 × 50 × 90 = 225.000 combinaties.

Bewust weggelaten omdat ze in slang of in combinatie ongelukkig uitvallen: *nut, screw, tool, knob, cock, nipple, hose, jug, crank, pipe, ball, rubber, hot, wet, loose, hard, stiff, fat, slow, dumb, crazy, broken*.

**Bijvoeglijke naamwoorden (50)**

Rusty, Shiny, Brass, Copper, Steel, Silver, Golden, Humming, Ticking, Whirring, Clicking, Buzzing, Spinning, Rolling, Sturdy, Steady, Nimble, Swift, Quiet, Patient, Clever, Careful, Bright, Polished, Oiled, Tidy, Trusty, Lucky, Sparky, Bouncy, Cosy, Gentle, Brave, Bold, Calm, Eager, Merry, Jolly, Curious, Tiny, Mighty, Little, Square, Round, Striped, Dotted, Folded, Paper, Iron, Cobalt

**Zelfstandige naamwoorden (50)**

Bolt, Gear, Cog, Sprocket, Spring, Lever, Pulley, Wrench, Hammer, Spanner, Rivet, Washer, Hinge, Valve, Gauge, Dial, Switch, Fuse, Magnet, Piston, Spindle, Wheel, Axle, Anvil, Kettle, Ladle, Funnel, Crate, Pallet, Conveyor, Robot, Engine, Turbine, Boiler, Chimney, Whistle, Clock, Compass, Ruler, Pencil, Stapler, Lantern, Ladder, Bucket, Barrel, Teapot, Byte, Widget, Gadget, Gizmo
