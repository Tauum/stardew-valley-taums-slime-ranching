# Slime Ranching Overhaul â€” Player Guide

For a compact tour of the mod's machines, furniture, and Stardew integration, see the [Mod TL;DR](MOD_TLDR.md).

## 2.0 Daycare progression currently being implemented

The basic Daycare uses `FarmHouse`; its first building upgrade changes to `FarmHouse1` and unlocks a separate Daycare Renovate choice at Robin. The second and third upgrades use `FarmHouse2`, and the third adds the Daycare's cellar. The Daycare remains an animal house for Domesticated Slimes. Room renovation choices are saved to the Daycare; crib removal is not offered. Wallpaper and flooring remain Daycare-specific. Newly started breeder cycles take 18 days at the basic tier and 14 days from the first upgrade onward; active cycles keep their saved ready day. This feature needs in-game testing; use the copied test save and follow `Planning/FEATURE_TESTING_WALKTHROUGH.md`.

> **2.0 redesign in progress:** the 2.0 feature branch is in active development, and feature checks are being collected for a later bulk test pass. The current architecture stores facility jobs in per-save SMAPI data, Domesticated Slime identity and genome fields in entity `modData`, and unique egg records in item metadata. Legacy external registry data is imported once. The player guide covers the fixed Mutagenic Pool rules now; other prototype sections remain provisional. Test only on copied saves and report in-game results together.

All `slime_ranching_*` SMAPI tester commands automatically use the currently loaded save, so leave the save-folder name out of the command. The commands warn and do nothing if no save is loaded. Commands that need other values still show those arguments below.

Inside the Daycare, `slime_ranching_inspect` also reports how many furniture slots are unlocked and assigned, and lists each unlocked slot ID. Empty slots are marked **empty**. For a saved assignment, it lists the expected furniture and tile, whether the placed object matches, and whether Stardew's native tool protection is active. The read-only Management Desk Progression preview marks an unassigned slot **EMPTY**. For assigned slots, it shows live tile state: **MISSING** means no object is at the saved tile, **MISMATCH** means a different object is there, **MATCH** means the assigned furniture is placed, and **PROTECTED** means the matching furniture has native tool protection. A mismatch row shows the expected furniture followed by the name of the object currently there. The preview also lists recipe furniture carried in your backpack, excluding items in chests and furniture already placed in the room. Saved assignments above the current tier are excluded from the unlocked count and shown as locked; fixed slot positions and editing are still in development, so a fresh save currently reports zero assigned slots.

For disposable-save editor checks, run `slime_ranching_set_furniture_slot_test <slot-id> <furniture-key> <tile-x> <tile-y>` from inside the Daycare. It saves assignment metadata for the current building but does not consume an item or place furniture. The tile must be inside the room and passable; wall/fixture tiles and tiles occupied by non-catalog objects are rejected. Furniture keys are `Trampoline`, `Cushion`, `Slide`, `Seesaw`, `HeatedSlimeStone`, `Tunnel`, `Pond`, `Swing`, `Lamp`, `FairyLights`, `BallPit`, and `WashingMachine`. Catalog furniture may already occupy the tile for replacement/protection checks. If an object matches the saved assignment, the command applies Stardew's tool protection; changing or clearing the assignment restores the catalog item's normal removable state. Use `slime_ranching_clear_furniture_slot_test <slot-id>` to remove the test assignment; this does not remove an in-room object. Both commands warn and do nothing if no save is loaded.

## Daycare wallpaper and flooring

On a copied save, bring an ordinary Wallpaper item and Flooring item into the Slime Daycare. Select each item and use it on an unobstructed valid wall or floor tile. The Daycare saves its patterns independently from the farmhouse. Leave and re-enter the Daycare to confirm the patterns remain; then check the farmhouse to confirm its decoration did not change. The full room-upgrade and reload sequence is in `Planning/FEATURE_TESTING_WALKTHROUGH.md`.

This guide covers the current early prototype. The Slime Breeder can reserve two Domesticated Slimes and produce a fixed-genome egg; new cycles take 18 days at the basic tier and 14 days from the first Daycare upgrade onward. Active cycles keep their saved ready day through an upgrade. A genome-specific egg can hatch in the Slime Incubator; managed slime behavior beyond breeding and basic animal behavior is still under development.

## Before you start

