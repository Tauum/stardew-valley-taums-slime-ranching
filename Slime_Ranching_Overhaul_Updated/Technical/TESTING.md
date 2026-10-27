# Test strategy

## Current foundation smoke test

Use a copy of a single-player save with at least one existing Slime Hutch and one mine slime.

1. Load it with the mod and check the SMAPI log for a successful external registry load. In the hutch, `slime_ranching_inspect` should report the vanilla slime count and no managed slimes.
2. Check `<installed mod folder>/data/saves/<save name>/first-seen/`: `first-seen-v1.json`, the original save file, and `SaveGameInfo` (if present) should exist. Keep the first snapshot and compare its timestamp and content after another load; it must not be replaced. Nothing should be created in the game's save folder by this mod.
3. Save and reload. The mod folder should now contain `slime-registry-v1.json`; the hutch should still report no managed slimes. Mine slimes should behave as before.
4. Repeat with a new save and an empty hutch; the first-seen JSON may exist before the game's first on-disk save.
5. Test a damaged first-seen file on a disposable mod-data copy. If the snapshot cannot be verified, the mod should log an error and leave its registry disabled for that load. Deleting the file permits a new baseline from the current save; it cannot reconstruct the original first-seen state.

The `TaumTest_322865388` copy passed the basic load, save, reload, and hutch inspection checks on 2026-10-26: seven vanilla slimes remained unmanaged and the external registry reloaded with zero records. Other foundation checks remain open.

## Vanilla slime isolation

1. Load the disposable test save and note the vanilla GreenSlime count in its Slime Hutch.
2. Run `slime_ranching_inspect`; confirm it reports the count and that those monsters remain unchanged.
3. Save and reload; verify the same vanilla slimes remain. The mod has no command which clears or converts them.

## Ranch Slime farm-animal prototype

This is a new custom farm-animal type in a separate Slime Daycare animal-house building; it does not modify existing monsters. The Daycare is free and builds instantly for testing. It uses custom resized exterior art and the Barn interior map. Ranch Slimes use the game's vanilla Green Slime body frames and face art recomposed as a FarmAnimal sprite sheet.

1. Deploy the mod to a disposable save, open Robin's construction menu, and build the Slime Daycare.
2. After construction finishes, run `slime_ranching_spawn_ranch_slime TaumTest_322865388` in the SMAPI console.
3. Verify the new animal is assigned to the Daycare and can be petted; existing hutch and mine slimes should be unchanged.
4. Sleep to save. Check the log for `Added 1 Ranch Slime genome record(s) to the external registry`, then inspect `data/saves/<save name>/slime-registry-v1.json`: the record should have `FarmAnimalId` matching the animal and a `Genome.Id` in that external file.
5. Reload and save again. Confirm the same `FarmAnimalId` retains the same `Genome.Id` and no duplicate record is added.
6. Run `slime_ranching_inspect` in a Slime Hutch and confirm its vanilla slime count has not changed.

Common and genome-specific Ranch Slime Eggs can now hatch in the custom Slime Incubator. Each Common Egg receives a fresh base Common genome when inserted. Custom exterior art is present; its final in-game alignment is still being checked. The interior still uses the Barn map.

## Slime Daycare building art and upgrades

1. Deploy the current build and ask Robin to build the Slime Daycare. Confirm the base building has the full pink roof and slime crest, rather than Barn art. It remains free and instant for testing.
2. Ask Robin to upgrade it. Upgrade 1 should complete instantly, change the roof to blue, and increase capacity. Ranch Slimes should remain assigned and the breeder should still recognize the building.
3. Upgrade again. Upgrade 2 should use the purple roof and larger upper window, retain the animals, and allow the breeder/spawn command to find it.
4. Confirm regular Barns are unchanged. The original art is preserved; the Daycare loads directly resized game textures from `assets/Buildings`. Building definitions and upgrade links are in `Services/BuildingContent.cs`.
5. Compare the building foundation's bottom-left corner with the blue footprint reference. Keep the roof fully visible; do not shift the fixed-size texture so far left that its roof is cropped. The fences can be aligned separately later.
6. During an active breeder cycle, check that the pale and rose hearts appear 30 screen pixels higher than their original position, are 20% larger, and use a 4-second float cycle. These latest heart changes have only been compiled so far.

## Breeder pair reservation and cycle timer

1. In the disposable `TaumTest_322865388` save, run `slime_ranching_spawn_ranch_slime TaumTest_322865388` twice so two Ranch Slimes live in the Daycare.
2. Run `slime_ranching_give_breeder TaumTest_322865388`, place the Slime Breeder inside the Daycare, then use the action button while pointing at it.
3. Click two Ranch Slime rows. Confirm each row highlights and the selected slimes appear in the chamber preview.
4. Click **Start 2-day Cycle**. This should reserve both selected parents automatically, stop their roaming, and start the timer. Confirm another breeder cannot offer them while reserved.
5. Close and reopen the menu or reload; confirm the timer resumes from the saved day instead of restarting.
6. Click **Cancel Cycle** and confirm the parents remain reserved. Click **Release Pair** and confirm they move again and return to the available list.

