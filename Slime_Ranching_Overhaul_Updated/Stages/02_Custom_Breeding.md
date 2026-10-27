# Stage 2 — Fully custom breeder (first gameplay feature)

Build a Slime Breeder for the Slime Daycare with custom selection UI. Exactly two mature, eligible Ranch Slime farm animals enter a transparent enclosure and are visually represented using their actual colors and genetic sizes, bouncing together throughout the cycle. Reserve parents so they cannot simultaneously roam/breed/be bound as companions. Approximate cycle target: two in-game days; exact balance adjustable. Implement parent cooldown and interruption/reload recovery.

At completion, compute the offspring genome **once** and keep it in the external mod registry. A prototype using distinct custom egg item IDs and external genome records is implemented; verify identity through inventory, chests, machines, and save/reload without writing genome data to the game save. Eggs with different genome IDs must not stack or lose identity. Integrate incubator hatching so a managed egg creates the stored juvenile exactly, with no reroll; unmanaged vanilla eggs retain vanilla behavior.

Do not change vanilla GreenSlime reproduction. Ranch Slimes are a separate FarmAnimal type and do not reproduce passively; all breeding happens through the custom Daycare breeder. Track source parents, generation, species, genetic size, shiny, rarity, mutation and ability identifiers in versioned data.

**Done when:** select → animated parents → egg → hatch → correct juvenile → save/reload, with all vanilla GreenSlime monsters unaffected.
