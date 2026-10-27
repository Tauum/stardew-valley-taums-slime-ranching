# Rarity, shiny, exceptional colors and mutations

Keep separate fields: `Species`, `RarityTier`, `IsShiny`, `MutationTrait`, `Size`.

- **Shiny:** rare cosmetic variant, ~10% performance increase; can coexist with eligible colors and sizes.
- **Golden exceptional species:** shop discount with strict cap; size must not yield unlimited discount.
- **Prismatic exceptional species:** rideable at Large/Giant, plus Adaptation (temporarily uses one base ability); mount speed rules separate from general size scaling.
- **Black exceptional species:** extends day from usual 2:00 AM pass-out to proposed 3:00 AM while active; needs day-transition safeguards.
- **White exceptional species:** begins day at proposed 5:00 AM if activated at sleep; needs morning initialization safeguards.
- **Shadow Clone:** rare mutation trait that makes temporary copies performing reduced inherited abilities; copies never produce resources independently without caps, breed or clone recursively.
- **Void:** removed as separate species; pocket inventory merged into Green Collector.

The current `IsShiny` visual prototype applies a subtle 22% brightness lift to the sprite. It is independent of size and color and has no gameplay/stat effect yet; the proposed ~10% performance increase remains undecided. Use `slime_ranching_set_shiny <save> <animal ID> true|false` to inspect it on a disposable save.

## Mutagenic Pool
The first pool prototype is implemented on `feature/mutagenic-pool-safe-failure`: place the Mutagenic Pool in the Slime Daycare, select one mature, unreserved Ranch Slime, and start an attempt. Defaults are 500 vanilla Slime for one day, with 5% chance plus 2 percentage points per failure, capped at 30%. The first trait is Shadow Clone. Failure never harms the slime, and its pity count persists in the external record. Chance and roll are fixed when an attempt starts, so reloading cannot reroll it. Success adds the `MutationTrait` genome field and clears pity. Cost, duration, chances, and shop price are configurable in `config.json`.

These numbers and the 15,000g Marnie price are test defaults, not final balance. A first visual echo prototype now draws a temporary translucent copy near mutated slimes in the Daycare. It is only a draw effect: there is no extra FarmAnimal, pathfinding, resource production, breeding, or inherited ability execution yet. The attempt and pity state live in the mod-folder registry. The 500 Slime inventory removal is handled by Stardew's normal save; sleep to persist both it and the external attempt. Test only on a disposable save until reload and crash recovery have been checked.

## Open balancing
Acquisition probabilities, independent shiny effects, pool trait weights, Black/White multiplayer compatibility and Prismatic mounting limits require playtests. Shiny inheritance is out of scope.

## Current locked implementation clarification
All specified exceptional slime types remain core scope, implemented after fundamentals. Keep shiny, exceptional color and mutation independent. Shadow Clone remains; Black and White change time boundaries, Golden discounts are capped, Prismatic mounts and adapts.

