# Slime Ranching Overhaul â€” Player Guide

## 2.0 Daycare progression currently being implemented

The basic Daycare uses `FarmHouse`; its first building upgrade changes to `FarmHouse1` and unlocks a separate Daycare Renovate choice at Robin. The second and third upgrades use `FarmHouse2`, and the third adds the Daycare's cellar. The Daycare remains an animal house for Ranch Slimes. Room renovation choices are saved to the Daycare; crib removal is not offered. Wallpaper and flooring remain Daycare-specific. Newly started breeder cycles take 18 days at the basic tier and 14 days from the first upgrade onward; active cycles keep their saved ready day. This feature needs in-game testing; use the copied test save and follow `Planning/FEATURE_TESTING_WALKTHROUGH.md`.

> **2.0 redesign in progress:** the 2.0 feature branch is in active development, and feature checks are being collected for a later bulk test pass. The current architecture stores facility jobs in per-save SMAPI data, Ranch Slime identity and genome fields in entity `modData`, and unique egg records in item metadata. Legacy external registry data is imported once. The player guide covers the fixed Mutagenic Pool rules now; other prototype sections remain provisional. Test only on copied saves and report in-game results together.

## Daycare wallpaper and flooring

On a copied save, bring an ordinary Wallpaper item and Flooring item into the Slime Daycare. Select each item and use it on an unobstructed valid wall or floor tile. The Daycare saves its patterns independently from the farmhouse. Leave and re-enter the Daycare to confirm the patterns remain; then check the farmhouse to confirm its decoration did not change. The full room-upgrade and reload sequence is in `Planning/FEATURE_TESTING_WALKTHROUGH.md`.

This guide covers the current early prototype. The Slime Breeder can reserve two Ranch Slimes and produce a fixed-genome egg; new cycles take 18 days at the basic tier and 14 days from the first Daycare upgrade onward. Active cycles keep their saved ready day through an upgrade. A genome-specific egg can hatch in the Slime Incubator; managed slime behavior beyond breeding and basic animal behavior is still under development.

## Before you start

- Test on a copied save. Vanilla Slime Eggs remain unchanged; Ranch Slime Eggs are created by the domesticated breeding system.
- Back up `<installed mod folder>/data` before replacing or removing the mod. It contains the external slime registry and first-seen snapshots.
- Permanent Daycare facilities are not sold by Marnie in the current build. Use the testing commands in each machine section to grant movable facility prototypes; existing placed objects and inventory items are retained for save compatibility. The current prototype shop sells enrichment furniture at provisional prices configured in `config.json`. Nearby Ranch Slimes may bounce on trampolines or briefly squish on cushions; these animations are decorative and do not change happiness or breeding outcomes.
- Marnie also sells a Slime Slide. Nearby Ranch Slimes can do a small hop and horizontal glide down it; the effect is decorative.
- The former Slime Playground, Slime Bed, and Compost Trough prototypes are no longer sold or active. Existing placed objects and inventory items are retained for save compatibility, but do not have interactions.
- After Maru repairs it, the Mutagenic Pool runs a two-day, nonlethal attempt on one mature Ranch Slime. Each attempt starts at 10%. During an active attempt, add 10 vanilla Slime for +1 percentage point; the segmented meter shows base chance, pity, and your contribution, up to a guaranteed 100%. Each slime keeps its own pity and contribution. Failure adds 1 percentage point of pity; contribution drops by 1 point only when it is above 2%. Success keeps pity and clears contribution. Canceling releases the slime without a roll and preserves all chance percentages. Approved outcomes are Starry, Spotted, Glowing, Crystalline, Mossy, Bubbly, Mushroom, Blooming, Firefly, Crowned, Cowboy, and Smelly. Each has a distinct cosmetic accent; mutations grant no gameplay ability. Old Shadow Clone traits remain supported as legacy data.
- The Gel Refinery is designed as a fixed facility at the final Daycare tier. Its current movable prototype is not sold by Marnie; use `slime_ranching_give_gel_refinery <save name>` for testing. Starting a new batch is locked until the Daycare reaches its final tier. Its provisional recipe uses 1 Broken Glasses and 10 vanilla Slime to make 1 Refined Quartz over one in-game day. Fuel and processing time can be tuned in GMCM or `config.json`. See the recipe section below and verify on a copied save.
- Marnie also sells a Slime Tunnel. Nearby idle Ranch Slimes can do a short decorative crawl-through; use `slime_ranching_give_tunnel <save name>` to test it. This animation does not change stats or save state.
- Marnie also sells a Heated Slime Stone. Nearby idle Ranch Slimes can rest beside its ember glow; use `slime_ranching_give_heated_stone <save name>` to test it. It is decorative and does not grant winter or friendship bonuses yet.
- Marnie also sells a Slime Pond. Nearby idle slimes briefly bob as if swimming; use `slime_ranching_give_pond <save name>` to test it. It currently produces no drops or friendship effects.
- Marnie also sells a Slime Swing. Nearby idle slimes briefly sway on it; use `slime_ranching_give_swing <save name>` to test it. This is a visual interaction only.
- Chromatic Crystals can focus a breeder cycle on one base offspring color. They are currently test-only items; use the SMAPI command `slime_ranching_give_crystals <save name>` while on a disposable save. A crystal does not create a hybrid or an exceptional species.
- Breeding previews show the possible offspring and abilities. Two same-color base slimes produce that base type; two different base types produce a two-ability hybrid. Matching hybrids produce the same hybrid, a hybrid/base pairing has equal odds of either type, and different hybrids choose from their four base contributors. Rare or exceptional species cannot breed.
- Ranch Slime offspring and genome-specific egg icons use a tint selected from the genome color. Hybrid genomes store a blend of their two base colors; the current sprite resolves that blend to the nearest base tint.
- The Slime Incubator, Gel Refinery, and Mutagenic Pool use 3Ã—2 placement and collision areas. The Slime Breeder retains its 5Ã—2 footprint.

