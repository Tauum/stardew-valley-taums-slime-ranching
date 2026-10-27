# Rider GPT task board

Work one checked item at a time; compile and test in-game after each vertical slice. Never ask GPT to implement the entire mod in one prompt.

For the next-session handoff and current visual state, see [`NEXT_SESSION.md`](NEXT_SESSION.md).

## Stage 1 — foundation
- [x] Create SMAPI mod skeleton and verify launch/logs.
- [x] Define versioned per-slime genome IDs and a registry under the installed mod folder. (Full save/reload validation of the live animal-to-genome link remains open.)
- [x] Build the separate Slime Daycare and spawn a Ranch Slime FarmAnimal into it; the game assigned a native animal ID.
- [x] Explicitly allow Ranch Slimes to live in the base Daycare using its valid-occupant building ID.
- [x] Add a 12-design exterior selector in GMCM, default to the top-left design, and keep all exterior tiers identical through upgrades.
- [ ] Verify all 12 designs in GMCM, plus 112x128 sprite sizing and a 7x4 footprint, in game.
- [ ] Verify buying a Ranch Slime from Marnie and assigning it to each Daycare tier via Robin's chooser.
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
- [x] Show selected parent species, size, shiny/mutation flags, and favorite food in the chamber preview.
- [x] Implement a two-day cycle and fixed-genome Ranch Slime Egg output with tooltip fields for parents, generation, and favorite food. Egg spawn and tooltip were confirmed in-game.
- [x] Persist breeder-ready notification state; retry the notice after an external write failure and avoid repeat notices after reload.
- [ ] Verify ready notification, collect flow, cancellation/release, and cycle/egg persistence after save/reload.
- [x] Add a one-day post-collection rest for both parents; the duration is provisional pending balance review.
- [ ] Verify different genome eggs remain distinct through inventory, chests, shipping, and machine transfers.
- [x] Implement a separate incubator for genome-specific eggs; create a Ranch Slime with the stored genome and preserve vanilla egg behavior.
- [x] Let converter Common Eggs hatch as base Common Ranch Slimes with a new external genome per inserted egg.
- [x] Show active incubation egg identity and day-based progress in the incubator menu.
- [x] Hatch Ranch Slimes as three-day juveniles; prevent growing slimes from entering a breeding pair.
- [x] Store authoritative maturity age and day clock in the external registry; mirror to native animal age for Stardew maturity behavior.
- [x] Use external maturity age for breeder growth labels and selection eligibility.
- [x] Render Ranch Slimes with one of seven tint sheets selected from their external genome color.
- [x] Tint genome-specific egg icons to match the nearest base genome color while preserving the vanilla egg sprite silhouette.
- [x] Cache the cross-save egg registry scan used to define genome-specific item assets; refresh it when egg state changes.
- [x] Decode each vanilla slime/egg source sprite sheet once and reuse its pixels to generate color variants.
- [x] Add seven Chromatic Crystal breeder inputs; a chosen crystal fixes offspring to its base color and clears hybrid/exceptional traits. Recipes and unlock progression remain undecided; confirm consumption, persistence, and hatching in game.
- [ ] Verify all seven tint colors on hatch and after save/reload.
- [ ] Verify juvenile appearance, daily growth, save/reload age, and breeder eligibility at maturity.
- [ ] Verify schema 7-to-9 migration preserves existing animals' native maturity and backs up the previous registry.
- [ ] Verify schema 7-to-9 migration and crystal selection/consumption, two-day cycle persistence, and resulting egg/hatchling traits.
- [ ] Verify incubation start, save/reload recovery, hatch, full-Daycare deferral, and duplicate-egg handling in game.
- [ ] Verify the incubator progress card at start, after one sleep, and when the egg is ready.
- [x] Add parent cooldown after egg collection; duration remains provisional.
- [ ] Add crash/reload recovery checks and actual in-world enclosure animation beyond the current menu preview.

## Stage 3 — genetics
- [x] Implement fixed primary recipes and six listed tertiary hybrid recipes; keep Pink recipes explicitly unresolved.
- [x] Separate size genetics from maturity; breed across five size categories using provisional weighted outcomes.
- [x] Scale Ranch Slime sprite art by genetic size inside a shared frame canvas.
- [ ] Validate size-specific animal placement, hitboxes, petting targets, and breeder previews in game.
- [x] Render the independent shiny flag as a subtle sheen without changing size or base-color assets.
- [x] Store base and hybrid ability identifiers in external genomes; execution remains for companion AI.
- [x] Restrict breeding inheritance to color/species and species-derived ability IDs; offspring use defaults for other traits.