- Test on a copied save. Vanilla Slime Eggs remain unchanged; Domesticated Slime Eggs are created by the domesticated breeding system.
- Back up `<installed mod folder>/data` before replacing or removing the mod. It contains the external slime registry and first-seen snapshots.
- Permanent Daycare facilities are not sold by Marnie in the current build. Use the testing commands in each machine section to grant movable facility prototypes; existing placed objects and inventory items are retained for save compatibility. Marnie sells reusable furniture recipes, not the furniture itself. The basic recipes become available after building a Daycare (Trampoline, Slide, Cushion, Seesaw, and Heated Slime Stone at 2,000g each); Pond, Tunnel, Swing, and Lamp unlock at the first upgrade (5,000g each); Fairy Lights, Ball Pit, and Washing Machine unlock at the final upgrade (10,000g each). Prices can be tuned in GMCM. Buy a recipe once, then craft as many copies as you need from the Crafting menu. A purchase shows **A new Slime Daycare furniture recipe has been learned.** The read-only Management Desk preview reports 3/7/15 furniture slots by tier and lists furniture in the backpack; placing or editing furniture through the desk is still in development.

Current furniture crafting materials are provisional balance values:

| Recipe | Ingredients |
|---|---|
| Trampoline | 50 Wood, 20 Fiber, 5 vanilla Slime |
| Cushion | 30 Fiber, 5 vanilla Slime |
| Slide | 50 Wood, 15 Stone |
| Seesaw | 75 Wood, 2 Iron Bars, 10 Fiber |
| Heated Slime Stone | 50 Stone, 5 Coal, 5 vanilla Slime |
| Pond | 75 Stone, 20 vanilla Slime, 10 Fiber |
| Tunnel | 60 Wood, 30 Fiber, 10 vanilla Slime |
| Swing | 60 Wood, 20 Fiber, 2 Iron Bars |
| Lamp | 30 Wood, 2 Iron Bars, 5 Coal |
| Fairy Lights | 50 Fiber, 20 Sap, 5 Solar Essence |
| Ball Pit | 50 Wood, 50 Fiber, 20 vanilla Slime |
| Washing Machine | 100 Wood, 10 Iron Bars, 20 vanilla Slime |

