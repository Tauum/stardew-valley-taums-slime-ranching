using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Draws bounded, transient echoes for mutated slimes without creating game animals.</summary>
internal sealed class ShadowCloneVisualService
{
    private const double CycleSeconds = 12;
    private const double EchoSeconds = 2.4;
    private const int MaximumVisibleEchoes = 4;
    private const float SpriteScale = 4f;
    private const double CandidateRefreshSeconds = 0.5;
    private readonly List<FarmAnimal> mutatedSlimes = new();
    private AnimalHouse? cachedHouse;
    private double refreshCandidatesAt;

    public void Clear()
    {
        this.cachedHouse = null;
        this.mutatedSlimes.Clear();
        this.refreshCandidatesAt = 0;
    }

    public void Draw(GameLocation location, SpriteBatch batch, SlimeRegistry registry)
    {
        if (location is not AnimalHouse house
            || !Game1.getFarm().buildings.Any(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && ReferenceEquals(building.GetIndoors(), house)))
        {
            this.Clear();
            return;
        }

        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (!ReferenceEquals(this.cachedHouse, house) || now >= this.refreshCandidatesAt)
            this.RefreshCandidates(house, registry, now);
        if (this.mutatedSlimes.Count == 0)
            return;

        int startIndex = (int)(now / EchoSeconds) % this.mutatedSlimes.Count;
        int drawn = 0;
        for (int step = 0; step < this.mutatedSlimes.Count && drawn < MaximumVisibleEchoes; step++)
        {
            FarmAnimal animal = this.mutatedSlimes[(startIndex + step) % this.mutatedSlimes.Count];
            if (!house.animals.TryGetValue(animal.myID.Value, out FarmAnimal? current) || !ReferenceEquals(current, animal))
                continue;

            double phase = ((now + animal.myID.Value % CycleSeconds) % CycleSeconds + CycleSeconds) % CycleSeconds;
            if (phase >= EchoSeconds)
                continue;

            DrawEcho(batch, animal, phase / EchoSeconds);
            drawn++;
        }
    }

    private void RefreshCandidates(AnimalHouse house, SlimeRegistry registry, double now)
    {
        this.cachedHouse = house;
        this.refreshCandidatesAt = now + CandidateRefreshSeconds;
        this.mutatedSlimes.Clear();
        foreach (FarmAnimal animal in house.animals.Values)
        {
            if (animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                && HasShadowCloneTrait(registry, animal.myID.Value))
                this.mutatedSlimes.Add(animal);
        }
        this.mutatedSlimes.Sort((a, b) => a.myID.Value.CompareTo(b.myID.Value));
    }

    private static void DrawEcho(SpriteBatch batch, FarmAnimal animal, double progress)
    {
        Rectangle source = animal.Sprite.SourceRect;
        if (source.Width <= 0 || source.Height <= 0)
            return;

        float fade = (float)Math.Sin(progress * Math.PI);
        Rectangle bounds = animal.GetBoundingBox();
        int direction = animal.myID.Value % 2 == 0 ? -1 : 1;
        float drift = direction * (18f + (float)progress * 14f);
        float rise = (float)Math.Sin(progress * Math.PI) * 12f;
        Vector2 worldPosition = new(
            bounds.Center.X + drift - source.Width * SpriteScale / 2f,
            bounds.Bottom - source.Height * SpriteScale - rise);
        Vector2 screenPosition = Game1.GlobalToLocal(Game1.viewport, worldPosition);
        Color tint = new(174, 149, 205, 150);
        batch.Draw(animal.Sprite.Texture, screenPosition, source, tint * (fade * 0.58f),
            0f, Vector2.Zero, SpriteScale, SpriteEffects.None, 0.9999f);
    }

    private static bool HasShadowCloneTrait(SlimeRegistry registry, long slimeId) =>
        string.Equals(registry.GetMutationTrait(slimeId), "Shadow Clone", StringComparison.Ordinal);
}
