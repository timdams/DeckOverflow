# Deck Overflow: Spike Design Doc

1 oktober 2026 · Tim Dams

De spike bewijst in één gevecht dat een C#-regelmotor in de browser een PixiJS-stage kan aansturen met genoeg juice. Wat hier werkt, wordt het skelet van de MVP-demo.

## Doel en scope

De spike beantwoordt drie vragen. Pas als alle drie "ja" zijn, bouwen we de MVP-demo op deze architectuur.

1. **Kan een C#-motor in de browser draaien met een aanvaardbare laadtijd?**
2. **Kan die motor een PixiJS-stage aansturen zonder dat de interop voelbaar vertraagt?**
3. **Voelt één gevecht al satisfying genoeg om er nog één te willen spelen?**

| In de spike | Bewust niet |
| --- | --- |
| Eén gevecht: speler tegen de Byte-Golem | Map, runs, loot, beloningsschermen |
| Hand van 5 kaarten uit een deck van 10, 3 energie | Codex, collectie, meta-progressie |
| Regels: `int`-afkappen, `byte`-overflow, schade, blok | Andere vijanden en concepten |
| Hit pause, shake, oprollende getallen, partikels, stijgende tonen | Definitieve art en muziek |
| Seed via URL, zodat elk gevecht reproduceerbaar is | Backend, login, opslag, dashboard |

## Architectuur

![De motor beslist, de stage speelt af: regelmotor, Blazor-shell en JavaScript-stage in de browser](architectuur.svg)

De stage meldt een gespeelde kaart, de shell vertaalt die naar een command, de motor geeft events terug en de stage speelt ze af. Alleen de motor kent de regels; de API komt pas bij de MVP.

## Solution-structuur

Drie projecten in de spike, één extra project pas bij de MVP. De motor heeft geen enkele afhankelijkheid naar web of UI.

```text
DeckOverflow.sln
├─ src/
│  ├─ DeckOverflow.Engine/            pure C#, geen dependencies
│  │  ├─ Values/                      TypedValue, ValueKind, regels per type
│  │  ├─ Combat/                      Combat, Combatant, Turn, Intent
│  │  ├─ Cards/                       CardDefinition, Effect, Deck
│  │  ├─ Commands/                    PlayCard, EndTurn
│  │  ├─ Events/                      GameEvent en subtypes
│  │  └─ Random/                      SeededRng
│  └─ DeckOverflow.Web/               Blazor WebAssembly shell
│     ├─ Pages/CombatPage.razor       host voor de stage, knoppen, hand
│     ├─ Interop/StageBridge.cs       stuurt events naar JS, ontvangt input
│     └─ wwwroot/stage/               PixiJS + GSAP + Howler (ES-modules)
│        ├─ stage.js                  init, scene graph
│        ├─ timeline.js               event-wachtrij naar animaties
│        ├─ juice.js                  presets: hit pause, shake, roll
│        └─ audio.js                  Howler, pitch per trigger
└─ tests/
   └─ DeckOverflow.Engine.Tests/      xUnit: regels, determinisme, scenario's
```

- **DeckOverflow.Engine:** alle regels. Testbaar zonder browser. Later ook herbruikbaar in de API, bijvoorbeeld om dagelijkse runs server-side te valideren.
- **DeckOverflow.Web:** dunne shell. Geen spelregels, alleen orkestratie tussen motor en stage.
- **wwwroot/stage:** alle juice. Geen spelregels, alleen het afspelen van events.
- **DeckOverflow.Api** (pas bij MVP): ASP.NET Core minimal API met SQLite, in Docker.

## Regelmotor

De motor werkt volgens één patroon: een command gaat erin, een lijst events komt eruit. De stage speelt die events af en leest nooit zelf de spelstatus.

