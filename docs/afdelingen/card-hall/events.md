# The Card Hall: events

De events van de deckbuilder en wat de stage ermee doet. De algemene afspraak (type, `seq`, platte velden, onbekende events negeren) staat in [architectuur.md](../../architectuur.md#eventcontract). Een nieuw event komt hier, in `GameEvent.cs` en in de handlers van de stage (`timeline.js`, `log.js`).

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
| `ModifiersScrapped` | pending, cost | wat wachtte, valt doorgestreept weg (command `ScrapModifiers`) |
| `SolidFlipped` | targetId, solid | de regel onder de Bool Ghost springt op; doorzichtig als hij niet solid is |
| `HitPassedThrough` | targetId | "passed through" boven de Bool Ghost, geen schade |
| `HitBounced` | targetId, expression, value | `1 % 3 = 1` boven de Rhythm Turtle, hij schudt even |
| `ModifiersApplied` | cardId, before, after, expression | De som "(6 + 3) × 2" verschijnt en rekent uit tot 18 |
| `RelicTriggered` | relicId | De naam van de relic licht op; het effect volgt als gewone events |
| `ValueRounded` | targetId, before, after, subject | `Math.Round` of `Convert` rondde af: "2.5 → 2" met de naam eronder (The Rounder, Measure Twice) |
| `ValueGrew` | targetId, before, factor, raw, after | "× 1.05" boven de vijand, HP rolt op; een afgekapt restje volgt als `ValueTruncated` (The Index) |
| `ConversionCrashed` | targetId, to, value | `Convert` paste niet: "OverflowException", de vijand schudt, zijn intent vervaagt |
| `AttackSkipped` | enemyId | De vijand crashte vorige beurt en valt niet aan |
| `HealingStopped` | targetId | De vijand liep over en is leeg: "EMPTY" boven zijn hoofd, hij heelt niet meer (Bottomless Jug) |
| `TextAppended` | targetId, before, added, after | Een treffer op tekst: "+ 6" plakt achteraan, de tekst in de balk wordt langer (The Label) |
| `TextParsed` | targetId, method, text, value, to | `int.Parse("406") = 406`: de tekst wordt een getal, de balk toont weer HP |
| `TextCrashed` | targetId, length, limit | De tekst werd te lang: "32/32", een glitch, en de vijand valt om (Effective Power) |
| `ExceptionThrown` | exception, expression | Je getypeerde aanval crashte, bv. `int.Parse(2.5 + "1")`: de naam van de exception, en je beurt eindigt |