## Slime Incubator

The Slime Incubator accepts a genome-specific Ranch Slime Egg from the player's inventory. Vanilla Slime Eggs are not accepted.

1. Request a testing incubator with `slime_ranching_give_incubator <exact save folder name>`.
2. Place it inside the Slime Daycare. Use it while holding a genome-specific egg in your inventory; select the egg in the menu.
3. The egg is removed from inventory; the saved incubator job keeps its genome and hatches after **7 in-game days**. The duration is fixed for new jobs. Any incubation already in progress retains its saved ready day when updating the mod.
4. While an egg is incubating, choose **Cancel** to review a confirmation. Confirming destroys that unique egg and gives you one Fried Egg; make sure your inventory has room. A canceled egg copy cannot be incubated later.
5. The new Ranch Slime appears in the Daycare with the egg's stored genome. If the Daycare is full, the ready hatch waits until space is available.

Rare Lab progression is saved separately for each incubator: 0% at the basic Daycare, 1% after the first building upgrade, and 5% at the final tier. In this build those values are recorded for progression checks but do not yet affect hatching; rare rolls, Slime contributions, catalysts, and the Egg CryoLab are still in development.

In 2.0, newly hatched Ranch Slimes mature after **7 in-game days**. The breeder marks growing slimes in its list and won't let them be selected. The testing spawn command lets you choose an age from 0 to 7 days.

An egg's genome ID can hatch only once, so copied breeder egg items with that same ID become inactive after the first hatch; canceling an incubation also permanently retires that egg ID. Vanilla eggs are unaffected. In 2.0, the seven-day duration is fixed; full-Daycare waiting and cancellation still need in-game confirmation.

### Test egg command

While the matching single-player save is loaded, use the SMAPI console to add two genome-specific test eggs. Replace the save name with the exact save folder name:

```text
slime_ranching_give_test_eggs <save folder name>
```

To generate one egg with randomized genome traits, add `random`:

```text
slime_ranching_give_test_eggs <save folder name> random
```

The randomized egg rolls a base color/species, optional secondary species, size, rarity, shiny state, eligible favorite food, generation, and (at 10%) one of the approved cosmetic mutations. Its parent links remain empty because this is a test genome, not offspring from a real pair. The default and `key=value` forms still give two eggs.

