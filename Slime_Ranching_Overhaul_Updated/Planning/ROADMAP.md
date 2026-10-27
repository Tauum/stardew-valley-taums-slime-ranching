# Roadmap — fundamentals first, all slime types last

**Current progress:** Stages 1–3 have a playable Ranch Slime/Daycare, external genome registry, breeding/incubation flows, and provisional color/size/shiny/mutation genetics. Stage 4 now includes trampoline, cushion, playground, slide, bed, tunnel, swing, heated stone, pond, and a functional Compost Trough. Stage 5 includes the Mutagenic Pool and a first Gel Refinery recipe. The recent vertical slices compile, but their new in-game persistence, art, and interaction checks are still pending. Stage 6 companion AI and production abilities remain major work. See [`TASK_BOARD.md`](TASK_BOARD.md) and [`NEXT_SESSION.md`](NEXT_SESSION.md) before continuing.

| Stage | Scope | Exit gate |
|---|---|---|
| 1 Foundation | SMAPI setup, external-only genome state, Slime Daycare animal identity, legacy snapshot | Vanilla Hutch and mine/wild slimes unchanged; repeated loads safe; mod-folder backup recovery tested |
| 2 Fully custom breeding | Select exactly two mature parents; visible enclosure animations; cycle/cooldown; fixed-genome egg identity held externally; hatch juvenile | Breed → egg → hatch → reload succeeds without identity loss |
| 3 Genetics framework | Color-only inheritance through fixed color-wheel recipes, matching egg tints, species-derived ability identifiers; other offspring traits use defaults | Color recipes, matching egg/slime tints, ability IDs, and persisted fixed egg genomes |
| 4 Furniture | Marnie's catalogue, visual enrichment interactions, functional Compost Trough | Placement persists; transient animation offsets always clean up; feeding effects survive reload |
| 5 Advanced machines | Mutagenic Pool with per-slime pity; breeder Chromatic Crystal consumables; first Gel Refinery recipe | Inputs, job timers, notifications, and outputs survive reload without loss or duplication |
| 6 Custom companion AI | Slime Core binding; bespoke following/pathfinding/location transitions/target selection/action scheduling/inventory | Companion reliably follows, acts, and never duplicates |
| 7 Complete slime types | Seven base roles, six tertiary hybrids, Black/White, Golden, Prismatic, Shadow Clone; rare powers | Every planned type has visuals, rules, and tested ability |
| 8 Stabilization | Balance, AI-generated sprites, single-player compatibility, save lifecycle and release | Regression suite and rollback instructions pass |

**Release sequencing:** A playable breeding prototype after Stage 2; a genetics alpha after Stage 3; all planned types and their powers only after Stage 7. Do not treat later implementation as removal from core scope.

**Deliberate deferrals:** Multiplayer, cosmetic transmutation, advanced ancestry UI. **Not cut:** full custom breeding, custom companion AI, any agreed slime types, interactive furniture, refinery or mutation pool.