The Washing Machine is decorative and has no washing behavior. The active refinery conversion is documented separately below.
The Management Desk Slime Wiki builds its material list from the same recipe definitions used by Crafting, so the in-game reference follows ingredient balance changes automatically. It also shows each recipe's current GMCM price by unlock tier.
- Furniture activities are decorative and do not change happiness or breeding outcomes. Domesticated Slimes bounce on Trampolines, squish on Cushions, hop and glide on Slides, sway together on Seesaws, rest beside Heated Slime Stones, bob in Ponds, crawl through Tunnels, sway on Swings, gather by Lamps, dip in Ball Pits, and watch the Fairy Lights glow in the evening.
- On a disposable save, `slime_ranching_give_washing_machine` grants one Slime Washing Machine for sprite and placement checks. It is a decorative item so far; it has no washing interaction or slime effect. The command automatically uses the loaded save and warns if no save is open.
- The former Slime Playground, Slime Bed, and Compost Trough prototypes are no longer sold or active. Existing placed objects and inventory items are retained for save compatibility, but do not have interactions.
- After Maru repairs it, the Mutagenic Pool runs a two-day, nonlethal attempt on one mature Domesticated Slime. Each attempt starts at 10%. During an active attempt, add 10 vanilla Slime for +1 percentage point; the segmented meter shows base chance, pity, and your contribution, up to a guaranteed 100%. Each slime keeps its own pity and contribution. Failure adds 1 percentage point of pity; contribution drops by 1 point only when it is above 2%. Success keeps pity and clears contribution. Canceling releases the slime without a roll and preserves all chance percentages. Approved outcomes are Starry, Spotted, Glowing, Crystalline, Mossy, Bubbly, Mushroom, Blooming, Firefly, Crowned, Cowboy, and Smelly. Each has a distinct cosmetic accent; mutations grant no gameplay ability. Old Shadow Clone traits remain supported as legacy data.
- The Gel Refinery is designed as a fixed facility at the final Daycare tier. Its current movable prototype is not sold by Marnie; use `slime_ranching_give_gel_refinery` for testing. Starting a new batch is locked until the Daycare reaches its final tier. Its provisional recipe uses 1 Broken Glasses and 10 vanilla Slime to make 1 Refined Quartz over one in-game day. Fuel and processing time can be tuned in GMCM or `config.json`. The fuel option has a 10-Slime minimum so this prototype cannot make direct sale-value profit. See the recipe section below and verify on a copied save.
- Marnie also sells a Slime Tunnel. Nearby idle Domesticated Slimes can do a short decorative crawl-through; use `slime_ranching_give_tunnel` to test it. This animation does not change stats or save state.
- Marnie also sells a Heated Slime Stone. Nearby idle Domesticated Slimes can rest beside its ember glow; use `slime_ranching_give_heated_stone` to test it. It is decorative and does not grant winter or friendship bonuses yet.
- Marnie also sells a Slime Pond. Nearby idle slimes briefly bob as if swimming; use `slime_ranching_give_pond` to test it. It currently produces no drops or friendship effects.
- Marnie also sells a Slime Swing. Nearby idle slimes briefly sway on it; use `slime_ranching_give_swing` to test it. This is a visual interaction only.
- The Seesaw, Slime Ball Pit, and Slime Lamp are activity prototypes with recipes in Marnie's upgrade-gated stock. On a copied save, the tester commands `slime_ranching_give_seesaw`, `slime_ranching_give_ball_pit`, and `slime_ranching_give_lamp` can grant them directly. Two nearby idle Domesticated Slimes can use a Seesaw together; a Ball Pit serves one slime; a Lamp uses Stardew lighting and can gather multiple slimes. These visual activities provide no happiness or breeding bonus.
- Domesticated Slimes only use an activity station if Stardew's pathfinder can find a route from the slime to an open approach tile beside it. A station blocked on every side is ignored until a route opens.
- Chromatic Crystals can focus a breeder cycle on one base offspring color. They are currently test-only items; use the SMAPI command `slime_ranching_give_crystals` while on a disposable save. A crystal does not create a hybrid or an exceptional species.
- Breeding previews show the possible offspring and abilities. Two same-color base slimes produce that base type; two different base types produce a two-ability hybrid. Matching hybrids produce the same hybrid, a hybrid/base pairing has equal odds of either type, and different hybrids choose from their four base contributors. Rare or exceptional species cannot breed.
- Domesticated Slime offspring and genome-specific egg icons use a tint selected from the genome color. Hybrid genomes store a blend of their two base colors; the current sprite resolves that blend to the nearest base tint.
- The Slime Incubator, Gel Refinery, and Mutagenic Pool use 3Ã—2 placement and collision areas. The Slime Breeder retains its 5Ã—2 footprint.

## Daycare construction and upgrades

Each save can have one Slime Daycare. Robin's Carpenter menu offers its construction for 10,000g, 350 Wood, 150 Stone, and 50 vanilla Slime; after construction starts, the basic option is hidden and Robin's upgrade path remains available for that building. If the player demolishes the Daycare, Robin offers the basic building again. Existing saves with multiple Daycares keep them; no buildings or residents are removed. The three farmhouse-matched upgrades cost 10,000g + 450 Wood, 65,000g + 100 Hardwood, and 100,000g with no materials. Capacities are 10 at basic tier, 20 at upgrades 1 and 2, and 30 at the final tier. The first upgrade also unlocks Daycare-specific room renovations, the Egg CryoLab, and Rare Labs; the final upgrade adds the cellar and unlocks the last-tier facilities. The full facility progression is still under development.

## Slime Incubator

The Slime Incubator accepts a genome-specific Domesticated Slime Egg from the player's inventory. Vanilla Slime Eggs are not accepted.

1. Request a testing incubator with `slime_ranching_give_incubator`.
2. Place it inside the Slime Daycare. Use it while holding a genome-specific egg in your inventory; select the egg in the menu.
3. The egg is removed from inventory; the saved incubator job keeps its genome and reaches its ready day after **7 in-game days**. The duration is fixed for new jobs. Any incubation already in progress retains its saved ready day when updating the mod.
4. While an egg is incubating, choose **Cancel** to review a confirmation. Confirming destroys that unique egg and gives you one Fried Egg; make sure your inventory has room. A canceled egg copy cannot be incubated later.
5. At the ready day, an unlocked Rare Lab fixes a single rare-or-regular result and saves it before hatch creation. A full Daycare waits with that result fixed until space is available. A fixed result cannot be canceled or rerolled.

