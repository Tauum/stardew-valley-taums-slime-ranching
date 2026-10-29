# Today's Feature Testing Walkthrough

Updated: 2026-10-29
Paused test checkpoint: `feature/ranch-slime-size-scaled-shadows` at `3799f68` (`Scale Ranch Slime ground shadows by genome size`; now merged into `master`)
Test save: `TaumTest_322865388` (use this copied save, not the main farm).
Status: **Paused** at Stage 6 on 2026-10-28 during the 2.0 design pivot. Resume this stage only when requested.

The separate 2.0 implementation sequence is tracked in `DEVELOPMENT_NOTES_2.0.md`. Feature 1, the save-safe data foundation, passed copied-save acceptance. Feature 2 uses the vanilla FarmHouse maps; test each Daycare tier separately from the paused 1.x Stage 6 below.

## 2.0 active test focus — Feature 2: FarmHouse-layout Daycare, upgrades, and renovations

**Status:** Implementation updated; in-game acceptance pending. Taum requested on 2026-10-29 that 2.0 features continue before in-game checks, with these steps collected for a later bulk test pass. The custom generated greybox has been superseded. The Daycare follows the FarmHouse progression (base plus three upgrades), keeps AnimalHouse behavior, adds a separate **Slime Daycare Renovations** choice to Robin's Renovate flow at tier 2, and adds a Daycare-specific cellar at tier 3. Renovation state is saved per Daycare; the crib-removal option is excluded.

**Map references:** Compare with Stardew's FarmHouse, FarmHouse1, FarmHouse2, FarmHouse_Cellar, and HomeRenovations. The original room experiments remain in the gallery as historical references; they are not the current Daycare layout.

1. Run **3 Build, Deploy, Run** and load the current copied **TaumTest** save prepared for 2.0, not the main farm. The old save ID at the top belongs to the paused 1.x checkpoint.
2. Enter the basic Slime Daycare. Confirm its interior is the same layout as the base farmhouse, with no kitchen machines, fireplace, Barn hay hopper, hay stacks, or vanilla egg incubator. Only Daycare-specific machines should appear. Check that Ranch Slimes are still residents of the Daycare and roam normally.
3. Buy the first Daycare upgrade. Confirm the interior changes from **FarmHouse** to **FarmHouse1**, capacity becomes 20, and Daycare renovations unlock.
4. At this first upgrade, talk to Robin and open **Renovate**. Confirm separate **Farmhouse Renovations** and **Slime Daycare Renovations** options. The Daycare option should contain room renovations without mixing in farmhouse options. Try **Open Bedroom**, **Add Southern Room**, and **Open Corner Room**. Confirm choices modify only the Daycare and persist after leaving/reloading. Confirm renovation is blocked if a slime, object, or player occupies its work area, and no crib-removal option appears.
5. With one ordinary Wallpaper item and one Flooring item in inventory, use each on an unobstructed valid Daycare wall or floor tile. Confirm each appears and one item is consumed. Check that the farmhouse decoration is unchanged. Leave and re-enter the Daycare and confirm both choices persist. Repeat after a room renovation.
6. Buy the second Daycare upgrade. Confirm the interior changes to **FarmHouse2**, capacity remains 20, and Daycare renovations and decoration persist.
7. Buy the final Daycare upgrade. Confirm it retains **FarmHouse2**, capacity becomes 30, and vanilla cellar stairs appear. Enter the Daycare's own cellar and exit back to the Daycare. Confirm the cellar has no default casks and the farmhouse cellar and its contents remain separate. Recheck wallpaper and flooring in expanded rooms.
8. Save, return to title, and reload. Verify the Daycare tier, renovations, cellar contents, decorations, capacity, and slimes. Confirm the kitchen machines, fireplace, hay hopper, hay stacks, vanilla egg incubator, and default cellar casks remain absent from the Daycare, while the farmhouse and vanilla Barn/Slime Hutch remain unchanged.
9. Record any AnimalHouse behavior that differs from farmhouse behavior (for example room transitions, furniture placement, or blocked animal paths). Do not mark Feature 2 accepted until all four maps, renovations, cellar entry/exit, animal behavior, and decoration pass in-game.

