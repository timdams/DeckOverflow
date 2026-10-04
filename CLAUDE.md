# Deck Overflow

Roguelike deckbuilder in de browser waarin de wereld gehoorzaamt aan C#. Doelgroep: eerstejaars programmeren, gebaseerd op *Zie Scherp Scherper*. Deze repo is de projectrepo voor het hele spel: ontwerp, spikes en later de game zelf.

## Modulair: lees alleen wat bij je taak hoort

De docs (en straks de code) zijn opgesplitst in **de wereld** en **één module per afdeling** (factory), zodat je context klein blijft. Lees niet alles; begin bij [docs/README.md](docs/README.md) en kies:

- **Altijd nuttig:** [docs/visie.md](docs/visie.md) (pijlers, antipatronen) en [docs/architectuur.md](docs/architectuur.md) (motor/shell/stage, eventcontract).
- **De wereld** ([docs/wereld/](docs/wereld/README.md)): plattegrond, ontgrendelen, mastery, Prikklok, onthulling, de [Codex](docs/wereld/codex.md), het [✗-register](docs/wereld/x-register.md), de [backend](docs/wereld/backend.md) (auth, klascode, Supabase). Todo's in [docs/wereld/todo.md](docs/wereld/todo.md).
- **Een afdeling** ([docs/afdelingen/](docs/afdelingen/)): elk in een eigen map met `README.md`, `todo.md`, `ideeen.md`, `events.md` en een eigen `CLAUDE.md` met de regels die alleen daar gelden. Nu [The Card Hall](docs/afdelingen/card-hall/README.md), de deckbuilder, [The Control Room](docs/afdelingen/control-room/README.md), regels voor een automaat, en [The Conveyor Belt](docs/afdelingen/conveyor-belt/README.md), band en machines op een rooster (nog niet te ontgrendelen): lees bij werk aan een ervan eerst haar `CLAUDE.md` ([Card Hall](docs/afdelingen/card-hall/CLAUDE.md), [Control Room](docs/afdelingen/control-room/CLAUDE.md), [Conveyor Belt](docs/afdelingen/conveyor-belt/CLAUDE.md)).

Houd het zo: wat voor één afdeling geldt, schrijf je in haar map, niet in de wereld-docs of hier. Een nieuwe afdeling krijgt een nieuwe map; ze deelt alleen via de wereld (Codex, ✗-register, voortgang).

## Prioriteit

Eerst de core game loop: vechten, een beloning kiezen, de map. Geluid, definitieve art en andere polish hebben nu geen prioriteit; placeholders volstaan. Steek er geen tijd in tenzij erom gevraagd wordt, en stel liever vragen over wat de loop leuk maakt.

## Indeling en wat je waar doet

