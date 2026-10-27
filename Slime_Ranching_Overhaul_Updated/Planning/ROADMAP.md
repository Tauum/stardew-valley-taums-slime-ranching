# Roadmap — fundamentals first, all slime types last

**Current progress:** Stage 1 has a playable Ranch Slime/Daycare prototype and a basic vanilla-isolation smoke check. Stage 2 has parent selection, automatic reservation, a two-day cycle, and genome-specific egg output; identity transfer/reload checks and hatching remain open. See [`TASK_BOARD.md`](TASK_BOARD.md) and [`NEXT_SESSION.md`](NEXT_SESSION.md) before continuing.

| Stage | Scope | Exit gate |
|---|---|---|
| 1 Foundation | SMAPI setup, external-only genome state, Slime Daycare animal identity, legacy snapshot | Vanilla Hutch and mine/wild slimes unchanged; repeated loads safe; mod-folder backup recovery tested |
| 2 Fully custom breeding | Select exactly two mature parents; visible enclosure animations; cycle/cooldown; fixed-genome egg identity held externally; hatch juvenile | Breed → egg → hatch → reload succeeds without identity loss |
| 3 Genetics framework | Fixed color-wheel recipes, hybrid ability identifiers, five inherited sizes, rarity/shiny/mutation fields; use placeholder species | Deterministic test fixtures for inheritance and no rerolls |
| 4 Furniture | Marnie's catalogue, placeable trampoline, beds, slide, tunnel, playground, heated stone, pond, compost trough | Buy/place/use, interactions animate, persist on reload |
| 5 Advanced machines | Mutagenic Pool with per-slime pity; breeder Chromatic Crystal consumables; Gel Refinery | Recipes and machine state survive reload; no unintended stacking |
| 6 Custom companion AI | Slime Core binding; bespoke following/pathfinding/location transitions/target selection/action scheduling/inventory | Companion reliably follows, acts, and never duplicates |
| 7 Complete slime types | Seven base roles, six tertiary hybrids, Black/White, Golden, Prismatic, Shadow Clone; rare powers | Every planned type has visuals, rules, and tested ability |
| 8 Stabilization | Balance, AI-generated sprites, single-player compatibility, save lifecycle and release | Regression suite and rollback instructions pass |

**Release sequencing:** A playable breeding prototype after Stage 2; a genetics alpha after Stage 3; all planned types and their powers only after Stage 7. Do not treat later implementation as removal from core scope.

**Deliberate deferrals:** Multiplayer, cosmetic transmutation, advanced ancestry UI. **Not cut:** full custom breeding, custom companion AI, any agreed slime types, interactive furniture, refinery or mutation pool.
