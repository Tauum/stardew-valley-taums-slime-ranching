# Vanilla isolation, first install, uninstall and reinstall

**Critical save-safety contract.** This file is authoritative for every sprint. Mod-owned state is stored under the installed mod folder, never in the Stardew save or on game entity/item `modData`.

## Scope invariant
Modded slime genetics, breeding, AI/furniture behavior and egg handling apply **only** to Ranch Slime farm animals in the mod's Slime Daycare, registered by their native FarmAnimal ID. Vanilla GreenSlime monsters in the Slime Hutch, mines, Skull Cavern, Secret Woods, wilderness and other locations are never altered. The new Daycare is a custom animal-house building with a temporary Barn interior and still needs in-game verification. The mod stores genome records externally and doesn't write to entity/item `modData`.

## First installation (per save, once)
1. Detect no first-seen marker for the current save in the installed mod folder.
2. Create and verify a versioned snapshot in that folder by reading, but not changing, the game save.
3. Leave legacy hutch slimes and all other game entities untouched on load. No automatic cleanup or adoption occurs.
4. Keep first-seen creation idempotent. Existing Slime Hutch monsters are never removed, adopted, or converted. Vanilla Slime Eggs stay unchanged in inventories, chests, drops, treasure, and the old press. Only placing one in the separate Slime Egg Converter consumes it and produces a mod Common Slime Egg.

## Normal save/load
Maintain stable IDs and a versioned per-save registry under `<installed mod folder>/data/saves/<save name>/` for genomes, eggs, breeders, reservations, mutations, furniture and companions. Validate data, migrate old schemas and never reroll egg genetics on reload. `config.json` holds preferences only. Do not use SMAPI `WriteSaveData` or game entity/item `modData` for mod state.

## Uninstall reality
A removed mod cannot run cleanup or migration. Back up the installed mod folder's `data` directory before updating or uninstalling; deleting it deletes the external registry. The new converter and Common Slime Egg are custom item IDs saved by Stardew, so they will be Error Items if the mod is removed while those items remain in a save. Their future genomes and other mod-owned state must still stay in the external registry; do not put that data in item or entity `modData`.

## Reinstall
If the mod folder's versioned registry remains, detect it and reconcile with any changes made while the mod was absent. Never silently overwrite current hutch slimes or restore duplicates. Reinstall cannot recover data deleted along with the mod folder unless the user kept a copy of its `data` directory.

## Suggested verification matrix
- Existing save with 0, 1, many hutches; legacy slimes present/absent.
- Mine slime combat/drops/spawns before and after install.
- First-seen snapshot creation, repeated load and corrupt external-state handling.
- Save/reload during breeder operation, with egg in inventory/chest and after hatch.
- Remove mod and load a **copy** of the save; play and save; reinstall and reconcile.
- Missing/corrupt registry, duplicate IDs, custom furniture and companions during uninstall.

## Engineering principle
Every Harmony patch or event handler begins with a location + entity scope guard; return vanilla behavior otherwise. Avoid global slime-class patches without precise guards.
