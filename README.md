# Deck Overflow

Een roguelike deckbuilder in de browser waarin de wereld gehoorzaamt aan C#. Het spel is Engelstalig; ontwerp, code-commentaar en documentatie zijn in het Nederlands. Wie de regels doorheeft, wint. Bedoeld voor eerstejaars programmeren en gebaseerd op de leerlijn van *Zie Scherp Scherper*.

Er staat geen code in beeld: types, overflow, integer deling en operatorvoorrang zijn de natuurwetten van het spel. Na een gevecht geeft de Codex het concept zijn naam en linkt naar het hoofdstuk in het boek.

**Spelen:** [timdams.github.io/DeckOverflow](https://timdams.github.io/DeckOverflow/) (het spel) en [/spike-8/](https://timdams.github.io/DeckOverflow/spike-8/) (de Controlekamer). Elke push naar `main` publiceert opnieuw via [pages.yml](.github/workflows/pages.yml).

## Waar staan we

| Fase | Wat | Stand |
| --- | --- | --- |
| Spike 1 | Eén gevecht tegen de Byte-Golem: C#-motor in de browser, PixiJS-stage, juice | Afgesloten, zie [spikes/01-byte-golem](spikes/01-byte-golem/) |
| Spike 2 | Types op elke vijand en aanval, Omgieten als kerngereedschap | Gebouwd, nog niet met spelers getest, zie [spikes/02-omgieten](spikes/02-omgieten/) |
| Spike 3 | De hele flow van een act: map, gevechten, beloningen, rustvuur, events, winkel, schat en baas | Gebouwd, nog niet met spelers getest, zie [spikes/03-vatenvallei](spikes/03-vatenvallei/) |
| Spike 4 | De eerste minuten: een starterdeck dat meteen werkt, een openingskeuze, een vroeg wondermoment, en het spel in het Engels | Gebouwd, nog niet met spelers getest, zie [spikes/04-eerste-minuten](spikes/04-eerste-minuten/) |
| Spike 5 | Tekenstijlen: elf stijlen voor kaarten en relics (van klei en tinnen speelgoed tot teletekst), te kiezen via een menu | Gebouwd, nog niet met spelers getest, zie [spikes/05-tekenstijlen](spikes/05-tekenstijlen/) |
| Spike 6 | De handleiding: het hele spel als montagehandleiding zonder woorden. Zwart op papier, kleur alleen voor types, een explosietekening als een vijand sterft | Gebouwd, nog niet met spelers getest, zie [spikes/06-handleiding](spikes/06-handleiding/) |
| Spike 7 | Niet volgens de handleiding: een eigen thema. Kaarten zijn overtredingen van de handleiding, elites zijn echte bugs (Level 256, Flight 501) met hun verhaal in de Codex | Gebouwd, nog niet met spelers getest, zie [spikes/07-fabriek](spikes/07-fabriek/) |
| Spike 8 | De Controlekamer (H5): regels opstellen voor een automaat en toekijken. De eerste afdeling die geen deckbuilder is, met vijf gevechten en goto fail als elite | Gebouwd, nog niet met spelers getest, zie [spikes/08-controlekamer](spikes/08-controlekamer/) |
| MVP-demo | Een afgewerkte Act 1 (H2 en H3): runs over acts met startpunten, de patch, Codex-pagina's en een lichte backend zonder accounts | Gestart: `src/` en `tests/` op de root, overgenomen uit spike 7. Het plan staat in het [Spike Design Doc](docs/spike-design-doc.md#van-spikes-naar-src) |

**Prioriteit nu:** de core game loop. Eerst moet vechten, een beloning kiezen en de map leuk zijn; geluid, definitieve art en polish komen daarna.

Playtest 1 bevestigde de techniek en dat de golem-ontdekking werkt, maar toonde ook dat één regel per vijand niet schaalt. Daarom rust het spel nu op twee systemen: alles heeft een type, en Omgieten verandert dat type.

## Documenten

- [Game Design Document](docs/game-design-document.md): visie, pijlers, kernsysteem, Act 1, encounters, kaarten, Codex, antipatronen.
- [Spike Design Doc](docs/spike-design-doc.md): architectuur, eventcontract, interop, succescriteria, de weg naar de MVP.
- [Product sheet](docs/product-sheet/product-sheet.html): A4-pitch voor instellingen.
- [Overzicht en bronnen](docs/README.md).

## Starten

Vereist: .NET 10 SDK. Vanuit de root van de repo:

```bash
dotnet test tests/DeckOverflow.Engine.Tests      # wat CI draait
dotnet run --project src/DeckOverflow.Web        # /?seed=255, of /?fight=golem voor één gevecht
python tools/cut_sheets.py                       # tekeningen opnieuw uitsnijden uit art/sheets/
```

## Indeling

```text
├─ src/
│  ├─ DeckOverflow.Engine/  de regelmotor: pure C#, geen dependencies
│  └─ DeckOverflow.Web/     Blazor-shell en PixiJS-stage
├─ tests/                   xUnit-tests van de motor
├─ art/sheets/              de gegenereerde tekenvellen
├─ tools/                   cut_sheets.py: van vel naar losse tekeningen
├─ docs/                ontwerpdocumenten
├─ spikes/
│  ├─ 01-byte-golem/    afgesloten spike, eigen .sln, draait los (tag spike-1)
│  ├─ 02-omgieten/      types en Omgieten: Vlottende Geest en Tinnen Kolos
│  ├─ 03-vatenvallei/   een hele act: map, beloningen, rustvuur, events, winkel, baas
│  ├─ 04-eerste-minuten/ starterdeck, openingskeuze, de Bottomless Jug, Engelse teksten
│  ├─ 05-tekenstijlen/  elf tekenstijlen voor kaarten en relics, stijlmenu
│  ├─ 06-handleiding/   het hele spel als montagehandleiding
│  ├─ 07-fabriek/       niet volgens de handleiding: overtredingen en echte bugs
│  └─ 08-controlekamer/ regels voor een automaat (H5): de eerste afdeling zonder kaarten
└─ .github/workflows/   CI per onderdeel: game.yml voor het spel, spike-N.yml per spike
```

Het spel zelf staat in `src/` en `tests/` op de root, overgenomen uit spike 7. De spikes blijven als referentie staan en veranderen niet meer.

## Architectuur

![De motor beslist, de stage speelt af](docs/architectuur.svg)

Een C#-regelmotor zonder UI of tijd neemt commands aan en geeft events terug. Een Blazor WebAssembly-shell orkestreert, en een JavaScript-stage (PixiJS, GSAP, Howler) speelt de events af als animatie en geluid. Spike 1 heeft deze opbouw bewezen; details staan in het [Spike Design Doc](docs/spike-design-doc.md#architectuur).