The pair, cycle dates, and resulting egg genome are stored in the mod-folder registry, not in game-save metadata. Parents remain in the Daycare and stop moving in place; the chamber is a menu preview, not a physical enclosure. The two-day cycle can produce an egg; verify its ready notification, collection, and persistence in game. The machine currently borrows the vanilla Egg-Press sprite as a visual placeholder.

## External genome-specific egg identity (prototype)

1. Deploy the current build to the disposable `TaumTest_322865388` save. Make room for two inventory items.
2. Run `slime_ranching_give_test_eggs TaumTest_322865388` in the SMAPI console. The mod should add two Ranch Slime Eggs with different qualified item IDs.
3. Inspect `<installed mod folder>/data/saves/TaumTest_322865388/slime-registry-v1.json`. It should now have two entries in `Eggs`; each has an `ItemId` and a genome with a unique `Id`, color, and liked food. Schema version should be 6. The game's save stores the inventory items; the inherited genome details live in this external registry.
4. Save, quit to title, and reload the same save. Confirm both eggs are still named separately and recognized. Check the external registry IDs still match each egg's item ID. For the all-save content definitions, the mod reads registered egg IDs from the mod's per-save registry files when `Data/Objects` loads.
5. Put one egg in a chest and repeat save/reload. Confirm both item identity and registry genome remain stable.

These command-created genomes are test fixtures only. The user confirmed an egg spawned successfully and its tooltip displayed color, size, rarity, mutation, generation, parent IDs, and favorite food. Save/reload, chest transfer, and incubator hatching remain to be verified. The converter still produces the generic Common Slime Egg. Back up the installed mod's `data` folder before testing if you need to preserve its external registry.

## Genome-specific egg incubation and hatching (prototype)

1. Use a disposable save with a built Slime Daycare. Run `slime_ranching_give_test_eggs TaumTest_322865388` and `slime_ranching_give_incubator TaumTest_322865388`.
2. Place the Slime Incubator inside the Daycare and use it. Select one of the genome-specific eggs from your inventory. Confirm exactly one item is consumed; sleep to persist both the game inventory and an `Incubations` entry with its egg ID and two-day ready date.
3. Reload while the egg is incubating. Confirm the menu reports the remaining days and the egg is not available to a second incubator.
   - The active-job panel should show species, rarity, generation, elapsed/total days, and a progress bar: 0% at insertion, 50% after one sleep, 100% when ready.
4. Sleep through the remaining time. At the start of the ready day, confirm a Ranch Slime appears in the Daycare and the HUD reports the hatch. Inspect `SlimeRecord.Genome` in the external registry; it should match the egg genome exactly.
5. Save and reload after hatching. Confirm the animal ID links back to that genome and any copied item with the same egg ID can't hatch another slime.
6. Confirm the new animal is visibly young, ages each day, and remains unavailable in the breeder for three days. Save/reload during growth; it should keep its age and become selectable on maturity.
7. Fill the Daycare before a hatch is ready. Confirm the egg stays in the incubator record and hatches after a resident is removed.
8. Convert a vanilla egg and insert the resulting Common Egg. It should hatch into a Green (`#00FF00`), Normal, Common Ranch Slime. Insert a second Common Egg and confirm it receives a different external genome ID. Vanilla Slime Eggs must remain unaccepted by this incubator; the vanilla Coop Incubator remains untouched.

The incubator runs inside the Daycare only, consumes Common or genome-specific Ranch Slime Eggs from inventory, and targets a two-day cycle. A Common Egg receives a fresh base genome when inserted; a breeder egg uses its existing genome. The incubator spawns the juvenile automatically when the next day begins. If the game and mod-folder state are saved at different moments, the external hatch record reserves the native animal ID and recovers against the next loaded game state. Duration and duplicate-item behavior are provisional and need playtesting.

## Breeder cycle egg output (prototype)

1. In the disposable save, make sure two Ranch Slimes are in the Daycare and give/place a Slime Breeder there. Select and reserve the pair, then start the two-day cycle.
2. Sleep through two days. On the morning it finishes, confirm one SMAPI HUD notification says a Ranch Slime Egg is ready. It should not repeat after reloading that day. Reopen the same breeder; its action button should now say **Collect Egg**.
3. Collect the egg. It should be a distinct Ranch Slime Egg with a new genome ID. Its genome should have `ParentA`, `ParentB`, and `Generation` fields; generation is one higher than the older parent. Current placeholder inheritance selects each parent's existing value independently and stores the result once.
4. Verify the parents are available again. Save, quit to title, and reload. The egg item ID should still resolve to the same external genome in `Eggs`; it must not be rerolled. The two-day timer and egg are tracked in the mod-folder registry.

