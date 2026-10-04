# Woordenlijst: termen en toon uit *Zie Scherp Scherper*

De Nederlandse spelteksten (`nl.json`) volgen de woorden en de toon van het handboek, zodat studenten in het spel herkennen wat ze in de cursus lazen. Alles hieronder komt uit de tekst zelf, met het bestand erbij.
Bron: `C:\Users\damst\KoofrNew\PROGPROJECTS\cursus\ziescherpscherper\content` (paden hieronder zijn relatief daartegen).

## Toon en stijl

- **Aanspreking: altijd `je`/`jij`**, nooit `gij`. `u` komt één keer voor, als grap in de goto-politie: "Hierbij wil ik u attent maken op een belangrijke, onbeschreven, wet" (`1_csharpbasics/0_csharpessentials.md`), en meteen daarna weer je: "Enneuh, ik hou je in't oog hoor!".
- **Ik-verteller, heel informeel.** De auteur praat zelf: "Ik besteed verderop een heel apart hoofdstuk..." (`1_csharpbasics/1_datatypes.md`), "Hoeveel krijg je van me? **0.0 euro, MUHAHAHAHA!!!**" (`1_csharpbasics/2_expressies.md`). Personages: de verteller (trivia), de voorman ("Zet je helm op en let alsjeblieft goed op"), Stagiair Steven (AI-fouten).
- **Vlaams Nederlands.** "neen" (identifier-tabel, `0_csharpessentials.md`), "ambetant" ("het kan namelijk subtiel en ambetant worden", `2_expressies.md`), "trivia om op café of Discord mee te pochen" (`1_datatypes.md`), "een quotering van 4.5 op 10" en "wat je op de lagere school geleerd hebt" (`3_data/4d_afronden.md`), "op een deftige manier verder" (`20_exceptions/0_exceptionhandling.md`), "Wat een dramatische start zeg". Ook *lijn* (code-lijn) naast *regel*, en Belgische context ("Op een Belgische computer wordt `9.81` gelezen als 981", `3_data/4b_inputconverten.md`).
- **Humor**: popcultuur en zelfspot. "*Aah, Data, een geliefkoosd personage uit Star Trek.* ... *Ahum, sorry. I got carried away.*" (`3_data/4_converteren_casting.md`); "Een heel verhaal, dit wordt mooi / Is dit een haiku?" (comment-voorbeeld). Engelse uitroepen tussendoor: "But wait... it gets worse!", "better safe than sorry", "*I wish*".
- **Hoe een regel klinkt**: kort, vetgedrukt, vaak met een gevolg erbij. "**Statements eindigen steeds met een puntkomma**", "**De types die je in je expressies gebruikt bepalen ook het type van het resultaat.**" (met "Lees deze zin enkele keren luidop voor"), "**De ondergrens telt wél mee, de bovengrens niet.**" (`3_data/random.md`), "**Let hier op!**". Waarschuwingen in de vorm "Let goed op:", "Opletten geblazen dus", "Goed opletten nu.".

### Engelse vaktermen: wat blijft, wat wordt vertaald

| Engels | In het boek | Lidwoord / voorbeeld |
|---|---|---|
| keyword | **keyword** (143x), *sleutelwoord* komt nooit voor | "gereserveerde C# keywords", "het `const` keyword" |
| casting / cast | **casting**, **casten**, "een *cast*"; omzetten als gewoon werkwoord | "aan casting doen", "omgezet ("gecast")" |
| narrowing / widening | **narrowing**, **widening** (Engels, cursief/vet) | "aan *narrowing* doen", "letterlijk het versmallen van de data" |
| parsing | **parsing**, **parsen** | "De variabele **parsen** met de `Parse()`-methode" |
| exception | **exception** én **uitzondering**, door elkaar | "een exception", "de exception", "uitzonderingen (**exceptions**)", "een uitzondering **opwerpen**" |
| overflow | **overflow** | "Dit heet **overflow**: de teller loopt over" |
| string | **string** (blijft), naast *tekst* | "een `string`", "de string" |
| compile | **compileren**, **de compiler** (81x) | "de compiler weigert", "gecompileerd worden" |
| compile error | **compilerfout** (6x), ook *compileerfout*, *compilatiefout*, *compiler-fout* | "geeft een compilerfout" |
| error message | **foutboodschap** (32x), ook *foutmelding* | "een dikke foutboodschap" |
| crash | **crashen**, "crasht" | "Het programma crasht niet." |
| bug | **bug** | "een bug", "vreemde bugs" |
| loop | **loop** (330x), *lus* zelden (21x); ook *herhaling*, *iteratie* | "de loop", "een loop", "oneindige loop" |
| array | **array** (de-woord) | "een array", "de array" (nooit "het array") |
| statement | **statement** | "Statements eindigen steeds met een puntkomma" |
| method | **methode**, meervoud **methoden** (223x, *methodes* 4x) | "de `Next` methode" |
| library | **bibliotheek** | "de Math-bibliotheek", "de Convert-bibliotheek" |
| random / seed | **random**, *willekeurig*; **seed** | "Random getallen genereren", "de zogenaamde *seed*" |
| debug / breakpoint | **debuggen**, **breakpoint** | |
| operator / operand | **operator**, **operand** (Engelse spelling, niet *operatoren* behalve één keer) | "binaire operator", "unaire operators" |
| expression | **expressie** | "booleaanse expressie" |