- **`todo.md` per module**: wat bewust is uitgesteld, in [docs/wereld/todo.md](docs/wereld/todo.md) en in de map van elke afdeling. Lees de todo van je module bij de start van een taak, werk eraan als het gevraagd wordt, en vul hem aan als je zelf iets uitstelt of een gat vindt dat je niet meteen dicht. Haal eruit wat af is. De `todo.md` op de root is alleen een wegwijzer.
- **`docs/`**: het ontwerp, modulair (zie hierboven), plus de product sheet. Deze repo is de enige bron; Claude Docs worden niet meer gebruikt of bijgewerkt. Schrijf ontwerpwijzigingen rechtstreeks in het document van de module.
- **`spikes/NN-naam/`**: afgesloten of lopende experimenten, elk met een eigen `.sln`, README en CI-workflow, los draaibaar. Een afgesloten spike verandert niet meer; bouw er niet op verder. Een nieuwe spike krijgt een nieuwe map.
- **`src/` en `tests/` op de root** zijn het echte spel, overgenomen uit spike 7 op 2 oktober 2026. Nieuw werk gebeurt daar. Code uit een spike neem je bewust over, niet door de spikemap te verplaatsen of te laten doorgroeien.
- **`art/sheets/` en `tools/cut_sheets.py`**: de gegenereerde tekenvellen en het script dat ze in losse tekeningen snijdt. Nieuwe art komt van hetzelfde model met het manualvel als referentie.
- **`art/todo.md`**: welke tekeningen nog ontbreken, over alle afdelingen heen, zodat we ze in batches kunnen genereren. Voeg je een vijand, kaart, relic, ✗-paneel, event of plaat toe zonder tekening (of met een reserve), zet hem er meteen in met wat erop moet staan. Haal hem eruit zodra hij uitgesneden en in `art.json` gekoppeld is.
- **`assetsin/`**: bruikbare audio, nu zes Kenney-pakketten (CC0, `.ogg`): digital, impact, interface, rpg, sci-fi en ui. Zoek hier eerst als je een geluid nodig hebt. Het is bronmateriaal: wat het spel gebruikt, kies je in `tools/copy_sfx.py`, dat het als `.ogg` en `.mp3` naar `wwwroot/audio/sfx/` kopieert. Zet elk gebruikt pakket in [CREDITS.md](CREDITS.md).
- Spike 1 staat ook in git onder de tag `spike-1`. Spike 2 (`spikes/02-omgieten/`), spike 3 (`spikes/03-vatenvallei/`, de hele act met map en beloningen) spike 4 (`spikes/04-eerste-minuten/`, starterdeck, openingskeuze, Engelse teksten) spike 5 (`spikes/05-tekenstijlen/`, elf tekenstijlen voor kaarten en relics) spike 6 (`spikes/06-handleiding/`, het hele spel als montagehandleiding) spike 7 (`spikes/07-fabriek/`, kaarten als overtredingen, elites als echte bugs) spike 8 (`spikes/08-controlekamer/`, regels opstellen voor een automaat, de eerste afdeling zonder kaarten) en spike 9 (`spikes/09-lopende-band/`, band en machines op een rooster voor H6) zijn gebouwd maar nog niet getest met spelers; spike 8 en 9 zijn overgenomen in het spel. Ze zijn referentie, geen werkplek meer.

## Commando's

Het spel draai je vanuit de root; een spike vanuit zijn eigen map, met dezelfde commando's:

```bash
dotnet test tests/DeckOverflow.Core.Tests        # wat CI draait: de gedeelde regels
dotnet test tests/DeckOverflow.CardHall.Tests    # en de deckbuilder
dotnet test tests/DeckOverflow.ControlRoom.Tests # en de Controlekamer (/control-room)
dotnet test tests/DeckOverflow.ConveyorBelt.Tests # en de Lopende Band (/conveyor-belt)
dotnet run --project src/DeckOverflow.Web        # /?seed=255, of /?fight=golem; lokaal ben je superuser
python tools/cut_sheets.py                       # tekeningen opnieuw uitsnijden
python tools/copy_sfx.py                         # geluiden opnieuw kopiëren (pip install soundfile lameenc)
```

## Architectuur: wie mag wat

Bewezen in spike 1 en de basis voor het spel. De motor beslist, de stage speelt af. Houd die grens scherp.

