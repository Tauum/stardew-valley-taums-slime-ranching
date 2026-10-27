# Slime Ranching Overhaul — Player Guide

This guide covers the current early prototype. The Slime Breeder can reserve two Ranch Slimes, run a two-day cycle, and produce an externally registered egg. A genome-specific egg can hatch in the Slime Incubator; managed slime behavior beyond breeding and basic animal behavior is still under development.

## Before you start

- Test on a copied save. The converter itself leaves unused vanilla eggs alone, but its output is a custom mod item saved in the game save.
- Back up `<installed mod folder>/data` before replacing or removing the mod. It contains the external slime registry and first-seen snapshots.
- Marnie sells the Converter, Breeder, Incubator, Slime Trampoline, and Slime Cushion. Prices are configurable in `config.json` and provisional. Nearby Ranch Slimes may bounce on trampolines or briefly squish on cushions; these animations are decorative and do not change happiness or breeding outcomes.
- Marnie also sells a Slime Playground. Two nearby Ranch Slimes can hop together on it; the interaction is decorative and currently grants no friendship or happiness bonus.
- Marnie also sells a Slime Slide. Nearby Ranch Slimes can do a small hop and horizontal glide down it; the effect is decorative.
- Marnie also sells a Slime Bed. Nearby Ranch Slimes can settle briefly into a resting pose; it has no gameplay effect yet.
- Marnie sells a Mutagenic Pool. It assigns one mature Ranch Slime for a nonlethal mutation attempt; defaults are 500 vanilla Slime, one day, 5% base chance, +2 percentage points per failure, and 30% cap. Use the GMCM settings page or `config.json` to tune it. Success adds the Shadow Clone trait; a temporary visual echo appears in the Daycare, but its planned abilities are not implemented. GMCM can disable the echo without removing the mutation.
- Marnie sells a Gel Refinery. Its first provisional recipe uses 1 Broken Glasses and 10 vanilla Slime to make 1 Refined Quartz over one in-game day. Fuel, time, and price can be tuned in GMCM or `config.json`; use `slime_ranching_give_gel_refinery <save name>` for testing. See the recipe section below and verify on a copied save.
- Marnie sells a Compost Trough. It can feed juvenile Ranch Slimes eligible monster drops, fishing trash, or algae once per day. A favorite item advances growth more; the growth values are configurable and provisional. Use `slime_ranching_give_compost_trough <save name>` to test it.
- Marnie also sells a Slime Tunnel. Nearby idle Ranch Slimes can do a short decorative crawl-through; use `slime_ranching_give_tunnel <save name>` to test it. This animation does not change stats or save state.
- Marnie also sells a Heated Slime Stone. Nearby idle Ranch Slimes can rest beside its ember glow; use `slime_ranching_give_heated_stone <save name>` to test it. It is decorative and does not grant winter or friendship bonuses yet.
- Marnie also sells a Slime Pond. Nearby idle slimes briefly bob as if swimming; use `slime_ranching_give_pond <save name>` to test it. It currently produces no drops or friendship effects.
- Marnie also sells a Slime Swing. Nearby idle slimes briefly sway on it; use `slime_ranching_give_swing <save name>` to test it. This is a visual interaction only.
- Chromatic Crystals can focus a breeder cycle on a base offspring color. They are currently test-only items; use the SMAPI command `slime_ranching_give_crystals <save name>` while on a disposable save.
- Ranch Slime offspring and genome-specific egg icons use a tint selected from the external genome color. The seven base colors and six hybrid recipes are supported; hybrid sprites still use the nearest base tint rather than continuous RGB blending.

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

Add an animal ID to inspect one managed Ranch Slime's genome, favorite food, maturity, mutation pity, and reservation status:

```text
slime_ranching_inspect <Ranch Slime animal ID>
```

## Mutagenic Pool

Buy the Pool from Marnie or request a test item with `slime_ranching_give_mutagenic_pool <exact save folder name>`. Place it inside the Slime Daycare, bring the configured amount of vanilla Slime in your inventory, and select one mature, unreserved Ranch Slime. Sleep to save the inventory cost and external attempt. The Pool records its chance and roll when started, so reloading doesn't reroll it. Failure never harms the slime; its next attempt gets the configured pity increase. Success sets the Shadow Clone trait and resets pity.

The attempt, roll, mutation trait, and pity count are stored under `<installed mod folder>/data/saves/<save folder name>/`. Vanilla inventory changes use Stardew's normal save. Use a copied test save, and keep the mod installed while its custom Pool item remains in the save. The visual echo is decorative; it is not another animal and produces no items or offspring.

## Gel Refinery

Place the refinery inside the Slime Daycare and use it with 1 Broken Glasses (`(O)170`) and the configured amount of vanilla Slime (`(O)766`) in your inventory. The current recipe produces one Refined Quartz (`(O)338`) after the configured number of in-game days. The output stays reserved in the machine until you collect it; a full inventory does not consume or discard the batch. These recipe and economy values are early prototypes. The running batch is in the external mod registry, while Stardew's regular save stores inventory changes.

## Compost Trough

