# Next session handoff

**Last updated:** 2026-10-27  
**Test save:** `TaumTest_322865388` (disposable copy)  
**Working tree:** `feature/five-size-inheritance`, chained after `perf/enrichment-scheduler-target-index`; breeder offspring now roll among five genetic sizes using provisional weighted outcomes, and trampoline arcs distinguish Small and Large too. Build passes; no in-game checks have been done for this branch.

## Current state

- The separate Slime Daycare and Ranch Slime animal prototype are in place. The vanilla Slime Hutch and vanilla GreenSlime monsters remain separate and unmanaged.
- Breeder parent selection works. Clicking **Start 2-day Cycle** reserves the chosen pair automatically. The cycle can create a genome-specific Ranch Slime Egg.
- After egg collection, both parents now receive a one-day rest period in external state. The menu labels resting slimes and blocks selection; duration is provisional pending balance review.
- The parent picker now scrolls its resident list and shows the visible range, so residents beyond the first four are reachable.
- Removed Ranch Slimes are detached from active ID matching while their genomes remain archived externally. Unfinished cycles without an egg are cleared; existing ready eggs remain collectable.
- Breeder-ready notice state is persisted. If writing the newly ready egg fails, the notification retries next morning; a successfully recorded notification won't repeat after reload.
- A separate Slime Incubator accepts Common or genome-specific eggs from inventory, stores a two-day job externally, and spawns a Ranch Slime when ready. Common Eggs receive a fresh Green (`#00FF00`), Normal, Common genome at insertion; breeder eggs keep their stored genome. A full Daycare defers hatching. Hatch ID reservation supports recovery between external and game saves; verify this lifecycle in game.
- While a job is active, the incubator menu shows the stored egg species/rarity/generation, elapsed and total in-game days, and a progress bar. The concept image's liquid modes and stability meter remain undefined and have no effect.
- Marnie's shop now sells the Converter (2,500g), Breeder (10,000g), Incubator (5,000g), and Slime Trampoline (2,500g). These prices are configurable in `config.json` and provisional. The trampoline is an indoor placeable BigCraftable with a generated 16x32 pixel sprite; its interaction is still a visual prototype.
- A proximity scheduler checks every 30 ticks for nearby Ranch Slimes and free trampolines. Tiny slimes get a higher/faster arc; Giant slimes compress more and get a lower/slower arc; Normal slimes are between them. Active offsets update per tick, reserve the trampoline for one slime, and restore on completion, leaving the Daycare, or returning to title. No pathfinding or sprite impact pose yet; test this interaction on the disposable save.
- After a trampoline interaction, the slime waits three seconds before becoming eligible again. This temporary cooldown is memory-only and resets when leaving the Daycare; it is a lightweight fairness rule, not persistent relationship data.
- Hatched slimes now start at FarmAnimal age 0 and take three in-game days to mature. The test spawn command creates mature animals; breeder rows label juveniles and block selection until maturity.
- Maturity age is authoritative in each external `SlimeRecord` (`AgeDays`, `AgeLastUpdatedDay`, and `AgeClockInitialized`) and is mirrored to Stardew's native `FarmAnimal.age` so its animal system can apply maturity. The registry has since advanced through schema 8; the v7 migration backed up its source as `.before-external-maturity-v7.bak`. Verify age progression and the latest migration. This does not add mod metadata to the game save; the native age field remains a compatibility mirror.
- Breeder candidate rows and selection now query external age directly, falling back to native age only before the record's age clock is initialized.
- Ranch Slime textures now have seven cached tint assets. On load and hatch, the runtime selects the nearest Red/Orange/Yellow/Green/Blue/Purple/Pink tint from the external genome hex color using a local-only sprite texture override; this does not store skin metadata in the game save. Run `slime_ranching_give_color_eggs <save name>` for seven test eggs and compare their hatch colors. Exact hybrid color blending remains undecided.
- Genome-specific egg items now use seven cached tints of the vanilla Green Slime Egg sprite, keeping its source silhouette and outline. Check item icons in inventory before hatching the color test eggs; verify hatchling tint and reload appearance as well.
- Genome egg definitions scan mod-folder save registries once and cache the resulting item data. Cache refresh is tied to the existing object-data invalidations when eggs are created or hatch status changes, avoiding repeated recursive disk scans.
- The vanilla Green Slime and Green Slime Egg source pixels are each decoded once per process and reused to build tint variants; verify visuals after load. The tint textures themselves remain normal SMAPI cached assets.
- Chromatic Crystals now focus a breeder cycle on one of seven base colors. Choose the crystal in the breeder panel before starting; the selected item is consumed at cycle start, and offspring become a Common single-color slime with hybrid/shiny/rarity/mutation traits cleared. The external pair record stores the selected color (schema 8); `slime_ranching_give_crystals <save name>` creates one of each for testing. Recipes and progression unlocks are undecided.
- Marnie also sells a Slime Cushion (configurable default 1,500g). Nearby Ranch Slimes can use it for a short gentle squish-and-spring animation. It shares the scheduler's one-user reservation and transient cleanup with trampolines; it grants no happiness or breeding bonus and changes no save data. This branch only passed compilation and needs in-game validation.
- Marnie also sells a Slime Playground (configurable default 6,000g). Two nearby idle Ranch Slimes are assigned together to one playground and perform synchronized hops; the reserved object cannot start another interaction during the animation. It currently has no friendship/happiness effect. Verify with multiple furniture items and after leaving the room mid-animation.
- Marnie also sells a Slime Slide (configurable default 3,000g). Nearby idle slimes perform a low hop and glide horizontally across it. The travel direction is fixed and the animation has no gameplay effect; verify offset cleanup and competing furniture reservations.
- Marnie also sells a Slime Bed (configurable default 1,800g). Nearby slimes lower briefly into a resting pose, then return to their normal offset. It does not change happiness, age, or breeding; verify interruption cleanup.
- The interaction scheduler is now named `EnrichmentInteractionService`. It handles all five implemented furniture types, caches occupied target tiles once per assignment pass, and computes each playground candidate distance once during sorting. Build passes with only the existing analyzer/compiler version warning; compare behavior/performance with a crowded Daycare in game.
- Breeder size inheritance now uses Tiny/Small/Normal/Large/Giant categories. The weights specified in the design notes are implemented; provisional defaults cover Tiny+Tiny, Normal+Normal, adjacent pairs, and wider gaps. Unknown legacy labels are treated as Normal only when rolling offspring. Small and Large also have distinct trampoline arcs. Visual size scaling remains open and requires in-game review.
- `slime_ranching_set_size <save> <animal ID> <size>` now sets a managed Ranch Slime's size in external state and rolls back the edit if writing fails. Pair it with `slime_ranching_spawn_ranch_slime` to set up known-size breeding pairs. This is a test/setup command; size still does not scale the sprite.
- The user confirmed the egg spawn worked. Its tooltip showed Ranch Slime, green (`#00FF00`), Normal size, Common rarity, no mutation, Generation 1, two parent IDs, and favorite food `Broken CD`.
- Egg hatching is implemented as a prototype. It was introduced in schema 6; the registry is currently schema 8. Verify egg identity through transfer/reload, then test incubation recovery and genome matching.
- Building art uses direct nearest-neighbor resized copies of the original images in `assets/Buildings/*_GameTexture.png`. The original full-size source images are preserved. Current frames are Base 160 by 160, Upgrade 1 176 by 160, and Upgrade 2 192 by 160; building widths are 10, 11, and 12 tiles.
- The Base game texture is shifted 8 source pixels left and 3 down from the directly resized original. `BuildingContent` applies a world draw offset of (3, 8) pixels to the Daycare sprite so its body/foundation can move without cropping the roof. The building's tile footprint/collision is unchanged. Confirm the foundation corner against the blue box in game; fence posts have not been separated or positioned independently.
- The breeding chimney hearts use a pale heart behind a rose-pink heart. They are now 30 screen pixels above the chimney reference, 20% larger, and use a 4-second float cycle. This revision has compiled but has not yet been checked in game.
- Latest build succeeded with 0 errors and the existing CS9057 compiler/analyzer version warning. No gameplay tests were run after the latest texture and heart changes.

