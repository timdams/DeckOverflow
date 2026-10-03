# Spike 6: de handleiding

In [spike 5](../05-tekenstijlen/) kwamen elf tekenstijlen naast elkaar. De montagehandleiding zonder woorden won. Spike 6 zet het hele spel in die taal: het gevecht, de map, de schermen, de vijanden en de effecten.

Vertrekpunt was een kopie van spike 5. Die blijft ongewijzigd. De motor is niet veranderd: alles gebeurt in de stage en de shell.

## Wat spike 6 moet aantonen

- Leest het spel als één geheel, van titelscherm tot explosietekening?
- Blijven de types in één blik leesbaar nu zij de enige kleur zijn?
- Voelen de effecten zonder pixels even sappig? Vooral de treffer, de overflow van de kruik en de dood van een vijand.
- Helpt de taal van een handleiding bij "eerst ervaren, dan benoemen", of leest ze als uitleg?

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

De seed staat in de URL (`/?seed=255`). Om één vijand snel te proberen: `/?fight=jug` (of `slime`, `knight`, `ghost`, `dripper`, `colossus`, `golem`, `reckoner`).

## De taal

**Zwart op papier, kleur is een type.** De wereld is zwart-wit. Alleen `int` (blauw), `double` (paars) en `byte` (geel) hebben kleur, elk met een eigen vorm. Zie je kleur, dan zie je een type. Damage, heal en block krijgen symbolen in plaats van kleuren. Een vijand die je omgiet, wordt ingekleurd in zijn nieuwe type, alsof iemand er met een markeerstift over gaat.

**Eén letter.** Helvetica of Arial, het klassieke handleidingslettertype. Geen webfonts: die staan op elke computer, ook op een schoolnetwerk. De pixelfonts van de vorige spikes zijn weg.

**Stappen.** Een beurt is een stap: een groot omcirkeld cijfer. Kosten staan in een stapcirkel. De rijen van de map zijn genummerd in de marge.

## Van pixels naar onderdelen

| Moment | Vroeger | Nu |
| --- | --- | --- |
| Treffer | witte flits, pixels | getande inslagster, snelheidslijnen, wegvliegende schroefjes en splinters |
| Afkappen | ".5" valt weg | een schaar knipt langs een stippellijn tussen "2" en ".5" |
| Overflow | flits, pixels | het getal bevriest op een zwart bord, een rondgaande pijl draait één keer, terug naar het begin |
| Blokkeren | cyaan pixels | een pijl ketst af op het schild, met een ✓-stempel |
| Genezen | groene pixels | kleine plusjes die opstijgen |
| Omgieten | kleurvlek | ① smelten, ② gieten; daarna is de figuur ingekleurd |
| Dood | sprite spat uiteen in pixels | **explosietekening**: de tekening valt in stukken uiteen langs gestippelde hulplijnen, met stapnummers |
| Kaart weigert | schudt | schudt, met een ✗-stempel |
| Einde | banner | paneel met het figuurtje dat juicht, of dat tussen de onderdelen de klantendienst belt |

De explosietekening heeft geen extra art nodig: de stage snijdt de tekening van de vijand in een raster (`juice.explode.pieces`) en laat alleen de stukken zien waar iets op staat. Alle getallen staan zoals altijd in `juice.js`.

## Art

Drie nieuwe vellen, gegenereerd met GPT-5.4 Image 2 (via OpenRouter), elk met het manualvel van spike 5 als referentiebeeld zodat de stijl gelijk blijft:

| Vel | Raster | Inhoud |
| --- | --- | --- |
| `characters` | 3×3 | de held (het figuurtje zonder gezicht) en de acht vijanden |
| `icons` | 5×4 | mapknopen, intents, HP, goud, stapels, batterij, juichen, crash, schaar, overflow, eindknop |
| `scenes` | 3×2 | Gieterij, Smeltkroes, Lekkende ton, kampvuur, winkel, titel |
| `manual` | 5×4 | kaarten en relics, overgenomen uit spike 5 |

```bash
python tools/cut_sheets.py
```

Het script maakt het wit rond elke tekening transparant (wit binnen een tekening blijft wit), geeft elk los stuk aan het vakje waar zijn midden ligt, en schrijft naar `wwwroot/art/{items,actors,icons,scenes}/`. Welke id welk plaatje krijgt, staat in [wwwroot/art/art.json](src/DeckOverflow.Web/wwwroot/art/art.json); de shell en de stage lezen dat bestand allebei.

Het stijlmenu van spike 5 is weg: er is nog maar één stijl.

## Open vragen

- Eén regel buigt: de grijze tinten in tekeningen (de arcering van de kaarten) tellen niet als kleur. Klopt dat voor testers?
- Damage en heal hebben geen kleur meer. Is `-6` naast `+40` nog in één blik te onderscheiden, of hebben ze een vorm nodig (een ✗ en een +)?
- De handleidingtaal kan de pijler "geen uitleg vooraf" ondergraven als ze te veel als instructie leest. Waar ligt die grens?
- Geluid is nog dat van de pixelspikes. Past een papieren klankkleur beter (ritselen, schroeven, een klik)?
