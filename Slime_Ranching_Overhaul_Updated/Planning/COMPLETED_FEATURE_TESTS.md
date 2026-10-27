# Completed Feature Test Stages

This file keeps completed test stages and their outcomes for reference. The active stage stays at the top of [FEATURE_TESTING_WALKTHROUGH.md](FEATURE_TESTING_WALKTHROUGH.md).

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
