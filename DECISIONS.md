# Beslissingen

Keuzes die bij de product owner liggen (zie [Rolverdeling](CLAUDE.md#rolverdeling)). De implementer zet een keuze hier met status OPEN en vraagt ze; de product owner vult **Beslissing** in en zet de status op BESLIST. Daarna krijgt ze één regel met datum onder [Beslist](docs/README.md#beslist), en de uitwerking in het document van haar module (D-001). Een nieuwe beslissing krijgt het volgende nummer; nummers worden niet hergebruikt.

```
## D-000 Titel
Status: OPEN | BESLIST

**Context:** waarom de keuze nu opduikt, en wat ervan afhangt.
**Opties:**
- A. ... Voor: ... Tegen: ...
- B. ...
**Aanbeveling:** de optie van de implementer, met de reden.
**Beslissing:** (vult de product owner in, met de datum)
```

## D-001 Verhouding tot "Beslist" in docs/README.md
Status: BESLIST

**Context:** [docs/README.md](docs/README.md#beslist) houdt al een lijst "Beslist" bij, kort en met datum, met de uitwerking in het document van het onderwerp. Met dit bestand komen er twee plekken waar beslissingen staan.
**Opties:**
- A. DECISIONS.md is de werkbank voor keuzes van de product owner. Wordt iets BESLIST, dan komt er ook één regel met datum in "Beslist" en de uitwerking in het document van de module. Voor: de docs blijven de enige bron voor het ontwerp, de modulaire opzet blijft. Tegen: een beslissing staat op twee plekken.
- B. Alles naar DECISIONS.md; "Beslist" in docs/README.md verdwijnt of verwijst hierheen. Voor: één plek. Tegen: het ontwerp leest niet meer op zichzelf, en DECISIONS.md groeit over alle afdelingen heen.
- C. Los van elkaar laten. Voor: geen werk. Tegen: ze lopen uit elkaar.
**Aanbeveling:** A. Het past bij "schrijf ontwerpwijzigingen rechtstreeks in het document van de module", en DECISIONS.md houdt ook de afgewogen opties bij, wat "Beslist" niet doet.
**Beslissing:** A (7 oktober 2026).

## D-002 Ideeën: hier of in de ideeen.md van een afdeling
Status: BESLIST

**Context:** De rolverdeling zegt dat ideeën onder "Ideeën" in DECISIONS.md komen. Elke afdeling heeft al een eigen `ideeen.md`, en de root-CLAUDE.md zegt: wat voor één afdeling geldt, schrijf je in haar map.
**Opties:**
- A. Alle ideeën hier. Voor: één lijst om door te nemen. Tegen: breekt de modulaire opzet; wie aan één afdeling werkt, leest ideeën van alle afdelingen.
- B. Een idee voor één afdeling in haar `ideeen.md`, een idee voor de wereld of over afdelingen heen hier. Voor: past bij de bestaande indeling. Tegen: de product owner moet op meer plekken kijken.
**Aanbeveling:** B. Tot er beslist is, volg ik A, zoals gevraagd.
**Beslissing:** geen van beide: één `ideeen.md` op de root, met een sectie per afdeling en een voor De wereld. De drie `ideeen.md`'s van de afdelingen zijn erin opgegaan (7 oktober 2026).

## D-003 Branches of rechtstreeks op main
Status: BESLIST

**Context:** De reviewer kijkt naar de diff tegenover main, maar tot nu toe gebeurt alle werk rechtstreeks op `main`. Daar is die diff leeg.
**Opties:**
- A. Op main blijven werken. De reviewer bekijkt dan wat nog niet naar `origin/main` gepusht is, plus wat nog niet gecommit is (zo staat hij nu ingesteld). Voor: niets verandert aan je werkwijze. Tegen: wie meteen pusht, heeft niets meer te reviewen; `/review HEAD~3` kan dan wel.
- B. Elke taak op een eigen branch, en mergen na de review. Voor: een duidelijke grens per taak, de diff is altijd de taak. Tegen: meer handelingen; GitHub Pages en CI draaien pas na de merge.
**Aanbeveling:** B voor taken die langer dan één sessie duren, A voor kleine dingen. De reviewer werkt al met beide.
**Beslissing:** A, alles op `main` (7 oktober 2026).

