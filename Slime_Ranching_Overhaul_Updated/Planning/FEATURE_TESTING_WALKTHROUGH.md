# Today's Feature Testing Walkthrough

Updated: 2026-10-27  
Current branch: `feature/ranch-slime-size-scaled-shadows`
Current HEAD: `3799f68` (`Scale Ranch Slime ground shadows by genome size`)
Test save: `TaumTest_322865388` (use this copied save, not the main farm).

## How to use this walkthrough

Test only the current stage. Reply with the results or issues. I will help resolve those first. When you say **continue**, I will present the next stage and update this file. Do not advance the stage until you say to continue.

## Current stage: 5 - Color inheritance, matching egg tint, and species abilities

**Commits:** `b8c97b3`, `7f8f926`, `d201190`, `1d02049`, `b6d6d9a`, `5170b45`, `3209a32`, `c730ce1`, `4d3939d`, `cc09405`, `9120a7c`.

Breeding passes on color/species identity only. The egg icon uses the same rendered color variant as the resulting slime. Ability identifiers are derived from the resulting species. Other offspring traits use fresh defaults: Normal size, Common rarity, non-shiny, no mutation, and a newly assigned favorite food. Parent IDs and generation remain as lineage metadata. Size, shiny, rarity, mutation, and food inheritance are out of scope.

**How to test:**

1. Run **3 Build, Deploy, Run**, load the copied `TaumTest_322865388` save, and enter the Slime Daycare.
2. Spawn two mature slimes and note their animal IDs:

   ```text
   slime_ranching_spawn_ranch_slime TaumTest_322865388 28
   slime_ranching_spawn_ranch_slime TaumTest_322865388 28
   ```
3. Set one parent to Red and one to Yellow. Give them different non-color traits so you can confirm those aren't passed to the offspring:

   ```text
   slime_ranching_set_color TaumTest_322865388 <red ID> Red
   slime_ranching_set_size TaumTest_322865388 <red ID> Giant
   slime_ranching_set_shiny TaumTest_322865388 <red ID> true
   slime_ranching_set_color TaumTest_322865388 <yellow ID> Yellow
   slime_ranching_set_size TaumTest_322865388 <yellow ID> Small
   slime_ranching_set_shiny TaumTest_322865388 <yellow ID> true
   ```

   Replace the placeholders with the IDs printed by the spawn commands. Confirm each parent has its chosen color, size, and shiny appearance.
4. Open the breeder, select both parents, start a cycle, and sleep until it completes. Collect the egg. Red + Yellow should produce an Orange Slime. Confirm the egg icon's visible tint matches the resulting slime's color. Its tooltip should show Orange species/color, Normal size, Common rarity, non-shiny, no mutation, parent links, and generation one higher than its parents.
5. Hatch the egg. Confirm the slime appears Orange and `slime_ranching_inspect <new animal ID>` reports the Orange species' ability identifier (`Nurturer`). It should remain Normal and non-shiny despite the parents' different sizes and shiny states. Its favorite food is assigned independently of the parents.
6. Save and reload. Confirm the egg/hatchling's color, ability identifier, and lineage remain stable and the egg icon still uses the same visible color variant as the slime.

**Expected:** color/species is the only inherited trait. The egg tint matches the resulting slime's rendered color; ability identifiers follow its species. Other offspring traits use the defaults above, while parent links and generation remain as ancestry metadata.

**Your results / issues:** pending.
## Remaining stages (pending your confirmation)
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

- **Current stage:** 5 - Color inheritance, matching egg tint, and species abilities.
- **Completed stages:** 1, 2, 3, and 4 - Daycare baseline; breeder selection/cycles/placement; incubator workflow; juvenile growth and breeding eligibility.
- **Next stage:** 6 - Breeder genome details and Chromatic Crystals, after you report Stage 5 results and say **continue**.
