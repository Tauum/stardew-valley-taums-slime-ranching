# Slime Core companions

A rare **Slime Core trinket** binds a real slime from the player's hutch. Equipped: follows player and uses abilities. Unequipped: dismisses; original identity is preserved and cannot be duplicated or bred while bound/active.

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
