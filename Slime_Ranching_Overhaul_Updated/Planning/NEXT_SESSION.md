# Next session handoff

**Last updated:** 2026-10-27  
**Test save:** `TaumTest_322865388` (disposable copy)  
**Working tree:** feature branch `feature/incubator-progress-display`, child of `feature/juvenile-maturity`; the incubator now shows egg identity and a day-based progress bar. Build passes; this UI change is not yet tested in game.

## Current state

- The separate Slime Daycare and Ranch Slime animal prototype are in place. The vanilla Slime Hutch and vanilla GreenSlime monsters remain separate and unmanaged.
- Breeder parent selection works. Clicking **Start 2-day Cycle** reserves the chosen pair automatically. The cycle can create a genome-specific Ranch Slime Egg.
- After egg collection, both parents now receive a one-day rest period in external state. The menu labels resting slimes and blocks selection; duration is provisional pending balance review.
- The parent picker now scrolls its resident list and shows the visible range, so residents beyond the first four are reachable.
- Removed Ranch Slimes are detached from active ID matching while their genomes remain archived externally. Unfinished cycles without an egg are cleared; existing ready eggs remain collectable.
- Breeder-ready notice state is persisted. If writing the newly ready egg fails, the notification retries next morning; a successfully recorded notification won't repeat after reload.
- A separate Slime Incubator accepts Common or genome-specific eggs from inventory, stores a two-day job externally, and spawns a Ranch Slime when ready. Common Eggs receive a fresh Green (`#00FF00`), Normal, Common genome at insertion; breeder eggs keep their stored genome. A full Daycare defers hatching. Hatch ID reservation supports recovery between external and game saves; verify this lifecycle in game.
- While a job is active, the incubator menu shows the stored egg species/rarity/generation, elapsed and total in-game days, and a progress bar. The concept image's liquid modes and stability meter remain undefined and have no effect.
- Hatched slimes now start at FarmAnimal age 0 and take three in-game days to mature. The test spawn command creates mature animals; breeder rows label juveniles and block selection until maturity.
- The user confirmed the egg spawn worked. Its tooltip showed Ranch Slime, green (`#00FF00`), Normal size, Common rarity, no mutation, Generation 1, two parent IDs, and favorite food `Broken CD`.
- Egg hatching is implemented as a prototype. The registry schema is now version 6; schema upgrades create a neighboring `.before-managed-egg-incubation-v6.bak` copy. Verify egg identity through transfer/reload, then test incubation recovery and genome matching before expanding the hatch system.
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
8. Confirm hatched slimes visibly mature over three days, stay ineligible for breeding while young, and retain their age across reload.
9. On a disposable copy, remove a parent during an unfinished cycle and verify its genome remains archived while the cycle is cleared. Repeat after the cycle has produced an egg and confirm that egg remains collectable.
10. Review provisional maturity, hatch duration, and duplicate-item behavior in `Planning/OPEN_DECISIONS.md`; update the acceptance checklist only after in-game confirmation.

## Relevant files

- `Planning/TASK_BOARD.md` — remaining implementation work and next in-game checks.
- `Planning/ACCEPTANCE_CHECKLIST.md` — release-gate status.
- `Technical/TESTING.md` — manual smoke and feature checks.
- `Services/BuildingContent.cs` — Daycare size, texture frames, and upgrade path.
- `ModEntry.cs` — heart animation and placement.
- `assets/Buildings/SlimeDaycare*GameTexture.png` — current resized in-game building images.

Back up the installed mod's `data` folder before replacing or removing the installed mod. Keep using the disposable test save for gameplay checks.
