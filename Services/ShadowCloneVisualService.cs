using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Draws bounded, transient echoes for mutated slimes without creating game animals.</summary>
internal static class ShadowCloneVisualService
{
    private const double CycleSeconds = 12;
    private const double EchoSeconds = 2.4;
    private const int MaximumVisibleEchoes = 4;
    private const float SpriteScale = 4f;

    public static void Draw(GameLocation location, SpriteBatch batch, SlimeRegistry registry)
    {
        if (location is not AnimalHouse house
            || !Game1.getFarm().buildings.Any(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && ReferenceEquals(building.GetIndoors(), house)))
            return;

        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        var visible = new List<(FarmAnimal Animal, double Progress)>();
        foreach (FarmAnimal animal in house.animals.Values
                     .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                         && HasShadowCloneTrait(registry, animal.myID.Value))
                     .OrderBy(animal => animal.myID.Value))
        {
            double phase = ((now + animal.myID.Value % CycleSeconds) % CycleSeconds + CycleSeconds) % CycleSeconds;
            if (phase < EchoSeconds)
                visible.Add((animal, phase / EchoSeconds));
        }

        foreach ((FarmAnimal animal, double progress) in visible.Take(MaximumVisibleEchoes))
        {
            Rectangle source = animal.Sprite.SourceRect;
            if (source.Width <= 0 || source.Height <= 0)
                continue;

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
    }

    private static bool HasShadowCloneTrait(SlimeRegistry registry, long slimeId) =>
        string.Equals(registry.GetMutationTrait(slimeId), "Shadow Clone", StringComparison.Ordinal);
}
