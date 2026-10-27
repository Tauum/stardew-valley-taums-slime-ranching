# Machines, consumables and refinery

## Basic Slime Breeder (Stage 1)
Only intentional breeding in the Slime Daycare. Parent selection; visible bouncing Ranch Slime parents in a glass chamber; egg output with genome stored externally. See Stage 2.

## Chromatic Crystal (Stage 2/6)
A breeder-slot consumable, **not furniture**. Seven pure base-color variants, override parent color/species, cannot make hybrids or exceptional species. Recipe/unlock TBD.

## Mutagenic Pool (Stage 6)
Prototype implemented: place the machine inside the Slime Daycare, choose one mature unreserved Ranch Slime, and consume vanilla Slime for a mutation attempt. It currently grants the Shadow Clone mutation trait with a per-slime pity chance; failure is safe. The attempt and pity counter persist in the mod-folder registry, while item consumption is committed by the normal Stardew save. Sleep after starting an attempt. Cost, duration, base chance, pity increase, cap, and Marnie price can be tuned in `config.json`; defaults are 500 Slime, 1 day, 5% base, +2 percentage points per failure, 30% cap, and 15,000g. They remain provisional. Shadow Clone's actual ability behavior is not implemented yet. Verify on a disposable save.

## Gel Refinery (Stage 6)
The first vertical slice is implemented. Place it inside the Slime Daycare and use the action button. The current provisional recipe consumes 1 Broken Glasses and configurable vanilla Slime fuel, then produces 1 Refined Quartz after the configured number of in-game days. A notice appears when the batch is ready; the output remains reserved in the machine until collected. If the farmer's inventory is full, collection can be retried later. The job is stored in the mod-folder external registry (schema 10); item consumption and collection are committed alongside the normal Stardew save, so sleeping saves both sides of the action.

Default test values are 10 Slime fuel, 1 day, and 12,000g at Marnie's. Fuel, duration, and price can be adjusted in `config.json` or the optional GMCM page. These are placeholders for balance review, not final economy decisions. Run `slime_ranching_give_gel_refinery <exact save folder name>` to test without buying one. Multiplayer is not supported.

No new artisan material catalogue is planned; recipes should use existing items and avoid arbitrage loops. The following examples are proposals, not implemented recipes:

| Example recipe | Rule |
|---|---|
| 20 Stone → 5 Clay | Slime fuel cost TBD |
| 10 Copper Ore → 2 Iron Ore | High cost and progression gate |
| Broken Glasses → Refined Quartz | Recycling |
| Mixed Seeds → seasonal crop seeds | Random valid seasonal output |
| Cosmetic transmutation | Later investigation: separate visual identity from item functionality |

Avoid arbitrage loops and item duplication. Conversion should be atomic: consume input and fuel once, persist output, prevent rerolls via save/load.
