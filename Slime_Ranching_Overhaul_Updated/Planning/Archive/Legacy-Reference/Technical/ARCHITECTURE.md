# Technical architecture (proposed)

**Target:** C#, SMAPI, Stardew Valley 1.6+. Confirm actual APIs, game internals and supported mod versions before coding.

## Suggested services
- `SlimeRegistry`: external genome records keyed to native Ranch Slime FarmAnimal IDs and lifecycle.
- `GeneticsService`: fixed recipe resolution, size and rarity rolls; pure deterministic functions.
- `BreedingService`: reservations, machine state, egg genome generation.
- `EggIdentityService`: identify an egg through transfers without game-save metadata, then inspect/hatch it without reroll; feasibility must be proven first.
- `EnrichmentInteractionService`: low-frequency assignment for trampolines, cushions, playgrounds, slides, and beds; reserves target tiles, supports group play, and restores temporary sprite offsets on cleanup. Occupied target tiles and playground distances are cached per assignment pass.
- `MutationService`: pool rolls and pity counters.
- `ProductionService`: item output and refinery conversions.
- `CompanionService`: bound slime lifecycle, follower and ability activation.
- `TimeEffectService`: isolated Black/White mechanics, compatibility guards.
- `MultiplayerSyncService`: host-authoritative changes and replication.

## Data sketch
```csharp
record SlimeGenome(
    Guid Id, Guid? ParentA, Guid? ParentB, int Generation,
    string PrimarySpecies, string? SecondarySpecies,
    string VisualColor, string Size, bool IsShiny,
    string RarityTier, string? MutationTrait,
    string? LikedFoodItemId);
```
Use a versioned schema and stable IDs in the mod folder. Ranch Slime FarmAnimal records link to Stardew's native animal ID; in-game save/reload validation is still pending. Maturity age and its day clock are authoritative in the external registry and mirrored to `FarmAnimal.age` for Stardew's normal maturity behavior. Distinguish genetic size from maturity and species from visual appearance. The external registry holds an egg's genome snapshot, and a prototype distinct item ID links each egg to that snapshot. Verify that identity through item transfers and save/reload. Use stable machine IDs, reserved parent IDs, state and completion day. Avoid duplicating a slime record when transitioning to a companion.

## Host authority
Host resolves all random rolls and authoritative inventory/machine transactions. Clients display state. Time-altering powers may need to be disabled in multiplayer until verified.

## Suggested directory for future source code
```
ModEntry.cs
Models/
Services/
Integration/
UI/
Assets/
Tests/
```

## Risk register
1. Vanilla breeding suppression and hatching hooks.
2. Custom sprite size and collision.
3. External egg identity through inventory moves and stacking.
4. Cross-location companion entities and inventory.
5. 2 AM cutoff / 6 AM start and multiplayer synchronization.
6. Interactions with other slime overhaul mods.

## Current implementation authority
See `Technical/IMPLEMENTATION_CONTRACT.md` and `Planning/ROADMAP.md` for latest decisions. Earlier fast-track shortcuts involving simplified breeding or vanilla follower AI are superseded.