If your inventory has no eligible egg, the incubator counts Domesticated Slime Egg copies that are already used or otherwise unavailable. A canceled copy remains unusable because egg IDs can only be incubated once.

Rare Lab progression is saved separately for each incubator: 0% at the basic Daycare, 1% after the first building upgrade, and 5% at the final tier. While an egg is incubating, contribute 10 vanilla Slime for +1 percentage point; the meter shows base chance, pity, and your contribution. A failed roll adds 1 point of pity and reduces contribution by 1 point only when contribution is above 2%; a successful roll clears contribution and retains pity. In an empty unlocked Rare Lab, choose a catalyst from the items in your inventory: 10 Gold Bars target Gold Slime, 1 Void Egg or 20 Void Essence target Void Slime, and 1 Prismatic Shard targets Prismatic Slime. The catalyst remains stored after a failed rare roll and is consumed by a successful rare roll. Without a catalyst, rare results are selected evenly from Gold, Void, and Prismatic Slime, independent of the egg's species. The rare species replaces species identity and rarity while preserving the egg's other genome traits. The Egg CryoLab remains in development.

The Egg CryoLab storage rules can be tested on an upgraded Daycare with `slime_ranching_give_cryolab`. Place the tester chest inside the Daycare and use it to store unused Domesticated Slime Eggs. It holds 36 slots, does not start incubation, and rejects vanilla eggs and other items. Rejection messages explain when an egg is already used, still belongs to a Breeder, or has a duplicate ID in your inventory or another CryoLab. Take a stored egg back to inventory before inserting it into an incubator. Its permanent fixture placement remains pending Daycare map acceptance.

For a duplicate-identity guard check, use `slime_ranching_duplicate_cryolab_egg` while inside a tier-1-or-higher Daycare that has an unused Domesticated Slime Egg in a tester CryoLab. It deliberately copies that egg's unique metadata into your inventory so you can try storing the same ID in a second Daycare. Use this command only on a disposable save, discard the duplicate after the check, and don't save while the duplicate exists.

To test inventory duplicate detection and hatch-once protection, place one unused Domesticated Slime Egg in your inventory, enter its Daycare, and run `slime_ranching_duplicate_inventory_egg`. The command adds a second item with the same egg ID. `slime_ranching_inspect` should report the shared ID and mark the later slot as a duplicate. Incubate either copy; after that ID hatches, the remaining copy must be unavailable. Use a disposable save and discard it after the check.

For a guaranteed rare-outcome check on a copied single-player save, enter the Daycare containing the target incubator and run `slime_ranching_set_rare_lab <incubator number> <pity 0-100> <contribution 0-100>`. Incubator numbers follow that Daycare's placed tile order from top-left to bottom-right; `slime_ranching_inspect` prints each number and tile. Set values only when that incubator has no egg or unclaimed hatch pending. At tier 3, its first incubator can be set to 100% with `slime_ranching_set_rare_lab 1 0 95`; this sets test values only and does not consume Slime. Inspect again to confirm the total before starting the egg.

In 2.0, newly hatched Domesticated Slimes mature after **7 in-game days**. The breeder marks growing slimes in its list and won't let them be selected. The testing spawn command lets you choose an age from 0 to 7 days.

An egg's genome ID can hatch only once, so copied breeder egg items with that same ID become inactive after the first hatch; canceling an incubation also permanently retires that egg ID. Vanilla eggs are unaffected. In 2.0, the seven-day duration is fixed; full-Daycare waiting and cancellation still need in-game confirmation.

### Test egg command

With the intended single-player save loaded, use the SMAPI console to add two genome-specific test eggs:

```text
slime_ranching_give_test_eggs
```

To generate one egg with randomized genome traits, add `random`:

```text
slime_ranching_give_test_eggs random
```

The randomized egg rolls a base color/species, optional secondary species, size, rarity, shiny state, eligible favorite food, generation, and (at 10%) one of the approved cosmetic mutations. Its parent links remain empty because this is a test genome, not offspring from a real pair. The default and `key=value` forms still give two eggs.

For controlled testing, pass one or more `key=value` options instead. Any omitted option keeps the command's default; explicit values apply to both eggs, while their genome IDs remain distinct:

