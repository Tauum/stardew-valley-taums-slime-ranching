# Completed Feature Test Stages

This file keeps completed test stages and their outcomes for reference. The active stage stays at the top of [FEATURE_TESTING_WALKTHROUGH.md](FEATURE_TESTING_WALKTHROUGH.md).

## Stage 5 - Color inheritance, matching egg tint, and species abilities

**Commits:** `b8c97b3`, `7f8f926`, `d201190`, `1d02049`, `b6d6d9a`, `5170b45`, `3209a32`, `c730ce1`, `4d3939d`, `cc09405`, `9120a7c`.

**What this group does:** breeding passes on color/species identity only. The egg icon uses the same rendered color variant as the resulting slime, and the ability identifier follows the offspring species. Other offspring traits start at Normal size, Common rarity, non-shiny, and no mutation; favorite food is assigned independently. Parent IDs and generation remain as lineage metadata.

**Recorded results:** the user configured Red/Giant/shiny and Yellow/Small/shiny parents. Their egg tooltip showed Orange Slime, Orange color, Normal size, Common rarity, no mutation, and Generation 1. The user hatched it and ran `slime_ranching_inspect 2635086060066083871`; it reported Orange Slime, color `#FF9500`, Normal, non-shiny, Common, no mutation, Generation 1, `[Nurturer]`, Joja Cola, and age `1/28` days. This confirms the offspring genome defaults and species ability. The user then directed that Stage 5 be closed and Stage 6 started; egg-icon visual comparison and reload persistence were not reported.

## Stage 1 — Daycare and Ranch Slime baseline

**Commits:** `05208aa`, `5f5e7f0`, `bde7553`, `d18fd18`, `f9a33ed`, `0d51fdd`, `853d144`.

**What this group does:** establishes the mod and Rider build/deploy/run configurations, the external slime registry, the Slime Daycare and Ranch Slime animal baseline, and recovery when a Ranch Slime is removed or the save is reloaded. It also includes the Daycare placement/entrance adjustments from this early part of the history. Later commits change the building exterior/footprint; those are covered by Stage 10 in the active walkthrough.

**How to test:**

1. In Rider, run **3 Build, Deploy, Run**. Confirm the build completes and SMAPI loads Taum's Slime Ranching without an error. The existing CS9057 analyzer/compiler version warning is known; it is not a build failure.
2. Load `TaumTest_322865388`. In the SMAPI console, run:

   ```text
   slime_ranching_inspect
   ```

   Confirm the command responds and reports the current room's slime information.
3. Enter the Slime Daycare. Confirm you can enter and move around. If you need a Ranch Slime, run:

   ```text
   slime_ranching_spawn_ranch_slime TaumTest_322865388 28
   ```

   Confirm it appears in the Daycare, can be approached/petted, and remains present after sleeping and reloading this copied save.
4. If you have a vanilla Slime Hutch, run `slime_ranching_inspect` inside it too. Confirm vanilla Green Slimes are still reported as vanilla and unchanged.
5. Check the installed mod folder's `data/saves/TaumTest_322865388/` directory. Confirm the mod registry exists there and is separate from the game's save file.
6. Optional safety/tool check: run `slime_ranching_backup_external_data TaumTest_322865388` and confirm it reports a ZIP backup. Run `slime_ranching_inspect <Ranch Slime animal ID>` to inspect one Ranch Slime's external record.

**Expected:** the Ranch Slime animal and its genome record survive sleep/reload; vanilla Hutch slimes are not converted or managed by this mod.

**Recorded results:**

- `slime_ranching_inspect` in the vanilla Slime Hutch reported 19 vanilla `GreenSlime` monsters left unchanged and 5 Ranch Slime genome records in the external registry. This check passed.
- The Animals menu showed the slime as petted while its friendship hearts remained empty. A temporary boost made one full heart visible and was confirmed working; it has since been removed. Normal friendship progression is restored in code. Friendship points already written to the copied game save remain there; this does not block breeder testing.
- The user confirmed the petting heart display test worked. Vanilla slimes were unchanged and the external registry contained five Ranch Slime records.

