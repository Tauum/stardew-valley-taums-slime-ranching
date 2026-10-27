# Slime Core companions

A rare **Slime Core trinket** binds a real managed Ranch Slime from the Slime Daycare. Equipped: follows player and uses abilities. Unequipped: dismisses; original identity is preserved and cannot be duplicated or bred while bound/active.

## Base signature abilities
- Red — Fighter: attacks hostile monsters.
- Yellow — Revealer: digs artifact spots and reveals hidden objects/secret-note clues.
- Blue — Irrigator: waters unwatered crops.
- Green — Collector: picks up drops into size-scaled inventory; incorporates Void pocket storage.
- Orange — Nurturer: improves animal happiness and occasionally pets animals.
- Violet — Prospector: grants occasional extra ore on qualifying mining actions.
- Pink — Healer: slowly restores player health.

Hybrids use both inherited signature abilities, with independent cooldowns and balance caps.

Genome records now store stable ability IDs for the seven base species and the implemented two-species hybrids. This is a data foundation only; no ability action, cooldown, or companion behavior executes yet.

## Exceptional abilities
- Golden — capped merchant discount.
- Prismatic — rideable (Large/Giant) and Adaptation (temporarily selects/rotates one base ability).
- Black — +1 hour at night (proposed 3 AM cutoff).
- White — +1 hour in morning (proposed 5 AM start).
- Shadow Clone mutation — temporary clones with reduced inherited ability; no recursive spawning.

The Mutagenic Pool can currently assign the Shadow Clone genome trait. In the Daycare, that trait produces a capped, temporary translucent sprite echo as a visual prototype; it is not a second FarmAnimal and cannot act, breed, or produce items. The planned inherited-ability behavior remains unimplemented.

## Size / shiny
Tiny prioritizes movement and activation frequency; Giant prioritizes quantity/radius. Shiny gives small (~10%) performance boost. Golden discounts and Black/White time are capped and don't multiply with size.

## Engineering concerns
Inventory ownership/overflow, item duplication, location transfers, event scenes, combat safety, mounts, time-based cutscenes, multiplayer day sync, save/reload and follower cleanup.

## Current locked implementation clarification
Custom AI, movement, pathfinding, action selection and transitions are mandatory. Do not substitute vanilla follower behavior. Void pocket storage merges into Green Collector. Prismatic has both mount and Adaptation; Shadow Clone is retained.

## First implementation slice proposal (review before code)

The foundation should prove identity and ownership before adding movement or abilities:

1. Allow one active Core per farmer in the single-player prototype.
2. Bind only a mature, managed Ranch Slime that is currently in the Slime Daycare and not reserved by breeding, mutation, or feeding.
3. Keep one native `FarmAnimal` and one external `SlimeRecord`; binding toggles ownership/active state and never spawns a duplicate slime.
4. Store the binding and transition state under the installed mod folder. Avoid game-entity `modData`. A unique Core item identifier may carry only the external genome GUID, following the existing genome-egg identity pattern.
5. Dismissal returns the same animal ID and genome to the Daycare. While active or bound, block breeder, Mutagenic Pool, and Compost Trough selection.
6. Start movement as same-location follow with safe stuck recovery. Add cross-location transitions only after save/reload recovery proves the native animal and external genome remain linked.
7. Implement one test action before species abilities, combat, inventory, mounting, and time effects. Each ability needs an explicit cooldown, target, cap, and save-transition rule.

### Decisions to review before the Core prototype

- Use Stardew's accessory/trinket slot for the Core, matching the concept, or begin with a custom inventory item while validating binding?
- How should a player acquire it: crafting, a Marnie purchase, a rare drop, or a quest reward? If a rare drop, which source and rate?
- Should a dismissed Core stay in inventory as the binding token, or should dismissal return the animal and consume the Core?
- The safe starting defaults above are one active companion and Daycare-only binding. Change either before movement work if desired.

Until these are reviewed, the existing `IsCompanion` genome flag remains data-only and no bound/active animal is spawned.

## Identity-binding prototype

The first binding slice toggles `SlimeRecord.IsCompanion` for one mature, managed Ranch Slime physically inside a Slime Daycare and not reserved by another job. It writes only to the mod-folder registry; it does not create an animal, add item metadata to the game save, or move the animal. While bound, breeder, Mutagenic Pool, Compost Trough, and enrichment interactions treat it as reserved. Test-only commands bind and dismiss the same FarmAnimal ID so save/reload identity can be checked before implementing the Core token or follower movement:

- `slime_ranching_bind_companion <exact save folder name> <animal ID>`
- `slime_ranching_dismiss_companion <exact save folder name>`

These commands are temporary test access, not the intended player-facing acquisition flow. Accessory slot/inventory token, Core acquisition, and dismissal behavior remain undecided; follower movement and abilities are not implemented.
