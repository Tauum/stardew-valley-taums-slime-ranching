# Stage 1 — Foundation and safety

Goal: prove the mod can coexist with vanilla before any breeding changes.

- Scope managed ranch behavior to Ranch Slime farm animals in the mod's Slime Daycare. Leave the vanilla Slime Hutch and all vanilla GreenSlime monsters unchanged, including mine, Skull Cavern, Secret Woods, and other wild slimes.
- Maintain per-save versioned registry of genomes, bound companions, breeder reservations, and machine state. `config.json` is preferences only.
- Store the registry and snapshots under the installed mod folder. Do not write mod data to the Stardew save or to game entity/item `modData`.
- On first install, snapshot preexisting Slime Hutch monsters into the mod folder. Leave them untouched; only mod-created Ranch Slime farm animals are managed.
- Plan vanilla-only save loads: uninstall executes no code; test vanilla tolerance of custom objects, keep backups, and offer reconciliation when reinstalled.
- Use a non-destructive test save and inspect SMAPI logs.

**Done when:** vanilla isolation verified in multiple locations; first-seen snapshot idempotent; mod-folder backup and reinstallation scenario tested.