```text
slime_ranching_give_test_eggs species=Mint_Slime secondary=none color=#65DDBB size=Giant rarity=Rare shiny=true food=(O)168 generation=4 mutation=Starry
```

Available keys: `species`, `secondary` (`none` clears it), `color` (`#RRGGBB`), `size` (`Small`, `Normal`, `Large`, or `Giant`), `rarity` (text tier), `shiny` (`true`/`false`), `food` (eligible item ID), `generation` (0-999), and `mutation` (`Starry`, `Spotted`, `Glowing`, `Crystalline`, `Mossy`, `Bubbly`, `Mushroom`, `Blooming`, `Firefly`, `Crowned`, `Cowboy`, `Smelly`, or `none`). Eligible food IDs include `(O)684` Bug Meat, `(O)766` Slime, `(O)168` Trash, and `(O)170` Broken Glasses. The `random` mode can't be combined with `key=value` options.

## Hutch inspection

Incubator job entries also show the saved egg genome and, once a Rare Lab outcome is fixed, the hatch genome. A regular hatch keeps the egg genome; a rare hatch shows its distinct genome ID and resolved species.

Enter the Slime Daycare or a vanilla Slime Hutch and run:

```text
slime_ranching_inspect
```

In the Daycare, this reports Domesticated Slime and genome counts, assignment-status totals, native resident capacity as `occupied/limit`, each ready Breeder egg's ID and saved genome summary, each incubator's saved Rare Lab chance state, every marked Egg CryoLab slot's egg ID, genome summary, and stack size, and each Domesticated Slime Egg in your inventory with its egg/genome IDs, stack size, registry match, incubation eligibility, and duplicate checks against inventory and this Daycare's CryoLab. Any unique egg stack larger than one is flagged as invalid. In the vanilla Slime Hutch, it reports the monster slime count and confirms those slimes remain unchanged.

For a disposable-save stack-safety check, run `slime_ranching_give_test_eggs malformed`. It adds two intentionally invalid Domesticated Slime Egg items. Run `slime_ranching_inspect` inside the Daycare; both slots should be reported as invalid and neither item should merge with the other in inventory or an ordinary chest. These are diagnostics only and cannot be incubated or stored in the Egg CryoLab.

Add an animal ID to inspect one managed Domesticated Slime's genome, favorite food, maturity, mutation pity, assignment status, native friendship/happiness/fullness values, enrichment activities used today, its next roaming check, and its live activity summary. While active, the summary includes activity kind, furniture item ID, furniture tile, participant count, and elapsed/total in-game minutes:

```text
slime_ranching_inspect <Domesticated Slime animal ID>
```

The assignment is reported as **Available**, **Selected for breeding**, **Breeding**, **Mutagenic Pool**, **Incubator hatch**, or **Companion**. Friendship, happiness, and fullness are the native animal values; the Feeding Trough and custom feeding effects are still under development.

### Slime Daycare Quest progress diagnostic

Tester commands use the save currently loaded in Stardew. Omit the save-folder argument from older examples. Running a command at the title screen warns and does nothing. This terminology reset also requires a new disposable test save; older saves use the previous animal and egg IDs.

On a copied development save, run:

```text
slime_ranching_quest_status
```

This read-only command reports the save-data schema and each saved Slime Daycare Quest milestone. After Robin's Pool prompt is accepted, it also reports the in-game day when Maru's mission becomes available. Story scenes and the starter-egg sequence are still being implemented. Construction and first-breeding milestones only advance after their earlier story prerequisites have been recorded. Robin's Pool prompt and Maru's repair handoff wait for the construction milestone.

To test the Robin-to-Maru handoff before the opening scenes are complete, use a disposable single-player save with a fully constructed Daycare and run:

```text
slime_ranching_test_maru_repair
```

This testing command marks the Lewis, Wizard, Marnie, and Daycare-construction ledger milestones so Robin's prompt can be tested. It skips those scenes and changes saved quest progress; do not use it on a main save. After priming, talk to Robin and follow the Maru repair steps below. When the opening story is implemented, test the normal story route separately without this helper.

In normal play, after Daycare construction, talk to Robin about the broken Mutagenic Pool and choose to ask Maru. The repair mission appears in the journal the next morning. Speak to Maru with 1 Battery Pack, 5 Iron Bars, and 10 Refined Quartz to hand over the materials and activate the Pool. The normal route is not connected to the opening story yet; use the testing command above only on a disposable copy to reach this handoff meanwhile.

