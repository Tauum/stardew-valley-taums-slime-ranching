# Species, color-wheel recipes and egg genetics

## Seven core species (locked role mapping)

| Base | Signature companion ability |
|---|---|
| Red | Fighter: attacks enemies |
| Yellow | Revealer: digs artifact spots and reveals hidden objects/secret-note clues where supported |
| Blue | Irrigator: waters crops |
| Green | Collector: gathers drops into inventory; incorporates former Void pocket storage idea |
| Orange | Nurturer: pets animals and improves happiness |
| Violet | Prospector: periodically grants bonus ore |
| Pink | Healer: gradually heals player |

## Conventional tertiary hybrids (proposed recipes)

| Parents | Offspring | Both abilities |
|---|---|---|
| Red + Orange | Red-Orange | Fighter + Nurturer |
| Yellow + Orange | Yellow-Orange | Revealer + Nurturer |
| Yellow + Green | Yellow-Green | Revealer + Collector |
| Blue + Green | Blue-Green | Irrigator + Collector |
| Blue + Violet | Blue-Violet | Irrigator + Prospector |
| Red + Violet | Red-Violet | Fighter + Prospector |

**Important unresolved color-wheel question:** Traditional primary pair recipes (Red+Yellow=Orange, Yellow+Blue=Green, Blue+Red=Violet) should likely produce those *pure secondary species*, not hybrids. Pink is outside the wheel and requires explicit recipes. No arbitrary combination automatically creates a hybrid. Decide behavior for undefined pairings (no breeding, one parent species, or other). These are design decisions, not implementation facts.

## Crystals
A consumable Chromatic Crystal inserted into the breeder overrides parent species to one of the seven pure base colors, never a hybrid, Golden, Prismatic, Black or White. Size and rarity rolls are separate. Exact acquisition recipes TBD.

## Eggs
Breeder outputs one Slime Egg with stored genome: parent IDs, species ID(s), color, size, rarity, shiny, mutation, companion ability IDs, generation. Tooltip/inspection exposes these fields. Different genomes do not stack. Egg hatching must use stored data without rerolling. Decide whether egg sprite colors are unique per species or recolored dynamically.

## Current locked implementation clarification
Breeding is FULLY CUSTOM: parent selection, visible bouncing enclosure, timers, cooldowns, and an externally stored egg genome with no reroll at hatch. Vanilla Slime Egg visuals are preferred, but a reliable external-only egg identity scheme is still required. Reusing vanilla eggs does not mean reusing vanilla reproduction.