## Stage 4 — furniture
- [x] Add the existing Converter, Breeder, and Incubator to Marnie's shop; add configurable stock for implemented enrichment furniture.
- [x] Add a placeable Slime Trampoline with a mod-owned pixel sprite and Marnie stock; nearby Ranch Slimes use a temporary size-aware bounce animation.
- [x] Add a placeable Slime Cushion with configurable Marnie stock and a gentle reserved squish animation using the enrichment scheduler.
- [x] Add Slime Playground group play: reserve one playground for two slimes and run their short hops together.
- [x] Add a Slime Slide with a low hop and horizontal glide animation; share furniture reservation and cleanup.
- [x] Add a low-frequency proximity assignment and temporary bounce offset for Ranch Slimes near a trampoline; tiny, normal, and giant genome sizes now use different arc heights, compression, and durations. Approach/pathing and sprite impact poses remain.
- [x] Add a short in-memory per-slime cooldown after trampoline use so nearby residents can take turns.
- [x] Add a Slime Tunnel with a short low crawl-through animation, generated arch sprite, configurable Marnie stock, and test command.
- [x] Add a Heated Slime Stone with a resting interaction, ember sprite, configurable Marnie stock, and test command.
- [x] Add a Slime Pond with a low swim-bob animation, generated water sprite, configurable Marnie stock, and test command.
- [x] Add a Slime Swing with a reversible oscillating draw offset, generated sprite, configurable Marnie stock, and test command.
- [x] Add Daycare Fairy Lights with generated colored-bulb art, dusk-to-night glow, configurable Marnie stock, and test command.
- [x] Prevent Ranch Slimes reserved by breeder, Mutagenic Pool, or other jobs from starting enrichment animations.
- [x] Stop an in-progress enrichment animation and restore its original draw offset if the slime becomes reserved by a ranching job.
- [x] Stop an in-progress enrichment animation and restore its original draw offset if its furniture is removed.
- [x] Reattach a reserved breeding pair and cycle record to a replacement breeder in the same Daycare instead of leaving its slimes stranded under a stale tile key.
- [x] Use each slime's stored liked food in the Compost Trough and consume only eligible items; review the provisional extra growth-day bonus.
- [x] Add Slime Bed rest animation with transient draw-offset cleanup.
- [x] Generalize the trampoline scheduler into an enrichment scheduler and cache occupied target tiles and group-pair distances per assignment pass.
- [ ] Ball pit; decide whether the visual-only Slime Pond should have capped existing-item drops. Tunnel, Heated Stone, Pond, and Swing interactions need in-game cleanup/position checks.

## Stage 5 — machines
- [x] Implement the Compost Trough's one-item-per-juvenile-per-day feeding flow, liked-food bonus, external maturity update, and configurable Marnie price/growth settings.
- [x] Add a schema 10-to-11 migration for the external Compost Trough daily-feeding clock.
- [x] Add `slime_ranching_give_compost_trough` for testing without a shop purchase.
- [ ] Verify favorite versus regular food growth, daily limit, no consumption for mature/busy slimes, and reload persistence in game.
- [ ] Review Compost Trough growth values, accepted algae/trash/drop items, daily cap, and shop progression.
- [x] Add a placeable Mutagenic Pool with configurable-duration attempts, a per-slime pity counter, and safe failure; Shadow Clone is currently data/visual-only. Verify the schema migration and reload lifecycle in game.
- [x] Make Mutagenic Pool cost, duration, pity step, and chance bounds configurable with runtime clamping.
- [x] Show the active pool assignment, locked chance, and day progress in a status card.
- [x] Add a single-player test command that gives the Mutagenic Pool without spending shop gold.
- [x] Extend `slime_ranching_inspect` to report Daycare mutation and machine-job counts.
- [x] Report the on-disk external-registry schema and per-machine job details from `slime_ranching_inspect`.
- [x] Add `slime_ranching_inspect <animal ID>` to log one managed slime's external genome and lifecycle state.
- [x] Add a command to archive the current save's registry and first-seen files as a timestamped ZIP under the mod folder.
- [x] Add optional Generic Mod Config Menu sliders for Mutagenic Pool balance; the mod remains usable without GMCM.
- [x] Expose all ranching machine and enrichment furniture prices in the optional GMCM page.
- [x] Cache mutated-slime candidates for the Shadow Clone renderer and rotate the four-echo draw budget fairly.
- [x] Add an optional GMCM toggle for the Shadow Clone visual prototype.
- [x] Add the Gel Refinery's first provisional recipe: 1 Broken Glasses + configurable vanilla Slime fuel -> 1 Refined Quartz, with external job progress, a ready notice, and inventory-safe collection.
- [x] Add Marnie stock/configuration, optional GMCM fuel/time settings, schema 9-to-10 migration, and `slime_ranching_give_gel_refinery` for testing.
- [ ] Verify refinery job persistence across sleep/reload, exactly-once input/output handling, full-inventory collection retry, and the schema 9-to-10 backup on a disposable save.
- [ ] Decide whether the first recipe, fuel amount, processing duration, shop price, and later recipe unlocks are balanced; current values are explicitly provisional.
- [ ] Extend the temporary Shadow Clone visual echo into bounded companion behavior; no clones may breed, produce resources, or recursively clone.
- [x] Chromatic Crystals as breeder consumables, **not furniture**; obtainment recipes and unlock progression remain open.
- [ ] Expand the Gel Refinery with reviewed recipes, progression gates, and anti-arbitrage checks; only its first recycling recipe is implemented.