Place it inside the Slime Daycare, select a juvenile and one eligible item from the two lists, then click **Feed Selected Slime**. Accepted food includes the genome favorite pool of monster drops/fishing trash, plus Seaweed, Green Algae, and White Algae. Each juvenile can eat once per in-game day. The default base growth is one day, with one extra day for its favorite food; it can never advance past maturity. Mature or currently reserved slimes cannot be fed. These are test values and can be tuned in GMCM or `config.json`.

## Field Guide book

The in-game **Slime Ranching Field Guide** is a short introduction to the current features. For testing, request it from the SMAPI console with the exact save folder name:

```text
slime_ranching_give_guidebook <exact save folder name>
```

Select the book and press the action button to open it. It is a custom item reference in the game save, so keep the mod installed while the book remains in that save. This markdown guide has the fuller usage details.

## Features still under development

- **Hatching:** Common and genome-specific Ranch Slime Eggs hatch in the custom incubator; vanilla eggs are unaffected.
- **Breeding:** The breeder, parent selection, two-day cycle, and fixed-genome egg output work. Five-size inheritance and the specified color-wheel recipes are implemented provisionally; review their outcomes and exceptional traits in game.
- **Egg details:** Hover over a genome-specific Ranch Slime Egg to view its stored species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- **Genetics and slime identity:** The registry model can hold genome data. Ranch Slime genomes are keyed to native FarmAnimal IDs; this still needs save/reload verification. Existing GreenSlime monsters are only captured in the first-seen snapshot; they aren't changed or adopted.
- **Food likes:** Favorite food is stored, inherited, shown in the breeder, and used by the Compost Trough's provisional extra growth bonus.
- **Furniture:** Trampoline, cushion, playground, slide, and bed animations are active prototypes in the Daycare, with no happiness or breeding effects yet.
- **Mutations:** The Mutagenic Pool can roll the Shadow Clone trait with per-slime pity. Its temporary Daycare echo is visual only; cloned abilities are not implemented.
- **Production and companions:** Slime-produced goods and custom companion AI are not active gameplay features yet.

## Ranch Slime animal prototype

The mod now defines a **Slime Daycare**, a separate animal-house building, and a **Ranch Slime** farm-animal type. For testing, the Daycare is free and completes instantly. The exterior uses custom pink, blue, and purple roof art derived from the original pixel images; the interior still uses the Barn map. Ranch Slimes use the game's vanilla Green Slime body frames and face art recomposed as a FarmAnimal sprite sheet. The exterior's final alignment is still being checked in game. Build the Daycare through Robin, then run this mod command in the SMAPI console:

```text
slime_ranching_spawn_ranch_slime <exact save folder name>
```

For the current test copy, use `slime_ranching_spawn_ranch_slime TaumTest_322865388`. It adds a farm-animal prototype without converting or removing any existing vanilla slimes. Petting, movement, Daycare assignment, and save/reload still need in-game verification. When the game saves, the mod records its genome externally and links it to the animal's native farm-animal ID. Standard animal properties like its name, home, age, and friendship remain part of Stardew's normal animal save data.

To set up controlled size-inheritance pairs, use `slime_ranching_set_size TaumTest_322865388 <animal ID> <Tiny|Small|Normal|Large|Giant>`. The command edits only that managed Ranch Slime's external genome record.

### Breeder selection preview

The breeder lists Daycare Ranch Slimes and previews selected parents in a chamber panel. Select two rows and click **Start 2-day Cycle**; the breeder reserves them automatically and starts the timer. The pair, start/ready days, and resulting genome are stored in the mod's external registry and survive reopening or reloading. Reserved slimes stop roaming where they are in the Daycare; the chamber remains a menu preview. Click **Cancel Cycle** to stop the timer and immediately release both parents. When egg formation finishes, the parents return to roaming in the Daycare and rest for one in-game day. The egg stays in the breeder until you click **Collect Egg**; it is never moved to a nearby chest automatically. Hover over the collected egg to inspect its stored genome. Use the custom Slime Incubator to hatch a managed egg.

## Data and save behavior

The mod keeps its registry and first-seen snapshot under:

```text
<installed mod folder>/data/saves/<save folder name>/
```

To create a timestamped ZIP backup of that save-specific mod data while playing, run:

```text
slime_ranching_backup_external_data <exact save folder name>
```

The archive is written under `<installed mod folder>/data/backups/`. It contains the external registry and first-seen snapshot, not the vanilla Stardew save. Keep making normal game-save backups separately.

The converter, Field Guide, and Common Slime Egg are custom item references in the Stardew save. If the mod is removed while one remains in a save, Stardew will show it as an Error Item. The Slime Daycare building and Ranch Slime animals are native game entities saved by Stardew. Existing `GreenSlime` monsters are not matched or changed. Ranch Slime genome records are stored externally and keyed by the native FarmAnimal ID; normal farm-animal properties remain in the save.

## Development book status

The in-game book is a short overview; this file is the detailed player guide. Expand both with a dedicated section as each feature is implemented and tested, keeping unavailable features clearly labeled.