## Code in het boek

- **Namen zijn Nederlands in camelCase**: `int leeftijd;`, `string leverAdres;`, `bool isGehuwd;` (`1_csharpbasics/1b_variabelen.md`), `temperatuurGisteren`, `hoofdMeting`, `aantalBussen`, `levens`, `schade`. Er sluipt soms Engels in (`result`, `myGen`, `numVal`, `converted`, `input`), maar Nederlands is de regel. Constanten in ALLCAPS met liggend streepje: `const double G_AARDE = 9.81;` (`3_constanten.md`).
- **Comments zijn Nederlands, kort, vaak met de uitkomst erbij**:
  - `Console.WriteLine(result); //We tonen resultaat op scherm: 15` (`0_csharpessentials.md`)
  - `int otherResult = 3.1 / 45.2; //dit is fout!!!` en `double getal2 = 2.0; //slim he` (`2_expressies.md`)
  - `int b = (int)2.9;   //2 (!)` (`3_data/4d_afronden.md`)
- Uitvoer en strings in de voorbeelden zijn ook Nederlands: `"Geef een getal:"`, `"Verkeerde invoer!"`.

## Begrippen

| Concept (Engels, in het spel) | Term in het boek | Hoofdstuk/bestand | Opmerking |
|---|---|---|---|
| variable | **variabele** | `1_csharpbasics/1b_variabelen.md` | "een plekje in het geheugen"; vergeleken met een **kluisje** met naambordje |
| identifier | **identifier** (ook "een naam oftewel **identifier**") | `0_csharpessentials.md`, `1b_variabelen.md` | regels: hoofdlettergevoelig, geen keywords, "liggend streepje" |
| declare / assign | **declareren**, **waarde toekennen**, **toewijzen** | `1b_variabelen.md` | "de **toekennings-operator** (`=`)", "van rechts naar links" |
| keyword | **keyword**, "reserved keywords" | `0_csharpessentials.md` | nooit *sleutelwoord* |
| literal | **literal** | `1b_variabelen.md` | "expliciet neergeschreven waarden in je code" |
| data type | **datatype** | `1_datatypes.md` | "een doosje waar maar één soort data in past" |
| int, byte, double, decimal, float | zelfde namen; **geheel getal**, **kommagetal** | `1_datatypes.md` | nooit *integer* als Nederlands woord in lopende tekst, wel "integers" soms (`4_converteren_casting.md`) |
| range | **bereik** | `1_datatypes.md`, `3_data/4c_math.md` | "Bereik (waardenverzameling)" |
| MaxValue / MinValue | `int.MaxValue`, "maximum- en minimumwaarde", "het grootste/kleinste `int`" | `3_data/4c_math.md` ("Bereik in code weten") | |
| integer overflow (byte wraps) | **overflow**, "de teller loopt over", "Over de grens gaan" | `3_data/4c_math.md`, `1_datatypes.md` | byte: "tel je 1 op bij een `byte` die op 255 staat, dan springt die zonder foutmelding terug naar 0" |
| expression | **expressie** | `1_csharpbasics/2_expressies.md` | "sequenties van bewerkingen die op 1 resultaat uitkomen" |
| operator / operand | **operator**, **operand**; binaire/unaire/ternaire operator | `2_expressies.md` | |
| operator precedence | **volgorde van berekeningen**; ook "volgorde van bewerkingen" | `2_expressies.md`, `4_beslissingen/1_logic_and_relationsoperator.md` | "Haakjes" eerst |
| integer division | geen eigen naam: "een `int` door een `int` deelt" | `2_expressies.md` ("But wait... it gets worse!") | "Je bent echter alle informatie na de komma kwijt." |
| truncation | **afkappen**, "kapt ... af", **truncatie** | `2_expressies.md`, `3_data/4d_afronden.md` | "het rondt dus niet af"; cast kapt af "richting nul" |
| modulo / remainder | **modulo**, **rest**, "rest na deling" | `2_expressies.md` | "de gehele rest" |
| ++ / -- (prefix/postfix) | **verkorte notatie** ("verkorte operator notaties"), "met 1 verhogen/verlagen" | `2_expressies.md` | prefix/postfix worden niet zo genoemd: "operator links / achter de operand" |
| constant | **constante**, `const` | `1_csharpbasics/3_constanten.md` | ook **magic number** (Engels) |
| bool, true/false | **bool** / **boolean**, **booleaanse expressie**; `true`/`false`, in tekst **waar** / **niet waar** | `1_datatypes.md`, `4_beslissingen/0_if.md` | |
| ! (not) | **niet-operator**, *NIET*, "inverteert" | `4_beslissingen/1_logic_and_relationsoperator.md` | ook EN (`&&`), OF (`||`), **logische operators**, **relationele operators** |
| string | **string**, **tekst** | `2_tekst/5_chars_strings.md` | "Een `string` is een reeks van 0, 1 of meerdere `char`-elementen." |
| string concatenation | **samenvoegen**, **aan elkaar plakken**, **geconcateneerd** / **concatenatie** | `2_tekst/6_stringInterpolation.md` | "De volgorde ... bepaalt wat de uitvoer zal zijn" |
| string length | `.Length`, "het aantal tekens" | `2_tekst/5_chars_strings.md`, `6_stringInterpolation.md` | "achter `Length` komen geen haakjes, het is geen methode" |
| character | **karakter** én **teken** (beide vaak) | `2_tekst/5_chars_strings.md` | "Een **enkel karakter** (cijfer, letter, leesteken, enz.)" |
| char is a number | **UNICODE** (hoofdletters), **ASCII**, "UNICODE-waarde" | `5_chars_strings.md`, `6_stringInterpolation.md` ("Optellen van char variabelen"), `3_data/4_converteren_casting.md` ("Een cijferteken is nog geen cijfer") | "voor een computer zijn `char`-variabelen niet meer dan getallen met een speciale betekenis" |
| casting | **casting**, **casten**, "een cast" | `3_data/4_converteren_casting.md` | "Appelen en peren" |
| explicit / implicit | **expliciet** / **impliciet** ("impliciete casting"-error) | idem | |
| narrowing / widening | **narrowing** ("versmallen"), **widening** ("verbreed") | idem | |
| Convert | **conversie**, **converteren**, "de **Convert**-bibliotheek", `Convert.ToInt32` | idem | "een zwarte doos"; `Convert.ToByte` staat niet in het boek |
| banker's rounding | **bankers rounding**, "afronden naar het **dichtstbijzijnde even getal**" | `3_data/4d_afronden.md` | `Convert.ToInt32(4.5)` geeft 4 |
| rounding / Math.Round | **afronden**, `Math.Round`, `MidpointRounding.AwayFromZero` | `3_data/4d_afronden.md`, `3_data/4c_math.md` | "Afkappen is niet hetzelfde als afronden" |
| decimals (digits after the point) | **cijfers na de komma**, "het deel na de komma" | `4d_afronden.md`, `4_converteren_casting.md` | *decimalen* komt maar 1x voor |
| parsing | **parsing**, **parsen**, `int.Parse` | `3_data/4_converteren_casting.md`, `3_data/4b_inputconverten.md` | "enkel bruikbaar om strings om te zetten" |
| exception | **exception** / **uitzondering**, "opwerpen", "opvangen", "afhandelen" | `20_exceptions/0_exceptionhandling.md` | `FormatException` staat erin; **`OverflowException` niet** |
| compile error vs runtime error | **compilerfout** ("de compiler weigert") vs **crashen** / exception "tijdens de uitvoer" | `0_intro/4_fouten.md`, `1b_variabelen.md`, `4c_math.md` | het woord *runtime error* komt niet voor |
| division by zero | "deel je twee gehele getallen door nul, dan crasht je programma" | `3_data/4c_math.md` | `DivideByZeroException`; bij double: ∞ en `NaN` |
| ternary operator | **ternaire operator** | `4_beslissingen/0_if.md` | "werkt met een vraagteken en een dubbelpunt" |
| random | **random**, **willekeurig**, "Random-generator", `Next`, **seed** | `3_data/random.md` | "De ondergrens telt wél mee, de bovengrens niet." |
| method | **methode** / **methoden**, **parameters**, **returntype**, **methode-signatuur**, aanroepen/oproepen | `6_methoden/0_intromethods.md` | "herkenbaar aan de ronde haakjes achteraan" |
| array | **array**, **index**, **element**, **lengte** | `7_arrays/1_ArraysBasics.md` | "een verzameling waarden van hetzelfde datatype"; `IndexOutOfRangeException` |
| loop | **loop**, **herhaling**, **iteratie**, **conditie**, "oneindige loop" | `5_herhalingen/0_loops_intro.md` | "while loop" zonder koppelteken |
| if / else | **if**, **beslissingen**, **vertakken** (*branching*) | `4_beslissingen/0_if.md` | "als dit waar is doe dan dat" |

