# Size genetics and maturity

**Independent axes:** size (Small/Normal/Large/Giant) and life stage (juvenile/adult). Small adults and Giant juveniles are valid.

## Visual size profiles (not inherited)

Breeding does not pass size from parents to offspring. Every bred slime starts at Normal size; size remains an independently configurable visual trait. Ranch Slimes use size-specific nearest-neighbor sprite sheets with 40x60 frame cells. Normal-size opaque pixels keep the vanilla 16x24 art dimensions; Small shrinks, Large expands, and Giant scales to 2.5x, bottom-aligned to keep their feet on the same baseline. In-game alignment, collision bounds, and petting targets still need validation.
## Companion scaling
- Small: smallest movement profile and capacity.
- Small: smallest sprite and capacity; faster activity.
- Normal: baseline.
- Large: larger amounts/radius and slower activations.
- Giant: 2.5x normal sprite size; largest amounts/radius, slowest activations.

Scale per ability (integer rounding, hard caps) rather than naively multiplying every effect. Discounts and time effects use special rules.

## Juvenile maturation
Proposed 7 days without food, ~4–5 with consistent Compost Trough feeding. Feeding accepts trash, algae and monster drops; food increases growth progress, not genetic size. Specific nutritional point values remain balancing parameters.

Each managed slime also has one persistent liked food, stored in its external `SlimeGenome` as a qualified vanilla item ID as an individual slime trait; offspring receive a separately assigned favorite food. The initial pool is Bug Meat, Slime, Bat Wing, Solar Essence, Void Essence, Joja Cola, Trash, Driftwood, Broken Glasses, Broken CD and Soggy Newspaper. Existing genomes missing a like receive a stable choice based on their slime ID. The Compost Trough prototype consumes an eligible item once per day per juvenile and advances maturity by one day; the stored favorite adds one provisional extra day. These amounts still need balance review.
