# Spike 8: de Controlekamer

De eerste afdeling die geen deckbuilder is. In de Controlekamer (H5, beslissingen) speel je niet zelf: je stelt de regels op van een automaat en kijkt hoe hij vecht. De regels worden van boven naar onder bekeken, en de eerste die klopt, wint. Dat is een `if` / `else if`-keten, maar zo heet het nergens in beeld.

Het ontwerp staat in het [GDD](../../docs/game-design-document.md), sectie "Afdelingen: elk hoofdstuk zijn eigen spelvorm". De spike begint vanaf nul en deelt geen code met het spel in `src/`.

## Wat spike 8 moet aantonen

1. **Is regels opstellen en toekijken leuk**, ook voor iemand zonder interesse in programmeren? Of voelt het als een oefening?
2. **Ontdekken spelers zonder uitleg dat de volgorde telt?** Gevecht 1 en 2 verlies je als de goede regel onder "always" staat. Het bord toont die regel dan als *never reached*.
3. **Lezen spelers de regels van de vijand en spelen ze erop in?** Dat is de test voor het idee "gambits worden intents in de deckbuilder": wie hier de regels van de vijand leest, kan dat later ook in een kaartgevecht.
4. **Zegt "Under the hood" iets?** De optionele C#-weergave toont je regels als `if` / `else if` / `else`. Klikken spelers erop, en herkennen ze hun eigen bord?

## Starten

Vereist: .NET 10 SDK. Draai de commando's vanuit deze map.

```bash
dotnet test tests/DeckOverflow.Engine.Tests
dotnet run --project src/DeckOverflow.Web
```

`/?all` zet alle gevechten open. Met `/?level=goto-fail` ga je meteen naar één gevecht (dan staat ook alles open).

## Hoe het werkt

- **Een regel:** *als* een voorwaarde (eventueel *en* een tweede) *dan* een zet. Elke beurt zet eerst de speler, dan de vijand.
- **Vier zetten**, voor elke automaat dezelfde, met eigen getallen: Whack (schade, dubbel als je opgeladen bent), Hold Firmly (blok tot je volgende zet), Patch Up (herstel, beperkt aantal keer), Wind Up (je volgende Whack is dubbel).
- **Voorwaarden:** always, my HP <, enemy HP <, I'm wound up, enemy is wound up, enemy has block, every n turns. De woordenschat groeit per gevecht; wat nieuw is, staat onder het bord.
- **De natuurwetten:** klopt een regel, dan voert de automaat de zet uit, ook als die niets doet. Een Patch Up zonder herstellingen kost je de beurt. Geen regel die klopt: de automaat staat stil. Na 40 beurten is de shift voorbij, en dat telt als verlies.
- **Terugkoppeling:** de regel die vuurt, licht op. Elke regel telt hoe vaak ze vuurde. Na het duel krijgt een regel die nooit vuurde het label *never reached* (een regel erboven klopte altijd eerst) of *never true*.
- **Score:** het aantal beurten en het aantal regels, en je beste resultaat per gevecht. Een klassement zit nog niet in de spike.

## De vijf gevechten

| # | Vijand | Zijn regels | Wat je ontdekt |
| --- | --- | --- | --- |
| 1 | The Stamper | always → Whack | Herstellen op tijd, en de volgorde: de herstelregel moet boven "always" |
| 2 | The Press | if I'm wound up → Whack; always → Wind Up | Zijn regels lezen: blokken als hij opgeladen is, en die regel bovenaan zetten |
| 3 | The Metronome | every 3 turns → Hold Firmly (20); always → Whack | Slaan op een schild is verspild: laad dan op (`%`, een knipoog naar Act 1) |
| 4 | The Mender | if my HP < 20 → Patch Up; if enemy is wound up → Hold Firmly; always → Whack | Na zijn laatste herstelling blijft zijn bovenste regel vuren en doet hij niets meer |
| 5 | goto fail (elite, echte bug) | regel 3 is een kopie van regel 2 zonder voorwaarde | Alles onder regel 3 wordt nooit bekeken: hij blokt en herstelt nooit, wat zijn regels ook beloven |

Na goto fail verschijnt in de Codex het verhaal van de echte bug: Apples dubbele `goto fail;` uit 2014.

**Afgestemd met een oplosser.** `Solver.Wins` rekent elke regelset zonder EN door. De getallen zijn zo gekozen dat in elk gevecht "alleen slaan" nipt verliest (het doodscherm toont hoeveel HP de vijand nog had) en er meerdere winnende regelsets bestaan. De tests in `LevelTests` pinnen dat vast: pas je een getal aan en faalt er een test, kijk dan eerst met de oplosser wat het gevecht nu doet.

| Gevecht | Winnende sets met 1 / 2 / 3 regels |
| --- | --- |
| The Stamper | 0 / 1 / – (maar 2 vakjes) |
| The Press | 0 / 1 / 21 |
| The Metronome | 0 / 14 / 1142 |
| The Mender | 0 / 1 / 48 |
| goto fail | 0 / 2 / 130 |

## Keuzes tijdens spike 8

- **Geen PixiJS.** De vraag is of de mechaniek leuk is, niet hoe hij eruitziet. De shell is gewone Blazor met CSS-animaties: een flits op de regel die vuurt, schudden bij een treffer, zwevende getallen. Komt de Controlekamer in het spel, dan krijgt hij een stage zoals de deckbuilder.
- **Keuzelijsten in plaats van slepen.** Regels kies je met keuzelijsten en verplaats je met ▲ ▼. Slepen is mooier, maar de keuzelijsten volstonden om de vraag te testen.
- **De motor is deterministisch en heeft geen RNG.** Dezelfde regels geven altijd hetzelfde duel. Zo kan een klassement later eerlijk vergelijken, en kan de oplosser alles doorrekenen.
- **Een regel die klopt, wordt altijd uitgevoerd.** Ook als de zet niets doet (Patch Up zonder herstellingen, Wind Up als je al opgeladen bent). Zo gedraagt een `if` zich ook, en daarop drijft The Mender.
- **"always" ergens anders dan onderaan wordt in C# `else if (true)`.** Dat is geldige C#, en het toont waarom alles eronder nooit loopt.
- **Plaatshoudertekeningen** uit het spel: de golem, de colossus, de ridder, de druppelaar en de geest. Eigen tekeningen pas als de spike overtuigt.
- **Speltekst in het Engels**, in `wwwroot/text/en.json`, zoals in het spel.

## Nog niet in de spike

- Gambits als intents van vijanden in de deckbuilder: dat vraagt werk aan de motor van het spel. Vraag 3 test eerst of spelers zulke regels überhaupt lezen.
- `switch` als tabel van gevallen, en NIET / OF als logische operatoren.
- Types: alle getallen zijn `int`, er is nog geen overflow of afkapping in de Controlekamer.
- Klassement, onderdelenlijst (mastery) en de koppeling met de fabrieksplattegrond.