- **Motor** kent alle spelregels, één project per afdeling (nu `DeckOverflow.CardHall`, `DeckOverflow.ControlRoom` en `DeckOverflow.ConveyorBelt`) bovenop **`DeckOverflow.Core`** (seeded RNG, getypeerde waarden en C#-regels, teksten, de Codex-catalogus en het contract van het ✗-register). Een afdeling verwijst alleen naar Core, nooit naar een andere afdeling. Pure C#, geen dependencies, geen `DateTime`, `Task.Delay` of `System.Random`. In de Card Hall verandert status alleen via `Combat.Handle(ICommand)` of, voor een hele run, `Run.Handle(ICommand)`; beide geven een lijst `GameEvent`s terug. In de Controlekamer speelt `Duel.Step()` één zet en geeft `DuelEvent`s terug. Op de Lopende Band rekent `Simulator.Run` een bord vooraf uit tot een lijst `Frame`s per testgeval.
- **Shell** (`DeckOverflow.Web`, Blazor WebAssembly) orkestreert en bevat geen regels. De wereld staat in `World/` (plattegrond, Codex, ✗-register, accountpaneel), `Backend/` en `Progress/`; een afdeling in `Features/<Afdeling>/`. Alleen de bruggen in `Interop/` praten met een stage (`StageBridge` voor de Card Hall, `ControlRoomStage` voor de Controlekamer, `ConveyorBeltStage` voor de Lopende Band).
- **Stage** (per afdeling, nu `wwwroot/card-hall/stage/`, `wwwroot/control-room/stage/` en `wwwroot/conveyor-belt/stage/`, PixiJS + GSAP + Howler) zet events om in animatie en geluid. Geen spelregels, en ze leest nooit zelf de spelstatus. Na elke `play` volgt een `sync` met de snapshot als waarheid.
- **Typeregels zijn echt .NET-gedrag**, geen nabootsing: `ByteRules` gebruikt `unchecked((byte)…)`, `IntRules` een gewone cast. Een nieuw type krijgt een eigen `XxxRules`-klasse.
- **Elke regel die iets bijzonders doet, meldt het met een eigen event.** Dat is de haak voor juice en de Codex.
- **Events zijn klein en plat**: ids en getallen, polymorf geserialiseerd met `[JsonDerivedType]`. Een nieuw event komt ook in de `events.md` van zijn afdeling en in de handlers van de stage. De stage negeert onbekende types.
- **Determinisme:** een eigen RNG met seed (PCG32), vastgepind met een test op de eerste getallen. Faalt die, dan veranderen alle bestaande seeds; pas de verwachte waarden niet zomaar aan.
- **Geen CDN:** libraries en fonts staan in `wwwroot/lib` en `wwwroot/fonts`, zodat het spel ook op een schoolnetwerk of GitHub Pages draait.
- Juice-getallen staan in `juice.js`. Stel de feel daar af, niet in de handlers.

## Ontwerpregels die code raken, in elke afdeling

Uit [docs/visie.md](docs/visie.md). Een wijziging die hiertegen ingaat, eerst voorleggen. Regels die alleen voor één afdeling gelden, staan in haar eigen `CLAUDE.md`.

- **C# is natuurkunde.** Regels zijn altijd consistent en buigen nooit voor de les of voor de speler: als `int` afkapt, kapt `int` altijd af, in elke afdeling.
- **Eerst ervaren, dan benoemen.** Geen tutorialschermen of uitleg vooraf. De echte naam van een concept verschijnt pas in de Codex.
- **Geen gamification:** geen vragen als poort, geen punten of badges voor leren, geen echt geld of FOMO-timers.
- **Compilefout of exception:** een ongeldige zet weigert. Alleen runtimefouten ontploffen.
- Elk gevecht of elke puzzel moet op meerdere manieren te winnen zijn. Begrijpen maakt het efficiënter, niet verplicht.
- **De docent maakt alleen een klascode** (voor het klassement). Een afdeling ontgrendel je zelf.

## Conventies

- Taal: het spel is Nederlands met Engels als optie (taalknop), de ontwikkeling Nederlands. Spelteksten staan alleen in `wwwroot/text/nl.json` en `en.json`, nooit in de code; de motor geeft sleutels en getallen (`TextRef`). Comments, XML-docs, testnamen (`PadB_herstel_op_golem_laat_byte_overlopen_en_wint`) en docs zijn Nederlands. Identifiers en ids (`floating-strike`, `jug`) zijn Engels.
- Een nieuwe kaart, vijand, relic of event krijgt zijn tekst in `nl.json` én `en.json`, met dezelfde plaatshouders; `StringsTests` faalt anders. Het Nederlands volgt de woorden en de toon van *Zie Scherp Scherper*: zie de [woordenlijst](docs/wereld/woordenlijst.md). Een nieuwe relic is een klasse met haken (`Relic`), geen `if` in `Combat` of `Run`.
- .NET 10, nullable aan, `sealed record` voor commands, events en snapshots.
- Nieuwe regels en scenario's krijgen een xUnit-test.
- Elke wijziging aan het spel (een vijand, kaart, relic, regel of getal, in welke afdeling ook) toets je af aan de Codex (`CodexCatalog`, `codex.*` in `nl.json` en `en.json`) en de ✗-panelen (`XRegister`, `xpanel.*`): kloppen de tekst, de code en de getallen nog, en kan het moment of paneel nog gebeuren? Pas ze mee aan, en meld in je samenvatting wat je controleerde, ook als er niets hoefde te veranderen.
