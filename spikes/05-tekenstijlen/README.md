# Spike 5: tekenstijlen

Tot spike 4 waren kaarten en relics placeholders: pixelicoontjes uit rechthoeken en relics als losse tekst. Spike 5 zet elf tekenstijlen naast elkaar, met een menu om te kiezen. Het doel is kiezen welke richting de game uitgaat, niet de definitieve art.

Vertrekpunt was een kopie van [spike 4](../04-eerste-minuten/). Die blijft ongewijzigd. Spelregels en motor zijn hetzelfde gebleven.

## Wat spike 5 moet aantonen

- Welke stijl testers kiezen als ze zelf mogen kiezen, en welke ze na een volledige run nog willen.
- Of de typekleuren (int blauw, double paars, byte geel) op een kaart in één blik leesbaar blijven naast de illustratie.
- Of een plaatje helpt om een kaart terug te herkennen, zonder de naam te lezen.

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

Het menu **Art: … ▾** staat rechtsboven op de map en de andere schermen, en rechtsonder tijdens een gevecht. Elke stijl toont er een voorbeeldje. De keuze wordt onthouden in `localStorage`.

## De stijlen

De eerste ronde waren drie gewone gamestijlen. Die bleken te generiek: ze zeggen "een game", niet "deze game". De tweede ronde komt uit het spel zelf.

| Id | Stijl | Waarom voor deze game |
| --- | --- | --- |
| `pixel` | 16-bit RPG-inventaris, teruggebracht naar 64×64 echte pixels | ronde 1, referentie |
| `sticker` | vinylstickers met witte die-cut-rand | ronde 1, referentie |
| `notebook` | inktkrabbels met rood accent, op papier | ronde 1, referentie |
| `clay` | plasticine, gefotografeerd | Omgieten is kneden: dezelfde klei in een andere vorm |
| `tin` | gelithografeerd opwindspeelgoed | de Tin Knight en de Tin Colossus in hun eigen wereld |
| `patent` | 19de-eeuwse octrooitekening, soms in doorsnede | alles is een mechanisme dat aan vaste regels gehoorzaamt |
| `manual` | montagehandleiding zonder woorden | "eerst ervaren, dan benoemen" in beeld |
| `cabinet` | specimen op een kaartje in een rariteitenkabinet | de Codex als verzamelalbum |
| `marginalia` | middeleeuwse kantlijn met beestjes | bugs, letterlijk |
| `cross-stitch` | kruissteek in een borduurring | een steek is een pixel, het stramien een vast aantal plaatsen |
| `teletext` | blokjesmozaïek in 7 kleuren, 2×3 per teken | de machine zelf; berekend, niet getekend |

Elk vel is gegenereerd met GPT-5.4 Image 2 (via OpenRouter) uit dezelfde onderwerpenlijst, in een raster van 5×4. Alleen het stijlblok van de prompt verschilt. De vellen staan in [art/sheets/](art/sheets/). Teletekst heeft geen vel: het script rekent het uit de stickerstijl.

Glitch en vaporwave zitten er bewust niet bij. Die bewaren we voor De slechte patch uit het GDD, zodat die act visueel echt kapot voelt.

## Van vel naar sprite

```bash
python tools/cut_sheets.py
```

Het script maakt de effen achtergrond transparant (alles in de achtergrondkleur dat de rand van het vel raakt), zoekt de losse stukken op het hele vel en geeft elk stuk aan het vakje waar zijn midden ligt. Zo blijft een tekening die over de grens van haar vakje steekt heel. Het schrijft `wwwroot/art/{stijl}/{naam}.png`.

- **Papierstijlen** (`notebook`, `patent`, `manual`): zwarte lijnen op wit verdwijnen op een donkere kaart, dus het papier blijft staan als een kaartje met ronde hoeken.
- **Pixelstijl:** naar 64×64 met 32 kleuren en harde alfa. De browser schaalt met nearest-neighbor op.
- **Teletekst:** de stickersprites gaan naar een raster van 22×15 tekens van elk 2×3 blokjes, met één voorgrondkleur per teken. Kleuren gaan via tint, niet via afstand: hout wordt rood, staal wit.

Een nieuw vel van een stijl vervangt het oude: het script neemt per stijl het nieuwste bestand. Een nieuwe stijl is een regel in `STYLES`, een id in `art.json` en een naam `art.<id>` in `en.json`.

Welke id welk plaatje krijgt, staat in [wwwroot/art/art.json](src/DeckOverflow.Web/wwwroot/art/art.json). Die manifest is de enige koppeling: de shell (`Art/ArtStyle.cs`) en de stage (`stage/art.js`) lezen allebei hetzelfde bestand. Een verbeterde kaart (`strike+`) deelt het plaatje van de gewone, beide Remolds delen één plaatje, Molded Rain gebruikt dat van Molded Strike. Een kaart zonder plaatje valt in de stage terug op het oude pixelicoon.

## Wat er veranderde

- **Stage:** kaarten tonen het plaatje in plaats van het pixelicoon. Relics rechtsboven hebben een icoon. `setArtStyle` wisselt de plaatjes in de hand zonder opnieuw te delen.
- **Menu:** `ArtMenu` toont alle stijlen uit `art.json` met een voorbeeldje.
- **Shell:** kaarttegels (beloning, winkel, deck) tonen het plaatje; relics in de topbalk, als beloning en in de winkel ook. De map toont de kist en het kampvuur als plaatje, het kampvuurscherm en de kist ook.
- **Bridge:** `ArtStyleAsync` en `SetArtStyleAsync`. De stijl is geen spelstatus: de motor weet er niets van.

Vijanden staan nog niet op de vellen; die blijven pixelplaceholders.

## Open vragen

- Het notebookvel gebruikt rood als accent, en rood betekent ook "kaart weigert". Botst dat in een gevecht?
- Klei en tin zijn foto's, de UI is plat en pixelig. Moet de rest van de interface mee met de stijl, of mag de art contrasteren?
- De stijlen met papier (patent, manual) maken van elke kaart een kaartje-op-een-kaart. Leest dat rustig of druk?