```csharp
public sealed class Combat
{
    public static Combat Start(CombatSetup setup, ulong seed) { /* ... */ }

    // Enige manier om de status te wijzigen
    public IReadOnlyList<GameEvent> Handle(ICommand command) { /* ... */ }

    // Alleen-lezen beeld voor de shell: hand, energie, HP
    public CombatSnapshot Snapshot() { /* ... */ }
}

public interface ICommand;
public sealed record PlayCard(int HandIndex, int TargetId) : ICommand;
public sealed record EndTurn : ICommand;
```

### Getypeerde waarden

Elke waarde in het spel draagt een C#-type. De regels per type zijn gewoon .NET-gedrag, geen nabootsing. Daardoor kan de Codex later tonen wat er echt gebeurde.

```csharp
public enum ValueKind { Int, Byte, Double, Bool, String }

public static class ByteRules
{
    public static (byte Result, bool Overflowed) Add(byte current, int amount)
    {
        byte result = unchecked((byte)(current + amount));
        return (result, current + amount > byte.MaxValue);
    }
}

public static class IntRules
{
    public static (int Result, double Lost) Truncate(double incoming)
    {
        int result = (int)incoming;
        return (result, incoming - result);
    }
}
```

### Ontwerpregels voor de motor

- **Deterministisch.** Een eigen RNG met seed (bijvoorbeeld PCG of xorshift), niet `System.Random`, zodat een seed over .NET-versies heen hetzelfde gevecht oplevert.
- **Geen tijd, geen UI.** Geen `DateTime`, geen `Task.Delay`, geen kennis van animatieduur.
- **Elke regel die iets bijzonders doet, meldt het.** Afkappen, overflow en concatenatie krijgen een eigen event. Dat is de haak voor zowel juice als Codex.
- **Events zijn klein en plat.** Ids en getallen, geen objectgrafen, zodat ze goedkoop over de interop gaan.

## Eventcontract

Het eventcontract is de enige afspraak tussen C# en JavaScript. Elk event heeft een `type`, een volgnummer `seq` en platte velden. De stage mag events die ze niet kent negeren, zodat de motor kan groeien zonder de stage te breken.

| Event | Velden | Wat de stage ermee doet |
| --- | --- | --- |
| `CardPlayed` | cardId, sourceId, targetId | Kaart vliegt van de hand naar het doel |
| `DamageDealt` | targetId, amount, hpBefore, hpAfter | Hit pause, flits, getal rolt af, shake naar grootte |
| `ValueTruncated` | targetId, before, after, lost | De decimaal breekt af en valt rinkelend weg |
| `ValueOverflowed` | targetId, before, added, after, max | Teller rolt op naar max, alles bevriest, klik, terug naar 0 |
| `BlockGained` | targetId, amount, total | Schild pulseert op, metalen tik |
| `Healed` | targetId, amount, hpAfter | Groene partikels omhoog |
| `IntentRevealed` | enemyId, expression, value | Expressie verschijnt boven de vijand |
| `CombatantDied` | targetId | Uiteenspatten, bas, korte vertraging |
| `TurnEnded` | turn | Hand schuift weg, nieuwe hand komt binnen |
| `BlockAbsorbed` | targetId, absorbed, remaining | Schild trilt, cyaan scherven, breekt bij 0 |
| `BlockExpired` | targetId, amount | Schild vervaagt bij de start van je beurt |
| `AttackLaunched` | sourceId, targetId, expression, value | Vijand schiet naar voren |
| `TurnStarted` | turn, energy | "BEURT 2" schuift door het beeld |
| `PlayRejected` | handIndex, reason | Kaart wiebelt terug, zoemer, grappige reden |
| `CombatEnded` | won | Banner GEWONNEN of GECRASHT |
| `IntentAssigned` | enemyId, expressionBefore, value | Toekenning (Zet op 1): de intent wordt overschreven door het nieuwe getal |
| `ModifierQueued` | label, pending | "+3" springt op bij de energiebol; wat wacht staat ernaast |
| `ModifiersApplied` | cardId, before, after, expression | De som "(6 + 3) × 2" verschijnt en rekent uit tot 18 |
| `RelicTriggered` | relicId | De naam van de relic licht op; het effect volgt als gewone events |
| `ValueRounded` | targetId, before, after, subject | `Math.Round` of `Convert` rondde af: "2.5 → 2" met de naam eronder (The Rounder, Measure Twice) |
| `ValueGrew` | targetId, before, factor, raw, after | "× 1.05" boven de vijand, HP rolt op; een afgekapt restje volgt als `ValueTruncated` (The Index) |
| `ConversionCrashed` | targetId, to, value | `Convert` paste niet: "OverflowException", de vijand schudt, zijn intent vervaagt |
| `AttackSkipped` | enemyId | De vijand crashte vorige beurt en valt niet aan |
| `ExceptionThrown` | exception, expression | Je getypeerde aanval crashte, bv. `int.Parse(2.5 + "1")`: de naam van de exception, en je beurt eindigt |