For controlled testing, pass one or more `key=value` options instead. Any omitted option keeps the command's default; explicit values apply to both eggs, while their genome IDs remain distinct:

```text
slime_ranching_give_test_eggs <save folder name> species=Mint_Slime secondary=none color=#65DDBB size=Giant rarity=Rare shiny=true food=(O)168 generation=4 mutation=Starry
```

Available keys: `species`, `secondary` (`none` clears it), `color` (`#RRGGBB`), `size` (`Small`, `Normal`, `Large`, or `Giant`), `rarity` (text tier), `shiny` (`true`/`false`), `food` (eligible item ID), `generation` (0-999), and `mutation` (`Starry`, `Spotted`, `Glowing`, `Crystalline`, `Mossy`, `Bubbly`, `Mushroom`, `Blooming`, `Firefly`, `Crowned`, `Cowboy`, `Smelly`, or `none`). Eligible food IDs include `(O)684` Bug Meat, `(O)766` Slime, `(O)168` Trash, and `(O)170` Broken Glasses. The `random` mode can't be combined with `key=value` options.

## Hutch inspection

Enter the Slime Daycare or a vanilla Slime Hutch and run:

```text
slime_ranching_inspect
```

In the Daycare, this reports Ranch Slime and genome counts, assignment-status totals, native resident capacity as `occupied/limit`, and each incubator's saved Rare Lab chance state. In the vanilla Slime Hutch, it reports the monster slime count and confirms those slimes remain unchanged.

Add an animal ID to inspect one managed Ranch Slime's genome, favorite food, maturity, mutation pity, assignment status, and native friendship/happiness/fullness values:

```text
slime_ranching_inspect <Ranch Slime animal ID>
```

The assignment is reported as **Available**, **Selected for breeding**, **Breeding**, **Mutagenic Pool**, **Incubator hatch**, or **Companion**. Friendship, happiness, and fullness are the native animal values; the Feeding Trough and custom feeding effects are still under development.

### Sanctuary quest progress diagnostic

On a copied development save, run:

```text
slime_ranching_quest_status
```

This read-only command reports the save-data schema and each saved Sanctuary introduction milestone. After Robin's Pool prompt is accepted, it also reports the in-game day when Maru's mission becomes available. Story scenes and the starter-egg sequence are still being implemented. Construction and first-breeding milestones only advance after their earlier story prerequisites have been recorded. Robin's Pool prompt and Maru's repair handoff wait for the construction milestone.

To test the Robin-to-Maru handoff before the opening scenes are complete, use a disposable single-player save with a fully constructed Daycare and run:

```text
slime_ranching_test_maru_repair <exact save folder name>
```

This testing command marks the Lewis, Wizard, Marnie, and Daycare-construction ledger milestones so Robin's prompt can be tested. It skips those scenes and changes saved quest progress; do not use it on a main save. After priming, talk to Robin and follow the Maru repair steps below. When the opening story is implemented, test the normal story route separately without this helper.

In normal play, after Daycare construction, talk to Robin about the broken Mutagenic Pool and choose to ask Maru. The repair mission appears in the journal the next morning. Speak to Maru with 1 Battery Pack, 5 Iron Bars, and 10 Refined Quartz to hand over the materials and activate the Pool. The normal route is not connected to the opening story yet; use the testing command above only on a disposable copy to reach this handoff meanwhile.

## Mutagenic Pool

After Maru repairs the Pool, use it inside the Slime Daycare and select one mature, unreserved Ranch Slime. Starting costs no Slime. The attempt takes two in-game days and begins at 10%; while it runs, each 10 vanilla Slime adds 1 percentage point and is consumed. The color-coded bar separates base chance, pity, and contribution. At 100%, the mutation is guaranteed. Each failed roll is safe and adds 1 percentage point of that slime's pity. On failure, contribution falls by 1 point only when above 2%; otherwise it carries forward unchanged. On success, pity stays and contribution resets to zero. Use **Cancel** during an attempt to release the slime without a roll; all chance percentages remain unchanged.

