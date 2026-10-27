# Stage 3 — Genetics framework

Define fixed wheel recipes for primaries (red/yellow/blue), secondaries (orange/green/violet), six tertiary hybrids, and special Pink rules still to decide. Hybrid species can carry two base ability identifiers, even before behaviors are implemented. Define four **genetic** sizes: Small, Normal, Large, and Giant; maturity/juvenile status is separate. Inheritance favors parental sizes. Small is the smallest profile; Giant is 2.5x the normal sprite size and can favor range/output/capacity with slower activity. Keep shiny as an independent visual/stat variant; rare colors and mutation traits are separate properties. Preserve parent IDs and generation. Chromatic Crystal override support should be possible in the data model, implemented in Stage 5.

**Done when:** inheritance tests cover same-color, hybrid, size extremes, rarity independence and fixed egg outcomes.
