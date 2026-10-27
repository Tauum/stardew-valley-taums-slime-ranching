# Acceptance checklist by release gate

## Current prototype evidence
- [x] Mod loads through SMAPI; current project build succeeds. The existing CS9057 analyzer/compiler-version warning remains.
- [x] External registry and first-seen snapshot use the installed mod folder.
- [x] Vanilla Hutch inspection showed seven vanilla slimes remained unmanaged; existing GreenSlime monsters are not converted.
- [x] Ranch Slime farm animal can be spawned into the separate Daycare.
- [x] Breeder UI selection works, and starting the cycle reserves the selected pair automatically.
- [x] A genome-specific Ranch Slime Egg spawned and its tooltip showed species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- [x] Parents receive a one-day, externally persisted rest period after egg collection; in-game verification remains pending.
- [x] Converter can be issued with its SMAPI command. Actual conversion still needs a documented end-to-end check.

## Sprint 1: core alpha
- [ ] Verify Ranch Slime petting, movement, native ID stability, and genome link over save/reload.
- [ ] Verify mod-folder backup and reinstall/recovery behavior.
- [ ] Verify the current Daycare art in-game: full roof, Base foundation corner aligned to the blue footprint, and all three upgrade tiers.
- [ ] Verify the revised chimney hearts in-game: 30 screen pixels higher than the original position, 20% larger, and a 4-second float cycle.

## Sprint 2: hutch alpha
- [ ] Complete repeatable breeder cycle, ready notification, egg collection, and external egg identity checks through save/reload and chest transfer.
- [ ] Confirm a collected pair is shown as resting for one day, remains resting after reload, and becomes eligible the following day.
- [ ] Implement and test managed-egg hatching into the recorded genome; keep vanilla eggs and slimes unchanged.
- [ ] Marnie sells functional furniture; trampoline bounces slimes.
- [ ] Mutagenic Pool pity and Gel Refinery item conversion persist.

## Sprint 3: companion alpha
- [ ] Slime Core binds one existing slime, follows and uses base/hybrid abilities.
- [ ] Collector inventory survives warps and reloads.

## Sprint 4: extended alpha
- [ ] Fixed color recipes, five genetic sizes, shiny/rare/mutation inheritance persist after save/load.
- [ ] Shadow Clone and Golden/Prismatic abilities are capped and tested.
- [ ] Black/White time changes either pass day-transition tests or remain disabled.
- [ ] Uninstall/reinstall instructions and smoke-test results are documented.