## Mutagenic Pool

After Maru repairs the Pool, use it inside the Slime Daycare and select one mature, unreserved Domesticated Slime. Starting costs no Slime. The attempt takes two in-game days and begins at 10%; while it runs, each 10 vanilla Slime adds 1 percentage point and is consumed. The color-coded bar separates base chance, pity, and contribution. At 100%, the mutation is guaranteed. Each failed roll is safe and adds 1 percentage point of that slime's pity. On failure, contribution falls by 1 point only when above 2%; otherwise it carries forward unchanged. On success, pity stays and contribution resets to zero. Use **Cancel** during an attempt to release the slime without a roll; all chance percentages remain unchanged.

Attempts, rolls, mutation traits, pity, and contributions are stored in the per-save mod data. Slime contributions use a save receipt so an interrupted save restores the resource and prior chance together. The approved traits are Starry, Spotted, Glowing, Crystalline, Mossy, Bubbly, Mushroom, Blooming, Firefly, Crowned, Cowboy, and Smelly. Their accents are twinkling stars, spots, a time-sensitive glow, crystal facets, a sprout, rising bubbles, a mushroom cap, a flower, orbiting fireflies, a crown, a cowboy hat, and odor wisps, respectively. They are visual only. The old Shadow Clone echo is retained only for compatible saves. Use a copied test save, and keep the mod installed while its custom Pool item remains in the save.

For chance testing on a disposable single-player save, use `slime_ranching_set_pool_test_state <animal ID> <failures 0-90> <contribution 0-90; total <= 90>`. It sets the selected mature, unmutated, unreserved Domesticated Slime's saved Pool chance state without starting an attempt or consuming Slime. Use only on a copied save.

## Gel Refinery

Place the refinery inside the Slime Daycare and reach the final building tier before starting a new batch. Then use it with 1 Broken Glasses (`(O)170`) and at least 10 vanilla Slime (`(O)766`) in your inventory; the current configured amount can be higher. The idle menu shows the Broken Glasses count, the exact Slime fuel requirement, and a **NEED INPUTS** state until both are available; clicking then explains what is missing without consuming anything. The current recipe produces one Refined Quartz (`(O)338`) after the configured number of in-game days. Its recipe guide lists this as the only active conversion and explains that inputs are consumed when a batch starts, only one batch runs per refinery, and finished output stays reserved until collected. A full inventory does not consume or discard the batch. These recipe and economy values are early prototypes. Start and collection changes reconcile the saved machine job with the Stardew inventory across reloads.

## Retired furniture prototypes

The old Slime Playground, Slime Bed, and Compost Trough are not part of the active 2.0 catalog. Their item definitions remain only so existing saves can load previously placed objects and stored items. They are inert. The active furniture recipes and craftable items are implemented; fixed room positions and editing remain in development.

## Field Guide book

The in-game **Slime Ranching Field Guide** is a short introduction to the current features. For testing, request it from the SMAPI console while the intended save is loaded:

```text
slime_ranching_give_guidebook
```

Select the book and press the action button to open it. It is a custom item reference in the game save, so keep the mod installed while the book remains in that save. This markdown guide has the fuller usage details.

## Daycare Management Desk tester preview

The Registry, Progression, and Slime Wiki sections have a read-only tester preview. To open it, stand inside the Slime Daycare with no other menu open and run this in the SMAPI console:

```text
slime_ranching_open_management_desk
```

The Registry lists resident Domesticated Slimes and their genome, status, and current activity. A selected resident's native friendship (0–1000), happiness (0–255), and fullness (0–255) appear as labeled bars with numeric values. They are informational; feeding and custom happiness effects are still under development. Progression shows the building tier, facilities found in that Daycare, current breeding-cycle duration, whether upgrade facilities are unlocked, the current base Rare Lab chance, and the next Robin upgrade's price and planned unlocks; at the final tier it reports that the Daycare is fully upgraded. The second upgrade currently has no additional machine unlock specified. Slime Wiki browses base, hybrid, and rare types, breeding rules, and the life cycle. It explains that incubation and growth each take seven days, while egg identity and Rare Lab results remain fixed; ability effects are planned companion directions and do not activate in normal play. Use number keys 1-3 to switch sections, Up/Down to move through a list, and Left/Right to switch Wiki categories. With a controller, the left/right triggers switch sections, D-pad Up/Down moves through lists, Left/Right changes Wiki categories (or sections outside the Wiki), and B closes the preview. Press Escape to close it with a keyboard. It does not change the save. Furniture editing and opening the menu from a placed desk are still under development.

