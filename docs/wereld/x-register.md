# Het ✗-register: achievements

De achievements van de hele fabriek. Het register hoort bij de wereld, niet bij één afdeling: elke afdeling levert haar eigen panelen aan, en de wereld bewaart, toont en laat ze opspringen.

## Wat een paneel is

- De achievements zijn ✗-panelen uit de handleiding: dingen die de handleiding verbiedt en die jij toch deed. Elke afdeling heeft een eigen pagina.
- **Alleen spelprestaties, nooit leerprestaties:** geen "lees tien Codex-pagina's" (zie [Antipatronen](../visie.md#antipatronen)). Een deel is verborgen, zodat geruchten zich op de speelplaats verspreiden.
- Een gewoon paneel dat je nog niet hebt, toont wat verboden is, als hint; een verborgen paneel blijft een silhouet.
- Een verdiend paneel springt van onderen op in een oranje kader (#e2790a) met zijn tekening.
- **Do not read the manual** is het enige paneel dat met de Codex te maken heeft: wie een pagina tot het einde leest, overtreedt de laatste regel van een game die draait om de handleiding niet volgen. Het is één verborgen paneel, geen teller per pagina, dus de Codex wordt geen vinkjeslijst. Het hoort bij de wereld, niet bij een afdeling.
- Voorbeelden voor latere afdelingen: een baas laten overlopen tot hij sterft, de Controlekamer winnen met één regel, de band duizend keer laten draaien zonder vast te lopen.

## Het systeem: hoe een afdeling panelen levert

Beslist op 3 oktober 2026: het register is één systeem dat elke afdeling gebruikt. Een afdeling hoeft alleen te zeggen *welke* panelen ze heeft en *wanneer* je er een verdient; de rest doet de wereld.

| Wie | Doet wat |
| --- | --- |
| **Afdeling** (in haar eigen motor) | Een lijst panelen (sleutel, verborgen of niet) en een herkenner: uit de events van een stap volgt welke panelen verdiend zijn. Emitteert `XPanelEarned(key)`, één keer per run of puzzel. |
| **Wereld** (shell) | Bewaart de verdiende panelen over runs en afdelingen heen (`PlayerProgress.XPanels`, later in Supabase). Toont het register met een pagina per afdeling, en de popup. Herkent zelf de panelen die bij geen afdeling horen (Do not read the manual). |
| **Teksten en art** | `xpanel.<key>.name` en `xpanel.<key>.text` in `en.json`; een tekening onder `panels/` in `art.json`. |

**Sleutels zijn uniek over de hele fabriek**, zodat de wereld ze in één verzameling kan bewaren. Een afdeling kiest ze vrij; een prefix is niet nodig zolang ze niet botsen.

**Gebouwd op 3 oktober 2026.** Het contract staat in Core (`DeckOverflow.Core.Achievements`):

```csharp
public sealed record XPanel(string Key, bool Hidden = false);
public interface IXPanelSource
{
    string Department { get; }            // card-hall, of world
    IReadOnlyList<XPanel> Panels { get; }
}
```

Hoe een afdeling een paneel herkent, blijft in haar eigen motor (bij de Card Hall `XRegister.Earned(events, enemy)`, uitgevoerd door `Run`, die `XPanelEarned` emitteert); het contract vraagt alleen de lijst. De wereld verzamelt alle bronnen in `World/XPanels.cs` (`Sources`, `All`), met daarin ook haar eigen paneel Do not read the manual. Een nieuwe afdeling voegt daar haar bron toe. Het register toont nog één lijst; een pagina per afdeling komt met de tweede afdeling (zie [todo.md](todo.md)).

## Gebouwd: de panelen van de Card Hall

Gebouwd op 3 oktober 2026: Do not overfill (een vijand helen tot hij omklapt en sterft), Do not write essays (Effective Power in één kaart laten crashen), Do not divide by everything (een bewuste intent tot 0 delen), Do not exceed 9 (500 of meer in één aanval), Do not read the wrong label (een `FormatException`), Do not convert rockets (Convert laten crashen op een reus), Do not wait for midnight (Y2K verslaan als zijn jaartal al 19100 is), Do not stop at chapter 4 (de laatste baas van de Card Hall, de deckbuilder; niet van de hele fabriek). Verborgen: Do not round down forever (The Index bevriest), Do not pour 300 into a byte (de Caster klapt om), en Do not read the manual (van de wereld). De motor herkent de panelen aan de events; de browser bewaart ze over runs heen.

## Open vragen

- [ ] Een paneel voor de Rhythm Turtle of de Bool Ghost? Kandidaat: de schildpad verslaan met alleen derde kaarten.
- [ ] Synchroniseren naar Supabase, zodat een tweede toestel je panelen toont (zie [todo.md](todo.md)).
