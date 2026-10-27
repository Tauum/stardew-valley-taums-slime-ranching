# Next session handoff

**Last updated:** 2026-10-27  
**Test save:** `TaumTest_322865388` (disposable copy)  
**Working tree:** feature branch `feature/breeder-list-scrolling`; parent rest and breeder list scrolling are implemented, but not tested in game.

## Current state

- The separate Slime Daycare and Ranch Slime animal prototype are in place. The vanilla Slime Hutch and vanilla GreenSlime monsters remain separate and unmanaged.
- Breeder parent selection works. Clicking **Start 2-day Cycle** reserves the chosen pair automatically. The cycle can create a genome-specific Ranch Slime Egg.
- After egg collection, both parents now receive a one-day rest period in external state. The menu labels resting slimes and blocks selection; duration is provisional pending balance review.
- The parent picker now scrolls its resident list and shows the visible range, so residents beyond the first four are reachable.
- The user confirmed the egg spawn worked. Its tooltip showed Ranch Slime, green (`#00FF00`), Normal size, Common rarity, no mutation, Generation 1, two parent IDs, and favorite food `Broken CD`.
- Egg hatching is not implemented. Verify item identity and its external genome record through collection, inventory/chest transfer, save, and reload before starting hatch work.
- Building art uses direct nearest-neighbor resized copies of the original images in `assets/Buildings/*_GameTexture.png`. The original full-size source images are preserved. Current frames are Base 160 by 160, Upgrade 1 176 by 160, and Upgrade 2 192 by 160; building widths are 10, 11, and 12 tiles.
- The Base game texture is shifted 8 source pixels left and 3 down from the directly resized original. `BuildingContent` applies a world draw offset of (3, 8) pixels to the Daycare sprite so its body/foundation can move without cropping the roof. The building's tile footprint/collision is unchanged. Confirm the foundation corner against the blue box in game; fence posts have not been separated or positioned independently.
- The breeding chimney hearts use a pale heart behind a rose-pink heart. They are now 30 screen pixels above the chimney reference, 20% larger, and use a 4-second float cycle. This revision has compiled but has not yet been checked in game.
- Latest build succeeded with 0 errors and the existing CS9057 compiler/analyzer version warning. No gameplay tests were run after the latest texture and heart changes.

## Pick up here

1. Run Rider job **3 Build, Deploy, Run** with `TaumTest_322865388` available.
2. Check that the complete roof renders and compare the building foundation's bottom-left corner with the blue footprint. Confirm the final building position after the requested 30-pixel-right adjustment; leave fence alignment for later.
3. Start a breeder cycle and check that hearts are 30 screen pixels higher than their original position, 20% larger, and float on a 4-second cycle. Update the acceptance checklist only after the user confirms the in-game result.
4. Collect an egg, place it in inventory and a chest in turn, save/reload, and confirm the item still resolves to the same external genome record.
5. Check parent rest: collect an egg, confirm both parents are marked resting, reload the save, and confirm they become eligible the following day.
6. Before hatch implementation, review the open egg/incubator decisions in `Planning/OPEN_DECISIONS.md`; preserve external-only genome storage and prevent duplicate hatches.

## Relevant files

- `Planning/TASK_BOARD.md` — remaining implementation work and next in-game checks.
- `Planning/ACCEPTANCE_CHECKLIST.md` — release-gate status.
- `Technical/TESTING.md` — manual smoke and feature checks.
- `Services/BuildingContent.cs` — Daycare size, texture frames, and upgrade path.
- `ModEntry.cs` — heart animation and placement.
- `assets/Buildings/SlimeDaycare*GameTexture.png` — current resized in-game building images.

Back up the installed mod's `data` folder before replacing or removing the installed mod. Keep using the disposable test save for gameplay checks.