## Stage 2 - Breeder selection, cycles, placement, and break recovery

**Commits:** `384d6e2`, `5b101e4`, `b0c7a35`, `d597a80`, `49525c3`, `18f4433`, `1bb64f1`, `14a8de1`, `d2a49e7`, `06d4222`.

**What this group does:** adds scrollable parent selection, a two-day breeding cycle, parent reservation and rest, cancellation, offspring egg creation, and breeder-specific placement/rendering over a native five-by-two Stardew furniture footprint. Active-cycle parents are represented in the breeder artwork and hidden from the Daycare view. Breaking a breeder clears its cycle and selection, returns its parents to roaming, and leaves a replacement breeder blank.

**How to test:**

1. On the copied `TaumTest_322865388` save, place the breeder in the Daycare. Check the five-by-two placement preview, obstruction rejection, collision, and foreground layering.
2. Spawn two mature parents with `slime_ranching_spawn_ranch_slime TaumTest_322865388 28`. Select both and start a cycle. Confirm their world sprites disappear while the cycle is active.
3. Cancel a cycle. Confirm both slimes reappear and roam, and become available for selection again.
4. Start another cycle and break the breeder before it finishes. Confirm the parents reappear and roam. Place a replacement at the same or another location; its selection should be blank and the old cycle should not return.
5. Start a fresh cycle and sleep two in-game days. Confirm the parents reappear, the ready notification appears, and the egg can be collected. Confirm the parents' rest period is shown if you try to reuse them immediately.

**Recorded result:** the user confirmed the breeder placement/preview behavior was working and marked Stage 2 complete. The placement ghost uses Stardew's live preview coordinates; the 5x2 footprint remains centered on the cursor tile. The build succeeds with the known analyzer/compiler-version warning.

## Focused verification — size-scaled Ranch Slime shadows

**Commit:** `3799f68`.

**What this adds:** the ground shadow under a Ranch Slime scales with the slime's genome size using the same multipliers as its sprite. Tiny legacy records normalize to Small; current Small, Normal, Large, and Giant slimes use their corresponding visual scales.

**Recorded result:** the user reported that everything looked fine. This focused visual check is complete.

## Stage 3 - Incubator workflow, hatch, and naming

**Commits:** `5e851e8`, `65450f5`, `7d72cc7`, `b602304`, `99c7ca5`, `9571b25`, `1d60ec2`, `8ebdc7e`, `b7cb3f9`, `7a6f56f`, `c1567f2`.

This stage covers Common and genome-specific egg incubation, the configurable hatch timer (15 days by default), recovery across menu/game reloads, hatch notifications, and the Name & Take prompt. Jobs already in progress keep their original ready date. The newly hatched slime should retain its egg genome and remain in the Daycare after collection.

**How to test:**

