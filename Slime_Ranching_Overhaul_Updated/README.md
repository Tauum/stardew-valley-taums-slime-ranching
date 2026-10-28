# Taum's Slime Ranching

A Stardew Valley 1.6+ SMAPI mod. The repository contains a playable 1.x prototype and the design for Slime Daycare 2.0; planned 2.0 features are not necessarily implemented. The 1.x feature test is paused at Stage 6.

## Start here

- [2.0 development notes](Planning/DEVELOPMENT_NOTES_2.0.md): the only active design and implementation reference.
- [Player guide](Guides/PLAYER_GUIDE.md): commands and instructions for the current prototype; some guidance is specific to 1.x.
- [Feature testing walkthrough](Planning/FEATURE_TESTING_WALKTHROUGH.md): paused 1.x test checkpoint.
- [Completed feature tests](Planning/COMPLETED_FEATURE_TESTS.md): historical results from tested 1.x stages.
- [Archive index](Planning/Archive/README.md): explains where superseded design sources are kept and how to treat them.

## Build and deploy

Rider run configurations:

1. `1 Rebuild Slime Ranching` builds the project.
2. `2 Copy Slime Ranching to Mods` deploys the build to the Stardew Valley Mods folder.
3. `3 Build, Deploy, Run` rebuilds, deploys, and launches Stardew through SMAPI.

The current 1.x prototype keeps its mod-owned data in the installed mod folder; back up its `data` directory before replacing or removing the mod. The 2.0 design changes save persistence to SMAPI save data plus entity `modData`; that migration has not been implemented yet. The deploy step preserves installed `config.json` and `data`.