Reply with any collision, sizing, or warp issues and a screenshot if the room differs from the texture preview. Do not mark Feature 2 complete until the in-game check passes.

## 2.0 bulk-test check — Feature 3 ledger and Feature 7 Maru repair (partial implementation)

**Status:** Schema migration, diagnostic, construction/breeding milestone hooks, Robin's prompt, and Maru's material-delivery handoff are implemented. The opening scenes, starter egg, and normal-play route to construction are not implemented. The Maru handoff can be tested with a guarded setup command on a disposable save with a completed Daycare. Keep both feature acceptances pending.

### Save migration check

1. Make a fresh copy of a 2.0 save with schema 12 or 13. Keep the original and the main farm untouched.
2. Run **3 Build, Deploy, Run**, then load that copied save.
3. In the SMAPI console, run `slime_ranching_quest_status`. Confirm it reports schema **14** as the target schema and lists the Sanctuary milestones. On a new save, `lewis_concern` should report `prerequisites met` (available but incomplete); the remaining milestones should report `waiting`.
4. Save, return to title, reload the same copied save, and run `slime_ranching_quest_status` again. Confirm schema 14 is retained and the milestone list is unchanged.

### Maru repair handoff check — can be tested before the opening scenes are complete

1. Use a disposable single-player save with a fully constructed Slime Daycare. To test the handoff before the opening scenes exist, run `slime_ranching_test_maru_repair <exact save folder name>`. This deliberately marks the Lewis, Wizard, Marnie, and construction ledger milestones on that save; it does not play their scenes. Confirm the command logs that the test is primed, then run `slime_ranching_quest_status` and verify those four milestones are complete. When the opening story is implemented, repeat this check on a separate copy using normal play instead of the helper.
2. Talk to Robin. Choose **Later** once and confirm `robin_pool_prompt` stays pending and Robin can offer the prompt again. Then choose **I'll talk to Maru**. Confirm `robin_pool_prompt=complete`, the mission is not added to the journal that day, and the status command reports an availability day one day after the current day. Reload on the same day and confirm both the date and prompt milestone persist.
3. Sleep one night. Confirm the Maru repair mission appears in the journal and `maru_repair_requested=complete` appears in `slime_ranching_quest_status`.
4. Interact with Maru while missing at least one repair material. Confirm she lists the missing amount and the inventory is unchanged.
5. With all materials, interact with Maru and choose **Not yet**. Confirm no items are consumed and the quest remains active. Interact again and choose **Hand over materials**.
6. Confirm exactly 1 Battery Pack, 5 Iron Bars, and 10 Refined Quartz are removed, the journal mission completes, and `maru_repair_completed=complete` appears in the status command.
7. Use the Mutagenic Pool. If the fixed Daycare Pool is not present in this build, add one with `slime_ranching_give_mutagenic_pool <exact save folder name>` and place it inside the Daycare. Confirm it opens normally after repair. Save, return to title, and reload. Confirm the materials remain spent, the mission does not reappear, the repair milestone remains complete, and the Pool stays active.
8. Keep Feature 3 and Feature 7 acceptance pending until the full Lewis-to-first-breeding flow, interrupted/skipped-scene recovery, Robin prompt, delivery cases, and Pool activation have been tested in-game.

## 2.0 bulk-test check — Feature 4: Ranch Slime maturity

**Status:** The save-backed age cap and native FarmAnimal maturity data use the approved seven-day duration. In-game age, eligibility, and reload checks are pending.

1. On a disposable single-player save with a completed Daycare, run `slime_ranching_spawn_ranch_slime <exact save folder name> 0`. Note the animal ID printed in the SMAPI console.
2. Run `slime_ranching_inspect <animal ID>`. Confirm the slime reports age `0/7` and cannot be selected as a breeder parent.
3. Sleep six nights, inspecting after each wakeup. Confirm age advances by one per in-game day, reaches `6/7`, and the breeder still marks it as growing and blocks selection.
4. Sleep one more night. Confirm the slime reports `7/7` and is selectable as a breeder parent.
5. Save, return to title, and reload. Confirm age remains `7/7`, the slime stays mature, and vanilla farm animals retain their normal maturity behavior.
6. On a separate disposable copy with an existing Ranch Slime older than seven days under the former 28-day prototype, confirm it remains mature after load and its age is normalized to `7/7`.

