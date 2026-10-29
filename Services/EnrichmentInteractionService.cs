using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Pathfinding;

namespace taums_slime_ranching.Services;

/// <summary>Staggers roaming checks and longer enrichment activities for Ranch Slimes.</summary>
internal sealed class EnrichmentInteractionService
{
    private const int AssignmentIntervalTicks = 30;
    private const float InteractionRadiusPixels = 128f;
    private static readonly Point[] ApproachTileOffsets =
    {
        new(0, -1),
        new(1, 0),
        new(0, 1),
        new(-1, 0)
    };
    private readonly Dictionary<long, Interaction> active = new();
    private int assignmentTicks;
    private AnimalHouse? activeHouse;
    private int activeDay = -1;

    public void Update(GameLocation? location, GameTime time, SlimeRegistry? registry)
    {
        if (location is not AnimalHouse house)
        {
            this.Clear();
            return;
        }

        if (!ReferenceEquals(this.activeHouse, house))
        {
            this.Clear();
            if (!IsSlimeDaycareInterior(house))
                return;
            this.activeHouse = house;
            this.activeDay = Game1.Date.TotalDays;
        }

        int currentMinute = GetMinutesOfDay(Game1.timeOfDay);
        if (this.activeDay != Game1.Date.TotalDays)
        {
            this.StopActiveInteractions();
            this.activeDay = Game1.Date.TotalDays;
        }
        this.UpdateActiveInteractions(house, (float)time.ElapsedGameTime.TotalSeconds * (10f / 7f), registry);
        if (++this.assignmentTicks < AssignmentIntervalTicks)
            return;
        this.assignmentTicks = 0;
        if (!IsSlimeDaycareInterior(house))
        {
            this.Clear();
            return;
        }
        if (registry is not null)
            this.AssignNearbySlimes(house, registry, Game1.Date.TotalDays, currentMinute);
    }

    public void Clear()
    {
        this.StopActiveInteractions();
        this.activeHouse = null;
        this.activeDay = -1;
        this.assignmentTicks = 0;
    }

    public string? GetActiveActivitySummary(long animalId)
    {
        if (!this.active.TryGetValue(animalId, out Interaction? interaction))
            return null;

        string activity = EnrichmentStationCatalog.TryGet(interaction.FurnitureQualifiedItemId, out EnrichmentStationDefinition station)
            ? station.ActivityKind.ToString()
            : interaction.FurnitureQualifiedItemId;
        return $"{activity}; participants {interaction.Animals.Length}; elapsed {interaction.ElapsedMinutes:0}/{interaction.DurationMinutes} in-game minutes";
    }

    private void StopActiveInteractions()
    {
        foreach (Interaction interaction in this.active.Values.Distinct())
            interaction.RestoreDrawOffsets();
        this.active.Clear();
    }