Run-events, die de stage negeert en de shell als melding toont, kwamen erbij met de acts: `ActCompleted` (act) na de baas van een act, en `ActStarted` (act) bij een nieuwe act, die ook een startpunt wordt.

Zes events kwamen erbij tijdens het bouwen van spike 1, de laatste vier in spike 3. `IntentRevealed.value` is sinds spike 3 leeg als het totaal verborgen blijft (de Rekenmeester). Het patroon bleef hetzelfde: de stage negeert wat ze niet kent.

```json
[
  { "seq": 41, "type": "CardPlayed", "cardId": "herstel", "sourceId": 0, "targetId": 1 },
  { "seq": 42, "type": "ValueOverflowed", "targetId": 1, "before": 250, "added": 6, "after": 0, "max": 255 },
  { "seq": 43, "type": "CombatantDied", "targetId": 1 }
]
```

In C# zijn het `record`-types die met `System.Text.Json` en een type-discriminator naar deze vorm geserialiseerd worden.

## Stage

De stage is een wachtrij die events omzet in animaties, één na één. Zolang de wachtrij speelt, is input geblokkeerd, zodat de speler altijd ziet wat er gebeurt voor hij verder kan.

- **Scene graph (PixiJS):** lagen voor achtergrond, vijanden, speler, hand, effecten en UI-getallen. Elke combatant is een container met sprite, HP-balk en intent-label.
- **Timeline (GSAP):** per event een kleine GSAP-timeline. De wachtrij voegt ze achter elkaar, met overlap waar het mag (een getal kan al oprollen terwijl de shake uitdooft).
- **Juice-presets:** een klein bestand met getallen, zodat de feel afstelbaar is zonder code te lezen.
- **Audio (Howler):** een sprite-bestand met korte geluiden. Toonhoogte via `rate`, die per opeenvolgende trigger in een combo stijgt.
- **Snelle modus:** één factor die alle duurtijden schaalt, behalve hit pause.

```js
// juice.js - startwaarden, af te stellen tijdens de spike
export const juice = {
  hitPauseMs:   { small: 40, medium: 70, large: 120 },
  shakePx:      { small: 2,  medium: 5,  large: 10 },
  rollMsPerUnit: 18,
  comboPitchStep: 0.06,
  overflow: { freezeMs: 350, rollToMaxMs: 600 }
};
```

De concrete getallen zijn vertrekpunten, geen beslissingen: ze worden in de spike op gevoel afgesteld.

## Interop

Per actie van de speler gaat er precies één bericht heen en één terug. De hand en alle kaarten leven in PixiJS, zodat ook hover en sleep-animaties juice krijgen; Blazor ziet alleen het resultaat.

1. **JS → .NET:** de speler speelt een kaart. De stage roept `OnCardPlayed(handIndex, targetId)` aan via een `DotNetObjectReference`.
2. **.NET:** de shell maakt een `PlayCard`-command, geeft het aan de motor en krijgt een lijst events terug.
3. **.NET → JS:** de hele lijst gaat in één keer naar `stage.play(events)`. Die geeft een promise terug die pas klaar is als de laatste animatie gedaan is.
4. **.NET → JS:** daarna stuurt de shell de nieuwe snapshot (hand, energie, HP), zodat de stage synchroon blijft.