Attempts, rolls, mutation traits, pity, and contributions are stored in the per-save mod data. Slime contributions use a save receipt so an interrupted save restores the resource and prior chance together. The approved traits are Starry, Spotted, Glowing, Crystalline, Mossy, Bubbly, Mushroom, Blooming, Firefly, Crowned, Cowboy, and Smelly. Their accents are twinkling stars, spots, a time-sensitive glow, crystal facets, a sprout, rising bubbles, a mushroom cap, a flower, orbiting fireflies, a crown, a cowboy hat, and odor wisps, respectively. They are visual only. The old Shadow Clone echo is retained only for compatible saves. Use a copied test save, and keep the mod installed while its custom Pool item remains in the save.

## Gel Refinery

Place the refinery inside the Slime Daycare and reach the final building tier before starting a new batch. Then use it with 1 Broken Glasses (`(O)170`) and the configured amount of vanilla Slime (`(O)766`) in your inventory. The current recipe produces one Refined Quartz (`(O)338`) after the configured number of in-game days. The output stays reserved in the machine until you collect it; a full inventory does not consume or discard the batch. These recipe and economy values are early prototypes. Start and collection changes reconcile the saved machine job with the Stardew inventory across reloads.

## Retired furniture prototypes

The old Slime Playground, Slime Bed, and Compost Trough are not part of the active 2.0 catalog. Their item definitions remain only so existing saves can load previously placed objects and stored items. They are inert; the approved furniture recipes and fixed-slot system are still under development.

## Field Guide book

The in-game **Slime Ranching Field Guide** is a short introduction to the current features. For testing, request it from the SMAPI console with the exact save folder name:

```text
slime_ranching_give_guidebook <exact save folder name>
```

Select the book and press the action button to open it. It is a custom item reference in the game save, so keep the mod installed while the book remains in that save. This markdown guide has the fuller usage details.

## Features still under development

- **Hatching:** Common and genome-specific Ranch Slime Eggs hatch in the custom incubator; vanilla eggs are unaffected.
- **Breeding:** The breeder, parent selection, 18-day basic-tier cycle, 14-day cycles from Daycare upgrade 1 onward, and fixed-genome egg output work. Active cycles keep their recorded ready day across upgrades. Offspring inherit color/species only; the egg icon uses the same rendered color variant as the slime, and ability identifiers follow its species. Other offspring traits start at Normal size, Common rarity, non-shiny, no mutation, and a newly assigned favorite food. Parent links and generation remain as lineage metadata.
- **Egg details:** Hover over a genome-specific Ranch Slime Egg to view its stored species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- **Genetics and slime identity:** The registry model can hold genome data. Ranch Slime genomes are keyed to native FarmAnimal IDs; this still needs save/reload verification. Existing GreenSlime monsters are only captured in the first-seen snapshot; they aren't changed or adopted.
- **Food likes:** Favorite food is stored and assigned independently for each offspring, and shown in the breeder. No feeding bonus has been approved; the legacy Compost Trough growth prototype is retired.
- **Furniture:** The old Playground, Bed, and Compost Trough prototypes are inactive. The approved 2.0 catalog, tier progression, reusable recipes, 3/7/15 fixed slots, and Management Desk edit mode are still under development.
- **Mutations:** The Mutagenic Pool now rolls from the approved twelve-trait list with per-slime pity and contribution. All twelve traits have distinct rendered cosmetic accents; old Shadow Clone records are compatibility data. In-game visual checks remain pending.
- **Production and companions:** Slime-produced goods and custom companion AI are not active gameplay features yet.

## Ranch Slime animal prototype

The 1.x prototype defines a **Slime Daycare**, a separate animal-house building, and a **Ranch Slime** farm-animal type. For testing, the Daycare is free and completes instantly. The exterior uses prototype roof art; Ranch Slimes use the game's vanilla Green Slime body frames and face art recomposed as a FarmAnimal sprite sheet. The earlier 1.x build used the Barn map inside. The active 2.0 branch now uses a custom Daycare interior without changing vanilla barns or the Slime Hutch. Build the Daycare through Robin, then run this mod command in the SMAPI console:

