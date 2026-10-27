# Machines, consumables and refinery

## Basic Slime Breeder (Stage 1)
Only intentional breeding in the Slime Daycare. Parent selection; visible bouncing Ranch Slime parents in a glass chamber; egg output with genome stored externally. See Stage 2.

## Chromatic Crystal (Stage 2/6)
A breeder-slot consumable, **not furniture**. Seven pure base-color variants, override parent color/species, cannot make hybrids or exceptional species. Recipe/unlock TBD.

## Mutagenic Pool (Stage 6)
Separate placeable machine. Fill with vanilla Slime, select adult, spend Slime for a chance to add a rare mutation; per-slime pity system; failure never kills. Does not recolor or resize the slime.

## Gel Refinery (Stage 6)
Stores vanilla Slime as fuel, accepts existing items and produces existing items. No new artisan material catalogue.

| Example recipe | Rule |
|---|---|
| 20 Stone → 5 Clay | Slime fuel cost TBD |
| 10 Copper Ore → 2 Iron Ore | High cost and progression gate |
| Broken Glasses → Refined Quartz | Recycling |
| Mixed Seeds → seasonal crop seeds | Random valid seasonal output |
| Cosmetic transmutation | Later investigation: separate visual identity from item functionality |

Avoid arbitrage loops and item duplication. Conversion should be atomic: consume input and fuel once, persist output, prevent rerolls via save/load.
