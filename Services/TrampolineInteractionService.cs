using Microsoft.Xna.Framework;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Runs short, local-only trampoline bounce animations for nearby Ranch Slimes.</summary>
internal sealed class TrampolineInteractionService
{
    private const int AssignmentIntervalTicks = 30;
    private const float InteractionRadiusPixels = 128f;
    private readonly Dictionary<long, Bounce> active = new();
    private int assignmentTicks;
    private AnimalHouse? activeHouse;

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

        this.UpdateActiveBounces(time.ElapsedGameTime.TotalSeconds);
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
        this.activeHouse = null;
        this.assignmentTicks = 0;
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
        var trampolineTiles = house.Objects.Values
            .Where(obj => obj.QualifiedItemId == FurnitureContent.TrampolineQualifiedId)
            .Select(obj => obj.TileLocation)
            .Where(tile => !this.active.Values.Any(bounce => bounce.TrampolineTile == tile))
            .ToList();
        if (trampolineTiles.Count == 0)
            return;

        foreach (FarmAnimal animal in house.animals.Values
                     .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                         && !this.active.ContainsKey(animal.myID.Value)))
        {
            Vector2 animalPosition = animal.Position;
            int nearestIndex = -1;
            float nearestDistance = InteractionRadiusPixels * InteractionRadiusPixels;
            for (int i = 0; i < trampolineTiles.Count; i++)
            {
                Vector2 center = trampolineTiles[i] * 64f + new Vector2(32f, 32f);
                float distance = Vector2.DistanceSquared(animalPosition, center);
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearestIndex = i;
            }

            if (nearestIndex < 0)
                continue;

            Vector2 trampolineTile = trampolineTiles[nearestIndex];
            trampolineTiles.RemoveAt(nearestIndex);
            (float duration, float bounceHeight, float compressionHeight) = registry.GetGenomeSize(animal.myID.Value)?.ToLowerInvariant() switch
            {
                "tiny" => (1.8f, 36f, 2f),
                "giant" => (3.2f, 21f, 9f),
                _ => (2.4f, 28f, 4f)
            };
            this.active[animal.myID.Value] = new Bounce(animal, trampolineTile, animal.drawOffset,
                duration, bounceHeight, compressionHeight);
        }
    }

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