    private static bool IsSlimeDaycareInterior(AnimalHouse house) =>
        Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && ReferenceEquals(building.GetIndoors(), house));

    private void UpdateActiveInteractions(AnimalHouse house, float elapsedGameMinutes, SlimeRegistry? registry)
    {
        List<Interaction>? completed = null;
        var updated = new HashSet<Interaction>();
        foreach (KeyValuePair<long, Interaction> entry in this.active)
        {
            Interaction interaction = entry.Value;
            if (!updated.Add(interaction))
                continue;

            bool interrupted = interaction.Animals.Any(animal => registry?.IsReserved(animal.myID.Value) == true
                || !house.animals.TryGetValue(animal.myID.Value, out FarmAnimal? currentAnimal)
                || !ReferenceEquals(currentAnimal, animal));
            if (interrupted
                || !house.Objects.TryGetValue(interaction.FurnitureTile, out StardewValley.Object? currentFurniture)
                || currentFurniture.QualifiedItemId != interaction.FurnitureQualifiedItemId)
            {
                interaction.RestoreDrawOffsets();
                (completed ??= new List<Interaction>()).Add(interaction);
                continue;
            }

            interaction.ElapsedMinutes += elapsedGameMinutes;
            if (interaction.ElapsedMinutes >= interaction.DurationMinutes)
            {
                interaction.RestoreDrawOffsets();
                (completed ??= new List<Interaction>()).Add(interaction);
                continue;
            }

            for (int i = 0; i < interaction.Animals.Length; i++)
            {
                FarmAnimal animal = interaction.Animals[i];
                if (interaction.IsLampGathering)
                {
                    float angle = interaction.SwayPhases[i] - MathF.PI / 2f;
                    float horizontalRadius = Math.Min(30f, 16f + interaction.Animals.Length * 1.2f);
                    float verticalRadius = Math.Min(20f, 9f + interaction.Animals.Length * 0.8f);
                    Vector2 lampBase = interaction.FurnitureTile * 64f + new Vector2(32f, 32f);
                    Vector2 gatheringPoint = lampBase + new Vector2(MathF.Cos(angle) * horizontalRadius, MathF.Sin(angle) * verticalRadius);
                    float gentleBob = MathF.Sin(interaction.ElapsedMinutes * 0.12f + interaction.SwayPhases[i]) * 1.5f;
                    animal.drawOffset = interaction.OriginalDrawOffsets[i]
                        + (gatheringPoint - animal.Position)
                        + new Vector2(0f, gentleBob);
                    continue;
                }

                if (interaction.IsBallPit)
                {
                    float cycleMinute = interaction.ElapsedMinutes % 24f;
                    float dipOffset = cycleMinute < 5f
                        ? cycleMinute / 5f * 10f
                        : cycleMinute < 8f
                            ? 10f
                            : cycleMinute < 10f
                                ? 10f - (cycleMinute - 8f) / 2f * 22f
                                : cycleMinute < 14f
                                    ? -12f * (1f - (cycleMinute - 10f) / 4f)
                                    : 0f;
                    Vector2 pitCenter = interaction.FurnitureTile * 64f + new Vector2(32f, 32f);
                    animal.drawOffset = interaction.OriginalDrawOffsets[i]
                        + (pitCenter - animal.Position)
                        + new Vector2(0f, dipOffset);
                    continue;
                }

                if (interaction.SwingDistancePixels > 0)
                {
                    float progress = interaction.ElapsedMinutes / interaction.DurationMinutes;
                    float sway = MathF.Sin(progress * MathF.PI * 2f + interaction.SwayPhases[i]);
                    Vector2 swayOffset = interaction.IsSeesaw
                        ? new Vector2(sway * interaction.SwingDistancePixels, -sway * 6f)
                        : new Vector2(sway * interaction.SwingDistancePixels, -MathF.Abs(sway) * 2f);
                    if (interaction.IsSeesaw)
                    {
                        Vector2 seat = interaction.FurnitureTile * 64f + new Vector2(i == 0 ? 20f : 44f, 32f);
                        animal.drawOffset = interaction.OriginalDrawOffsets[i] + (seat - animal.Position) + swayOffset;
                    }
                    else
                        animal.drawOffset = interaction.OriginalDrawOffsets[i] + swayOffset;
                    continue;
                }

                float elapsedMinutesFloat = interaction.ElapsedMinutes;
                float compressionProgress = Math.Clamp(elapsedMinutesFloat / interaction.CompressionDurationMinutes, 0f, 1f);
                if (interaction.IsResting)
                {
                    float restProgress = elapsedMinutesFloat / interaction.DurationMinutes;
                    float restAmount = restProgress < 0.2f
                        ? MathF.Sin(Math.Clamp(restProgress / 0.2f, 0f, 1f) * MathF.PI / 2f)
                        : restProgress > 0.8f
                            ? MathF.Sin(Math.Clamp((1f - restProgress) / 0.2f, 0f, 1f) * MathF.PI / 2f)
                            : 1f;
                    animal.drawOffset = interaction.OriginalDrawOffsets[i] + new Vector2(0f, interaction.CompressionHeightPixels * restAmount);
                    continue;
                }

                if (elapsedMinutesFloat < interaction.CompressionDurationMinutes)
                {
                    animal.drawOffset = interaction.OriginalDrawOffsets[i]
                        + new Vector2(0f, interaction.CompressionHeightPixels * MathF.Sin(compressionProgress * MathF.PI / 2f));
                    continue;
                }

                float flightDuration = Math.Max(0.01f, interaction.DurationMinutes - interaction.CompressionDurationMinutes);
                float flightProgress = Math.Clamp((elapsedMinutesFloat - interaction.CompressionDurationMinutes) / flightDuration, 0f, 1f);
                float hop = MathF.Sin(flightProgress * MathF.PI);
                float slide = interaction.HorizontalTravelPixels * flightProgress;
                animal.drawOffset = interaction.OriginalDrawOffsets[i] + new Vector2(slide, -hop * interaction.BounceHeightPixels);
            }
        }

        if (completed is not null)
            foreach (Interaction interaction in completed)
                foreach (FarmAnimal animal in interaction.Animals)
                    this.active.Remove(animal.myID.Value);
    }

    private void AssignNearbySlimes(AnimalHouse house, SlimeRegistry registry, int currentDay, int currentMinute)
    {
        HashSet<Vector2> occupiedTiles = this.active.Values
            .Select(interaction => interaction.FurnitureTile)
            .ToHashSet();
        var furnitureSpots = new List<FurnitureSpot>();
        foreach (StardewValley.Object obj in house.Objects.Values)
        {
            if (occupiedTiles.Contains(obj.TileLocation)
                || !EnrichmentStationCatalog.TryGet(obj.QualifiedItemId, out EnrichmentStationDefinition definition))
                continue;

            furnitureSpots.Add(new FurnitureSpot(obj.TileLocation, definition));
        }
        var idleSlimes = new List<(FarmAnimal Animal, Models.SlimeRecord Record)>();
        var activityCandidates = new List<(FarmAnimal Animal, Models.SlimeRecord Record)>();
        var reachabilityCache = new Dictionary<(long AnimalId, Vector2 Tile), bool>();
        var checkedThisPass = new HashSet<long>();
        foreach (FarmAnimal animal in house.animals.Values.Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
            && !this.active.ContainsKey(animal.myID.Value)
            && !registry.IsReserved(animal.myID.Value)))
        {
            if (!registry.TryGetSlimeRecord(animal.myID.Value, out Models.SlimeRecord record))
                continue;
            idleSlimes.Add((animal, record));
            if (record.EnrichmentActivityDay != currentDay)
            {
                record.EnrichmentActivityDay = currentDay;
                record.EnrichmentActivitiesToday = 0;
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
            }
            if (record.EnrichmentActivitiesToday >= 4)
                continue;
            if (record.NextEnrichmentCheckMinute < 0)
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
            if (currentMinute < record.NextEnrichmentCheckMinute)
                continue;

            checkedThisPass.Add(animal.myID.Value);
            // After each roaming period, each slime independently gets a 50% chance to use furniture.
            if (Game1.random.NextDouble() >= 0.5)
            {
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
                continue;
            }

            activityCandidates.Add((animal, record));
        }

        for (int i = activityCandidates.Count - 1; i > 0; i--)
        {
            int swapIndex = Game1.random.Next(i + 1);
            (activityCandidates[i], activityCandidates[swapIndex]) = (activityCandidates[swapIndex], activityCandidates[i]);
        }
        HashSet<long> passedSelection = activityCandidates.Select(candidate => candidate.Animal.myID.Value).ToHashSet();

        foreach ((FarmAnimal animal, Models.SlimeRecord record) in activityCandidates)
        {
            if (this.active.ContainsKey(animal.myID.Value) || record.EnrichmentActivitiesToday >= 4)
                continue;

            Vector2 animalPosition = animal.Position;
            FurnitureSpot? selectedSpot = null;
            FarmAnimal? partner = null;
            Models.SlimeRecord? partnerRecord = null;
            foreach (FurnitureSpot spot in furnitureSpots
                .OrderBy(spot => Vector2.DistanceSquared(animalPosition, spot.Tile * 64f + new Vector2(32f, 32f))))
            {
                Vector2 center = spot.Tile * 64f + new Vector2(32f, 32f);
                float distance = Vector2.DistanceSquared(animalPosition, center);
                if (distance >= InteractionRadiusPixels * InteractionRadiusPixels
                    || !HasReachableApproach(house, animal, spot, reachabilityCache))
                    continue;

                if (spot.Definition.ActivityKind == EnrichmentActivityKind.PairedSwing)
                {
                    foreach ((FarmAnimal candidate, Models.SlimeRecord candidateRecord) in idleSlimes)
                    {
                        if (candidate.myID.Value == animal.myID.Value
                            || this.active.ContainsKey(candidate.myID.Value)
                            || candidateRecord.EnrichmentActivitiesToday >= 4
                            || Vector2.DistanceSquared(candidate.Position, center) >= InteractionRadiusPixels * InteractionRadiusPixels
                            || !HasReachableApproach(house, candidate, spot, reachabilityCache))
                            continue;

                        long candidateId = candidate.myID.Value;
                        if (checkedThisPass.Contains(candidateId) && !passedSelection.Contains(candidateId))
                            continue;
                        if (!checkedThisPass.Contains(candidateId))
                        {
                            checkedThisPass.Add(candidateId);
                            if (Game1.random.NextDouble() >= 0.5)
                            {
                                candidateRecord.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
                                continue;
                            }
                        }

                        partner = candidate;
                        partnerRecord = candidateRecord;
                        break;
                    }

                    if (partner is null)
                        continue;
                }

                selectedSpot = spot;
                break;
            }

            if (selectedSpot is not FurnitureSpot spotToUse)
            {
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
                continue;
            }

            int durationMinutes = Game1.random.Next(120, 181);
            if (spotToUse.Definition.ActivityKind == EnrichmentActivityKind.LampGathering)
            {
                Vector2 lampCenter = spotToUse.Tile * 64f + new Vector2(32f, 32f);
                var gatherers = activityCandidates
                    .Where(candidate => candidate.Animal.myID.Value != animal.myID.Value
                        && !this.active.ContainsKey(candidate.Animal.myID.Value)
                        && candidate.Record.EnrichmentActivitiesToday < 4
                        && Vector2.DistanceSquared(candidate.Animal.Position, lampCenter) < InteractionRadiusPixels * InteractionRadiusPixels
                        && IsNearestAvailableSpot(house, candidate.Animal, spotToUse, furnitureSpots, reachabilityCache))
                    .Select(candidate => (candidate.Animal, candidate.Record))
                    .Prepend((animal, record))
                    .ToArray();
                furnitureSpots.Remove(spotToUse);
                foreach ((FarmAnimal gatherer, Models.SlimeRecord gathererRecord) in gatherers)
                    this.RecordActivity(gathererRecord, currentMinute, durationMinutes);
                this.StartLampGathering(gatherers.Select(gatherer => gatherer.Item1).ToArray(), spotToUse, durationMinutes);
                continue;
            }

            furnitureSpots.Remove(spotToUse);
            this.RecordActivity(record, currentMinute, durationMinutes);
            if (spotToUse.Definition.ActivityKind == EnrichmentActivityKind.PairedSwing && partner is not null && partnerRecord is not null)
            {
                this.RecordActivity(partnerRecord, currentMinute, durationMinutes);
                this.StartPairedInteraction(animal, partner, spotToUse, durationMinutes);
                continue;
            }

            if (spotToUse.Definition.ActivityKind == EnrichmentActivityKind.BallPit)
            {
                this.StartInteraction(animal, spotToUse, durationMinutes, 0f, 0f, isBallPit: true);
                continue;
            }

            if (spotToUse.Definition.ActivityKind == EnrichmentActivityKind.Swing)
            {
                this.StartInteraction(animal, spotToUse, durationMinutes, 0f, 0f,
                    swingDistance: spotToUse.Definition.SwingDistancePixels);
                continue;
            }

            (float bounceHeight, float compressionHeight, float slideDistance, bool isResting) =
                GetBounceParameters(animal.myID.Value, registry, spotToUse.Definition);
            this.StartInteraction(animal, spotToUse, durationMinutes,
                bounceHeight, compressionHeight, slideDistance, isResting,
                swingDistance: spotToUse.Definition.SwingDistancePixels);
        }
    }

    private void RecordActivity(Models.SlimeRecord record, int currentMinute, int durationMinutes)
    {
        record.EnrichmentActivitiesToday++;
        record.NextEnrichmentCheckMinute = currentMinute + durationMinutes + Game1.random.Next(120, 241);
    }

    private void StartInteraction(FarmAnimal animal, FurnitureSpot furniture, int durationMinutes,
        float bounceHeight, float compressionHeight,
        float slideDistance = 0f, bool isResting = false, float swingDistance = 0f, bool isBallPit = false)
    {
        this.StartInteraction(new[] { animal }, new[] { 0f }, furniture, durationMinutes,
            bounceHeight, compressionHeight, slideDistance, isResting, swingDistance, isBallPit: isBallPit);
    }

    private void StartPairedInteraction(FarmAnimal first, FarmAnimal second, FurnitureSpot furniture, int durationMinutes)
    {
        this.StartInteraction(new[] { first, second }, new[] { 0f, MathF.PI }, furniture, durationMinutes,
            0f, 0f, swingDistance: furniture.Definition.SwingDistancePixels, isSeesaw: true);
    }

    private void StartLampGathering(FarmAnimal[] animals, FurnitureSpot furniture, int durationMinutes)
    {
        float[] phases = Enumerable.Range(0, animals.Length)
            .Select(index => MathF.PI * 2f * index / animals.Length)
            .ToArray();
        this.StartInteraction(animals, phases, furniture, durationMinutes,
            0f, 0f, isLampGathering: true);
    }

    private static bool IsNearestAvailableSpot(AnimalHouse house, FarmAnimal animal, FurnitureSpot expectedSpot,
        List<FurnitureSpot> availableSpots, Dictionary<(long AnimalId, Vector2 Tile), bool> reachabilityCache)
    {
        Vector2 animalPosition = animal.Position;
        FurnitureSpot? nearestSpot = null;
        float nearestDistance = InteractionRadiusPixels * InteractionRadiusPixels;
        foreach (FurnitureSpot spot in availableSpots)
        {
            float distance = Vector2.DistanceSquared(animalPosition, spot.Tile * 64f + new Vector2(32f, 32f));
            if (distance >= nearestDistance || !HasReachableApproach(house, animal, spot, reachabilityCache))
                continue;

            nearestSpot = spot;
            nearestDistance = distance;
        }

        return nearestSpot is FurnitureSpot nearest
            && nearest.Tile == expectedSpot.Tile
            && nearest.QualifiedItemId == expectedSpot.QualifiedItemId;
    }

    private static bool HasReachableApproach(AnimalHouse house, FarmAnimal animal, FurnitureSpot station,
        Dictionary<(long AnimalId, Vector2 Tile), bool> reachabilityCache)
    {
        var cacheKey = (animal.myID.Value, station.Tile);
        if (reachabilityCache.TryGetValue(cacheKey, out bool reachable))
            return reachable;

        Point stationTile = new((int)station.Tile.X, (int)station.Tile.Y);
        int pathSearchLimit = house.map.Layers[0].LayerWidth * house.map.Layers[0].LayerHeight;
        foreach (Point approachOffset in ApproachTileOffsets)
        {
            Point approachTile = stationTile + approachOffset;
            if (approachTile.X < 0 || approachTile.Y < 0
                || approachTile.X >= house.map.Layers[0].LayerWidth
                || approachTile.Y >= house.map.Layers[0].LayerHeight)
                continue;

            if (PathFindController.findPath(animal.TilePoint, approachTile, PathFindController.isAtEndPoint,
                house, animal, pathSearchLimit) is not null)
            {
                reachabilityCache[cacheKey] = true;
                return true;
            }
        }

        reachabilityCache[cacheKey] = false;
        return false;
    }

    private static (float BounceHeight, float CompressionHeight, float SlideDistance, bool IsResting) GetBounceParameters(
        long animalId, SlimeRegistry registry, EnrichmentStationDefinition station)
    {
        if (!station.UsesGenomeSizeBounce)
            return (station.BounceHeightPixels, station.CompressionHeightPixels, station.HorizontalTravelPixels, station.IsResting);

        return registry.GetGenomeSize(animalId)?.ToLowerInvariant() switch
        {
            "small" => (32f, 3f, 0f, false),
            "large" => (24f, 7f, 0f, false),
            "giant" => (21f, 9f, 0f, false),
            _ => (28f, 4f, 0f, false)
        };
    }

    private void StartInteraction(FarmAnimal[] animals, float[] swayPhases, FurnitureSpot furniture, int durationMinutes,
        float bounceHeight, float compressionHeight,
        float slideDistance = 0f, bool isResting = false, float swingDistance = 0f, bool isSeesaw = false, bool isBallPit = false, bool isLampGathering = false)
    {
        var interaction = new Interaction(animals, swayPhases, furniture.Tile, furniture.QualifiedItemId,
            durationMinutes, bounceHeight, compressionHeight, slideDistance, isResting, swingDistance, isSeesaw, isBallPit, isLampGathering);
        foreach (FarmAnimal animal in animals)
            this.active[animal.myID.Value] = interaction;
    }

    private static int GetMinutesOfDay(int timeOfDay) => timeOfDay / 100 * 60 + timeOfDay % 100;

    private readonly record struct FurnitureSpot(Vector2 Tile, EnrichmentStationDefinition Definition)
    {
        public string QualifiedItemId => this.Definition.QualifiedItemId;
    }

    private sealed class Interaction
    {
        public Interaction(FarmAnimal[] animals, float[] swayPhases, Vector2 furnitureTile, string furnitureQualifiedItemId,
            int durationMinutes, float bounceHeightPixels, float compressionHeightPixels, float horizontalTravelPixels,
            bool isResting, float swingDistancePixels, bool isSeesaw, bool isBallPit, bool isLampGathering)
        {
            this.Animals = animals;
            this.SwayPhases = swayPhases;
            this.OriginalDrawOffsets = animals.Select(animal => animal.drawOffset).ToArray();
            this.FurnitureTile = furnitureTile;
            this.FurnitureQualifiedItemId = furnitureQualifiedItemId;
            this.DurationMinutes = durationMinutes;
            this.BounceHeightPixels = bounceHeightPixels;
            this.CompressionHeightPixels = compressionHeightPixels;
            this.HorizontalTravelPixels = horizontalTravelPixels;
            this.IsResting = isResting;
            this.SwingDistancePixels = swingDistancePixels;
            this.IsSeesaw = isSeesaw;
            this.IsBallPit = isBallPit;
            this.IsLampGathering = isLampGathering;
            this.CompressionDurationMinutes = durationMinutes * 0.16f;
        }

        public FarmAnimal[] Animals { get; }
        public Vector2[] OriginalDrawOffsets { get; }
        public float[] SwayPhases { get; }
        public Vector2 FurnitureTile { get; }
        public string FurnitureQualifiedItemId { get; }
        public int DurationMinutes { get; }
        public float BounceHeightPixels { get; }
        public float CompressionHeightPixels { get; }
        public float HorizontalTravelPixels { get; }
        public bool IsResting { get; }
        public float SwingDistancePixels { get; }
        public bool IsSeesaw { get; }
        public bool IsBallPit { get; }
        public bool IsLampGathering { get; }
        public float CompressionDurationMinutes { get; }
        public float ElapsedMinutes { get; set; }

        public void RestoreDrawOffsets()
        {
            for (int i = 0; i < this.Animals.Length; i++)
                this.Animals[i].drawOffset = this.OriginalDrawOffsets[i];
        }
    }
}