## Pick up here

1. Run Rider job **3 Build, Deploy, Run** with `TaumTest_322865388` available.
2. Check the full Daycare roof, foundation against the blue footprint, and chimney hearts (30 screen pixels higher, 20% larger, four-second float). Current building draw offset is `(3, 8)`; fence alignment remains later work.
3. Spawn at least five Ranch Slimes, open the breeder, scroll to the last row, and select two residents. Complete a cycle; check the ready notification appears once, then collect the egg.
4. Confirm both parents show one day of rest after collection, remain resting after reload, and become eligible the following day.
5. Transfer a breeder egg through inventory/chest, save/reload, and confirm the same external genome ID. Insert it into the Slime Incubator and confirm the hatched slime retains those exact traits.
6. Run `slime_ranching_give_converter TaumTest_322865388` and `slime_ranching_give_incubator TaumTest_322865388`. Convert a vanilla egg, then hatch its Common Egg; expect a green, normal, common Ranch Slime. Repeat with a second Common Egg and confirm a distinct external genome ID.
7. Save/reload during incubation. Fill the Daycare for another ready hatch and confirm it waits until space is available. Check that vanilla Slime Eggs remain unchanged and unusable in the custom incubator.
   - In the incubator menu, check the progress card immediately after insertion (0%), after one sleep (50%), and when ready (100%). Confirm the status text and genome label fit the panel.