This is the first fixed-output slice. It does not yet apply color-wheel recipes, species traits, or physical chamber placement. A completed cycle with a full inventory leaves the egg ready in the breeder.

## Slime Egg Converter

The new converter is a distinct big craftable `(BC)Taum.SlimeRanching_EggConverter`. It accepts one green `(O)680`, blue `(O)413`, red `(O)437`, purple `(O)439`, or tiger `(O)857` vanilla Slime Egg and produces one mod Common Slime Egg after ten in-game minutes. It uses the vanilla Slime Egg-Press sprite as a placeholder. Existing vanilla eggs in inventory, chests, treasure, drops, and the old press remain unchanged until deliberately inserted. The output is a custom item ID stored in the game save; removing the mod while it remains in the save leaves an Error Item.

1. Build the Slime Daycare in the disposable `TaumTest_322865388` save. Run `slime_ranching_give_converter TaumTest_322865388` in the SMAPI console and place the machine in the Daycare.
2. Leave a vanilla egg in inventory and another in a chest. Sleep and reload: both must retain their original colors and item IDs.
3. The hutch's existing Slime Egg-Press has a Red Slime Egg ready. Collect it and verify it remains red. Insert it into the new converter. After ten in-game minutes, collect one mod Common Slime Egg.
4. Try inserting a non-egg item and the produced mod Common Slime Egg; neither should be consumed. Check that the old Slime Egg-Press still accepts Slime and that slain slimes still use vanilla egg drops.
5. Sleep and reload with the converter placed and a mod egg in inventory or chest. The machine and mod egg should still be recognized while the mod is installed. The generic converter egg has no genome record and is not incubatable.

## Field Guide book

1. Load a disposable single-player save and run `slime_ranching_give_guidebook <exact save folder name>`.
2. Confirm the Field Guide appears in inventory. Select it and press the action button; the guide page should open and explain the current feature limits.
3. Close the page. The book should remain in inventory and should not be consumed.
4. Save and reload with the book present. Confirm the item remains recognized while the mod is installed.

## External food-like persistence

On a disposable mod-data copy, put one record with `Genome.Id` set to `00000001-0000-0000-0000-000000000000` and omit `Genome.LikedFoodItemId`. Load and save: the external `slime-registry-v1.json` should contain `"LikedFoodItemId": "(O)766"` inside `Genome`. Reload and save again: it must keep the same value. This only tests external persistence; it does not link the record to a live slime.

For migration, put `LikedFoodItemId` on the record outside `Genome` in a test copy. After load/save, it should appear only inside `Genome`, and `slime-registry-v1.json.before-food-like-move.bak` should preserve the original file.

## Breeder list scrolling and parent rest

1. Spawn at least five Ranch Slimes in the Daycare and open the breeder. Scroll over the left list; the range indicator should change and the final resident should be selectable.
2. Select two residents and complete a cycle. After collecting the egg, confirm the pair shows `RESTING 1d` and can't be selected immediately.
3. Save and reload. Confirm the parents remain resting, then become eligible on the next in-game day. The one-day duration is provisional and will be reviewed after playtesting.

## Removed-animal reconciliation

1. On a disposable save, start a breeding cycle and remove one parent through the game's animal controls or a test-only method.
2. Save and reload. The external registry should preserve the removed slime's genome as an unlinked archive and clear the unfinished cycle.
3. Repeat with a cycle whose egg is already ready. The ready egg should remain collectable even if a parent has since been removed.
4. Confirm vanilla Hutch Green Slimes are not counted, detached, or changed by this reconciliation.

## Unit tests
- Recipe table is symmetric where intended and never creates undefined hybrids.
- Crystal override always yields pure base color and preserves unrelated genetic rolls.
- Size probability distributions approximate configured weights under seeded simulations.
- Rarity, shiny and mutation rolls are independent where specified.
- Pool pity caps and resets correctly; no resource spent twice.
- Refinery conversion cannot yield free item loops.

## Integration tests
- Existing save with slimes; new save; save/reload mid-breeding; reload with egg in chest.
- Parent removed, breeder broken, hutch full, egg output occupied, player cancels.
- Hatching an egg after transferring it to another location; corrupt external identity/state fallback.
- Furniture interactions with many slimes and giant sprites; offscreen hutch.
- Companion item pickup, full inventory, travel, death/pass-out, festival and sleep.
- Black/White day transition with events, quests and multiplayer.

## Release checks
No duplicate entities/items, no save corruption, stable FPS in full hutch, clear configuration, compatible removal path, reproducible regression tests.