```csharp
public sealed class StageBridge(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _stage;

    public async Task InitAsync(ElementReference host, DotNetObjectReference<CombatPage> page)
    {
        _stage = await js.InvokeAsync<IJSObjectReference>("import", "./stage/stage.js");
        await _stage.InvokeVoidAsync("init", host, page);
    }

    public ValueTask PlayAsync(IReadOnlyList<GameEvent> events) =>
        _stage!.InvokeVoidAsync("play", events);

    public ValueTask SyncAsync(CombatSnapshot snapshot) =>
        _stage!.InvokeVoidAsync("sync", snapshot);

    public async ValueTask DisposeAsync()
    {
        if (_stage is not null) await _stage.DisposeAsync();
    }
}
```

De spike start met `IJSRuntime` omdat dat het eenvoudigst is. Blijkt de overhead toch meetbaar, dan is `[JSImport]`/`[JSExport]` de snellere route; de grens via `StageBridge` maakt die wissel lokaal.

## Spike-scenario: de Byte-Golem

Het scenario raakt in één gevecht beide regels (afkappen en overflow) en het grootste juice-moment. Met de standaardseed (255) zit Herstel in de eerste hand.

**Opzet.** Speler: 50 HP (`int`), 3 energie, deck van 5× Slag (6 schade), 4× Schild (5 blok), 1× Herstel (+6 HP op een doelwit naar keuze). Byte-Golem: 250 van 255 HP (`byte`), valt aan met `15 / 2.0` en heelt zichzelf +2 na elke aanval.

**Pad A: de speler snapt het niet meteen**

1. Speler speelt Slag: `DamageDealt`, golem 244. Kleine hit pause, getal rolt af.
2. Speler speelt Schild: `BlockGained`, 5 blok.
3. Einde beurt. Golem valt aan voor 7,5. Blok vangt 5 op, 2,5 blijft over. Speler-HP is een `int`: `ValueTruncated` toont hoe de ,5 afbreekt, speler verliest 2.
4. Golem heelt +2. Na een paar beurten slaan en helen ziet de speler dat dit lang gaat duren.

**Pad B: de speler snapt het**

1. Speler speelt Herstel op de golem: 250 + 6 = 256 past niet in een `byte`.
2. `ValueOverflowed`: teller rolt op tot 255, alles bevriest, klik, teller springt naar 0.
3. `CombatantDied`: de golem spat uiteen, bas-drop. Gevecht gewonnen in één kaart.

Variant om te testen: zit de golem op 252 als je heelt, dan landt hij op 2. Daarna nog één Slag: een combo van twee events die nog beter kan voelen dan de instant kill.

## Keuzes tijdens de spike

Bij het bouwen kwamen regels boven die het doc nog openliet. De tests in `DeckOverflow.Engine.Tests` leggen ze vast.

- **Schade stopt op 0, ook bij `byte`.** Echte underflow (`2 - 6` wordt `252`) zou de variant met een golem op 2 breken. Dat is een kandidaat-mechaniek voor later.
- **Herstel op een `int`-speler gaat niet boven max HP.** Dat is een spelregel. Bij `byte` geldt de typegrens, en die loopt over.
- **De golem heelt ook via `ByteRules`.** Staat hij op 254, dan doodt hij zichzelf met +2.
- **Blok is een `int`.** Een halve schade kost een hele blokpunt: 7,5 schade op 10 blok laat 2 blok over.
- **De stage weigert ongeldige doelwitten al zelf.** De motor weigert alleen wat de stage niet kan weten, zoals te weinig energie.
- **Seed-determinisme is vastgepind** met een test op de eerste getallen van PCG32. Faalt die, dan veranderen alle bestaande seeds.
- **Bibliotheken en fonts zitten in `wwwroot/lib` en `wwwroot/fonts`.** Zo draait de spike zonder CDN, ook op GitHub Pages of een schoolnetwerk.
- **De HUD linksboven meet de succescriteria live:** fps, minimum fps tijdens de animatie, klik tot eerste frame, motortijd en laadtijd.