## Opvallende formuleringen

Kort genoeg om in het spel of de Codex te hergebruiken:

- "C# **kapt het cijfergedeelte na de komma gewoon af** (dat heet *truncatie*), het rondt dus niet af." (`1_csharpbasics/2_expressies.md`)
- "**Er zal `4` op het scherm verschijnen!** (niet `4.5` daar dat geen `int` is)." (idem)
- "**Ik besef dat hierbij data verloren kan gaan** ... "Ik draag de volledige verantwoordelijkheid voor de gevolgen hiervan verderop in het programma."" (`3_data/4_converteren_casting.md`, over casting)
- "*het is goed, je mag informatie weggooien*" (idem, wat je met een cast tegen de compiler zegt)
- "Je kan geen appelen in peren veranderen zonder magie" (idem)
- "Dit heet **overflow**: de teller loopt over, zoals de kilometerteller van een oude auto die na 999999 opnieuw vanaf 0 begint. Je programma crasht niet, waarschuwt niet" (`3_data/4c_math.md`)
- "255 is sneller bereikt dan je denkt" (`1_csharpbasics/1_datatypes.md`, over `byte`)
- "Doe je niets, dan krijg je bankers rounding, en dat is zelden wat je bedoelde." (`3_data/4d_afronden.md`)
- "Een crash is vervelend, maar wijst je tenminste de exacte regel aan waar het misliep." (`3_data/4c_math.md`)
- "voor een computer zijn `char`-variabelen niet meer dan getallen met een speciale betekenis." (`2_tekst/5_chars_strings.md`)
- "Zodra een `string` meedoet, is het resultaat een `string`. Haakjes gaan voor." (`2_tekst/6_stringInterpolation.md`, bijschrift)
- "Je code is niet stuk, je hebt gewoon een andere vraag gesteld dan je dacht." (`3_data/4c_math.md`)
- "Zonder variabelen ben je aan het programmeren aan een programma dat ... hoegenaamd niets kan onthouden." (`1_datatypes.md`)

