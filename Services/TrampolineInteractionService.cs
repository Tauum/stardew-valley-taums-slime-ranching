using Microsoft.Xna.Framework;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Runs short, local-only trampoline bounce animations for nearby Ranch Slimes.</summary>
internal sealed class TrampolineInteractionService
{
    private const int AssignmentIntervalTicks = 30;
    private const float InteractionRadiusPixels = 128f;
    private readonly Dictionary<long, Bounce> active = new();
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
        this.UpdateActiveBounces(deltaSeconds);
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
        foreach (Bounce bounce in this.active.Values)
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

    private void UpdateActiveBounces(double elapsedSeconds)
    {
        List<long>? completed = null;
        foreach (KeyValuePair<long, Bounce> entry in this.active)
        {
            long id = entry.Key;
            Bounce bounce = entry.Value;
            bounce.ElapsedSeconds += (float)elapsedSeconds;
            if (bounce.ElapsedSeconds >= bounce.DurationSeconds)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset;
                (completed ??= new List<long>()).Add(id);
                continue;
            }

            float compressionProgress = Math.Clamp(bounce.ElapsedSeconds / bounce.CompressionDurationSeconds, 0f, 1f);
            if (bounce.ElapsedSeconds < bounce.CompressionDurationSeconds)
            {
                bounce.Animal.drawOffset = bounce.OriginalDrawOffset
                    + new Vector2(0f, bounce.CompressionHeightPixels * MathF.Sin(compressionProgress * MathF.PI / 2f));
                continue;
            }

            float flightDuration = Math.Max(0.01f, bounce.DurationSeconds - bounce.CompressionDurationSeconds);
            float flightProgress = Math.Clamp((bounce.ElapsedSeconds - bounce.CompressionDurationSeconds) / flightDuration, 0f, 1f);
            float hop = MathF.Sin(flightProgress * MathF.PI);
            bounce.Animal.drawOffset = bounce.OriginalDrawOffset + new Vector2(0f, -hop * bounce.BounceHeightPixels);
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

        var furnitureSpots = house.Objects.Values
            .Where(obj => obj.QualifiedItemId == FurnitureContent.TrampolineQualifiedId
                || obj.QualifiedItemId == FurnitureContent.SlimeCushionQualifiedId)
            .Select(obj => new FurnitureSpot(obj.TileLocation, obj.QualifiedItemId))
            .Where(spot => !this.active.Values.Any(bounce => bounce.TrampolineTile == spot.Tile))
            .ToList();
        if (furnitureSpots.Count == 0)
            return;

        foreach (FarmAnimal animal in house.animals.Values
                     .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                         && !this.active.ContainsKey(animal.myID.Value)
                         && !this.cooldowns.ContainsKey(animal.myID.Value)))
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
            (float duration, float bounceHeight, float compressionHeight) = spot.QualifiedItemId == FurnitureContent.SlimeCushionQualifiedId
                ? (1.8f, 9f, 10f)
                : registry.GetGenomeSize(animal.myID.Value)?.ToLowerInvariant() switch
                {
                    "tiny" => (1.8f, 36f, 2f),
                    "giant" => (3.2f, 21f, 9f),
                    _ => (2.4f, 28f, 4f)
                };
            this.active[animal.myID.Value] = new Bounce(animal, spot.Tile, animal.drawOffset,
                duration, bounceHeight, compressionHeight);
            this.cooldowns[animal.myID.Value] = this.elapsedSeconds + duration + 3f;
        }
    }

    private readonly record struct FurnitureSpot(Vector2 Tile, string QualifiedItemId);

    private sealed class Bounce
    {
        public Bounce(FarmAnimal animal, Vector2 trampolineTile, Vector2 originalDrawOffset,
            float durationSeconds, float bounceHeightPixels, float compressionHeightPixels)
        {
            this.Animal = animal;
            this.TrampolineTile = trampolineTile;
            this.OriginalDrawOffset = originalDrawOffset;
            this.DurationSeconds = durationSeconds;
            this.BounceHeightPixels = bounceHeightPixels;
            this.CompressionHeightPixels = compressionHeightPixels;
            this.CompressionDurationSeconds = durationSeconds * 0.16f;
        }

        public FarmAnimal Animal { get; }
        public Vector2 TrampolineTile { get; }
        public Vector2 OriginalDrawOffset { get; }
        public float DurationSeconds { get; }
        public float BounceHeightPixels { get; }
        public float CompressionHeightPixels { get; }
        public float CompressionDurationSeconds { get; }
        public float ElapsedSeconds { get; set; }
    }
}
