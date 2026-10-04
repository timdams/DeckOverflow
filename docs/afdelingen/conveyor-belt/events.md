# The Conveyor Belt: wat de stage afspeelt

De Lopende Band heeft geen eventlijst zoals de Card Hall en de Controlekamer. De motor rekent elk testgeval vooraf uit (`Simulator.Run`): een lijst `Frame`s, één per tik, en een afloop (`Ending`). De shell geeft die frame per frame aan de stage, en daarna de afloop.

| Oproep | Velden | Wat de stage ermee doet |
| --- | --- | --- |
| `setLevel` | width, height, product (pad naar de tekening), labels (woorden van de shell) | Het rooster, de vloerlijn, de tekening van het product laden |
| `setBoard` | per vakje: kind (`belt`, `machine`, `gate`, `counter`, `source`, `output`), richtingen, tekst, fixed | Een betonnen vloer. Band is rollen die draaien, met een pijlpunt; een bocht of samenkomst krijgt een draaischijf, en onder elk stuk loopt de band door naar zijn ingangen en uitgangen. Een machine is een kast met een meter, een poort een rond ventiel met de labels *ja* en *nee*, een teller een telwerk (↻ nog eens, ⇥ klaar), de bron een trechter, de uitgang een gearceerd laadperron *naar het front*. Vaste stukken zitten met vier schroefjes vast |
| `select` | x, y | Een gestippelde rand rond het gekozen vakje |
| `frame` | x, y, value, kind, note (`Overflow`, `Truncated`), gate, growth, counters, product | Het product rijdt naar zijn vakje en groeit naar de bestelling toe. Een machine pompt en vonkt (`machine`); afkappen laat zaagsel vallen (`✂ .5`, `tinkle`); een byte die overloopt, schokt (`glitch`). Een wissel klakt om (`latch`), een telwerk telt (`tick`) |
| `ending` | ending (`Delivered`, `Wrong`, `FellOff`, `Forever`), loop (bij `Forever`: de vakjes van het rondje, uit `CaseRun.LoopFrom`) | Goedgekeurd: een stempel in inkt en wegwandelen (`stamp`, `approve`). Afgekeurd: een oranje ✗-stempel en in de bak (`stamp`, `buzz`, `bin`). Gevallen: tuimelen (`fall`). Oneindige loop: het licht gaat uit, alleen het rondje brandt in oranje, twee pijlen draaien eromheen, het product rijdt rondjes die steeds sneller gaan met een tik die steeds hoger klinkt, een telwerk telt ze, en dan stopt de band (`overheat`, `alarm`, `tick`). Het donker blijft tot de volgende tik |
| `clearCrate` | | Het product van de band halen |

In een verborgen tabblad slaat de stage de animaties over; de shell gaat gewoon verder. Laadt de stage niet, dan speelt de band zonder beeld.
