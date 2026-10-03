# Act 2: De Drukkerij

Gebouwd op 3 oktober 2026. Act 2 dekt hoofdstuk 3 van Zie Scherp Scherper: tekst gebruiken in code. Het boek behandelt `char` (één teken, een Unicode-getal), `string` (een reeks `char`s), escape characters, strings samenvoegen en interpolatie, en vreemde tekens tonen. In het Engelse spel heet de act **The Print Shop**: een drukkerij vol losse letterblokjes, etiketten en berichten. Elk letterblokje is een getal, en dat is de kern van de act.

| Concept | Waar het in de game zit |
| --- | --- |
| `char` is een getal | Type Block, de letterkaarten |
| Een cijferteken is nog geen cijfer (`'0'` is 48) | Type Block |
| `string` plakt | Paper Golem, Ink |
| `Length` | Count Letters, de baas |
| String interpolatie | De baas |
| Unicode | Elite Effective Power |

## Gewone vijanden

| Vijand | Mechaniek | Wat je ontdekt |
| --- | --- | --- |
| Type Block | Zijn HP is één `char`: `'0'`. Op de balk staat een 0, maar hij heeft 48 HP, want `'0'` is 48. Schade trekt af van de code: na 6 schade staat er `'*'` (42). Hij sterft bij `'\0'`. | Een cijferteken is geen cijfer; een `char` is een getal |
| Paper Golem | Een gewone `int`, maar zijn aanvallen zijn tekst die hij plakt en dan pas omzet: `"1" + 2` is 12, de beurt erna `1 + 2` is 3. De intent toont het totaal. | Tekst plakt, getallen tellen op: dat zie je aan wat er op jou afkomt |

Daarnaast blijven vijanden uit act 1 in de pool, want de regels van eerdere acts blijven gelden.

## Elites

- **Effective Power** verhuist van act 1 naar hier: een bericht dat crasht vanaf 32 tekens. Plakken is de bedoeling, en `"2.5"` plakt drie tekens.
- **Y2K** (gebouwd op 3 oktober 2026): zijn HP is een jaartal als tekst, `"1997"`. Na elke beurt schrijft hij het opnieuw als `"19" + jaar`, met echte concatenatie: `"1998"`, `"1999"`, en dan `"19100"`. Wat je eraan plakte, is dan weg. Hij crasht vanaf 12 tekens, dus je moet hem in één beurt lang genoeg maken: een Strike plakt `"6"` (één teken), een Floating Strike drie keer `"2.5"` (negen tekens). De les: een `double` is lange tekst, en `"19" + 100` is geen 2000. Na middernacht is hij een teken langer en dus makkelijker; wie wacht, krijgt het ✗-paneel *Do not wait for midnight*. Count Letters werkt ook: zijn HP wordt 4 of 5, maar alleen tot zijn volgende beurt. Aanvallen 12, 14, 18. Het Codex-moment is de omslag zelf: `"19" + 100` werd `"19100"`.

## Baas: The Typesetter

Een figuur van de fabriek die zinnen zet met losse letters. Zijn HP is een zin, bijvoorbeeld `"THE MANUAL IS ALWAYS RIGHT"`. Tekst kan je niet doodslaan: elke treffer plakt eraan vast, zoals bij elke `string`. Met de nieuwe kaart **Count Letters** wordt zijn HP de `Length` van zijn zin: 26. Wie eerst slaat en dan telt, vecht tegen een langere zin.

Elke drie beurten zet hij een nieuwe zin met string interpolatie: `$"YOU HIT ME FOR {schade}. I WROTE IT DOWN, WORD FOR WORD."`, met de schade van jouw laatste beurt erin. Daarna is hij weer tekst, en moet je opnieuw tellen. De puzzel: tel op het juiste moment, en sla hard tussen twee zinnen in. Zonder Count Letters is er een tweede uitweg: plakken tot de zin 120 tekens lang is, dan crasht ze, net als bij Effective Power.

## Nieuwe kaarten

| Kaart | Wat ze doet | Concept |
| --- | --- | --- |
| Count Letters | Een tekstvijand wordt een `int` met de `Length` van zijn tekst als HP | `Length` |
| Letter A (zeldzaam, kost 2) | `+ 'A'` op je volgende aanval: `6 + 'A'` is 71. Tegen tekst plakt een `char`: `"40" + 'A'` is `"40A"`, en dat parset niet meer | `char` is een getal |

De letterkaart is bewust zeldzaam en duur: een `char` is meteen 65 of meer.

## Wat er verschuift

- **Act 1 wordt puur H2.** Ink en Read blijven in de pool van act 1 (tekst ervaren voor je hem benoemt), maar Effective Power verhuist naar act 2. Act 1 krijgt een tweede elite: **The Counter** (de weergaventeller van Gangnam Style, 2014). Hij begint 30 onder `int.MaxValue` en telt elke beurt 9 op; wie hem heelt of lang genoeg overleeft, ziet hem unchecked omklappen naar min twee miljard.
- **De Gieterij wordt act 3**, met dezelfde inhoud.
- **Codex:** String concatenation en String length gaan open vanaf act 2, Casting, Convert, Math.Round en Parsing vanaf act 3. Er is een nieuwe pagina **A char is a number** voor Type Block en de letterkaart. De pagina Integer overflow geldt nu voor `byte` én `int`, en String length ook voor Count Letters.
- **Er komt één nieuwe actplaat** via imagen.