8. Confirm hatched slimes visibly mature over three days, stay ineligible for breeding while young, and retain their age across reload. Check the external registry's `AgeDays` and day clock fields.
9. On a disposable copy, remove a parent during an unfinished cycle and verify its genome remains archived while the cycle is cleared. Repeat after the cycle has produced an egg and confirm that egg remains collectable.
10. Review provisional maturity, hatch duration, and duplicate-item behavior in `Planning/OPEN_DECISIONS.md`; update the acceptance checklist only after in-game confirmation.
11. Test crystal breeding: run `slime_ranching_give_crystals TaumTest_322865388`, open the breeder, click the color selector until the desired color is shown, and start the cycle. Check one crystal was consumed, reload mid-cycle, collect the egg after two days, and confirm the egg tooltip and hatchling have that base color and Common/non-hybrid traits.
12. Verify schema 8 migration creates `.before-chromatic-crystals-v8.bak` and preserves existing pair timers and eggs.
13. Check Marnie's Slime Cushion stock and its indoor placement. Let nearby slimes use a cushion alongside a trampoline; confirm the cushion animation stays low and short, furniture does not get double-booked, and offsets reset when leaving the room.
14. Check the Slime Playground with zero, one, two, and four nearby slimes. Confirm exactly two use each available playground at a time, group hops synchronize, and the other enrichment objects remain independently available.
15. Check the Slime Slide animation alongside other furniture, then leave the room mid-glide and confirm position resets.
16. Check Marnie's Slime Bed stock and the brief rest animation. Leave the Daycare mid-animation and confirm the slime's position resets without affecting age or breeding status.

## Relevant files

- `Planning/TASK_BOARD.md` — remaining implementation work and next in-game checks.
- `Planning/ACCEPTANCE_CHECKLIST.md` — release-gate status.
- `Technical/TESTING.md` — manual smoke and feature checks.
- `Services/BuildingContent.cs` — Daycare size, texture frames, and upgrade path.
- `ModEntry.cs` — heart animation and placement.
- `assets/Buildings/SlimeDaycare*GameTexture.png` — current resized in-game building images.

Back up the installed mod's `data` folder before replacing or removing the installed mod. Keep using the disposable test save for gameplay checks.
