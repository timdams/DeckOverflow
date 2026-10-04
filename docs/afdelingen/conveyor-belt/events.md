# The Conveyor Belt: wat de stage afspeelt

De Lopende Band heeft geen eventlijst zoals de Card Hall en de Controlekamer. De motor rekent elk testgeval vooraf uit (`Simulator.Run`): een lijst `Frame`s, één per tik, en een afloop (`Ending`). De shell geeft die frame per frame aan de stage, en daarna de afloop.

| Oproep | Velden | Wat de stage ermee doet |
| --- | --- | --- |
| `setLevel` | width, height, product (pad naar de tekening), labels (woorden van de shell) | Het rooster, de vloerlijn, de tekening van het product laden |
| `setBoard` | per vakje: kind (`belt`, `machine`, `gate`, `counter`, `source`, `output`), richtingen, tekst, fixed | Band met meelopende pijltjes, machines met boutjes, een wissel met een volle (klopt) en een gestippelde (klopt niet) uitgang, een telwerk, een trechter en een verzendpoort. Vaste stukken krijgen een grijze rand |
| `select` | x, y | Een gestippelde rand rond het gekozen vakje |
| `frame` | x, y, value, kind, note (`Overflow`, `Truncated`), gate, growth, counters, product | Het product rijdt naar zijn vakje en groeit naar de bestelling toe. Een machine pompt en vonkt (`machine`); afkappen laat zaagsel vallen (`✂ .5`, `tinkle`); een byte die overloopt, schokt (`glitch`). Een wissel klakt om (`latch`), een telwerk telt (`tick`) |
| `ending` | ending (`Delivered`, `Wrong`, `FellOff`, `Forever`) | Goedgekeurd: stempel en wegwandelen (`stamp`, `approve`). Afgekeurd: stempel en in de bak (`stamp`, `buzz`, `bin`). Gevallen: tuimelen (`fall`). Oneindige loop: rook, een zwaailicht en het woord voor oververhit (`overheat`, twee keer `alarm`) |
| `clearCrate` | | Het product van de band halen |

In een verborgen tabblad slaat de stage de animaties over; de shell gaat gewoon verder. Laadt de stage niet, dan speelt de band zonder beeld.
