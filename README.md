# Deck Overflow

Een roguelike deckbuilder in de browser waarin de wereld gehoorzaamt aan C#. Het spel is Engelstalig; ontwerp, code-commentaar en documentatie zijn in het Nederlands. Wie de regels doorheeft, wint. Bedoeld voor eerstejaars programmeren en gebaseerd op de leerlijn van *Zie Scherp Scherper*.

Er staat geen code in beeld: types, overflow, integer deling en operatorvoorrang zijn de natuurwetten van het spel. Na een gevecht geeft de Codex het concept zijn naam en linkt naar het hoofdstuk in het boek.

## Waar staan we

| Fase | Wat | Stand |
| --- | --- | --- |
| Spike 1 | Eén gevecht tegen de Byte-Golem: C#-motor in de browser, PixiJS-stage, juice | Afgesloten, zie [spikes/01-byte-golem](spikes/01-byte-golem/) |
| Spike 2 | Types op elke vijand en aanval, Omgieten als kerngereedschap | Gebouwd, nog niet met spelers getest, zie [spikes/02-omgieten](spikes/02-omgieten/) |
| Spike 3 | De hele flow van een act: map, gevechten, beloningen, rustvuur, events, winkel, schat en baas | Gebouwd, nog niet met spelers getest, zie [spikes/03-vatenvallei](spikes/03-vatenvallei/) |
| Spike 4 | De eerste minuten: een starterdeck dat meteen werkt, een openingskeuze, een vroeg wondermoment, en het spel in het Engels | Gebouwd, nog niet met spelers getest, zie [spikes/04-eerste-minuten](spikes/04-eerste-minuten/) |
| MVP-demo | Een speelbare Act 1: De Vatenvallei, met map, beloningen, Codex en een eenvoudig docentdashboard | Nog niet gestart |

**Prioriteit nu:** de core game loop. Eerst moet vechten, een beloning kiezen en de map leuk zijn; geluid, definitieve art en polish komen daarna.

Playtest 1 bevestigde de techniek en dat de golem-ontdekking werkt, maar toonde ook dat één regel per vijand niet schaalt. Daarom rust het spel nu op twee systemen: alles heeft een type, en Omgieten verandert dat type.

## Documenten

- [Game Design Document](docs/game-design-document.md): visie, pijlers, kernsysteem, Act 1, encounters, kaarten, Codex, antipatronen.
- [Spike Design Doc](docs/spike-design-doc.md): architectuur, eventcontract, interop, succescriteria, de weg naar de MVP.
- [Product sheet](docs/product-sheet/product-sheet.html): A4-pitch voor instellingen.
- [Overzicht en bronnen](docs/README.md).

## Indeling

```text
├─ docs/                ontwerpdocumenten
├─ spikes/
│  ├─ 01-byte-golem/    afgesloten spike, eigen .sln, draait los (tag spike-1)
│  ├─ 02-omgieten/      types en Omgieten: Vlottende Geest en Tinnen Kolos
│  ├─ 03-vatenvallei/   een hele act: map, beloningen, rustvuur, events, winkel, baas
│  └─ 04-eerste-minuten/ starterdeck, openingskeuze, de Bottomless Jug, Engelse teksten
└─ .github/workflows/   CI per onderdeel
```

De code van het spel zelf komt later in `src/` en `tests/` op de root. Wat uit een spike de moeite waard is, nemen we daar bewust over in plaats van de spike door te laten groeien.

## Architectuur

![De motor beslist, de stage speelt af](docs/architectuur.svg)

Een C#-regelmotor zonder UI of tijd neemt commands aan en geeft events terug. Een Blazor WebAssembly-shell orkestreert, en een JavaScript-stage (PixiJS, GSAP, Howler) speelt de events af als animatie en geluid. Spike 1 heeft deze opbouw bewezen; details staan in het [Spike Design Doc](docs/spike-design-doc.md#architectuur).