## Stage 6 — custom companion AI
- [ ] Review the Slime Core first-slice defaults and decide its accessory slot, acquisition, and dismissal/token behavior.
- [x] Add external-only test binding for one existing mature Daycare Ranch Slime without creating or duplicating a FarmAnimal or genome record.
- [ ] Verify binding survives save/reload with the same animal ID and genome GUID, and a bound slime is excluded from machine/furniture interactions.
- [ ] Replace test commands with a reviewed Core acquisition and bind/dismiss item flow.
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
- [ ] Check the breeder's two genome summaries with a hybrid, a Shadow Clone, and a parent with each long favorite-food label; confirm text does not collide or clip.
- [ ] Mark five or more Ranch Slimes as Shadow Clone and verify the four visible echoes rotate without frame stutter in a crowded Daycare.
- [ ] On the disposable `TaumTest_322865388` save, buy/place a Mutagenic Pool in the Daycare; confirm a juvenile, busy, or already-mutated slime can't be selected.
- [ ] Start an attempt with 500 vanilla Slime, sleep, and verify the success/failure message, stored trait or pity increment, and unchanged slime health/existence. Reload mid-cycle and ensure the stored roll does not change.
- [ ] Verify the per-slime odds increase by 2 points per failure to 30%, reset after success, and remain independent between slimes; review these provisional values before release.
- [ ] Verify config overrides for guaranteed failure/success, lower cost, and alternate duration; check invalid values are safely clamped.
- [ ] Verify GMCM save/reset, live Pool tuning, and Marnie's refreshed price while an attempt is active.
- [ ] Verify GMCM price changes for Marnie's machines and furniture refresh the shop after saving.
- [ ] Check the Pool status card with one- and multi-day durations, then reload an active job and confirm the assignment and progress are restored.
- [ ] Verify schema 8 migrates to schema 9, creates `.before-mutagenic-pool-v9.bak`, and preserves existing genomes, eggs, breeding pairs, and incubations.
- [ ] On the disposable save, run `slime_ranching_set_mutation TaumTest_322865388 <animal ID> ShadowClone`; confirm a faint temporary duplicate appears near the slime only inside the Daycare. Then use `none` and confirm it stops; reload with Shadow Clone enabled and check it returns.
- [ ] Use `slime_ranching_inspect` with no jobs and with breeder/incubator/pool jobs active; confirm counts match the current Daycare only.
- [ ] Run `slime_ranching_backup_external_data TaumTest_322865388`; confirm the timestamped ZIP contains the current registry and first-seen snapshot and is stored under `data/backups/`.
- [ ] Deploy current build to `TaumTest_322865388`; check the complete Base roof and confirm its foundation corner touches the blue footprint's bottom-left, then check both upgrades.
- [ ] Check that hearts are 30 screen pixels higher than their original position, 20% larger, and float on a 4-second cycle.
- [ ] Continue with breeder-ready notification, egg persistence, and incubator hatching checks.

## Reusable Rider prompt
> Implement only the next unchecked task. Inspect existing code and Stardew/SMAPI API before choosing hooks. Do not affect non-managed slimes or write mod state into the game save. Keep external state versioned and recoverable. Include tests, a manual in-game verification checklist, and explain any uncertain API assumptions. Do not silently simplify the custom breeder or companion AI.