## Playtest 1 en spike 2

Playtest 1 bevestigde de techniek en de golem-ontdekking, maar toonde dat de link met C# te smal was.

- **Wat werkte:** pad B ontdekken, de golem helen tot hij omklapt, was grappig en bleef hangen.
- **Wat niet:** tegen een gewone vijand blijft het schade doen en blokken, zoals in Slay the Spire. Eén regel per vijand schaalt niet.
- **Besluit:** types op elke vijand en elke aanval, plus Omgieten als kerngereedschap (zie het [Game Design Document](game-design-document.md#kernsysteem-types-en-omgieten)). In de game heet het nog niet "cast"; de Codex-pagina opent pas bij hoofdstuk 4.

### Scope spike 2

| Erbij | Bewust nog niet |
| --- | --- |
| Eén gewone vijand met een type, bijvoorbeeld een `double`-geest met een decimaal schild | Statuseffecten |
| Getypeerde aanvallen in het deck: `int`- en `double`-kaarten | De beurt als expressie |
| Omgieten naar `byte` en naar `int` | Map, extra vijanden, acts |

Nieuw event: `TypeChanged` (targetId, from, to). De vijand smelt en giet zich om in een nieuwe vorm, met de kleur en het kader van zijn nieuwe type. Gebouwd in [spike 2](../spikes/02-omgieten/README.md), met extra velden voor HP, max HP en blok na het omgieten.

### Motor

- Het type van een combatant wordt veranderlijk. De regels per type blijven in `ByteRules` en `IntRules`, aangevuld met `DoubleRules`.
- Omgieten volgt echte C#-conversie: `double` naar `int` kapt af, en `int` naar `byte` klapt om in een `unchecked`-context. Een vijand met 300 HP die naar `byte` gaat, houdt er 44 over. Dat wordt een groot juice-moment.
- Van `double` naar `byte` loopt via `int`, omdat een rechtstreekse conversie van een te grote `double` naar een geheel type in C# geen vastgelegd resultaat heeft.

### Wat spike 2 moet aantonen

- Testers kiezen hun aanvalstype bewust; we laten ze hardop denken.
- Testers vinden zonder hint dat Omgieten plus helen ook op een andere vijand dan de golem werkt.
- Na de test vragen we: "Voelde dit gewone gevecht als Slay the Spire?" Het antwoord moet nee zijn.

## Spike 3: de hele act

Spike 2 bouwde één gevecht met types en Omgieten. Spike 3 legt de core game loop errond: een map kiezen, vechten, een beloning kiezen, herhalen tot de baas. Gebouwd in [spike 3](../spikes/03-vatenvallei/README.md).

| Erbij | Bewust nog niet |
| --- | --- |
| Een korte act: 8 rijen plus de baas, paden die niet kruisen, gegenereerd uit de seed | De volledige Act 1 van 15 knopen en 25 tot 35 minuten |
| Gevecht, elite, rustvuur, event (De Smeltkroes, Het Lekkende Vat), winkel, schat, baas | Codex, collectie, meta-progressie |
| HP, goud, deck en relics die doorlopen; 1 kaart uit 3 na elk gevecht | Meerdere vijanden per gevecht |
| Slijmklodder, Tinnen Ridder, Druppelaar en een eerste Rekenmeester | Bool-schim, Papieren Golem, De Naamloze |
| Nieuwe kaarten rond toekenning en volgorde: Zet op 1, Voeg toe, Verdubbel, Byteval | Statuseffecten, de beurt als expressie |
| Vijf relics, verbeteren aan het rustvuur, Omgieten in de Smeltkroes | Zeldzaamheidsglans, kist-anticipatie, pity timer |

### Motor

- **`Run` naast `Combat`, met hetzelfde patroon.** `Run.Handle(ICommand)` geeft events terug en `Run.Snapshot()` is de waarheid voor de shell. Gevechtscommands gaan door naar het lopende gevecht; HP, goud en deck gaan na het gevecht terug naar de run.
- **De map volgt uit de seed**, loot uit een aparte RNG-stroom en elk gevecht uit een eigen seed per knoop. Zo blijft een run reproduceerbaar zonder dat een extra beloning de map verandert.
- **Een vijand heeft een patroon van intents**, één per beurt. De Rekenmeester verbergt zijn totaal: hier is rekenen bewust de kern.
- **Modifiers volgen de volgorde van toekenning:** eerst +3 en dan ×2 is `(6 + 3) × 2`, omgekeerd `6 × 2 + 3`. Dat is het eerste stukje operatorvoorrang buiten de intents.

Nieuwe run-events: `NodeEntered`, `GoldChanged`, `RunHpChanged`, `CardAdded`, `CardRemoved`, `CardTransformed`, `RelicGained`, `RunRejected` en `RunEnded`. De stage negeert ze; de shell toont ze als melding.

### Wat spike 3 moet aantonen

- Testers kiezen hun pad bewust en wegen risico tegen beloning. We laten ze hardop denken op de map.
- Een beloning voelt als een keuze, en Overslaan wordt soms gekozen.
- Een run duurt 10 tot 15 minuten, en na verlies klikt de tester zelf op "Nieuwe run".

## Spike 4: de eerste minuten

Het begin van spike 3 speelde stroef: drie van de tien starterkaarten deden niets in het eerste gevecht, en het eerste aha-moment kwam pas bij een elite. Spike 4 werkt aan de eerste minuten, naar het voorbeeld van Slay the Spire. Gebouwd in [spike 4](../spikes/04-eerste-minuten/README.md).

| Erbij | Bewust nog niet |
| --- | --- |
| Een starterdeck waarvan elke kaart meteen werkt: 4× Strike, 2× Floating Strike, 3× Shield, 1× Add | Een tweede personage of starterdeck |
| De Gieterij: een openingskeuze tussen een kaart, een zichtbare relic en goud | Ruilen of risico's in de openingskeuze |
| De Bottomless Jug achter een vroege `?`: een byte die zich voorbij 255 drinkt | Meer wonder-encounters |
| Het spel in het Engels, alle spelteksten in `en.json` | Een Nederlandse vertaling |

### Motor

- **Geen spelteksten in de motor.** Snapshots en events dragen sleutels en getallen (`TextRef`); shell en stage zoeken de tekst op in `wwwroot/text/en.json`. Kaartnamen hangen aan de id, kaartteksten volgen uit het effect.
- **Relics zijn klassen met haken.** `Combat` en `Run` roepen haken aan en kennen geen relic bij naam.
- **Elk gevecht blijft te winnen:** zonder kaart die naar byte giet, wordt de Tinnen Kolos de Byte-Golem.
- `PlayRejected.Reason` en `RunRejected.Reason` zijn nu sleutels, bv. `reject.no-energy`. Geen nieuwe events.

### Wat spike 4 moet aantonen

- Na 2 minuten weet een tester wat elke kaart in zijn hand doet, zonder uitleg.
- De keuze in de Gieterij voelt als een eigen start.
- Het omklappen van de kruik lokt een hardop "wacht, wat?" uit, en de tester kan daarna zeggen waarom het gebeurde.

## Succescriteria

De spike slaagt als hij technisch vlot draait en als testers het gevecht willen herspelen. De drempels hieronder zijn voorstellen om bij de start vast te leggen.

| Criterium | Voorstel | Hoe meten |
| --- | --- | --- |
| Laadtijd tot speelbaar | Onder 5 s, koude cache, gewone schoollaptop | Browser devtools, netwerk en performance |
| Downloadgrootte | Onder 5 MB gecomprimeerd | Publish-output met trimming, brotli |
| Vloeiendheid | 60 fps tijdens de overflow-animatie, nooit onder 30 | PixiJS ticker, devtools performance |
| Reactietijd | Onder 50 ms van klik tot eerste animatieframe | Timestamps in `StageBridge` en `stage.js` |
| Correctheid | Alle regels en seed-determinisme groen in xUnit | CI-run op de motor |
| Feel | Minstens 4 van 5 testers willen nog een gevecht | Korte playtest met studenten en collega's |
| Ontdekking | Noteren hoeveel testers pad B zelf vinden, zonder hint | Observatie tijdens de playtest |

De laatste rij is geen slaag-of-faal-criterium, maar de eerste meting van het hele concept: ontdekken spelers een C#-regel door te spelen?

## Van spike naar MVP-demo

De MVP-demo is een speelbare, afgewerkte Act 1 (hoofdstuk 2 en 3 van Zie Scherp Scherper), zoals beschreven in het [Game Design Document](game-design-document.md). De architectuur van de spike blijft, er komen lagen bij.

| Laag | Spike 7 | MVP-demo |
| --- | --- | --- |
| Motor | Act 1 van spike 4: map, beloningen, relics, events, 8 vijanden | Alle concepten van H2 en H3 (o.a. `string` + `int`, `char`, `++`, `const`), elites Level 256 en Effective Power, de Rekenmeester; de patch-mechaniek; runs over acts met startpunten |
| Shell | Map, beloning, rust, winkel, events, Codex-kader na een bug | Codex-pagina's met echte C# en een link naar het hoofdstuk; startpunt kiezen met een gedraft deck; klascode invoeren |
| Stage | Handleidingstijl, explosietekening, schaar, rondgaande pijl | Revisie-stempel voor de patch; juice voor de nieuwe concepten |
| Audio | Placeholders uit de pixelspikes | Een papieren klankkleur, pas als de loop leuk is |
| Backend | Geen | Supabase zonder eigen server: gastaccounts met optionele registratie, voortgang, dagelijkse seed, klasklassement met gegenereerde bijnamen, klascode; zie [Hosting, accounts en data](#hosting-accounts-en-data) |
| Docent | Geen | Een act vrijgeven voor de klas via de klascode; een dashboard pas na de MVP |
| Taal | Engels | Engels; Nederlands is één extra tekstbestand |

Volgorde: eerst de motor uitbreiden met tests, dan de shell-schermen, dan pas de backend. Zo is de demo al lokaal speelbaar voor er een server nodig is.

**Prioriteit:** eerst de core game loop: vechten, een beloning kiezen, de map. Geluid, definitieve art en andere polish wachten tot die loop leuk is; tot dan volstaan de placeholders.

### Van spikes naar src/

Beslist op 2 oktober 2026, na spike 7. De echte game start in `src/` en `tests/` op de root van de repo, als bewuste kopie van spike 7. De spikemappen blijven zoals ze zijn.

| Wat | Uit | Opmerking |
| --- | --- | --- |
| Motor met tests | spike 7 (= motor van spike 4) | 1306 tests, PCG32-seeds vastgepind |
| Shell en stage | spike 7 | handleidingstijl, geen stijlmenu meer |
| Art en snijscript | spike 6 en 7 | `tools/cut_sheets.py` en de vellen in `art/sheets/` |
| Teksten | spike 7, `en.json` | kaartnamen als overtredingen van de handleiding |
| Niet mee | spike 5 | het stijlmenu en de pixelsprites |

1. `src/` en `tests/` opzetten uit spike 7, met CI.
2. Runs over acts: kortere map van 6 rijen, startpunten met een gedraft deck.
3. De concepten van H2 en H3 in de motor, elk met een eigen `XxxRules`-klasse en tests.
4. Elite Effective Power en de patch-mechaniek (`const`).
5. Codex-pagina's met echte C# en links naar het boek.
6. Hosting op GitHub Pages en de backend op Supabase.
7. Playtest met 5 studenten en 2 collega's, pas als Act 1 af is.

## Hosting, accounts en data

Beslist op 3 oktober 2026. Iedereen mag Deck Overflow spelen: studenten, maar ook leerlingen uit het middelbaar en wie het toevallig vindt. Er spelen dus minderjarigen mee, en dat stuurt elke keuze hieronder: zo weinig gegevens als het spel nodig heeft, en niets wat publiek naar een persoon wijst.

### Hosting: GitHub Pages

De game is een statische site (Blazor WebAssembly), dus een server is niet nodig.

- **Eén Pages-site voor alles.** Eén workflow publiceert het spel op de root en elke spike die we willen testen in een submap (`/spike-8/`). Zo heeft elke playtest een eigen link zonder iets te installeren.
- **`.nojekyll` in de output.** Zonder dat bestand negeert Pages de map `_framework/` van Blazor, en laadt er niets.
- **`<base href>` per map.** De site staat onder `/DeckOverflow/`, niet op `/`. De workflow zet de juiste base bij het publiceren, tenzij er later een eigen domein komt.
- **Geen deep links nodig.** Het spel werkt met querystrings op `/` (`?seed=…`, `?level=…`), dus de gebruikelijke 404-omweg voor SPA's is overbodig.
- **De repo moet publiek zijn** voor gratis Pages.
- **`<script type="importmap"></script>` in `index.html`.** .NET 10 zet een fingerprint in de naam van `dotnet.js`; de import map vertaalt die. Lokaal doet de devserver dat, op Pages laadt zonder die regel niets.
- Pages serveert de Brotli-bestanden van Blazor niet, dus de eerste keer laden is enkele MB zwaarder. Op te lossen met een eigen loader als de laadtijd een probleem blijkt.

### Accounts: eerst gast, later bewaren

1. **Je speelt meteen als gast.** Geen loginscherm vooraf: het spel moet aanvoelen als gewoon een deckbuilder, zie de onthulling in het GDD. Technisch een anoniem account in Supabase, gebonden aan je browser.
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

### Data: Supabase

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

## Risico's en open vragen

Het grootste technische risico is de laadtijd van Blazor WebAssembly op schoollaptops; het grootste ontwerprisico is dat pad B niet ontdekt wordt.

| Risico | Gevolg | Uitwijk |
| --- | --- | --- |
| Blazor-download te groot of te traag | Studenten haken af voor het spel start | Trimming en AOT testen; uiterste uitwijk: motor naar TypeScript porten met dezelfde tests als specificatie |
| Interop-overhead merkbaar | Kaarten voelen stroef | Overstap naar `[JSImport]`/`[JSExport]`, events bundelen |
| Audio start niet | Browsers blokkeren geluid tot de eerste klik | Startscherm met "Speel"-knop die audio ontgrendelt |
| Motor en stage lopen uit sync | Getoonde HP klopt niet | Na elke `play` een `sync` met de snapshot als waarheid |
| Pad B wordt niet ontdekt | Het concept werkt niet zonder hint | Intent of vijandtekst subtiel laten verwijzen naar 255 |

- [ ] Licenties van PixiJS, GSAP en Howler nakijken voor gebruik in onderwijs en eventuele verkoop
- [ ] Placeholder-art en -geluid kiezen (vrije assetpacks of zelf gemaakt)
- [x] Hosting van de spike: GitHub Pages (beslist op 3 oktober 2026)
- [x] Publieke GitHub-repo en Pages-workflow: [timdams/DeckOverflow](https://github.com/timdams/DeckOverflow), gepubliceerd op [timdams.github.io/DeckOverflow](https://timdams.github.io/DeckOverflow/) (3 oktober 2026)
- [ ] Misbruik van gastaccounts: volstaan de limieten van Supabase, of is er een captcha nodig? Een captcha botst met "meteen spelen".
- [ ] Woordenlijsten voor bijnamen samenstellen en nakijken op ongelukkige combinaties
- [ ] Privacyverklaring in eenvoudige taal schrijven
- [ ] Playtesters vastleggen: 5 studenten en 2 collega's
