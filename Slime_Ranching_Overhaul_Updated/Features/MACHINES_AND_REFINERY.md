# Machines, consumables and refinery

## Basic Slime Breeder (Stage 1)
Only intentional breeding in the Slime Daycare. Parent selection; visible bouncing Ranch Slime parents in a glass chamber; egg output with genome stored externally. See Stage 2.

## Chromatic Crystal (Stage 2/6)
A breeder-slot consumable, **not furniture**. Seven pure base-color variants, override parent color/species, cannot make hybrids or exceptional species. Recipe/unlock TBD.

## Mutagenic Pool (Stage 6)
Prototype implemented: place the machine inside the Slime Daycare, choose one mature unreserved Ranch Slime, and consume 500 vanilla Slime for a one-day attempt. It currently grants the Shadow Clone mutation trait with a per-slime pity chance; failure is safe. The attempt and pity counter persist in the mod-folder registry, while item consumption is committed by the normal Stardew save. Sleep after starting an attempt. The 500-Slime cost, 5% base chance, +2 percentage points per failure, 30% cap, and 15,000g Marnie price are provisional test defaults. Shadow Clone's actual clone behavior is not implemented yet. Verify on a disposable save.

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
