# The Control Room: events

De events van een duel en wat de stage ermee doet. De algemene afspraak (type, platte velden, onbekende events negeren) staat in [architectuur.md](../../architectuur.md#eventcontract). Een nieuw event komt hier, in `DuelEvent.cs`, in de handlers van `control-room/stage/stage.js` en in het log van `ControlRoomPage`.

Anders dan in de Card Hall rekent de shell het hele duel vooraf uit (`DuelRecording`): per zet een lijst events en een momentopname (`DuelSnapshot`). De stage speelt de events af en krijgt daarna de momentopname als waarheid (`sync`). Bij terugspoelen krijgt ze alleen de momentopname. In een verborgen tabblad slaat ze de animaties over, en laadt de stage niet, dan speelt het duel zonder beeld verder.

| Event | Velden | Wat de stage ermee doet |
| --- | --- | --- |
| `TurnStarted` | turn | Niets: de beurt staat op het bordje van de testcel, in de shell |
| `RuleFired` | side, ruleIndex, move | Een klikje per regel boven deze (bekeken, klopte niet), een hogere klik voor deze, en een kort knikje; de shell laat de regel oplichten op het bord |
| `NoRuleMatched` | side | "..." boven het hoofd: de automaat staat stil |
| `ValueTruncated` | target, before, after | Het kommagetal verschijnt grijs, dan "(int) n": wat na de komma stond, valt weg |
| `ValueOverflowed` | side, before, added, after, repairsLeft | De balk loopt vol tot 255, bevriest, en klapt om naar het kleine getal |
| `ValueRounded` | target, before, after | Het kommagetal verschijnt grijs, dan "Math.Round n" |
| `TextAppended` | target, before, added, after | "+ 5.5" zweeft op, de tekst in de balk groeit, de balk loopt vol naar de crashlengte |
| `TextCrashed` | target, length, limit | Het hele beeld schudt, "te lang: crash" |
| `CounterOverflowed` | side, before, after | "255 + 1 → 0" boven het hoofd, bevriezen, glitch; de teller onder de balk springt naar 0 |
| `TypeChanged` | target, from, to, before, after, wrapped, method | Nieuwe badge en tint; een cast die overloopt, bevriest eerst en klapt dan om. "(byte) 88" |
| `TypeUnchanged` | target, kind | Grijs "al een byte": de regel vuurde, er verandert niets |
| `ConversionCrashed` | target, value | Glitch, schudden, "crasht!" (geen "exception": dat woord komt pas in H10) |
| `MoveSkipped` | side | Grijs "zet valt weg", een wankel knikje |
| `TextParsed` | target, text, value | "int.Parse(\"20\") → 20" zweeft op; de tekst wordt een `int`: nieuwe badge, tint en een gewone HP-balk |
| `ParseCrashed` | side, text | De lezer schudt, glitch, "geen getal: crasht!"; zijn volgende zet valt weg (`MoveSkipped`) |
| `BlockExpired` | side, amount | De beschermplaat vervaagt |
| `Whacked` | side, damage (kan een kommagetal zijn), absorbed, dealt, hpAfter, wasCharged | Aanloop en klap; geblokt deel als grijs getal, de rest als schade met shake en hit pause. Opgeladen: ster en snelheidslijnen |
| `Braced` | side, amount, total | Beschermplaat springt op, "+n blok" |
| `Repaired` | side, amount, hpAfter, repairsLeft | "+n", de HP-balk rolt op |
| `RepairEmpty` | side | "niets meer", zoemer, kort schudden |
| `WoundUp` | side, wasCharged | Indrukken als een veer, ×2 boven het hoofd; al opgeladen: grijs "al opgeladen" |
| `DuelEnded` | outcome, turn | De verliezer valt om, een stempel: geslaagd, stilgevallen of shift voorbij |

`DuelSnapshot`: turn, player en enemy (`BotState`: key, hp, maxHp (na Lezen het gelezen getal), kind (bij `Char` toont de stage de HP als letter met het getal erbij), block, charged, repairsLeft, repairs, kind, text, crashLength, counter, dead), de tellers per regel (`playerChecked`, `playerFired`, `enemyChecked`, `enemyFired`) en outcome.