## Features still under development

- **Hatching:** Common and genome-specific Domesticated Slime Eggs hatch in the custom incubator; vanilla eggs are unaffected.
- **Breeding:** The breeder, parent selection, 18-day basic-tier cycle, 14-day cycles from Daycare upgrade 1 onward, and fixed-genome egg output work. Active cycles keep their recorded ready day across upgrades. Offspring inherit color/species only; the egg icon uses the same rendered color variant as the slime, and ability identifiers follow its species. Other offspring traits start at Normal size, Common rarity, non-shiny, no mutation, and a newly assigned favorite food. Parent links and generation remain as lineage metadata.
- **Egg details:** Hover over a genome-specific Domesticated Slime Egg to view its stored species, color, size, rarity, mutation, generation, parent IDs, and favorite food.
- **Genetics and slime identity:** The registry model can hold genome data. Domesticated Slime genomes are keyed to native FarmAnimal IDs; this still needs save/reload verification. Existing GreenSlime monsters are only captured in the first-seen snapshot; they aren't changed or adopted.
- **Food likes:** Favorite food is stored and assigned independently for each offspring, and shown in the breeder. No feeding bonus has been approved; the legacy Compost Trough growth prototype is retired.
- **Furniture:** The old Playground, Bed, and Compost Trough prototypes are inactive. All twelve approved recipes and craftable furniture items are implemented, including the tiered Marnie shop and learned notice. The read-only desk preview and slot test commands expose saved assignments, but fixed in-room placement and Management Desk edit mode remain in development.
- **Mutations:** The Mutagenic Pool now rolls from the approved twelve-trait list with per-slime pity and contribution. All twelve traits have distinct rendered cosmetic accents; old Shadow Clone records are compatibility data. In-game visual checks remain pending.
- **Production and companions:** Slime-produced goods and custom companion AI are not active gameplay features yet.

## Domesticated Slime animal prototype

The 1.x prototype defines a **Slime Daycare**, a separate animal-house building, and a **Domesticated Slime** farm-animal type. For testing, the Daycare is free and completes instantly. The exterior uses prototype roof art; Domesticated Slimes use the game's vanilla Green Slime body frames and face art recomposed as a FarmAnimal sprite sheet. The earlier 1.x build used the Barn map inside. The active 2.0 branch now uses a custom Daycare interior without changing vanilla barns or the Slime Hutch. Build the Daycare through Robin, then run this mod command in the SMAPI console:

```text
slime_ranching_spawn_domesticated_slime <age in days 0-7> [random]
```

For a 2.0 test copy, use `slime_ranching_spawn_domesticated_slime 7` to spawn a mature slime, or use `slime_ranching_spawn_domesticated_slime 0` to spawn a newborn. Add the optional `random` tag (for example, `slime_ranching_spawn_domesticated_slime 0 random`) to keep the chosen age while randomizing the genome traits: primary species/color, a 30% chance of a secondary species, size, rarity, a 10% shiny chance, favorite food, generation (0â€“6), and a 10% chance of an approved mutation. Without the tag, the command uses the default green Domesticated Slime genome: no secondary species, Normal size, Common rarity, not shiny, generation 0, and no mutation; the mod assigns its favorite food. Ages 1â€“6 create juveniles that need the remaining days to reach maturity. The command adds a Domesticated Slime without converting or removing any vanilla slimes, and immediately saves its genome to the mod's active save data. The animal itself and standard properties like its name, home, age, and friendship are stored by Stardew when the game saves.

To set a managed Domesticated Slime's visual size for testing, use `slime_ranching_set_size <animal ID> <Small|Normal|Large|Giant>`. This does not affect offspring size; bred offspring currently start at Normal size.

On a disposable save, these additional commands set an existing managed Domesticated Slime's test genome: `slime_ranching_set_color <animal ID> <Red|Orange|Yellow|Green|Blue|Purple|Pink>` changes its base species and color; `slime_ranching_set_shiny <animal ID> <true|false>` changes its shiny flag; and `slime_ranching_set_mutation <animal ID> <Starry|Spotted|Glowing|Crystalline|Mossy|Bubbly|Mushroom|Blooming|Firefly|Crowned|Cowboy|Smelly|none>` applies or clears a cosmetic mutation. Use `slime_ranching_give_color_eggs` to add seven unique visual-color test eggs; leave seven inventory slots free.