## Keuzes voor het spel

Wat `nl.json` daarbovenop vastlegt, zodat elke nieuwe tekst hetzelfde klinkt:

- **Code blijft C#.** Keywords, types en methoden (`int`, `byte`, `Convert.ToByte`, `int.Parse`) worden nooit vertaald. Comments in Codex-code zijn Nederlands, net als lokale namen die alleen in het voorbeeld staan (`aanval`, `schade`, `helft`). Namen die de motor zelf invult of in een aanval toont (`hp`, `block`, `cards`, `turn`, `isSolid`, `shadow`, `count`) blijven Engels, anders botst het voorbeeld met wat de speler in het gevecht ziet.
- **Getallen in codenotatie**: `3.5`, nooit `3,5`, en `2147483647` zonder duizendtallen. Zo ziet een `double` er in het spel uit zoals in je code.
- **Vaste woorden**: schade, blok, HP (levenspunten voluit), energie, goud, beurt, gevecht, baas, kampvuur, deck, run, seed, relic(s). Afkappen (nooit "naar beneden afronden") en afronden apart houden. Overflow en "de teller loopt over" voor een byte die rondgaat. Crashen en een exception "opwerpen". Een compilerfout: "dat compileert niet".
- **Omgieten** is het spelwoord voor casten (zoals spike 2); de Codex geeft het de echte naam: casting.
- **Kaartnamen** zijn Nederlandse woordspelingen in de sfeer van de montagehandleiding, geen letterlijke vertaling. Kaarten met "Floating" heten "Zwevende …" (een `float` is een zwevendekommagetal). Hoofdletters zoals in gewone Nederlandse zinnen: "Zwevende bouten", niet "Zwevende Bouten". Namen van vijanden en plekken met een lidwoord krijgen wel een hoofdletter: "De Teller", "De Vatenvallei".
- **Namen van echte bugs** blijven zoals ze bekend zijn: Y2K, Effective Power, Level 256.
- **Toon**: je/jij, kort, droog, licht Vlaams ("neen", "kuisen", "Hou je kleingeld"), zonder het aan te dikken.
