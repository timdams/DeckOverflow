# Act 3: De Gieterij

Act 3 dekt hoofdstuk 4: werken met data. Act 1 liet types en tekst voelen; de Gieterij gaat over **expliciet omzetten**. Een gieterij giet gesmolten metaal in mallen, en dat is casting, letterlijk. Hier krijgt Omgieten zijn echte naam. In het Engelstalige spel heet de act The Mold Works, omdat het openingsevent van act 1 al The Foundry heet.

| Concept | Waar het in de game zit |
| --- | --- |
| Expliciete cast kapt af | Force Fit, Flight 501, Codex "Casting" na de baas |
| Impliciete omzetting (klein naar groot) | The Ingot |
| `Convert` rondt af en is checked: een `OverflowException` als het niet past | Measure Twice, Flight 501 |
| `Math.Round`, bankiersafronding | The Rounder |
| `int.Parse` en een ongeldige tekst | Read the Label, The Label |
| Afkappen tegenover afronden | Elite The Index |

## Gewone vijanden

| Vijand | Mechaniek | Wat je ontdekt | Andere manier om te winnen |
| --- | --- | --- | --- |
| The Label | Zijn HP is tekst: `"40"`. Schade plakt eraan vast (`"40" + 6` is `"406"`), dus wie eerst slaat, parset daarna een veel groter getal. Een Floating-kaart maakt er `"402.5"` van, en dan crasht het parsen. | Tekst is geen getal: eerst parsen. Omgieten weigert, want `(int)"40"` compileert niet. | Measure Twice: `Convert.ToByte("40")` parset ook, zolang het onder 256 blijft. Zonder kaart die tekst omzet krijg je de Rounder in zijn plaats |
| The Rounder | `double`-HP. Hij rondt elke inkomende treffer af met `Math.Round`: 1.5 wordt 2, maar 2.5 ook, en 3.5 wordt 4. Welke meervoudige kaart je speelt, maakt het verschil. | Bankiersafronding, tegenover het afkappen van een `int` | Met Force Fit naar `int` omgieten: dan kapt hij af en rondt hij nooit meer af |
| Raw Ingot | Een eenvoudige `int` die na elke aanval wat blok opbouwt | Niets nieuws: een adempauze tussen de puzzels | Gewoon slaan |

## Elites

- **Flight 501** (Ariane 5, 1996): 506 HP als `int`, te sterk om plat te slaan. Omgieten naar `byte` maakt er 250 van. Measure Twice (`Convert.ToByte`) klapt niet om maar gooit een `OverflowException`, zoals bij de echte raket: hij crasht en slaat zijn aanval over. Zonder echte cast naar `byte` in je deck krijg je The Index in zijn plaats. Flight 501 zat eerst in act 1, maar hoort bij casting.
- **The Index** (Vancouver Stock Exchange, 1982): zijn HP groeit elke beurt met `(int)(HP * 1.05)`. Hij kapt af in plaats van af te ronden, dus onder 20 HP eet het afkappen de groei op: `(int)(19 * 1.05)` is `(int)19.95` is 19. Wie hem onder de 20 krijgt, ziet zijn groei stilvallen, net als de echte index die maanden zakte.

## Baas: The Caster

Een figuur van de fabriek die zichzelf na elke aanval in een andere mal giet: van `int` naar `double` naar `byte`, en weer van voren af. Als `int` kapt hij je decimalen af, als `double` neemt hij alles exact, als `byte` loopt hij over als je hem heelt. Hij begint met 300 HP, en dat past niet in een `byte`: wie hem boven 255 houdt tot hij een `byte` wordt, ziet hem omklappen (`(byte)300` is 44). Het slotgevecht vat act 1 en act 2 samen. Daarna opent de Codex-pagina "Casting" met de echte naam.

## Nieuwe kaarten

| Kaart | Wat ze doet | Concept |
| --- | --- | --- |
| Measure Twice | Zet een vijand om naar `byte` met `Convert.ToByte`: afronden in plaats van afkappen. Past het niet, dan een `OverflowException`: de vijand crasht en slaat zijn aanval over. | Convert tegenover cast: checked tegenover unchecked |
| Read the Label | Tekst-HP wordt een getal (`int.Parse`). Op ongeldige tekst crasht ze, en je beurt eindigt. | Parse |

Round Off (`Math.Round` op een vijand) en Square Root (`Math.Sqrt` op zijn HP) zijn voorlopig geschrapt. Round Off verschuift hoogstens een halve HP en doet dus te weinig; Square Root maakt van elke baas een gevecht van één kaart.
