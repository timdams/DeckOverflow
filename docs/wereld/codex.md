# De Codex

De Codex is de brug van spel naar cursus, en hij is nooit verplicht. Een pagina opent pas als een regel een gevecht echt heeft beslist, niet bij de eerste ontmoeting.

Een Codex-pagina heeft vier lagen, die de speler zelf openklikt:

1. **Wat er gebeurde.** Een korte replay van het moment: "Je heelde de golem met 6 en zijn HP klapte naar 0."
2. **De naam.** "Dit heet integer overflow."
3. **Onder de motorkap.** Hier verschijnt voor het eerst echte C#: drie tot vijf regels die precies dat moment nabootsen.
4. **Verder lezen.** Link naar het hoofdstuk in Zie Scherp Scherper.

Wie een pagina leest, krijgt niets extra. De beloning zit in de volgende run: je weet nu iets wat je vijand niet verwacht.

**Zo werkt het nu (3 oktober 2026).** Tijdens een gevecht onthoudt de motor per regel het eerste moment waarop ze iets deed, met de getallen erbij. Als het gevecht voorbij is, gewonnen of verloren, gaan die pagina's open. De getallen vullen "wat er gebeurde" en de code, zodat de pagina jouw moment naspeelt: `byte hp = 250; hp += 6; // 0`. Een pagina heeft een minimale act: Omgieten voel je in act 1, maar "Casting" en "Parsing" openen pas in act 3, bij het hoofdstuk waar ze in het boek staan. Open pagina's bewaart de browser over runs heen; later komt dat in Supabase.

