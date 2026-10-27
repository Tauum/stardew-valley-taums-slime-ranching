using Microsoft.Xna.Framework;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Schedules short, transient interactions with enrichment furniture for nearby Ranch Slimes.</summary>
internal sealed class EnrichmentInteractionService
{
    private const int AssignmentIntervalTicks = 30;
    private const float InteractionRadiusPixels = 128f;
    private readonly Dictionary<long, Interaction> active = new();
    private readonly Dictionary<long, float> cooldowns = new();
    private int assignmentTicks;
    private AnimalHouse? activeHouse;
    private float elapsedSeconds;

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
        }

        float deltaSeconds = (float)time.ElapsedGameTime.TotalSeconds;
        this.elapsedSeconds += deltaSeconds;
        this.UpdateActiveInteractions(deltaSeconds, registry);
        if (++this.assignmentTicks < AssignmentIntervalTicks)
            return;
        this.assignmentTicks = 0;
        if (!IsSlimeDaycareInterior(house))
        {
            this.Clear();
            return;
        }
        if (registry is not null)
            this.AssignNearbySlimes(house, registry);
    }

    public void Clear()
    {
        foreach (Interaction bounce in this.active.Values)
            bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
        this.active.Clear();
        this.cooldowns.Clear();
        this.activeHouse = null;
        this.assignmentTicks = 0;
        this.elapsedSeconds = 0;
    }

    private static bool IsSlimeDaycareInterior(AnimalHouse house) =>
        Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && ReferenceEquals(building.GetIndoors(), house));

    private void UpdateActiveInteractions(double elapsedSeconds, SlimeRegistry? registry)
    {
        List<long>? completed = null;
        foreach (KeyValuePair<long, Interaction> entry in this.active)
        {
            long id = entry.Key;
            Interaction bounce = entry.Value;
            if (registry?.IsReserved(id) == true)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
                (completed ??= new List<long>()).Add(id);
                continue;
            }

            bounce.ElapsedSeconds += (float)elapsedSeconds;
            if (bounce.ElapsedSeconds >= bounce.DurationSeconds)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
                (completed ??= new List<long>()).Add(id);
                continue;
            }

            if (bounce.SwingDistancePixels > 0)
            {
                float progress = bounce.ElapsedSeconds / bounce.DurationSeconds;
                float sway = MathF.Sin(progress * MathF.PI * 2f);
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset
                    + new Vector2(sway * bounce.SwingDistancePixels, -MathF.Abs(sway) * 2f);
                continue;
            }

            float compressionProgress = Math.Clamp(bounce.ElapsedSeconds / bounce.CompressionDurationSeconds, 0f, 1f);
            if (bounce.IsResting)
            {
                float restProgress = bounce.ElapsedSeconds / bounce.DurationSeconds;
                float restAmount = restProgress < 0.2f
                    ? MathF.Sin(Math.Clamp(restProgress / 0.2f, 0f, 1f) * MathF.PI / 2f)
                    : restProgress > 0.8f
                        ? MathF.Sin(Math.Clamp((1f - restProgress) / 0.2f, 0f, 1f) * MathF.PI / 2f)
                        : 1f;
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset + new Vector2(0f, bounce.CompressionHeightPixels * restAmount);
                continue;
            }

            if (bounce.ElapsedSeconds < bounce.CompressionDurationSeconds)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset
                    + new Vector2(0f, bounce.CompressionHeightPixels * MathF.Sin(compressionProgress * MathF.PI / 2f));
                continue;
            }

            float flightDuration = Math.Max(0.01f, bounce.DurationSeconds - bounce.CompressionDurationSeconds);
            float flightProgress = Math.Clamp((bounce.ElapsedSeconds - bounce.CompressionDurationSeconds) / flightDuration, 0f, 1f);
            float hop = MathF.Sin(flightProgress * MathF.PI);
            float slide = bounce.HorizontalTravelPixels * flightProgress;
            bounce.Animal.drawOffset = bounce.OriginalDrawOffset + new Vector2(slide, -hop * bounce.BounceHeightPixels);
        }

        if (completed is not null)
            foreach (long id in completed)
                this.active.Remove(id);
    }

    private void AssignNearbySlimes(AnimalHouse house, SlimeRegistry registry)
    {
        List<long>? expiredCooldowns = null;
        foreach (KeyValuePair<long, float> entry in this.cooldowns)
        {
            if (entry.Value <= this.elapsedSeconds)
                (expiredCooldowns ??= new List<long>()).Add(entry.Key);
        }
        if (expiredCooldowns is not null)
            foreach (long id in expiredCooldowns)
                this.cooldowns.Remove(id);

        HashSet<Vector2> occupiedTiles = this.active.Values
            .Select(interaction => interaction.FurnitureTile)
            .ToHashSet();
        var furnitureSpots = house.Objects.Values
            .Where(obj => obj.QualifiedItemId == FurnitureContent.TrampolineQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeCushionQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimePlaygroundQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeSlideQualifiedId
            || obj.QualifiedItemId == FurnitureContent.SlimeBedQualifiedId
            || obj.QualifiedItemId == FurnitureContent.SlimeTunnelQualifiedId
            || obj.QualifiedItemId == FurnitureContent.HeatedSlimeStoneQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimePondQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeSwingQualifiedId)
            .Select(obj => new FurnitureSpot(obj.TileLocation, obj.QualifiedItemId))
            .Where(spot => !occupiedTiles.Contains(spot.Tile))
            .ToList();
        if (furnitureSpots.Count == 0)
            return;

        List<FarmAnimal> idleSlimes = house.animals.Values
                     .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                         && !this.active.ContainsKey(animal.myID.Value)
                         && !this.cooldowns.ContainsKey(animal.myID.Value)
                         && !registry.IsReserved(animal.myID.Value))
            .ToList();

        // Playgrounds are group furniture: reserve one spot for two nearby slimes and start their hops together.
        foreach (FurnitureSpot playground in furnitureSpots.Where(spot => spot.QualifiedItemId == FurnitureContent.SlimePlaygroundQualifiedId).ToList())
        {
            Vector2 center = playground.Tile * 64f + new Vector2(32f, 32f);
            FarmAnimal[] pair = idleSlimes
                .Select(animal => (Animal: animal, Distance: Vector2.DistanceSquared(animal.Position, center)))
                .Where(candidate => candidate.Distance < InteractionRadiusPixels * InteractionRadiusPixels)
                .OrderBy(candidate => candidate.Distance)
                .Take(2)
                .Select(candidate => candidate.Animal)
                .ToArray();
            furnitureSpots.Remove(playground);
            if (pair.Length < 2)
                continue;

            foreach (FarmAnimal animal in pair)
            {
                this.StartInteraction(animal, playground.Tile, 2.6f, 18f, 5f);
                idleSlimes.Remove(animal);
            }
        }

        foreach (FarmAnimal animal in idleSlimes)
        {
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
                continue;

            FurnitureSpot spot = furnitureSpots[nearestIndex];
            furnitureSpots.RemoveAt(nearestIndex);
            if (spot.QualifiedItemId == FurnitureContent.SlimeSwingQualifiedId)
            {
                this.StartInteraction(animal, spot.Tile, 3.8f, 2f, 0f, swingDistance: 8f);
                continue;
            }

            (float duration, float bounceHeight, float compressionHeight, float slideDistance, bool isResting) = spot.QualifiedItemId switch
            {
                var id when id == FurnitureContent.SlimeCushionQualifiedId => (1.8f, 9f, 10f, 0f, false),
                var id when id == FurnitureContent.SlimeSlideQualifiedId => (2.2f, 8f, 3f, 30f, false),
                var id when id == FurnitureContent.SlimeBedQualifiedId => (4f, 0f, 7f, 0f, true),
                var id when id == FurnitureContent.SlimeTunnelQualifiedId => (2.8f, 5f, 2f, 42f, false),
                var id when id == FurnitureContent.HeatedSlimeStoneQualifiedId => (4.4f, 0f, 6f, 0f, true),
                var id when id == FurnitureContent.SlimePondQualifiedId => (3.2f, 4f, 0f, 0f, false),
                _ => registry.GetGenomeSize(animal.myID.Value)?.ToLowerInvariant() switch
                {
                    "tiny" => (1.8f, 36f, 2f, 0f, false),
                    "small" => (2.1f, 32f, 3f, 0f, false),
                    "large" => (2.8f, 24f, 7f, 0f, false),
                    "giant" => (3.2f, 21f, 9f, 0f, false),
                    _ => (2.4f, 28f, 4f, 0f, false)
                }
            };
            this.StartInteraction(animal, spot.Tile, duration, bounceHeight, compressionHeight, slideDistance, isResting);
        }
    }

    private void StartInteraction(FarmAnimal animal, Vector2 furnitureTile, float duration, float bounceHeight, float compressionHeight,
        float slideDistance = 0f, bool isResting = false, float swingDistance = 0f)
    {
        this.active[animal.myID.Value] = new Interaction(animal, furnitureTile, animal.drawOffset,
            duration, bounceHeight, compressionHeight, slideDistance, isResting, swingDistance);
        this.cooldowns[animal.myID.Value] = this.elapsedSeconds + duration + 3f;
    }

    private readonly record struct FurnitureSpot(Vector2 Tile, string QualifiedItemId);

    private sealed class Interaction
    {
        public Interaction(FarmAnimal animal, Vector2 furnitureTile, Vector2 originalDrawOffset,
            float durationSeconds, float bounceHeightPixels, float compressionHeightPixels, float horizontalTravelPixels,
            bool isResting, float swingDistancePixels)
        {
            this.Animal = animal;
            this.FurnitureTile = furnitureTile;
            this.OriginalDrawOffset = originalDrawOffset;
            this.DurationSeconds = durationSeconds;
            this.BounceHeightPixels = bounceHeightPixels;
            this.CompressionHeightPixels = compressionHeightPixels;
            this.HorizontalTravelPixels = horizontalTravelPixels;
            this.IsResting = isResting;
            this.SwingDistancePixels = swingDistancePixels;
            this.CompressionDurationSeconds = durationSeconds * 0.16f;
        }

        public FarmAnimal Animal { get; }
        public Vector2 FurnitureTile { get; }
        public Vector2 OriginalDrawOffset { get; }
        public float DurationSeconds { get; }
        public float BounceHeightPixels { get; }
        public float CompressionHeightPixels { get; }
        public float HorizontalTravelPixels { get; }
        public bool IsResting { get; }
        public float SwingDistancePixels { get; }
        public float CompressionDurationSeconds { get; }
        public float ElapsedSeconds { get; set; }
    }
}
