# Today's Feature Testing Walkthrough

Updated: 2026-10-29
Paused test checkpoint: `feature/ranch-slime-size-scaled-shadows` at `3799f68` (`Scale Ranch Slime ground shadows by genome size`; now merged into `master`)
Test save: `TaumTest_322865388` (use this copied save, not the main farm).
Status: **Paused** at Stage 6 on 2026-10-28 during the 2.0 design pivot. Resume this stage only when requested.

The separate 2.0 implementation sequence is tracked in `DEVELOPMENT_NOTES_2.0.md`. Feature 1, the save-safe data foundation, passed copied-save acceptance. Feature 2 uses the vanilla FarmHouse maps; test each Daycare tier separately from the paused 1.x Stage 6 below.

## 2.0 active test focus — Feature 2: FarmHouse-layout Daycare, upgrades, and renovations

**Status:** Implementation updated; in-game acceptance pending. The custom generated greybox has been superseded. The Daycare follows the FarmHouse progression (base plus three upgrades), keeps AnimalHouse behavior, adds a separate **Slime Daycare Renovations** choice to Robin's Renovate flow at tier 2, and adds a Daycare-specific cellar at tier 3. Renovation state is saved per Daycare; the crib-removal option is excluded.

**Map references:** Compare with Stardew's FarmHouse, FarmHouse1, FarmHouse2, FarmHouse_Cellar, and HomeRenovations. The original room experiments remain in the gallery as historical references; they are not the current Daycare layout.

1. Run **3 Build, Deploy, Run** and load the current copied **TaumTest** save prepared for 2.0, not the main farm. The old save ID at the top belongs to the paused 1.x checkpoint.
2. Enter the basic Slime Daycare. Confirm its interior is the same layout as the base farmhouse, with no kitchen machines, fireplace, Barn hay hopper, hay stacks, or vanilla egg incubator. Only Daycare-specific machines should appear. Check that Ranch Slimes are still residents of the Daycare and roam normally.
3. Upgrade the Daycare once and confirm FarmHouse1; upgrade to tier 2 and confirm FarmHouse2. Check capacities, resident slimes, and wallpaper/flooring at each step.
4. At tier 2, talk to Robin and open **Renovate**. Confirm the choice menu has separate **Farmhouse Renovations** and **Slime Daycare Renovations** options. The Daycare option should contain the room renovations without mixing in farmhouse options. Try **Open Bedroom**, **Add Southern Room**, and **Open Corner Room**. Confirm choices modify only the Daycare and persist after leaving/reloading. Confirm the renovation is blocked if a slime, object, or player occupies its work area, and that no crib-removal option appears.
5. Upgrade to tier 3. Confirm the vanilla cellar stairs appear, entering leads to the Daycare's own cellar, and exiting returns to the Daycare. Confirm the cellar has no default casks and that the farmhouse cellar and its contents remain separate. Recheck wallpaper and flooring in expanded rooms.
6. Save, return to title, and reload. Verify the Daycare tier, renovations, cellar contents, decorations, capacity, and slimes. Confirm the kitchen machines, fireplace, hay hopper, hay stacks, vanilla egg incubator, and default cellar casks remain absent from the Daycare, while the farmhouse and vanilla Barn/Slime Hutch remain unchanged.
7. Record any AnimalHouse behavior that differs from farmhouse behavior (for example room transitions, furniture placement, or blocked animal paths). Do not mark Feature 2 accepted until all four tiers, renovations, cellar entry/exit, animal behavior, and decoration pass in-game.

Reply with any collision, sizing, or warp issues and a screenshot if the room differs from the texture preview. Do not mark Feature 2 complete until the in-game check passes.

## How to use this walkthrough

Test only the current stage. Reply with the results or issues. I will help resolve those first. When you say **continue**, I will present the next stage and update this file. Do not advance the stage until you say to continue.

## Current stage: 6 - Breeder genome details and Chromatic Crystals

**Commits:** `2c94486`, `42a4a00`, `b74b6cc`.

This stage covers the parent genome details shown in the breeder and Chromatic Crystals, which focus a breeding cycle on a chosen offspring color. The crystal is consumed when the cycle starts. Without one, offspring color follows the normal parent-color breeding result.

**How to test:**

1. Run **3 Build, Deploy, Run**, load the copied `TaumTest_322865388` save, and enter the Slime Daycare.
2. Spawn two mature Ranch Slimes and note their IDs:

   ```text
   slime_ranching_spawn_ranch_slime TaumTest_322865388 28
   slime_ranching_spawn_ranch_slime TaumTest_322865388 28
   ```

3. Give the parents distinct colors and traits, replacing the ID placeholders with the IDs printed above:

   ```text
   slime_ranching_set_color TaumTest_322865388 <first ID> Blue
   slime_ranching_set_size TaumTest_322865388 <first ID> Giant
   slime_ranching_set_shiny TaumTest_322865388 <first ID> true
   slime_ranching_set_color TaumTest_322865388 <second ID> Yellow
   slime_ranching_set_size TaumTest_322865388 <second ID> Small
   ```

4. Give yourself the seven test crystals. Make room for seven inventory items first:

   ```text
   slime_ranching_give_crystals TaumTest_322865388
   ```

5. Open the breeder and select the two parents. Confirm the pod and parent details show their names/species, colors, sizes, and shiny state correctly. Change to the breeder's **Settings** tab and confirm the color-focus selector is visible and usable.
6. Return to **Breeding**. Click the crystal selector until it says **Red**, then start the cycle. Confirm one Red Chromatic Crystal is consumed. Sleep until the cycle completes and collect the egg. Its tooltip and visible egg tint should identify a Red Slime, overriding the parents' Blue/Yellow color result.
7. Open the breeder again and check that the selected parents' genome details remain correct. Optionally hatch the egg and run `slime_ranching_inspect <new animal ID>` to confirm the offspring is Red and its ability follows Red Slime.

**Expected:** parent genome details are readable and match the configured parents. Selecting a crystal shows the focused color, consumes exactly one matching crystal when the cycle starts, and produces an egg of that color.
## Remaining stages (pending your confirmation)
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

- **Current stage:** 6 - Breeder genome details and Chromatic Crystals.
- **Completed stages:** 1, 2, 3, 4, and 5 - Daycare baseline; breeder selection/cycles/placement; incubator workflow; juvenile growth and breeding eligibility; color/species inheritance and offspring defaults.
- **Next stage:** 7 - Enrichment furniture and autonomous activity schedule, after you report Stage 6 results and say **continue**.
