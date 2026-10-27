# Slime Ranching Overhaul — Current Development Plan

**Status:** Early playable prototype; breeding cycles, fixed-genome eggs, and a custom managed-egg incubator are implemented, while full genetics/gameplay systems remain in development. **Stack:** Stardew Valley 1.6+, SMAPI, C#, Rider + GPT. **Release scope:** single-player first; multiplayer deferred.

The current code stores the versioned genome registry and first-seen snapshot under `<installed mod folder>/data/saves/<save name>/`. Ranch Slimes use Stardew's `FarmAnimal` type and native animal ID; their genomes stay in the mod folder. The Slime Daycare is separate from the vanilla Slime Hutch, and existing vanilla GreenSlime monsters are left alone. The Daycare has three custom exterior textures derived by resizing the original pixel art, and Ranch Slimes use a green slime sprite sheet. Spawn a test Ranch Slime with `slime_ranching_spawn_ranch_slime <exact save folder name>`. A Slime Breeder reserves two Ranch Slimes, runs a two-day cycle, and creates a fixed-genome egg stored in the external registry. The Slime Incubator consumes a genome-specific egg and hatches it into a Ranch Slime after two days. Both features are implemented but still need in-game persistence/recovery verification. The separate Slime Egg Converter accepts any of the five vanilla Slime Eggs and produces one generic Common Slime Egg after ten in-game minutes; that generic egg has no genome and cannot hatch yet.

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
- Furniture is bought from Marnie and slimes physically use it (trampoline, beds, slides, etc.).
- AI-assisted/generated artwork is planned; placeholder art first. Generated images will require sprite-sheet preparation and in-game QA.
- Single-player v1; multiplayer explicitly deferred. Cosmetic item transmutation and elaborate family-tree UI may follow v1.

## Time planning
Custom breeding and custom companion AI remain expensive. Rough **20–40 h** for a save-safe breeding prototype and **110–190 h** for a feature-complete single-player alpha, with substantial uncertainty depending on SMAPI integration and artwork iteration. Not a promise.

## Important
Uninstalling cannot execute cleanup logic. All mod state is stored in the installed mod folder, so back up its `data` directory before removing or replacing the mod. Removing that directory loses the external registry. `config.json` contains preferences only.

## Next session

Continue from [`Planning/NEXT_SESSION.md`](Planning/NEXT_SESSION.md). The disposable test save is `TaumTest_322865388`.
