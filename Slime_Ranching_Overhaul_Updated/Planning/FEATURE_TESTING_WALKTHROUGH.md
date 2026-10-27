# Today's Feature Testing Walkthrough

Updated: 2026-10-27  
Current branch: `feature/staggered-slime-enrichment`  
Current HEAD: `e6783a7` (`Stagger daily slime enrichment activities`)  
Test save: `TaumTest_322865388` (use this copied save, not the main farm).

## How to use this walkthrough

Test only the current stage. Reply with the results or issues. I will help resolve those first. When you say **continue**, I will present the next stage and update this file. Do not advance the stage until you say to continue.

Each stage groups related commits from today's first-parent history so its behavior can be tested together. Stage details below record what changed, test steps, and a space for results/issues.

## Current stage: 1 — Daycare and Ranch Slime baseline

**Commits:** `05208aa`, `5f5e7f0`, `bde7553`, `d18fd18`, `f9a33ed`, `0d51fdd`, `853d144`.

**What this group does:** establishes the mod and Rider build/deploy/run configurations, the external slime registry, the Slime Daycare and Ranch Slime animal baseline, and recovery when a Ranch Slime is removed or the save is reloaded. It also includes the current Daycare placement/entrance adjustments in this early part of the history. Later commits change the building exterior/footprint again; those are checked in Stage 10.

**How to test:**

1. In Rider, run **3 Build, Deploy, Run**. Confirm the build completes and SMAPI loads Taum's Slime Ranching without an error. The existing CS9057 analyzer/compiler version warning is known; it is not a build failure.
2. Load `TaumTest_322865388`. In the SMAPI console, run:

   ```text
   slime_ranching_inspect
   ```

   Confirm the command responds and reports the current room's slime information.
3. Enter the Slime Daycare. Confirm you can enter and move around. If you need a Ranch Slime, run:

   ```text
   slime_ranching_spawn_ranch_slime TaumTest_322865388
   ```

   Confirm it appears in the Daycare, can be approached/petted, and remains present after sleeping and reloading this copied save.
4. If you have a vanilla Slime Hutch, run `slime_ranching_inspect` inside it too. Confirm vanilla Green Slimes are still reported as vanilla and unchanged.
5. Check the installed mod folder's `data/saves/TaumTest_322865388/` directory. Confirm the mod registry exists there and is separate from the game's save file.
6. Optional safety/tool check: run `slime_ranching_backup_external_data TaumTest_322865388` and confirm it reports a ZIP backup. Run `slime_ranching_inspect <Ranch Slime animal ID>` to inspect one Ranch Slime's external record.

**Expected:** the Ranch Slime animal and its genome record survive sleep/reload; vanilla Hutch slimes are not converted or managed by this mod.

**Your results / issues:**

- `slime_ranching_inspect` in the vanilla Slime Hutch reported 19 vanilla `GreenSlime` monsters left unchanged and 5 Ranch Slime genome records in the external registry. This check passed.
- The Animals menu shows the slime as petted, while its friendship hearts remain empty. One pet adds 15 friendship points; the menu displays progress in half-heart steps of 100 points. A newly spawned slime may need about seven consecutive daily pets before the first half-heart appears. Verify that increment over several in-game days. Stage 1 remains current until this is confirmed.

## Remaining stages (pending your confirmation)

### Stage 2 — Breeder selection, cycles, and machine footprint

**Commits:** `384d6e2`, `5b101e4`, `b0c7a35`, `d597a80`, `49525c3`, `18f4433`, `1bb64f1`, `14a8de1`, `d2a49e7`.

Breeder parent selection and scrolling, two-day cycles, parent reservation/stationary behavior, cancellation/replacement recovery, parent rest, 5×2 collision/placement, native green placement preview, and sprite sizing/alignment. Test placing against obstructions, selecting parents, starting and cancelling a cycle (cancelling must immediately release both parents), replacing the machine, and collecting a completed egg. There is no separate **Release Pair** button.

### Stage 3 — Incubator workflow, hatch, and naming

**Commits:** `5e851e8`, `65450f5`, `7d72cc7`, `b602304`, `99c7ca5`, `9571b25`, `1d60ec2`, `8ebdc7e`, `b7cb3f9`.

Managed and Common Egg incubation, progress and ready notifications/recovery, incubator art/UI/footprint, hatchling ready state, and the **Name & Take** naming prompt. Test a two-day incubation, close/reopen/reload recovery, name the hatchling, and verify it becomes available in the Daycare after collection.

### Stage 4 — Juvenile growth and breeding eligibility

**Commits:** `c46caa9`, `6b4fd17`, `3989b8d`.

