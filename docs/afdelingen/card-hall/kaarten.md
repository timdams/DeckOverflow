# Kaarten en relics

### Kaartnamen

Elke kaartnaam is een instructie of een overtreding, nooit een wapen. "Floating" blijft het woord voor `double`, omdat het later naar `float` en `double` verwijst. De spelnamen zijn Engels; de werktitels in dit document blijven Nederlands.

| Werktitel | Spelnaam | Wat je ziet |
| --- | --- | --- |
| Slag | Whack | het figuurtje klopt met de vlakke hand op een machine |
| Zware Slag | Hammer It In | een pin die te groot is voor het gat |
| Schild | Hold Firmly | het figuurtje houdt een kantelende kast tegen |
| Dik Schild | Two-Person Lift | twee figuurtjes tillen samen |
| Herstel | Fill Past the Line | een vat met een maxstreep, de vloeistof erboven |
| Voeg toe | Spare Screw | het schroefje dat altijd overblijft |
| Dubbel | Second Pair of Hands | het figuurtje met vier armen |
| Giet om | Force Fit | een ronde pin in een vierkant gat |
| Zet op 1 | Wrong Label | een nieuw etiket over het oude |
| Vlottende Slag | Floating Bolts | drie bouten die zweven |

## Kaarten en relics

Het starterdeck is bewust saai, zodat elke beloning een echte keuze wordt.

**Starterdeck (10 kaarten, gebouwd):** 4× Whack (6 schade), 2× Floating Bolts (3× 2.5), 3× Hold Firmly (5 blok), 1× Spare Screw (+3 op je volgende kaart).

**Gebouwd (25 kaarten).** Elke kaart heeft een verbeterde versie aan het rustvuur.

| Kaart | Kost | Effect | Concept |
| --- | --- | --- | --- |
| Whack | 1 | 6 schade (`int`) | |
| Floating Bolts | 1 | 3× 2.5 schade (`double`) | `double`, afkappen op een `int` |
| Hammer It In | 2 | 14 schade | |
| Floating Parts | 1 | 4× 1.5 schade | `double` |
| Hold Firmly / Two-Person Lift | 1 / 2 | 5 / 13 blok | |
| Fill Past the Line | 1 | +6 HP op een doelwit naar keuze | overflow, als je het doorhebt |
| Force Fit | 1 | Giet een vijand om naar `int` of naar `byte` (twee kaarten) | casting |
| Squeeze In (zeldzaam) | 2 | Omgieten naar `byte` en dan 6 helen | overflow |
| Wrong Label | 1 | Zet de aanval van een vijand op 1 | toekenning |
| Spare Screw | 0 | `+ 3` op je volgende kaart | `+=` |
| Second Pair of Hands | 1 | `× 2` op je volgende kaart | |
| Floating Point | 0 | `× 1.0`: je volgende aanval wordt een `double` | type van een expressie |
| Split | 0 | `/ 2`, maar je volgende kaart slaat twee keer | deling van gehele getallen |
| Ink | 1 | `+ "1"`: tekst plakt aan je volgende kaart | `string` en `+` |
| Read (zeldzaam) | 2 | `int.Parse(…)` op je volgende kaart | parsen |
| Count Letters (act 2) | 1 | Een tekstvijand wordt een `int` met zijn `Length` als HP | `Length` |
| Letter A (act 2, zeldzaam) | 2 | `+ 'A'` op je volgende kaart | `char` is een getal |
| Measure Twice (act 3) | 1 | `Convert.ToByte` op een vijand | Convert tegenover cast |
| Read the Label (act 3) | 1 | `int.Parse` op tekst-HP | parse |
| Flip | 1 | `isSolid = !isSolid` op een vijand met een `bool` | `bool`, `!` |
| Remainder | 1 | `% 5` op de aanval van een vijand | modulo |

Molded Bolts en Molded Parts zijn de omgegoten versies van de Floating-kaarten (event De Smeltkroes).

**Gebouwde relics (8):** Counter (elke derde kaart kost 0), Floating Point (eerste Floating-kaart kost 0), Scrap Pouch (bewaart wat afkappen verliest en vuurt het af), Anchor Barrel (blok bij de start), Great Pot (max HP), Ink Well (eerste Ink of Read kost 0), Tally Counter (heel `++count` na elk gewonnen gevecht), Coin Mold (50% meer goud, afgerond met `Math.Round`).

**Oorspronkelijk ontwerp.** De tabellen hieronder zijn de eerste kaartenlijst. Flip, Voorsprong, Snelle Steek, Haakjes en Etiketmaker wachten op hun vijanden.

| Kaart | Kost | Effect | Concept |
| --- | --- | --- | --- |
| Zet op 1 | 1 | Zet de aanval van een vijand op 1 voor deze beurt | Toekenning |
| Voeg toe | 0 | +3 aan je volgende kaart | `+=` |
| Vlottende Slag | 2 | 4,5 schade; exact op een `double`, een `int`-doelwit kapt af tot 4 | `double` |
| Floating Point | 0 | `× 1.0`: je volgende aanval wordt een `double` (verbeterd: `× 1.5`) | Type van een expressie |
| Ink | 1 | `+ "1"`: tekst plakt aan je volgende aanval; tekst raakt geen getal | `string` en `+` |
| Read (zeldzaam) | 2 | `int.Parse(…)` op je volgende aanval. Ongeldige tekst geeft een `FormatException` en kost je de beurt | Parsen, ervaren in act 1 |
| Flip | 0 | Draai een `bool`-toestand om | `bool` |
| Split | 0 | `/ 2`, maar je volgende kaart slaat twee keer: `9 / 2` is twee keer 4 (verbeterd: `/ 2.0`, twee keer 4.5) | Integer deling |
| Voorsprong | 1 | Kracht +1, dan slaan | `++i` |
| Herstel | 1 | +6 HP op een doelwit naar keuze | Overflow (als je het doorhebt) |

| Relic | Effect | Concept |
| --- | --- | --- |
| Haakjes | Eén keer per gevecht: bepaal welk deel van een expressie eerst gerekend wordt | Operatorvoorrang |
| Restzak | Bewaart de rest van elke deling; bij 5 vuurt hij 5 schade af | Modulo |
| Vlottende Komma | Je eerste Vlottende kaart per gevecht kost 0 | `double` |
| Teller | Elke derde kaart die je speelt, kost 0 | Modulo en tellen |
| Etiketmaker | De Naamloze toont zijn echte etiket één beurt vooraf | Identifiers |
