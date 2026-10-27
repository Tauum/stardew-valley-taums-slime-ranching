# Rarity, shiny, exceptional colors and mutations

Keep separate fields: `Species`, `RarityTier`, `IsShiny`, `MutationTrait`, `Size`.

- **Shiny:** rare cosmetic variant, ~10% performance increase; can coexist with eligible colors and sizes.
- **Golden exceptional species:** shop discount with strict cap; size must not yield unlimited discount.
- **Prismatic exceptional species:** rideable at Large/Giant, plus Adaptation (temporarily uses one base ability); mount speed rules separate from general size scaling.
- **Black exceptional species:** extends day from usual 2:00 AM pass-out to proposed 3:00 AM while active; needs day-transition safeguards.
- **White exceptional species:** begins day at proposed 5:00 AM if activated at sleep; needs morning initialization safeguards.
- **Shadow Clone:** rare mutation trait that makes temporary copies performing reduced inherited abilities; copies never produce resources independently without caps, breed or clone recursively.
- **Void:** removed as separate species; pocket inventory merged into Green Collector.

## Mutagenic Pool
Consumes a substantial quantity of vanilla Slime per attempt (proposed 500); adult slime is temporarily assigned. Proposed 1 day processing, base 5% chance + 2 percentage points per failed attempt, cap 30%. Per-slime pity counter persists; success resets it. No slime death on failure. Mutation pool grants **traits**, not exceptional colors. Other mutation outcomes TBD.

## Open balancing
Acquisition probabilities, shiny inheritance, exceptional breeding rules, pool trait weights, Black/White multiplayer compatibility and Prismatic mounting limits require playtests.

## Current locked implementation clarification
All specified exceptional slime types remain core scope, implemented after fundamentals. Keep shiny, exceptional color and mutation independent. Shadow Clone remains; Black and White change time boundaries, Golden discounts are capped, Prismatic mounts and adapts.
