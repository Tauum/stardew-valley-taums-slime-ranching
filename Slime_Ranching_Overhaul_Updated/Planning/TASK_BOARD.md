# Rider GPT task board

Work one checked item at a time; compile and test in-game after each vertical slice. Never ask GPT to implement the entire mod in one prompt.

For the next-session handoff and current visual state, see [`NEXT_SESSION.md`](NEXT_SESSION.md).

## Stage 1 — foundation
- [x] Create SMAPI mod skeleton and verify launch/logs.
- [x] Define versioned per-slime genome IDs and a registry under the installed mod folder. (Full save/reload validation of the live animal-to-genome link remains open.)
- [x] Build the separate Slime Daycare and spawn a Ranch Slime FarmAnimal into it; the game assigned a native animal ID.
- [ ] Verify petting, movement, and that the native ID keeps the same external genome link across save/reload.
- [x] Implement idempotent first-install detection and first-seen snapshot of legacy hutch slimes in the mod folder.
- [x] Keep vanilla Slime Hutch and monster slimes untouched; modded Ranch Slimes and other mod systems use the separate Slime Daycare.
- [x] Reconcile missing Ranch Slime animal IDs on load/save, preserve orphaned genome records, and cancel only unfinished cycles without an egg.
- [ ] Add backup/reinstall reconciliation and recovery tests.

The first-seen load/save/hutch-inspection smoke check passed on the disposable `TaumTest_322865388` copy: seven vanilla hutch slimes stayed unmanaged and the external registry reloaded. A Ranch Slime spawn and egg tooltip were also observed. This does not prove full FarmAnimal/genome stability or recovery after reinstall.

## Stage 2 — fully custom breeding
- [x] Open breeder UI, select two Ranch Slime rows, and display selected parents in the menu preview.
- [x] Scroll through all Daycare residents in the breeder parent list; only four rows are visible at once.
- [x] Start the cycle from the selection screen; it automatically reserves the parents.
- [x] Implement a two-day cycle and fixed-genome Ranch Slime Egg output with tooltip fields for parents, generation, and favorite food. Egg spawn and tooltip were confirmed in-game.
- [x] Persist breeder-ready notification state; retry the notice after an external write failure and avoid repeat notices after reload.
- [ ] Verify ready notification, collect flow, cancellation/release, and cycle/egg persistence after save/reload.
- [x] Add a one-day post-collection rest for both parents; the duration is provisional pending balance review.
- [ ] Verify different genome eggs remain distinct through inventory, chests, shipping, and machine transfers.
- [x] Implement a separate incubator for genome-specific eggs; create a Ranch Slime with the stored genome and preserve vanilla egg behavior.
- [x] Let converter Common Eggs hatch as base Common Ranch Slimes with a new external genome per inserted egg.
- [x] Show active incubation egg identity and day-based progress in the incubator menu.
- [x] Hatch Ranch Slimes as three-day juveniles; prevent growing slimes from entering a breeding pair.
- [ ] Verify juvenile appearance, daily growth, save/reload age, and breeder eligibility at maturity.
- [ ] Verify incubation start, save/reload recovery, hatch, full-Daycare deferral, and duplicate-egg handling in game.
- [ ] Verify the incubator progress card at start, after one sleep, and when the egg is ready.
- [x] Add parent cooldown after egg collection; duration remains provisional.
- [ ] Add crash/reload recovery checks and actual in-world enclosure animation beyond the current menu preview.

## Stage 3 — genetics
- [ ] Fixed color-wheel recipe table and tests; record unresolved Pink recipes explicitly.
- [ ] Separate size genetics from maturity; implement five sizes and visual scaling.
- [ ] Independent shiny/rarity/mutation flags; store ancestry and hybrid ability IDs.

## Stage 4 — furniture
- [ ] Marnie's shop inventory; trampoline animation and interaction framework.
- [x] Add the existing Converter, Breeder, and Incubator to Marnie's shop; custom placeable enrichment furniture remains unimplemented.
- [ ] Use each slime's stored liked food when the Compost Trough is implemented; decide its bonus and consume only eligible items.
- [ ] Beds, slide, tunnel, ball pit, heated stone, pond, compost trough; persistence and QA.

## Stage 5 — machines
- [ ] Mutagenic Pool with per-slime pity and safe failure.
- [ ] Chromatic Crystals as breeder consumables, **not furniture**.
- [ ] Gel Refinery stored Slime and vanilla-item conversion recipes.

## Stage 6 — custom companion AI
- [ ] Slime Core bind/unbind, ownership, transfer and anti-duplication rules.
- [ ] Custom movement, pathfinding, transitions, stuck recovery, action scheduler.
- [ ] Inventory, ability targeting, cooldown and size/shiny scaling.

## Stage 7 — all types
- [ ] Seven base ability behaviors and six hybrids.
- [ ] Black extra night hour, White extra morning hour; test day-transition edge cases.
- [ ] Golden discount caps; Prismatic mount + Adaptation; Shadow Clone without recursive duplication.
- [ ] AI-generated art prepared as usable pixel sprites; visuals reviewed in-game.

## Stage 8 — release
- [ ] Reinstall/vanilla-load test matrix, compatibility and economy balance.
- [ ] Documentation and recovery instructions; single-player alpha packaging.

## Next in-game verification
- [ ] Deploy current build to `TaumTest_322865388`; check the complete Base roof and confirm its foundation corner touches the blue footprint's bottom-left, then check both upgrades.
- [ ] Check that hearts are 30 screen pixels higher than their original position, 20% larger, and float on a 4-second cycle.
- [ ] Continue with breeder-ready notification, egg persistence, and incubator hatching checks.

## Reusable Rider prompt
> Implement only the next unchecked task. Inspect existing code and Stardew/SMAPI API before choosing hooks. Do not affect non-managed slimes or write mod state into the game save. Keep external state versioned and recoverable. Include tests, a manual in-game verification checklist, and explain any uncertain API assumptions. Do not silently simplify the custom breeder or companion AI.
