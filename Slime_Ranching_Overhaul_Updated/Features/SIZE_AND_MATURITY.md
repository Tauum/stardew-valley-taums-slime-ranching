# Size genetics and maturity

**Independent axes:** size (Tiny/Small/Normal/Large/Giant) and life stage (juvenile/adult). Tiny adults and Giant juveniles are valid.

## Initial inheritance examples (tunable)
- Small+Small: 75% Small, 20% Normal, 5% Tiny.
- Large+Large: 75% Large, 15% Normal, 10% Giant.
- Small+Large: 20% Small, 60% Normal, 20% Large.
- Giant+Giant: 85% Giant, 15% Large.

Use a configurable generalized weighted model for other pairs. Seeded tests should check distributions.

## Implemented prototype inheritance

Breeder offspring now roll among Tiny/Small/Normal/Large/Giant rather than copying one parent's size. The four specified examples above use those weights. Same-size Tiny parents provisionally roll 75% Tiny, 20% Small, 5% Normal; Normal parents roll 80% Normal, 10% Small, 10% Large. Adjacent mixed sizes split evenly; pairs two steps apart use 20% lower, 60% midpoint, 20% upper. Wider gaps use 25% for either endpoint and 50% for the middle category/categories. Unknown legacy size names are treated as Normal for this roll. Review these assumptions before balancing.

Ranch Slimes now use size-specific nearest-neighbor sprite sheets with 24x32 frame cells. Normal-size opaque pixels keep the vanilla 16x24 art dimensions; Tiny and Small shrink, while Large and Giant expand within the transparent frame, bottom-aligned to keep their feet on the same baseline. In-game alignment, collision bounds, and petting targets still need validation.

## Companion scaling
- Tiny: highest movement speed and shortest activation cooldown; smallest amount/capacity.
- Small: fast, slightly reduced amount.
- Normal: baseline.
- Large: larger amounts/radius and slower activations.
- Giant: largest amounts/radius, slowest activations.

Scale per ability (integer rounding, hard caps) rather than naively multiplying every effect. Discounts and time effects use special rules.

## Juvenile maturation
Proposed 7 days without food, ~4–5 with consistent Compost Trough feeding. Feeding accepts trash, algae and monster drops; food increases growth progress, not genetic size. Specific nutritional point values remain balancing parameters.

Each managed slime also has one persistent liked food, stored in its external `SlimeGenome` as a qualified vanilla item ID so it can later be inherited. The initial pool is Bug Meat, Slime, Bat Wing, Solar Essence, Void Essence, Joja Cola, Trash, Driftwood, Broken Glasses, Broken CD and Soggy Newspaper. Existing genomes missing a like receive a stable choice based on their slime ID. The Compost Trough prototype consumes an eligible item once per day per juvenile and advances maturity by one day; the stored favorite adds one provisional extra day. These amounts still need balance review.
