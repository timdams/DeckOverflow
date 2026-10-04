# The Control Room: regels voor een automaat

Afdeling ⑤ op de plattegrond, hoofdstuk 5 van Zie Scherp Scherper (beslissingen). Je speelt niet zelf: je stelt de regels op van een automaat en kijkt hoe hij vecht. De regels worden van boven naar onder bekeken, en de eerste die klopt, wint. Dat is een `if` / `else if`-keten, maar zo heet het pas in de Codex. Verder: [events van de motor](events.md), [ideeën en open vragen](ideeen.md), [todo](todo.md).

**Gebouwd op 4 oktober 2026** als echte afdeling in het spel, overgenomen uit [spike 8](../../../spikes/08-controlekamer/README.md). De spike bleef een los prototype op `/spike-8/` en verandert niet meer.

## De loop: een reeks puzzels

Vijftien gevechten na elkaar, elk een eigen puzzel, vrij te herspelen om beter te scoren. Geen run, geen map: het voorbeeld is Opus Magnum, niet Slay the Spire.

1. **Lees de vijand.** Zijn regels staan rechts, in gewone zinnen, met een korte typering eronder.
2. **Bouw je bord.** Sleep tegels uit de gereedschapsbak op je regels, met de muis of met een vinger (`shared/touch-drag.js`), of tik op een tegel en dan op een vakje. De volgorde verander je door te slepen of met ▲▼.
3. **Start en kijk.** Het duel speelt zet per zet; de regel die vuurt, licht op. Pauzeren, één zet, sneller (1×, 2×, 4×), en een tijdlijn om terug te spoelen naar elke zet.
4. **Lees de afloop.** Elke regel telt hoe vaak ze vuurde; na het duel krijgt een regel die nooit vuurde het label *nooit bereikt* (een regel erboven klopte altijd eerst) of *nooit waar*.
5. **Verbeter.** Twee scores, elk apart: beurten en regels. Een histogram toont waar je bord staat tussen alle winnende borden.