1. Run **3 Build, Deploy, Run**, load `TaumTest_322865388`, and enter the Slime Daycare.
2. In the SMAPI console, run `slime_ranching_give_test_eggs TaumTest_322865388` and `slime_ranching_give_incubator TaumTest_322865388` if you need test eggs or an incubator. Place the incubator inside the Daycare. Check that its preview, placement, and movement-blocking area cover exactly 3-by-2 tiles and the custom incubator ghost follows that area.

   The egg command still works with just the save name. Optional `key=value` metadata overrides let you control the genomes. For example:

   ```text
   slime_ranching_give_test_eggs TaumTest_322865388 species=Mint_Slime secondary=none color=#65DDBB size=Giant rarity=Rare shiny=true food=(O)168 generation=4 mutation=ShadowClone
   ```

   Options are `species`, `secondary`, `color` (`#RRGGBB`), `size`, `rarity`, `shiny`, `food`, `generation`, and `mutation`. Values apply to both eggs; their genome IDs remain distinct. Food must be an eligible monster-drop or fishing-trash qualified item ID, such as `(O)684` or `(O)168`.

   To instead generate one egg with randomized traits, run the command with the standalone `random` option (don't combine it with `key=value` options):

   ```text
   slime_ranching_give_test_eggs TaumTest_322865388 random
   ```
3. Open the incubator menu before starting. Confirm the displayed default duration is **15 days**. It can be changed in Generic Mod Config Menu from 1 to 28 days; changing it affects new cycles only.
4. If you want to finish the workflow test quickly, set **Incubation days** to 1 in GMCM before starting a fresh cycle. Select a test egg and start incubation; confirm exactly one egg leaves your inventory and the menu shows the chosen duration. Save and reload while the job is active; confirm its ready date and progress remain unchanged.
5. Try to start that same genome-specific egg in a second incubator. Confirm it is unavailable there.
6. Sleep until the active cycle is ready. Confirm the notification appears and the unclaimed hatchling is hidden from the Daycare and breeder list. Open the incubator and choose the ready slime.
7. Enter a custom name in the prompt. Confirm the slime appears in the Daycare with that name and the incubator becomes available again.
8. Reopen the incubator and save/reload once more. Confirm the hatchling isn't duplicated and its genome-specific appearance remains.

**Expected:** the incubator consumes one eligible egg, uses the selected duration (15 days by default), recovers its job and hatchling across reloads, then lets you name and collect exactly one Ranch Slime with the egg's genome.

**Recorded result:** The user confirmed Stage 3 testing is complete with no remaining issues.

## Stage 4 - Juvenile growth and breeding eligibility

**Commits:** `c46caa9`, `6b4fd17`, `3989b8d`.

Ranch Slimes mature at 28 in-game days. Their age and maturity are synchronized through the mod's external registry, and juveniles must be excluded from breeder parent selection until mature.

**How to test:**

1. Run **3 Build, Deploy, Run**, load the copied `TaumTest_322865388` save, and enter the Slime Daycare.
2. In the SMAPI console, spawn a newborn: `slime_ranching_spawn_ranch_slime TaumTest_322865388 0`. Note its animal ID from the command output. Run `slime_ranching_inspect <animal ID>` and confirm it reports age 0 and juvenile/not mature.

   This test command controls age only. It registers the slime with a generated genome ID and the default Ranch Slime traits: green color, no secondary species, Normal size, Common rarity, not shiny, no mutation, and generation 0. Its favorite food is assigned by the mod. These traits are held steady/defaulted so this stage tests age and breeding eligibility; genome trait controls are covered in later stages.

   To keep the chosen age but randomize the genome traits, add the `random` tag:

   ```text
   slime_ranching_spawn_ranch_slime TaumTest_322865388 0 random
   ```

   This randomizes color/species, optional secondary species, size, rarity, shiny chance, favorite food, generation, and mutation. For the age-gating checks below, use the default form first so the test focuses on maturity.
3. Open the Slime Breeder. Confirm the newborn is shown as growing or otherwise unavailable, and cannot be selected as a parent.
4. Sleep one in-game day, then inspect the same animal again. Confirm its age advanced and it remains unavailable as a parent.
5. For a quick maturity-boundary check, spawn another slime at age 27 with `slime_ranching_spawn_ranch_slime TaumTest_322865388 27`. Before sleeping, the breeder should say `1d to maturity` (one day remaining, not one day old) and prevent selection. Sleep one day, then confirm it reaches age 28 and can now be selected in the breeder.
6. Save and reload the copied save. Inspect both slimes again and confirm their ages and maturity/eligibility remain correct.

**Expected:** juveniles gain age as in-game days pass, remain ineligible for breeding below 28 days, and become selectable when they reach 28 days. Their age and maturity survive save/reload.

**Recorded result:** The user confirmed a 27-day-old Ranch Slime became breedable after sleeping one in-game day. The maturity boundary passed, and the user marked Stage 4 complete.
