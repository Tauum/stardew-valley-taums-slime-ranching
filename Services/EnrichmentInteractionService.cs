using Microsoft.Xna.Framework;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Staggers roaming checks and longer enrichment activities for Ranch Slimes.</summary>
internal sealed class EnrichmentInteractionService
{
    private const int AssignmentIntervalTicks = 30;
    private const float InteractionRadiusPixels = 128f;
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

    private void StopActiveInteractions()
    {
        foreach (Interaction bounce in this.active.Values)
            bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
        this.active.Clear();
    }

    private static bool IsSlimeDaycareInterior(AnimalHouse house) =>
        Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && ReferenceEquals(building.GetIndoors(), house));

    private void UpdateActiveInteractions(AnimalHouse house, float elapsedGameMinutes, SlimeRegistry? registry)
    {
        List<long>? completed = null;
        foreach (KeyValuePair<long, Interaction> entry in this.active)
        {
            long id = entry.Key;
            Interaction bounce = entry.Value;
            if (registry?.IsReserved(id) == true
                || !house.animals.TryGetValue(id, out FarmAnimal? currentAnimal)
                || !ReferenceEquals(currentAnimal, bounce.Animal)
                || !house.Objects.TryGetValue(bounce.FurnitureTile, out StardewValley.Object? currentFurniture)
                || currentFurniture.QualifiedItemId != bounce.FurnitureQualifiedItemId)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
                (completed ??= new List<long>()).Add(id);
                continue;
            }

            bounce.ElapsedMinutes += elapsedGameMinutes;
            if (bounce.ElapsedMinutes >= bounce.DurationMinutes)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
                (completed ??= new List<long>()).Add(id);
                continue;
            }

            if (bounce.SwingDistancePixels > 0)
            {
                float progress = bounce.ElapsedMinutes / bounce.DurationMinutes;
                float sway = MathF.Sin(progress * MathF.PI * 2f);
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset
                    + new Vector2(sway * bounce.SwingDistancePixels, -MathF.Abs(sway) * 2f);
                continue;
            }

            float elapsedMinutesFloat = bounce.ElapsedMinutes;
            float compressionProgress = Math.Clamp(elapsedMinutesFloat / bounce.CompressionDurationMinutes, 0f, 1f);
            if (bounce.IsResting)
            {
                float restProgress = elapsedMinutesFloat / bounce.DurationMinutes;
                float restAmount = restProgress < 0.2f
                    ? MathF.Sin(Math.Clamp(restProgress / 0.2f, 0f, 1f) * MathF.PI / 2f)
                    : restProgress > 0.8f
                        ? MathF.Sin(Math.Clamp((1f - restProgress) / 0.2f, 0f, 1f) * MathF.PI / 2f)
                        : 1f;
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset + new Vector2(0f, bounce.CompressionHeightPixels * restAmount);
                continue;
            }

            if (elapsedMinutesFloat < bounce.CompressionDurationMinutes)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset
                    + new Vector2(0f, bounce.CompressionHeightPixels * MathF.Sin(compressionProgress * MathF.PI / 2f));
                continue;
            }

            float flightDuration = Math.Max(0.01f, bounce.DurationMinutes - bounce.CompressionDurationMinutes);
            float flightProgress = Math.Clamp((elapsedMinutesFloat - bounce.CompressionDurationMinutes) / flightDuration, 0f, 1f);
            float hop = MathF.Sin(flightProgress * MathF.PI);
            float slide = bounce.HorizontalTravelPixels * flightProgress;
            bounce.Animal.drawOffset = bounce.OriginalDrawOffset + new Vector2(slide, -hop * bounce.BounceHeightPixels);
        }

        if (completed is not null)
            foreach (long id in completed)
                this.active.Remove(id);
    }

    private void AssignNearbySlimes(AnimalHouse house, SlimeRegistry registry, int currentDay, int currentMinute)
    {
        HashSet<Vector2> occupiedTiles = this.active.Values
            .Select(interaction => interaction.FurnitureTile)
            .ToHashSet();
        var furnitureSpots = house.Objects.Values
            .Where(obj => obj.QualifiedItemId == FurnitureContent.TrampolineQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeCushionQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeSlideQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeTunnelQualifiedId
                || obj.QualifiedItemId == FurnitureContent.HeatedSlimeStoneQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimePondQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeSwingQualifiedId)
            .Select(obj => new FurnitureSpot(obj.TileLocation, obj.QualifiedItemId))
            .Where(spot => !occupiedTiles.Contains(spot.Tile))
            .ToList();
        List<FarmAnimal> idleSlimes = house.animals.Values
                     .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                         && !this.active.ContainsKey(animal.myID.Value)
                         && !registry.IsReserved(animal.myID.Value))
            .ToList();

        foreach (FarmAnimal animal in idleSlimes)
        {
            if (!registry.TryGetSlimeRecord(animal.myID.Value, out Models.SlimeRecord record))
                continue;
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

            // After each roaming period, each slime independently gets a 50% chance to use furniture.
            if (Game1.random.NextDouble() >= 0.5)
            {
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
                continue;
            }

            Vector2 animalPosition = animal.Position;
            int nearestIndex = -1;
            float nearestDistance = InteractionRadiusPixels * InteractionRadiusPixels;
            for (int i = 0; i < furnitureSpots.Count; i++)
            {
                Vector2 center = furnitureSpots[i].Tile * 64f + new Vector2(32f, 32f);
                float distance = Vector2.DistanceSquared(animalPosition, center);
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearestIndex = i;
            }

            if (nearestIndex < 0)
            {
                record.NextEnrichmentCheckMinute = currentMinute + Game1.random.Next(120, 241);
                continue;
            }

            FurnitureSpot spot = furnitureSpots[nearestIndex];
            furnitureSpots.RemoveAt(nearestIndex);
            int durationMinutes = Game1.random.Next(120, 181);
            record.EnrichmentActivitiesToday++;
            record.NextEnrichmentCheckMinute = currentMinute + durationMinutes + Game1.random.Next(120, 241);
            if (spot.QualifiedItemId == FurnitureContent.SlimeSwingQualifiedId)
            {
                this.StartInteraction(animal, spot, durationMinutes, 2f, 2f, 0f, swingDistance: 8f);
                continue;
            }

            (float bounceHeight, float compressionHeight, float slideDistance, bool isResting) = spot.QualifiedItemId switch
            {
                var id when id == FurnitureContent.SlimeCushionQualifiedId => (9f, 10f, 0f, false),
                var id when id == FurnitureContent.SlimeSlideQualifiedId => (8f, 3f, 30f, false),
                var id when id == FurnitureContent.SlimeTunnelQualifiedId => (5f, 2f, 42f, false),
                var id when id == FurnitureContent.HeatedSlimeStoneQualifiedId => (0f, 6f, 0f, true),
                var id when id == FurnitureContent.SlimePondQualifiedId => (4f, 0f, 0f, false),
                _ => registry.GetGenomeSize(animal.myID.Value)?.ToLowerInvariant() switch
                {
                    "small" => (32f, 3f, 0f, false),
                    "large" => (24f, 7f, 0f, false),
                    "giant" => (21f, 9f, 0f, false),
                    _ => (28f, 4f, 0f, false)
                }
            };
            this.StartInteraction(animal, spot, durationMinutes,
                bounceHeight, compressionHeight, slideDistance, isResting);
        }
    }

    private void StartInteraction(FarmAnimal animal, FurnitureSpot furniture, int durationMinutes,
        float bounceHeight, float compressionHeight,
        float slideDistance = 0f, bool isResting = false, float swingDistance = 0f)
    {
        this.active[animal.myID.Value] = new Interaction(animal, furniture.Tile, furniture.QualifiedItemId, animal.drawOffset,
            durationMinutes, bounceHeight, compressionHeight, slideDistance, isResting, swingDistance);
    }

    private static int GetMinutesOfDay(int timeOfDay) => timeOfDay / 100 * 60 + timeOfDay % 100;

    private readonly record struct FurnitureSpot(Vector2 Tile, string QualifiedItemId);

    private sealed class Interaction
    {
        public Interaction(FarmAnimal animal, Vector2 furnitureTile, string furnitureQualifiedItemId, Vector2 originalDrawOffset,
            int durationMinutes, float bounceHeightPixels, float compressionHeightPixels, float horizontalTravelPixels,
            bool isResting, float swingDistancePixels)
        {
            this.Animal = animal;
            this.FurnitureTile = furnitureTile;
            this.FurnitureQualifiedItemId = furnitureQualifiedItemId;
            this.OriginalDrawOffset = originalDrawOffset;
            this.DurationMinutes = durationMinutes;
            this.BounceHeightPixels = bounceHeightPixels;
            this.CompressionHeightPixels = compressionHeightPixels;
            this.HorizontalTravelPixels = horizontalTravelPixels;
            this.IsResting = isResting;
            this.SwingDistancePixels = swingDistancePixels;
            this.CompressionDurationMinutes = durationMinutes * 0.16f;
        }

        public FarmAnimal Animal { get; }
        public Vector2 FurnitureTile { get; }
        public string FurnitureQualifiedItemId { get; }
        public Vector2 OriginalDrawOffset { get; }
        public int DurationMinutes { get; }
        public float BounceHeightPixels { get; }
        public float CompressionHeightPixels { get; }
        public float HorizontalTravelPixels { get; }
        public bool IsResting { get; }
        public float SwingDistancePixels { get; }
        public float CompressionDurationMinutes { get; }
        public float ElapsedMinutes { get; set; }
    }
}