```text
slime_ranching_spawn_ranch_slime <exact save folder name> <age in days 0-7> [random]
```

For a 2.0 test copy, use `slime_ranching_spawn_ranch_slime <save folder name> 7` to spawn a mature slime, or use `slime_ranching_spawn_ranch_slime <save folder name> 0` to spawn a newborn. Add the optional `random` tag (for example, `slime_ranching_spawn_ranch_slime <save folder name> 0 random`) to keep the chosen age while randomizing the genome traits: primary species/color, a 30% chance of a secondary species, size, rarity, a 10% shiny chance, favorite food, generation (0â€“6), and a 10% chance of an approved mutation. Without the tag, the command uses the default green Ranch Slime genome: no secondary species, Normal size, Common rarity, not shiny, generation 0, and no mutation; the mod assigns its favorite food. Ages 1â€“6 create juveniles that need the remaining days to reach maturity. The command adds a Ranch Slime without converting or removing any vanilla slimes, and immediately saves its genome to the mod's active save data. The animal itself and standard properties like its name, home, age, and friendship are stored by Stardew when the game saves.

To set a managed Ranch Slime's visual size for testing, use `slime_ranching_set_size <save folder name> <animal ID> <Small|Normal|Large|Giant>`. This does not affect offspring size; bred offspring currently start at Normal size.

### Breeder selection preview

The breeder lists Daycare Ranch Slimes and previews selected parents in a chamber panel. Select two rows and click **Start Breeding**; the breeder reserves them automatically and starts an 18-day timer at the basic tier, or a 14-day timer from the first Daycare upgrade onward. The pair, start/ready days, and resulting genome are stored in the active save data and survive reopening or reloading. Active cycles retain their saved ready day if you upgrade the Daycare. Reserved slimes stop roaming where they are in the Daycare; the chamber remains a menu preview. Click **Cancel Cycle** to stop the timer and immediately release both parents. When egg formation finishes, the parents return to roaming in the Daycare and rest for one in-game day. The egg stays in the breeder until you click **Collect Egg**; it is never moved to a nearby chest automatically. Hover over the collected egg to inspect its stored genome. Use the custom Slime Incubator to hatch a managed egg.

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

## 2.0 Daycare interior

The Slime Daycare uses the same interior layout as the farmhouse. Its basic, first-upgrade, and final-upgrade tiers follow the farmhouse's three layouts. It remains an animal house, so domesticated slimes continue to live and roam inside. Dedicated machine rooms will be fitted into this familiar layout as room upgrades are implemented.

**Decorating the Daycare:** Use normal Stardew **Wallpaper** and indoor **Flooring** items from a shop or catalogue. Select the item and apply it as you would in the farmhouse. Each Daycare saves its own choices when you save the game. Outdoor craftable paths are a separate item type. This support applies to the Daycare; it does not turn other barns into decoratable rooms.

To review it, run **3 Build, Deploy, Run** on the copied test save, enter the Slime Daycare, and check the entrance, perimeter collision and all open floor—including the former breeder, incubator and pond areas. The room uses normal tile layers so walls and foreground trim can overlap the player correctly. Leave and re-enter, then confirm the vanilla Barn and Slime Hutch still use their normal interiors. In the review gallery, search for **Daycare Floor**, **Daycare Walls**, or **Daycare Layers Comparison** to inspect the separated surfaces without launching the game. This is still Feature 2; report any map-load error, wrong overlap or blocked walkway before proceeding.

The Field Guide, Slime Breeder, Slime Incubator, and genome-specific Ranch Slime Eggs are custom item references in the Stardew save. If the mod is removed while one remains in a save, Stardew will show it as an Error Item. The Slime Daycare building and Ranch Slime animals are native game entities saved by Stardew. Existing `GreenSlime` monsters are not matched or changed. Ranch Slime genome records are stored externally and keyed by the native FarmAnimal ID; normal farm-animal properties remain in the save.

## Development book status

The in-game book is a short overview; this file is the detailed player guide. Expand both with a dedicated section as each feature is implemented and tested, keeping unavailable features clearly labeled.
