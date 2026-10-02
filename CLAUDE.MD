# Deck Overflow

Roguelike deckbuilder in de browser waarin de wereld gehoorzaamt aan C#. Doelgroep: eerstejaars programmeren, gebaseerd op *Zie Scherp Scherper*. Deze repo is de spike: één gevecht tegen de Byte-Golem.

Lees eerst wat je nodig hebt:

- [docs/spike-design-doc.md](docs/spike-design-doc.md): architectuur, eventcontract, interop, scope spike 2.
- [docs/game-design-document.md](docs/game-design-document.md): pijlers, kernsysteem, vijanden, kaarten, antipatronen.
- [README.md](README.md): starten, bediening, structuur en keuzes tijdens de spike.

## Commando's

```bash
dotnet test tests/DeckOverflow.Engine.Tests      # wat CI draait
dotnet run --project src/DeckOverflow.Web        # /?seed=255 kiest een seed
dotnet publish src/DeckOverflow.Web -c Release   # downloadgrootte meten
```

## Architectuur: wie mag wat

De motor beslist, de stage speelt af. Houd die grens scherp.

- **`DeckOverflow.Engine`** kent alle spelregels. Pure C#, geen dependencies, geen `DateTime`, `Task.Delay` of `System.Random`. Status verandert alleen via `Combat.Handle(ICommand)`, dat een lijst `GameEvent`s teruggeeft.
- **`DeckOverflow.Web`** is een dunne shell: orkestratie, geen regels. Alleen `Interop/StageBridge.cs` praat met JavaScript.
- **`wwwroot/stage/`** zet events om in animatie en geluid. Geen spelregels, en de stage leest nooit zelf de spelstatus. Na elke `play` volgt een `sync` met de snapshot als waarheid.
- **Typeregels zijn echt .NET-gedrag**, geen nabootsing: `ByteRules` gebruikt `unchecked((byte)…)`, `IntRules` een gewone cast. Een nieuw type krijgt een eigen `XxxRules`-klasse.
- **Elke regel die iets bijzonders doet, meldt het met een eigen event.** Dat is de haak voor juice en later de Codex.
- **Events zijn klein en plat**: ids en getallen. Een nieuw event voeg je toe aan `GameEvent.cs` met `[JsonDerivedType]`, aan de tabel in het Spike Design Doc en aan de handlers in `timeline.js`. De stage negeert onbekende types, dus een event zonder handler breekt niets.
- **Determinisme:** `SeededRng` (PCG32) is vastgepind in `SeededRngTests.Reeks_is_vastgepind`. Faalt die test, dan veranderen alle bestaande seeds. Pas de verwachte waarden niet zomaar aan.
- **Geen CDN:** PixiJS, GSAP, Howler en de fonts staan in `wwwroot/lib` en `wwwroot/fonts`. Voeg nieuwe libraries daar ook toe.
- Juice-getallen staan in `juice.js`. Stel de feel daar af, niet in de handlers.

## Ontwerpregels die code raken

Uit het GDD. Een wijziging die hiertegen ingaat, eerst voorleggen.

- **C# is natuurkunde.** Regels zijn altijd consistent en buigen nooit voor de les of voor de speler: als `int` afkapt, kapt `int` altijd af.
- **Geen code in beeld in Act 1.** Kaarten tonen getallen, letters en woorden. Echte C# verschijnt pas in de Codex.
- **Eerst ervaren, dan benoemen.** Geen tutorialschermen of uitleg vooraf. In de game heet casting "Omgieten"; het woord "cast" verschijnt pas in de Codex.
- **Geen gamification:** geen vragen als poort, geen punten of badges, geen echt geld of FOMO-timers.
- **Compilefout of exception:** een kaart op een ongeldig doelwit weigert (de stage vangt dat af). Alleen runtimefouten ontploffen.
- Elk gevecht moet op meerdere manieren te winnen zijn. Begrijpen maakt het efficiënter, niet verplicht.

## Conventies

- Taal: Nederlands voor spelteksten, comments, XML-docs en testnamen (`PadB_herstel_op_golem_laat_byte_overlopen_en_wint`). Identifiers in code blijven Engels.
- .NET 10, nullable aan, `sealed record` voor commands, events en snapshots.
- Nieuwe regels en scenario's krijgen een xUnit-test in `tests/DeckOverflow.Engine.Tests`. Gebruik `TestHelpers` voor de standaardopzet.

## Documentatie

`docs/` bevat kopieën van gedeelde Claude Docs (links in [docs/README.md](docs/README.md)). Neem je een ontwerpkeuze tijdens het bouwen, noteer die dan in "Keuzes tijdens de spike" in de README en meld dat ze ook in het Spike Design Doc moet.