## 2.0 bulk-test check — Feature 5: basic breeder timing

**Status:** New basic-tier cycles use the approved 18-day duration. Existing cycle dates remain unchanged. In-game timing, cancellation, output, and reload checks are pending.

1. On a disposable single-player save with a completed Daycare, run `slime_ranching_spawn_ranch_slime <exact save folder name> 7` twice and note both animal IDs. Run `slime_ranching_give_breeder <exact save folder name>`, place the Breeder in the Daycare, and open it.
2. Select the two mature Ranch Slimes. Confirm both appear as the selected parents and the basic cycle displays **18 Days**.
3. Start breeding. Confirm the UI reports 18 days remaining and the parent reservations persist after closing and reopening the menu.
4. Sleep five nights. Confirm 13 days remain. Save, return to title, reload, and confirm the same ready date and remaining time are shown.
5. Before the ready day, cancel the cycle. Confirm no egg is created, both parents are selectable again, and starting a new cycle sets a new 18-day ready date.
6. Let the replacement cycle reach its ready day. Confirm the egg-ready notice appears, both parents are released into the Daycare, the egg remains collectible once, and the saved genome remains available after reload.

## 2.0 bulk-test check — Feature 6: fixed incubator timing and capacity

**Status:** New incubation jobs take seven in-game days; saved active jobs retain their recorded ready day. Active-job cancellation now requires confirmation, destroys the unique egg identity, and awards one vanilla Fried Egg. Existing hatch naming and full-capacity blocking remain in place. In-game checks are pending.

1. On a disposable single-player save with a completed Daycare, run `slime_ranching_give_test_eggs <exact save folder name> random` and `slime_ranching_give_incubator <exact save folder name>`. Place the incubator inside the Daycare.
2. Open the incubator with the test egg in inventory. Confirm the menu shows **7 Days**, select the egg, and start incubation. Confirm exactly one egg is removed.
3. Sleep one night and reopen the incubator. Confirm progress is `1 / 7 days`. After six total nights, confirm it is still incubating at `6 / 7` and no animal has hatched.
4. Sleep the seventh night. Confirm exactly one Ranch Slime hatches in the Daycare with the egg's stored genome. Reopen the incubator and use **Name & Take**; confirm the name prompt appears and the egg cannot hatch again from a copied item with the same genome ID.
5. To verify capacity blocking, fill the Daycare to its 10-resident limit with mature Ranch Slimes, start one egg, and sleep seven nights. Confirm the incubator reports it is ready but waiting for space, and the Daycare never exceeds 10 residents. Sell one test Ranch Slime through its normal Animal Query menu (its configured sell price is 0g), sleep once, and confirm the waiting hatch then appears.
6. On a separate copied save, start an incubation with the previous build's 15-day config, save, then update to this build. Confirm that job retains its original ready day while a new job takes seven days.
7. On a copied save with a free inventory slot, start another incubation and reopen that machine before hatch. Select **Cancel** and then **Keep incubating**; confirm the same egg and progress remain. Select **Cancel** again and confirm **Destroy egg**; verify the job disappears and exactly one Fried Egg appears in inventory.
8. Before canceling, keep a copy of that same unique egg item in a chest. After confirming cancellation, try to incubate the copied item; confirm the incubator rejects it. Also attempt cancellation with a full inventory and verify the job remains intact and the game explains that space is needed for the Fried Egg.
9. After confirming cancellation, sleep to save and reload. Verify the job stays canceled, the Fried Egg remains, and the copied egg ID remains unusable. On a separate copy, cancel and return to title without saving; reload and confirm the pending transaction restores the original incubation and does not award an unsaved Fried Egg.
10. Load a schema-14 copy with an active incubation, allow migration to schema 16, save/reload, and verify its ready day is unchanged. Confirm a saved cancellation stays finalized after reload.

## How to use this walkthrough

The 2.0 checks are being collected for one bulk test pass at Taum's request. Run the checks marked ready on disposable copied saves, then report results and issues together. Keep each feature's acceptance pending until its in-game checks pass. The separate 1.x sequence remains paused at Stage 6; resume it only if requested.

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