Juvenile maturation and the authoritative external age clock. Test that an incubated slime grows over three in-game days and cannot be selected as a breeder parent until mature.

### Stage 5 — Genome inheritance and visual variants

**Commits:** `b8c97b3`, `7f8f926`, `d201190`, `1d02049`, `b6d6d9a`, `5170b45`, `3209a32`, `c730ce1`, `4d3939d`, `cc09405`, `9120a7c`.

Five-size inheritance and sprites, shiny appearance, inherited colors, custom egg icon tints, asset caching, and species ability identifiers. Test controlled parent sizes/colors, offspring appearance, egg color art, and save/reload persistence.

### Stage 6 — Breeder genome details and Chromatic Crystals

**Commits:** `2c94486`, `42a4a00`, `b74b6cc`.

Color-focused breeding, color setup command, and parent genome details in the breeder. Test the crystal selector and confirm the selected color is reflected by the offspring; inspect the displayed parent genomes.

### Stage 7 — Enrichment furniture and autonomous activity schedule

**Commits:** `00304d2`, `b9bab98`, `6d90a7a`, `3638e64`, `e288119`, `bdafd45`, `17e0ade`, `6ce4b08`, `467a0e0`, `4159065`, `b8f4253`, `7df791a`, `debcb2c`, `7931c85`, `6871880`, `afe4cfb`, `878cd21`, `8b6e8b2`, `e6783a7`.

Placeable trampoline and size-based bounce, cushion/playground/slide/bed/tunnel/stone/pond/swing/light interactions, interaction cleanup and reservation handling, a shared scheduler, and the current staggered routine: roam 2–4 in-game hours, 50% activity chance, 2–3 hours using nearest nearby furniture, up to four activities per slime per day. Test with several slimes and several nearby furniture pieces over an in-game day. Allow for random missed checks.

### Stage 8 — Mutagenic Pool, pity, and visual echo

**Commits:** `287acaf`, `f165628`, `1274df8`, `87abed8`, `56c3176`, `772fc5b`, `6aa4674`, `0dbdf76`, `9aa0bc7`, `49d1172`, `97421c1`.

Safe-failure mutation attempts, Shadow Clone visual echo, configurable chance/pity/progress, test and inspection commands, and GMCM controls. Test one attempt, failure pity, success, reload recovery, and the visual-only echo.

### Stage 9 — Gel Refinery, Compost Trough, and machine diagnostics

**Commits:** `dea1491`, `fe2ee94`, `37f5176`.

The provisional Broken Glasses/Slime-to-Refined Quartz recipe, external job inspection, and juvenile feeding/growth. Test input validation, processing completion and collection, one-feed-per-day behavior, favorite-food bonus, and inspect output.

### Stage 10 — Daycare exterior, footprint, and building entry

**Commits:** `1f8bf04`, `e19f5e3`, `c362e19`, `ca4eb1e`, `5e97781`.

Exterior selection groundwork, 8×5 footprint/door alignment, and the later switch back to the vanilla Deluxe Barn exterior/footprint. Test entry, exit, building placement, upgrade/appearance settings, and whether existing Daycare construction remains accessible.

### Stage 11 — Marnie's stock, prices, and companion groundwork

**Commits:** `f66a153`, `0060436`, `ea6fa43`, `a785f3f`, `6b423e3`.

Ranching shop items and configurable prices, plus a test-only companion identity binding. Test buying multiple copies and GMCM price updates. Companion following/abilities are not part of this test.

### Stage 12 — Machine UI and visual review gallery

**Commits:** `0838216`, `cd9c010`, `9db1c17`, `123c7d1`, `a71801c`, `6616416`, `12c6e67`, `14145cf`, `84f85be`, `d8e6578`.

Refinery/Pool UI and footprints, machine reference styling/pixel art, breeder world rendering and separate machine icons, breeder/incubator menu redesign, offline asset gallery, and Rider gallery run configuration. Test the actual in-game screens, then run the review-gallery configuration and compare its UI/assets with the game.

### Stage 13 — Egg tooltip and incubator reservation corrections

**Commits:** `0cc4ec4`, `d022bdd`, `d184832`.

Readable parent names (including old registered slimes), corrected egg color texture selection and item name, hatch instruction at the top of the tooltip, and hiding an unclaimed incubator hatchling from breeder candidates. Test with a newly bred egg and a ready-but-unclaimed hatchling.

## Walkthrough state

- **Current stage:** 1 — Daycare and Ranch Slime baseline.
- **Completed stages:** none recorded.
- **Next stage:** 2 — only after you report Stage 1 results and say **continue**.
