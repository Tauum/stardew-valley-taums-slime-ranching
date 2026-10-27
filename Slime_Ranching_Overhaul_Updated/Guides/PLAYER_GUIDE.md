# Slime Ranching Overhaul — Player Guide

This guide covers the current early prototype. The Slime Breeder can reserve two Ranch Slimes, run a two-day cycle, and produce an externally registered egg. A genome-specific egg can hatch in the Slime Incubator; managed slime behavior beyond breeding and basic animal behavior is still under development.

## Before you start

- Test on a copied save. The converter itself leaves unused vanilla eggs alone, but its output is a custom mod item saved in the game save.
- Back up `<installed mod folder>/data` before replacing or removing the mod. It contains the external slime registry and first-seen snapshots.
- Marnie sells the Converter, Breeder, and Incubator. Their current prices are configurable in `config.json` and are provisional for balancing. Testing commands can still provide the machines directly.

## Slime Egg Converter

The Slime Egg Converter turns one vanilla Slime Egg into one **Common Slime Egg** after ten in-game minutes.

### Get and place a converter

To obtain a Converter through normal play, buy one from Marnie at her Ranch. For a test copy, open the SMAPI console while the save is loaded and run:

```text
slime_ranching_give_converter <exact save folder name>
```

For the test copy used during development:

```text
slime_ranching_give_converter TaumTest_322865388
```

Place the converter somewhere you can access. You don't need to be in the Slime Hutch.

### Convert an egg

Put one green, blue, red, purple, or tiger Slime Egg into the converter. Collect the Common Slime Egg when processing finishes. Eggs elsewhere—in inventory, chests, treasure, drops, or the original Slime Egg-Press—remain vanilla eggs unless you put them into this converter.

The vanilla Coop Incubator does not hatch mod eggs; use the custom Slime Incubator below.

## Slime Incubator

The Slime Incubator accepts a Common Slime Egg or a genome-specific Ranch Slime Egg from the player's inventory. Vanilla Slime Eggs are not accepted.

1. Request a testing incubator with `slime_ranching_give_incubator <exact save folder name>`.
2. Place it inside the Slime Daycare. Use it while holding a genome-specific egg in your inventory; select the egg in the menu.
3. The egg is removed from inventory; sleep to save the incubator job in the mod-folder registry. A Common Egg receives a fresh Green, Normal, Common genome at insertion. A breeder egg keeps its existing genome. Both hatch automatically at the start of the day two days later.
4. The new Ranch Slime appears in the Daycare with the egg's stored genome. If the Daycare is full, the ready hatch waits until space is available.

Hatched Ranch Slimes grow for three in-game days before becoming breeding candidates. The breeder marks growing slimes in its list and won't let them be selected. The testing spawn command creates mature Ranch Slimes so you can test breeding immediately.

An egg's genome ID can hatch only once, so copied breeder egg items with that same ID become inactive after the first hatch. Each Common Egg insertion creates a separate genome ID. Vanilla eggs are unaffected. The incubator's two-day duration and full-Daycare behavior are provisional for playtesting.

## Hutch inspection

Enter the Slime Daycare or a vanilla Slime Hutch and run:

```text
slime_ranching_inspect
```

In the Daycare, this reports the Ranch Slime count and external genome record count. In the vanilla Slime Hutch, it reports the monster slime count and confirms those slimes remain unchanged.

## Field Guide book

The in-game **Slime Ranching Field Guide** is a short introduction to the current features. For testing, request it from the SMAPI console with the exact save folder name:

```text
slime_ranching_give_guidebook <exact save folder name>
```

Select the book and press the action button to open it. It is a custom item reference in the game save, so keep the mod installed while the book remains in that save. This markdown guide has the fuller usage details.

## Features still under development

- **Hatching:** Common and genome-specific Ranch Slime Eggs hatch in the custom incubator; vanilla eggs are unaffected.
- **Breeding:** The breeder, parent selection, two-day cycle, and fixed-genome egg output work. Inheritance is a placeholder and needs balancing and in-game verification.
- **Egg details:** Hover over a genome-specific Ranch Slime Egg to view its stored species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- **Genetics and slime identity:** The registry model can hold genome data. Ranch Slime genomes are keyed to native FarmAnimal IDs; this still needs save/reload verification. Existing GreenSlime monsters are only captured in the first-seen snapshot; they aren't changed or adopted.
- **Food likes, furniture, production, and companions:** These systems are planned but aren't active gameplay features yet.

## Ranch Slime animal prototype

The mod now defines a **Slime Daycare**, a separate animal-house building, and a **Ranch Slime** farm-animal type. For testing, the Daycare is free and completes instantly. The exterior uses custom pink, blue, and purple roof art derived from the original pixel images; the interior still uses the Barn map. Ranch Slimes use the game's vanilla Green Slime body frames and face art recomposed as a FarmAnimal sprite sheet. The exterior's final alignment is still being checked in game. Build the Daycare through Robin, then run this mod command in the SMAPI console:

```text
slime_ranching_spawn_ranch_slime <exact save folder name>
```

For the current test copy, use `slime_ranching_spawn_ranch_slime TaumTest_322865388`. It adds a farm-animal prototype without converting or removing any existing vanilla slimes. Petting, movement, Daycare assignment, and save/reload still need in-game verification. When the game saves, the mod records its genome externally and links it to the animal's native farm-animal ID. Standard animal properties like its name, home, age, and friendship remain part of Stardew's normal animal save data.

### Breeder selection preview

The breeder lists Daycare Ranch Slimes and previews selected parents in a chamber panel. Select two rows and click **Start 2-day Cycle**; the breeder reserves them automatically and starts the timer. The pair, start/ready days, and resulting genome are stored in the mod's external registry and survive reopening or reloading. Reserved slimes stop roaming where they are in the Daycare; the chamber remains a menu preview. Use **Cancel Cycle** to stop the timer or **Release Pair** when no cycle is active. When the cycle finishes, collect the fixed-genome egg from the breeder and hover over it to inspect the stored genome. Parents then rest for one in-game day; resting parents are marked in the menu and cannot be selected. Use the custom Slime Incubator to hatch a managed egg.

## Data and save behavior

The mod keeps its registry and first-seen snapshot under:

```text
<installed mod folder>/data/saves/<save folder name>/
```

The converter, Field Guide, and Common Slime Egg are custom item references in the Stardew save. If the mod is removed while one remains in a save, Stardew will show it as an Error Item. The Slime Daycare building and Ranch Slime animals are native game entities saved by Stardew. Existing `GreenSlime` monsters are not matched or changed. Ranch Slime genome records are stored externally and keyed by the native FarmAnimal ID; normal farm-animal properties remain in the save.

## Development book status

The in-game book is a short overview; this file is the detailed player guide. Expand both with a dedicated section as each feature is implemented and tested, keeping unavailable features clearly labeled.