For facility and furniture checks, `slime_ranching_give_breeder`, `slime_ranching_give_mutagenic_pool`, and `slime_ranching_give_fairy_lights` each grant one test item to place inside the Daycare. The Pool command bypasses the normal Robin-to-Maru repair handoff; use it only to check the machine prototype. Fairy Lights currently show their colored glow from 6:00 PM to 2:00 AM, but their final timing and fixed slot position remain undecided.

The companion commands are identity tests only. `slime_ranching_bind_companion <animal ID>` binds one mature, unreserved Daycare slime; it remains in the Daycare because follower movement and the Core item are not implemented. `slime_ranching_dismiss_companion` clears that test binding while leaving the same slime in the Daycare. Both commands change saved test state, so use a disposable save.

### Breeder selection preview

The breeder lists Daycare Domesticated Slimes and previews selected parents in a chamber panel. Select two rows and click **Start Breeding**; the breeder reserves them automatically and starts an 18-day timer at the basic tier, or a 14-day timer from the first Daycare upgrade onward. The pair, start/ready days, and resulting genome are stored in the active save data and survive reopening or reloading. Active cycles retain their saved ready day if you upgrade the Daycare. Reserved slimes stop roaming where they are in the Daycare; the chamber remains a menu preview. Click **Cancel Cycle** to stop the timer and immediately release both parents. When egg formation finishes, the parents return to roaming in the Daycare and rest for one in-game day. The egg stays in the breeder until you click **Collect Egg**; it is never moved to a nearby chest automatically. Hover over the collected egg to inspect its stored genome. Use the custom Slime Incubator to hatch a managed egg.

## Data and save behavior

The mod keeps its registry and first-seen snapshot under:

```text
<installed mod folder>/data/saves/<save folder name>/
```

To create a timestamped ZIP backup of that save-specific mod data while playing, run:

```text
slime_ranching_backup_external_data
```

The archive is written under `<installed mod folder>/data/backups/`. It contains the external registry and first-seen snapshot, not the vanilla Stardew save. Keep making normal game-save backups separately.

## 2.0 Daycare interior

The Slime Daycare remains an animal house, so Domesticated Slimes continue to live and roam inside its farmhouse-style layout. The basic tier uses `FarmHouse`, the first upgrade uses `FarmHouse1`, and the second and final upgrades use `FarmHouse2`; the final tier also adds the Daycare's cellar. Dedicated machine rooms will be fitted into this familiar layout as room upgrades are implemented.

**Decorating the Daycare:** Use normal Stardew **Wallpaper** and indoor **Flooring** items from a shop or catalogue. Select the item and apply it as you would in the farmhouse. Each Daycare saves its own choices when you save the game. Outdoor craftable paths are a separate item type. This support applies to the Daycare; it does not turn other barns into decoratable rooms.

To review it, run **3 Build, Deploy, Run** on the copied test save, enter the Slime Daycare, and check the entrance, perimeter collision and all open floor—including the former breeder, incubator and pond areas. The room uses normal tile layers so walls and foreground trim can overlap the player correctly. Leave and re-enter, then confirm the vanilla Barn and Slime Hutch still use their normal interiors. In the review gallery, search for **Daycare Floor**, **Daycare Walls**, or **Daycare Layers Comparison** to inspect the separated surfaces without launching the game. This is still Feature 2; report any map-load error, wrong overlap or blocked walkway before proceeding.

The Field Guide, Slime Breeder, Slime Incubator, and genome-specific Domesticated Slime Eggs are custom item references in the Stardew save. If the mod is removed while one remains in a save, Stardew will show it as an Error Item. The Slime Daycare building and Domesticated Slime animals are native game entities saved by Stardew. Existing `GreenSlime` monsters are not matched or changed. Domesticated Slime genome records are stored in each animal's `modData`; save-wide machine jobs and progression use per-save SMAPI data. Legacy external registry data is migration input only.

## Development book status

The in-game book is a short overview; this file is the detailed player guide. Expand both with a dedicated section as each feature is implemented and tested, keeping unavailable features clearly labeled.
