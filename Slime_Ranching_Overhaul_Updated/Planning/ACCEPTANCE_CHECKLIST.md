# Acceptance checklist by release gate

## Current prototype evidence
- [x] Mod loads through SMAPI; current project build succeeds. The existing CS9057 analyzer/compiler-version warning remains.
- [x] External registry and first-seen snapshot use the installed mod folder.
- [x] Vanilla Hutch inspection showed seven vanilla slimes remained unmanaged; existing GreenSlime monsters are not converted.
- [x] Ranch Slime farm animal can be spawned into the separate Daycare.
- [x] Breeder UI selection works, and starting the cycle reserves the selected pair automatically.
- [x] A genome-specific Ranch Slime Egg spawned and its tooltip showed species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- [x] Breeder-ready notification state is persisted and retries after an external write failure; notification behavior still needs in-game verification.
- [x] Parents receive a one-day, externally persisted rest period after egg collection; in-game verification remains pending.
- [x] Breeder candidate list scrolls so Daycares with more than four slimes can access every resident; in-game verification remains pending.
- [x] Missing Ranch Slime IDs are detached on load/save without deleting their genome archive; unfinished cycles without an egg are cleared.
- [x] A separate Slime Incubator accepts managed genome eggs and creates a Ranch Slime linked to the stored genome; the hatch lifecycle still needs in-game verification.
- [x] Converter Common Eggs create an external base Common genome when inserted; vanilla eggs remain unaffected.
- [x] Incubator offspring use Stardew's juvenile age and three-day maturity; the breeder blocks juveniles. Visual and in-game growth checks remain open.
- [x] Store authoritative maturity age/day state in the external registry and mirror it to native animal age; schema migration and in-game age/reload checks remain open.
- [x] Converter can be issued with its SMAPI command. Actual conversion still needs a documented end-to-end check.
- [x] Gel Refinery has a Broken Glasses + Slime -> Refined Quartz first recipe, external batch record, ready notice, full-inventory deferral, Marnie/GMCM settings, and test command; in-game persistence/economy checks remain open.
- [x] Compost Trough consumes one eligible item per juvenile per day, applies a favorite-food growth bonus, writes the feed clock externally, and blocks mature/busy slimes; in-game and balance checks remain open.
- [x] Slime Tunnel, Heated Slime Stone, and Slime Pond have generated sprites and transient enrichment animations; offsets/reservation behavior need in-game verification.
- [x] `slime_ranching_inspect` reports the external schema and per-machine job details; run it against live jobs after save/reload.

## Sprint 1: core alpha
- [ ] Verify Ranch Slime petting, movement, native ID stability, and genome link over save/reload.
- [ ] Verify mod-folder backup and reinstall/recovery behavior.
- [ ] Remove a Ranch Slime with a disposable test setup; confirm its genome archive remains, unfinished cycle is cancelled, and an already produced egg remains collectable.
- [ ] Verify the current Daycare art in-game: full roof, Base foundation corner aligned to the blue footprint, and all three upgrade tiers.
- [ ] Verify the revised chimney hearts in-game: 30 screen pixels higher than the original position, 20% larger, and a 4-second float cycle.

## Sprint 2: hutch alpha
- [ ] Complete repeatable breeder cycle, ready notification, egg collection, and external egg identity checks through save/reload and chest transfer.
- [ ] Confirm a collected pair is shown as resting for one day, remains resting after reload, and becomes eligible the following day.
- [ ] Spawn at least five Ranch Slimes and confirm scrolling reaches the last resident and selects the correct two parents.
- [ ] Verify managed-egg hatching into the recorded genome; keep vanilla eggs and slimes unchanged.
- [ ] Verify incubator recovery across save/reload, full-Daycare deferral, and one-hatch-per-genome behavior.
- [ ] Confirm a hatched slime visibly grows for three days, retains age after reload, and becomes eligible for breeding on the maturity day.
- [ ] Convert vanilla eggs and hatch two Common Eggs; confirm each gets a separate external genome ID.
- [ ] Marnie sells the functional and decorative furniture stock; check trampoline, tunnel, Heated Stone, and Pond interactions and cleanup.
- [ ] Mutagenic Pool pity, Gel Refinery item conversion, and Compost Trough maturation persist after reload.

## Sprint 3: companion alpha
- [ ] Slime Core binds one existing slime, follows and uses base/hybrid abilities.
- [ ] Collector inventory survives warps and reloads.

## Sprint 4: extended alpha
- [ ] Fixed color recipes, five genetic sizes, shiny/rare/mutation inheritance persist after save/load.
- [ ] Shadow Clone and Golden/Prismatic abilities are capped and tested.
- [ ] Black/White time changes either pass day-transition tests or remain disabled.
- [ ] Uninstall/reinstall instructions and smoke-test results are documented.
