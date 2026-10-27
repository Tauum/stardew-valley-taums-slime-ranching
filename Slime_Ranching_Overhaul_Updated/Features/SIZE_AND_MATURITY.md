# Size genetics and maturity

**Independent axes:** size (Tiny/Small/Normal/Large/Giant) and life stage (juvenile/adult). Tiny adults and Giant juveniles are valid.

## Initial inheritance examples (tunable)
- Small+Small: 75% Small, 20% Normal, 5% Tiny.
- Large+Large: 75% Large, 15% Normal, 10% Giant.
- Small+Large: 20% Small, 60% Normal, 20% Large.
- Giant+Giant: 85% Giant, 15% Large.

Use a configurable generalized weighted model for other pairs. Seeded tests should check distributions.

## Companion scaling
- Tiny: highest movement speed and shortest activation cooldown; smallest amount/capacity.
- Small: fast, slightly reduced amount.
- Normal: baseline.
- Large: larger amounts/radius and slower activations.
- Giant: largest amounts/radius, slowest activations.

Scale per ability (integer rounding, hard caps) rather than naively multiplying every effect. Discounts and time effects use special rules.

## Juvenile maturation
Proposed 7 days without food, ~4–5 with consistent Compost Trough feeding. Feeding accepts trash, algae and monster drops; food increases growth progress, not genetic size. Specific nutritional point values remain balancing parameters.

Each managed slime also has one persistent liked food, stored in its external `SlimeGenome` as a qualified vanilla item ID so it can later be inherited. The initial pool is Bug Meat, Slime, Bat Wing, Solar Essence, Void Essence, Joja Cola, Trash, Driftwood, Broken Glasses, Broken CD and Soggy Newspaper. Existing genomes missing a like receive a stable choice based on their slime ID. The effect of feeding a liked item is still to be balanced with the trough system; no item is consumed by this data-only feature.
