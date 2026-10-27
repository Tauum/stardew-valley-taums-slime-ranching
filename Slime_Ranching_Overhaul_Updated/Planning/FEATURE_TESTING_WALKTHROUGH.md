# Today's Feature Testing Walkthrough

Updated: 2026-10-27  
Current branch: `feature/test-egg-metadata-options`  
Current HEAD: `9a16303` (`Remove temporary Ranch Slime petting friendship boost`)  
Test save: `TaumTest_322865388` (use this copied save, not the main farm).

## How to use this walkthrough

Test only the current stage. Reply with the results or issues. I will help resolve those first. When you say **continue**, I will present the next stage and update this file. Do not advance the stage until you say to continue.

## Current stage: 3 - Incubator workflow, hatch, and naming

**Commits:** `5e851e8`, `65450f5`, `7d72cc7`, `b602304`, `99c7ca5`, `9571b25`, `1d60ec2`, `8ebdc7e`, `b7cb3f9`.

This stage covers Common and genome-specific egg incubation, 
the two-day job and recovery across menu/game reloads, hatch notifications,  and the Name & Take prompt. 
The newly hatched slime should retain its egg genome and remain in the Daycare after collection.

**How to test:**

1. Run **3 Build, Deploy, Run**, load `TaumTest_322865388`, and enter the Slime Daycare.
2. In the SMAPI console, run `slime_ranching_give_test_eggs TaumTest_322865388` and `slime_ranching_give_incubator TaumTest_322865388` if you need test eggs or an incubator. Place the incubator inside the Daycare. Check that its preview, placement, and movement-blocking area cover exactly 3×2 tiles and the custom incubator ghost follows that area.

   The egg command still works with just the save name. Optional `key=value` metadata overrides let you control the genomes. For example:

   ```text
   slime_ranching_give_test_eggs TaumTest_322865388 species=Mint_Slime secondary=none color=#65DDBB size=Giant rarity=Rare shiny=true food=(O)168 generation=4 mutation=ShadowClone
   ```

   Options are `species`, `secondary`, `color` (`#RRGGBB`), `size`, `rarity`, `shiny`, `food`, `generation`, and `mutation`. Values apply to both eggs; their genome IDs remain distinct. Food must be an eligible monster-drop or fishing-trash qualified item ID, such as `(O)684` or `(O)168`.

   To instead generate one egg with randomized traits, run the command with the standalone `random` option (don't combine it with `key=value` options):

   ```text
   slime_ranching_give_test_eggs TaumTest_322865388 random
   ```
3. Hold a test egg and use the incubator. Select the egg, start incubation, and confirm the egg leaves your inventory.
4. Close and reopen the incubator menu. Confirm the job remains active, shows time remaining, and the same egg can't be started in another incubator.
5. Save and reload the copied save while incubation is active. Reopen the incubator and confirm its progress persisted.
6. Sleep through the two-day incubation. Check for the ready notification and the hatchling in the Daycare. Open the incubator and choose the ready slime.
7. Enter a custom name in the naming prompt. Confirm the slime is collected into the Daycare with that name and the incubator becomes available again.
8. Reopen the incubator and save/reload once more. Confirm the hatchling isn't duplicated and its genome-specific appearance remains.

**Expected:** the incubator consumes one eligible egg, recovers its job and hatchling across reloads, then lets you name and collect exactly one Ranch Slime with the egg's genome.

**Your results / issues:** pending.
## Remaining stages (pending your confirmation)
### Stage 4 — Juvenile growth and breeding eligibility

**Commits:** `c46caa9`, `6b4fd17`, `3989b8d`.

Juvenile maturation and the authoritative external age clock. Ranch Slimes mature after 28 in-game days. Test that a newborn grows to maturity and cannot be selected as a breeder parent until it reaches age 28.

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

- **Current stage:** 3 - Incubator workflow, hatch, and naming.
- **Completed stages:** 1 and 2 - Daycare baseline; breeder selection, cycles, placement, and break recovery.
- **Next stage:** 4 - only after you report Stage 3 results and say **continue**.
