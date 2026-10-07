---
description: Laat de reviewer-agent los op de huidige branch en vat het resultaat samen
argument-hint: "[basis, bv. HEAD~3]"
---

Start de agent `reviewer` op de huidige branch. Geef hem alleen de basis mee: `$ARGUMENTS` (leeg betekent: de standaard uit zijn instructies). Leg hem niet uit wat je gebouwd hebt of waarom; hij werkt bewust zonder die context.

Vat daarna zijn rapport samen voor de product owner:

1. Het oordeel en de tests, in één regel.
2. De blockers, daarna de shoulds, elk met pad:regel. De nits alleen als aantal, tenzij er niets anders is.
3. Wat Lotte en Yusuf opmerkten, in een paar regels.
4. De verborgen beslissingen.

Pas niets aan naar aanleiding van de review. Stel voor welke bevindingen je oppakt, en vraag of je de verborgen beslissingen als OPEN in DECISIONS.md zet.