| Pagina | Hoofdstuk | Gaat open bij |
| --- | --- | --- |
| Variables | H2 | een bewuste intent die met jouw blok, kaarten of energie rekent |
| Identifiers | H2 | een kaart op The Nameless die weigert omdat zijn naam niet klopt (`Shadow`, `2shadow`, `sha-dow`) |
| Integer truncation | H2 | schade met decimalen op een `int` |
| Integer division | H2 | Split, of een bewuste intent met `/` |
| Modulo | H2 | een treffer op de Rhythm Turtle, of Remainder |
| ++ and -- | H2 | een Tighten-kaart, de aanval van de Twin Shooters, of de heling van de Tally Counter |
| Booleans | H2 | de Bool Ghost omdraaien, met een treffer of Flip |
| Integer overflow | H2 | helen tot een `byte` omklapt |
| Operator precedence | H2 | de Rekenmeester verslaan, of een aanval die Move the Brackets omschreef |
| Constants | H2 | Wrong Label of Remainder op een vijand wiens aanval `const` werd |
| String concatenation | H3 | Ink, een treffer op The Label of Effective Power, of Y2K die `"19" + 100` schrijft |
| String length | H3 | Effective Power, Y2K of de Typesetter laten crashen, of Count Letters |
| A char is a number | H3 | schade op Type Block, of Letter A |
| Casting | H4, vanaf act 3 | omgieten, ook de baas die zichzelf omgiet |
| Convert | H4, vanaf act 3 | Measure Twice |
| Math.Round | H4, vanaf act 3 | de Rounder |
| Parsing | H4, vanaf act 3 | Read of Read the Label |
| (Card Hall-pagina's) | H2 tot H4 | ook in de Controlekamer: afkappen (De Snoeier), overflow (De Overbelaster, Dag 248), samenvoegen en lengte (De Telex), Math.Round (De Schatter), casting (De Reus), Convert (De Titaan, alleen een geslaagde conversie). Exceptions niet: in H4 heet het crashen |
| If - else if | H5 | Controlekamer: een regel van jou vuurt terwijl een regel eronder ook klopte |
| Relational operators | H5 | Controlekamer: een regel van jou met `<` of `==` vuurt |
| Logical operators | H5 | Controlekamer: een regel van jou met EN, OF of NIET vuurt |
| While en do while | H6 | Lopende Band: een poort ziet hetzelfde product een tweede keer |
| For | H6 | Lopende Band: een teller is klaar |
| Nested loops | H6 | Lopende Band: een teller is klaar terwijl een andere nog telt |
| Infinite loop | H6 | Lopende Band: dezelfde toestand komt terug (de Zune op dag 366, of je eigen band). Ook een band die niet lukt, opent de pagina |
| (Card Hall-pagina's) | H2 tot H4 | ook op de Lopende Band: modulo, overflow (Level 256), een int delen door een int (de Splijter) en lengte (het Etiket) |
| Exceptions | H10 | nog nergens: in H2 tot H4 heet het crashen, zoals in het boek, en een crash opent deze pagina niet (beslist op 4 oktober 2026). Ze wacht op een afdeling voor H10 |

De laatste laag linkt naar de juiste pagina in de [online versie van het boek](https://timdams.github.io/ziescherpscherper/content/README.html), waar het kan met een anker (bv. `#conversie`).

## De Codex is het boek

Beslist op 3 oktober 2026. De Codex is geordend volgens Zie Scherp Scherper, niet volgens de spelvormen: een tabblad per hoofdstuk, H2 tot H18. Elke afdeling kan pagina's vullen. Deling van gehele getallen voel je in de deckbuilder en later op de Lopende Band; `if` en `else if` komen uit de Controlekamer. Zo is de Codex het ene ding dat in elke spelvorm hetzelfde blijft, en volgt hij het boek dat de student in de les gebruikt.

**De Codex is ook de onderdelenlijst.** De mastery op de [fabrieksplattegrond](README.md#mastery-de-onderdelenlijst) en de Codex zijn hetzelfde: concepten per hoofdstuk, met drie toestanden.

| Onderdelenlijst | Codex |
| --- | --- |
| In de zak | Silhouet (`???`) |
| Uitgepakt | Pagina open: je voelde de regel |
| Gemonteerd | Stempel op de pagina: je gebruikte de regel bewust, in meerdere runs of in twee spelvormen |

Na de onthulling toont een pagina ook waar je de regel voelde, met kleine icoontjes per afdeling. Dat is de transfer die we zoeken: dezelfde regel in een andere vorm herkennen. Stempel en icoontjes wachten tot er een tweede spelvorm in het spel zit.

**Teasen met mate.**

- Voor de onthulling staan H2 tot H4 open, plus elk hoofdstuk waarin al een pagina openging (Exceptions, H10, kan vroeg opduiken). De rest is dichtgeniet: alleen een nummer, geen naam, geen spelvorm, geen aantal pagina's.
- Eén tabblad laat iets lekken: bij H5 steekt een hoekje uit met een regel zoals in een handleiding, `if … then …`. Meer niet.
- De teller telt alleen open tabbladen: "3 van 10 pagina's", nooit "3 van 87". Een album van 87 lege vakjes motiveert verzamelaars, maar ontmoedigt net de zwakkere studenten.
- Bij de onthulling springen de nietjes eruit, en de tabbladen van de afdelingen die openkomen, vouwen open.
- De Codex vertelt nooit dat er andere spelvormen komen. Hij mag laten zien dát het boek dikker is, niet hoe het verder gaat.

**Zien dat er meer staat.** Een open pagina is een stapeltje van vier bladen. Zolang je ze niet allemaal omsloeg, zie je de randen van de bladen eronder, een ezelsoor en een teller (`1/4`), en de knop om verder te lezen is de meest opvallende op de pagina. Een tabblad met een half gelezen pagina krijgt een stip. Het spel onthoudt per pagina hoe ver je las. Dit is een herinnering, geen beloning: lezen levert niets op.

**Een mini-animatie van jouw moment.** Het eerste blad toont in de beeldtaal van het gevecht wat er gebeurde, met jouw getallen: een teller die van 250 oprolt tot 255 en omklapt naar 0, een schaar die `.5` van `2.5` knipt, `30 / (5 + 1)` dat uitrekent tot 5. Met ↻ speel je hem opnieuw af; wie minder beweging wil (systeeminstelling), ziet meteen het eindbeeld.

## Open vragen

## Codex

- [ ] **Een pagina voor `++`.** De Tally Counter heelt `++count`, maar daar opent geen pagina. Een pagina "Increment" (H2) kan opengaan bij de eerste heling.
- [ ] **Stempels voor gemonteerd**: de definitie staat vast (zie [mastery](README.md#mastery-de-onderdelenlijst)), het bouwen nog niet.
- [ ] **Waar je de regel voelde**: icoontjes per afdeling op een pagina, zodra er een tweede spelvorm meedoet.
