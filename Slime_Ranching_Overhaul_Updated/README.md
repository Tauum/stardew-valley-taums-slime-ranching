# Slime Ranching Overhaul — Current Development Plan

**Status:** Early playable prototype; breeding cycles, fixed-genome eggs, and a custom managed-egg incubator are implemented, while full genetics/gameplay systems remain in development. **Stack:** Stardew Valley 1.6+, SMAPI, C#, Rider + GPT. **Release scope:** single-player first; multiplayer deferred.

The current code stores the versioned genome registry and first-seen snapshot under `<installed mod folder>/data/saves/<save name>/`. Ranch Slimes use Stardew's `FarmAnimal` type and native animal ID; their genomes and authoritative maturity clock stay in the mod folder. The Slime Daycare is separate from the vanilla Slime Hutch, and existing vanilla GreenSlime monsters are left alone. The Slime Breeder reserves two Ranch Slimes, runs a two-day cycle, and creates a fixed-genome egg; the Slime Incubator hatches Common Eggs into base genomes and breeder eggs into their stored genome. The Mutagenic Pool now has a one-day safe-failure attempt with per-slime pity and a data-only Shadow Clone trait. A temporary visual echo is shown in the Daycare for mutated slimes. A test command can give the Pool, while optional GMCM settings expose Pool balance and shop prices. These are prototypes and need in-game persistence/recovery verification. See `Guides/PLAYER_GUIDE.md` and `Technical/TESTING.md`.

The latest building texture and chimney-heart adjustments compile but have not yet been checked in game. The Daycare uses a world draw offset of (3, 8) pixels; the Base image keeps its full roof. The hearts are 30 screen pixels higher than their original position and use a 4-second float cycle. See [`Planning/NEXT_SESSION.md`](Planning/NEXT_SESSION.md) for the exact pickup point.

Use `Technical/TESTING.md` to check breeder cycles, distinct genome-specific egg items, incubator hatching, external registry persistence, and reload behavior. Current parent-to-offspring inheritance is still a prototype.

## Rider build jobs

Run `1 Rebuild Slime Ranching`, then `2 Copy Slime Ranching to Mods` from Rider's run configurations, or run `3 Build, Deploy, Run` to do both and launch Stardew through SMAPI. The combined job stays open while the game runs and shows SMAPI output or launch errors in Rider. The copy step installs to `E:\Games\Steam\steamapps\common\Stardew Valley\Mods\taums-slime-ranching` and preserves the installed `config.json` and `data` directory. A normal project build still compiles without deploying.

## Read in order
1. `Planning/ROADMAP.md` — eight incremental stages and gates.
2. `Stages/01_Foundation.md` — vanilla isolation, Slime Daycare animal identity, first-install snapshot and recovery.
3. `Stages/02_Custom_Breeding.md` — custom breeder, two animated parents, and fixed-genome eggs.
4. `Planning/TASK_BOARD.md` — small, testable Rider GPT tasks.
5. `Technical/VANILLA_ISOLATION_AND_LIFECYCLE.md` — save-safety requirements.

## Player guide

See `Guides/PLAYER_GUIDE.md` for detailed instructions on the converter, inspection command, field guide book, data locations, and which features are still unavailable. For testing, `slime_ranching_give_guidebook <exact save folder name>` adds the in-game field guide.

## Locked scope
- **Fully custom breeding**, not vanilla reproduction with a menu: the breeder reserves two Ranch Slimes, produces a fixed-genome egg, and the custom incubator hatches managed eggs into a matching Ranch Slime. The current chamber animation is a menu preview.
- Vanilla Slime Egg items/sprites are preferred. Distinct genome-specific egg IDs and external genome records are implemented as a prototype; identity through inventory, chests, machines, incubators, and save/reload still needs verification. Do not write mod metadata into the game save.
- Colors, genetic sizes, rarity, and mutations are separate persistent data. All proposed slime types and hybrid abilities remain in scope, but species powers are implemented late.
- **Custom companion AI and movement**, including navigation, actions, storage and riding, are mandatory—not a vanilla follower reskin.
- Marnie sells the core ranching machines plus Slime Trampolines and Cushions at configurable provisional prices. Nearby Ranch Slimes can perform temporary size-aware trampoline bounces or gentle cushion squishes; pathfinding and richer sprite poses remain unfinished.
- The Slime Playground supports a synchronized two-slime hop interaction. These furniture interactions are visual prototypes and do not yet affect happiness or friendship.
- A Slime Slide adds a low hop and horizontal glide animation for nearby Ranch Slimes.
- A Slime Bed adds a short resting pose animation. The enrichment objects are visual prototypes with no happiness or breeding effects yet.
- Breeding size now inherits across Tiny, Small, Normal, Large, and Giant using provisional weights; a setup command can assign sizes to managed slimes for testing.
- The Mutagenic Pool uses configurable cost, duration, pity increment, chance cap, and shop price. Shadow Clone currently has a visual echo only; its companion abilities remain planned.
- Chromatic Crystals can focus breeder offspring on one of seven base colors. Crystal acquisition and progression are undecided; a SMAPI command supplies test crystals.
- AI-assisted/generated artwork is planned; placeholder art first. Generated images will require sprite-sheet preparation and in-game QA.
- Single-player v1; multiplayer explicitly deferred. Cosmetic item transmutation and elaborate family-tree UI may follow v1.

## Time planning
Custom breeding and custom companion AI remain expensive. Rough **20–40 h** for a save-safe breeding prototype and **110–190 h** for a feature-complete single-player alpha, with substantial uncertainty depending on SMAPI integration and artwork iteration. Not a promise.

## Important
Uninstalling cannot execute cleanup logic. All mod state is stored in the installed mod folder, so back up its `data` directory before removing or replacing the mod. Removing that directory loses the external registry. `config.json` contains preferences only.

## Next session

Continue from [`Planning/NEXT_SESSION.md`](Planning/NEXT_SESSION.md). The disposable test save is `TaumTest_322865388`.