Je bord blijft per gevecht bewaard. Het volgende gevecht gaat open zodra je het vorige wint (voor de [superuser](../../wereld/backend.md#superuser) staat alles open, en `?level=sentry` springt naar één gevecht).

## Het scherm: twee kasten en een testcel

Beslist op 4 oktober 2026, met de [beeldtaal](../../wereld/beeldtaal.md). De Controlekamer is een echte controlekamer: je bedient een schakelkast en kijkt door een raam naar de testcel. **Gebouwd op 4 oktober 2026** (`ControlRoomPage`, `RuleBoard`, `control-room.css`); de stage zelf bleef dezelfde.

- **Links jouw schakelkast.** Elke regel is een plaat met een lamp, een stapnummer, *als* een voorwaarde *dan* een zet, en hoe vaak ze vuurde. Een vrije plaats is een gestippelde plaat. Eronder de gereedschapsbak: een metalen bak met twee vakken, voorwaarden en zetten. Een voorwaarde is een rond plaatje, een zet een hoekig.
- **Onder de kast het luik *onder de motorkap*.** Dicht is het een gearceerd luik; open toont het je bord als C#, en tijdens het duel licht de regel op die nu vuurt.
- **In het midden de testcel.** Het duel zie je door een raam met een dikke zwarte lijst, schroeven en een bordje met het gevecht en de beurt. Eronder de grote ronde startknop, de snelheden, en telwerken voor beurten en regels.
- **De tijdlijn is een ponsband:** per beurt een gaatje voor jou en een voor de vijand, met het nummer van de regel die vuurde (een streepje als er niets klopte). Wat nog moet komen, blijft een leeg gaatje: de band verklapt niets. Klik op een gaatje om naar die zet te springen, ook terug.
- **Rechts de kast van de vijand, verzegeld:** dezelfde platen en lampen, met zijn typering eronder. Een elite is oranje gemerkt en krijgt eronder een **dossier**: de naam van de bug, wie en wanneer ("goto fail; (Apple, 2014)"). Het verhaal zelf komt pas na de overwinning, in de uitslag en de Codex.
- **Tijdens het duel knipperen de twee kasten naar elkaar.** De lamp van de regel die vuurt, brandt, en je hoort de keten: een klikje per regel die bekeken werd en niet klopte, dan een hogere klik voor de regel die vuurt. Een regel die nooit bekeken wordt, blijft donker; het stempel *nooit bereikt* valt pas als het duel voorbij is.

## De natuurwetten

- **Een regel:** *als* een voorwaarde, eventueel met een tweede via *EN* of *OF*, eventueel omgedraaid met *NIET*, *dan* een zet. Elke beurt zet eerst de speler, dan de vijand.
- **Types, zoals in de Card Hall:** een automaat met tekst als HP verliest niets bij een klap: het getal plakt erachter, en bij zijn crashlengte valt hij om. Een automaat met een teller telt elke eigen zet op als een `byte`. Een automaat die afrondt (`Math.Round`) in plaats van afkapt, stuurt .5 naar het dichtste even getal. Verder heeft elke automaat een geheel type. Schade met een kommagetal wordt afgekapt (blok vangt een halve punt op als een hele). Een `int` lapt op tot zijn maximum; een `byte` kent alleen 255 en loopt daarna over, en wie zo op 0 belandt, valt door zijn eigen zet om. Schade stopt op 0.
- **Vier zetten**, voor elke automaat dezelfde, met eigen getallen: Mep (schade, dubbel als je opgeladen bent), Goed vasthouden (blok tot je volgende zet), Oplappen (herstel, beperkt aantal keer), Opwinden (je volgende Mep is dubbel). Vanaf gevecht 13 komen er twee bij, met de woorden van de Card Hall: **Omgieten (byte)**, een cast die nooit controleert (`(byte)600` is 88), en **Converteren (byte)**, `Convert.ToByte`, dat wel controleert: past de vijand niet, dan crasht hij en valt zijn volgende zet weg. Op het bord en in het log heet dat "crasht", niet "OverflowException". Op een vijand die al een byte is, verandert geen van beide iets, maar de regel klopte, dus de zet is weg.
- **Voorwaarden:** altijd, mijn HP <, vijand HP <, ik ben opgeladen, vijand is opgeladen, vijand heeft blok, elke n beurten. De woordenschat groeit per gevecht.
- **Een regel die klopt, wordt altijd uitgevoerd**, ook als de zet niets doet (Oplappen zonder herstellingen, Opwinden als je al opgeladen bent). Zo gedraagt een `if` zich ook.
- **Geen regel die klopt:** de automaat staat stil. **Na 40 beurten** is de shift voorbij, en dat telt als verlies.
- **Deterministisch, zonder RNG:** dezelfde regels geven altijd hetzelfde duel. Daarom kan de shell het hele duel vooraf uitrekenen (`DuelRecording`) en vrij terugspoelen, en kan een klassement later eerlijk vergelijken.

## De gevechten: drie lagen

De puzzels worden stelselmatig complexer, en de laatste laag vraagt kennis uit de vorige hoofdstukken:

1. **De basis van H5** (1 tot 5): de volgorde van een keten, vergelijkingen, de eerste echte bug.
2. **Logica** (6 tot 8): OF, NIET, en een elite die je alleen verslaat als je zijn regels leest.
3. **Terugblik op H2 tot H4** (9 tot 15): de types gedragen zich zoals in de Card Hall (`IntRules`, `ByteRules`, `TypedValue` uit Core). Afkappen, overflow, tekst en afronden beslissen hier welke regels werken, en de lessen spreken elkaar bewust tegen: tegen een `int` loont opwinden (de halve punt blijft), tegen tekst niet (`5.5` is langer dan `11`).

Afgestemd met de oplosser (`Solver`): in elk gevecht verliest "alleen meppen" nipt, en zijn er meerdere winnende borden. De tests in `LevelTests` en `HistogramTests` pinnen dat vast.

| # | Vijand | Zijn regels | Wat je ontdekt | Winnende borden (1 / 2 / 3 regels) |
| --- | --- | --- | --- | --- |
| 1 | De Stempelaar | altijd → Mep | Herstellen op tijd, en de volgorde: de herstelregel moet boven "altijd" | 0 / 1 / (2 vakjes) |
| 2 | De Pers | opgeladen → Mep; altijd → Opwinden | Zijn regels lezen: blokken als hij opgeladen is | 0 / 1 / 22 |
| 3 | De Metronoom | elke 3 beurten → Goed vasthouden (20); altijd → Mep | Meppen op een schild is verspild: laad dan op | 0 / 14 / 1156 |
| 4 | De Hersteller | HP < 20 → Oplappen; jij opgeladen → vasthouden; altijd → Mep | Na zijn laatste herstelling blijft zijn bovenste regel vuren en doet hij niets meer | 0 / 1 / 49 |
| 5 | goto fail (elite, echte bug) | regel 3 is een kopie van regel 2 zonder voorwaarde | Alles onder regel 3 wordt nooit bekeken | 0 / 2 / 132 |
| 6 | De Schildwacht | opgeladen → Mep; jij opgeladen OF jij blok → Opwinden; altijd → Mep | OF: twee redenen, één reactie. Een schild lokt een dubbele klap uit | 0 / 5 / 309 |
| 7 | De Dwarsligger | opgeladen → Mep; NIET jij blok → Mep; altijd → Opwinden | NIET: hij slaat zolang jij geen schild hebt | 0 / 3 / 213 |
| 8 | Knight Capital (elite, echte bug) | bovenaan een oude regel "opgeladen → Opwinden"; jij opgeladen EN hij < 20 → Opwinden; … | De oude regel wordt weer bereikt, en daarna doet hij niets anders meer | 0 / 3 / 295 |
| 9 | De Snoeier (H2: afkappen) | altijd → Mep. Jouw Mep is 2.5, zijn HP een `int` | Elke klap wordt 2, maar opgeladen is het 5.0 en valt er niets af: eerst opwinden loont. Met een Mep van 2.0 winnen 20 keer minder borden | 0 / 6 / 657 |
| 10 | De Overbelaster (H2: overflow) | altijd → Mep (30). Jouw HP is een `byte` van 250, Oplappen +40 | Zonder oplappen wint niets; wie oplapt boven 215 loopt over (220 + 40 → 4) | 0 / 3 / 311 |
| 11 | De Telex (H3: tekst) | altijd → Mep (8). Zijn HP is de tekst `"40"`, crash bij 18 tekens; jouw Mep is 5.5 | Een klap plakt (`"40" + 5.5` is `"405.5"`). `5.5` is drie tekens, opgeladen `11` maar twee: opwinden is hier slecht. Met een Mep van 5.0 wint niets. "vijand HP <" bestaat niet tegen tekst | 0 / 3 / 170 |
| 12 | De Schatter (H4: afronden) | elke 2 beurten → vasthouden (1); altijd → Mep (7). Hij rondt af met `Math.Round`; jouw Mep is 3.5 | 3.5 wordt 4, maar achter zijn schild wordt 2.5 een 2: naar het dichtste even getal. Afronden geeft twee keer zoveel winnende borden als afkappen | 0 / 25 / 3340 |
| 13 | De Reus (H4: casting, H2: modulo) | altijd → Mep (4). 600 HP als `int`; jouw Mep is 10 | Wegmeppen lukt niet. `(byte)` houdt `hp % 256` over: meteen omgieten maakt 600 tot 88, pas onder 500 wordt het 244 en verlies je. Een regel "altijd → omgieten" bovenaan blijft vuren als hij al een byte is | 0 / 0 / 735 |
| 14 | De Titaan (H4: Convert) | elke 3 beurten → Opwinden; opgeladen → Mep; altijd → Mep (4). 480 HP | Zolang hij boven 255 staat, laat `Convert.ToByte` hem crashen en valt zijn (opgeladen) zet weg. Onder 256 lukt de conversie gewoon. Omgieten blijft een optie | 0 / 0 / 112 |
| 15 | Dag 248 (elite, echte bug) | NIET mijn teller < 240 → Mep (9). Zijn teller is een `byte` die elke beurt optelt vanaf 240 | Na 16 beurten loopt de teller over naar 0 en klopt er niets meer: hij valt stil. Wie dat leest, houdt het tot dan uit | 0 / 2 / 264 |

De histogrammen tellen borden tot 3 regels zonder EN, OF of NIET; met die operatoren zijn er nog meer oplossingen.

### Echte bugs

- **goto fail (Apple, 2014):** één lijn stond twee keer in de code, zonder voorwaarde; de belangrijkste controle eronder werd overgeslagen.
- **Dag 248, de Boeing 787 (2015):** een teller in de stroomregeling liep over na 248 dagen onafgebroken stroom; dan vielen alle generatoren tegelijk uit. Het is nooit gebeurd, omdat de toestellen geregeld volledig uitgeschakeld moesten worden.
- **Knight Capital (2012):** een oude functie (Power Peg) werd al jaren niet meer gebruikt. Een hergebruikte vlag maakte ze weer bereikbaar op één server die de update miste; in 45 minuten verloor het bedrijf 440 miljoen dollar. Geen doden, zoals [de visie](../../visie.md) vraagt.

## Wat ze met de wereld deelt

- **Codex uit de Card Hall:** *Afkappen naar int*, *Overflow*, *Strings samenvoegen*, *De lengte van een string*, *Math.Round*, *Casting* en *Convert* gaan ook hier open (Convert alleen als de conversie lukt). Een crash opent hier geen pagina: het woord *exception* hoort bij H10, in H4 heet het crashen, zoals in het boek; met dezelfde teksten (het doelwit heet via `enemy.<key>`).
- **Codex (H5):** *If - else if* (een hogere regel won terwijl een lagere ook klopte), *Relationele operators* (een vergelijking met `<` of `==` vuurde) en *Logische operators* (een regel met EN, OF of NIET vuurde). Telkens met de getallen van jouw eerste moment. Na een overwinning op een elite staat het verhaal van de bug in de uitslag.
- **✗-register:** *Geen dode code laten staan* (winnen met een regel die nooit bekeken werd), *Niet stilstaan* (winnen terwijl je automaat een beurt stilstond), *Niet stoppen bij hoofdstuk 5* (het laatste gevecht), en verborgen: *Geen overuren maken* (de shift laten aflopen) en *Niet zonder kras terugbrengen* (winnen zonder HP te verliezen).
- **Voortgang:** per gevecht je beste beurten, je beste aantal regels en je laatste bord (`PlayerProgress.ControlRoom`). Nog alleen in de browser, zie [todo](todo.md).
- **Ontgrendelen:** de laatste baas van de Card Hall geeft de sleutel. Voorlopig staat de Controlekamer op *binnenkort* (beslist op 4 oktober 2026): wie de sleutel heeft, krijgt een tease maar nog geen toegang, tot ze af is (zie [de wereld](../../wereld/README.md#ontgrendelen) en [todo](todo.md)). De [superuser](../../wereld/backend.md#superuser) kan er al in.

## Onder de motorkap

Het luik onder je kast toont je bord als C#: een `if` / `else if`-keten. Een "altijd" onderaan wordt `else`; een "altijd" ergens anders wordt `else if (true)`, zodat je ziet waarom alles eronder nooit loopt. Methodenamen blijven C# (`Whack()`), de rest volgt de taal van het spel.
