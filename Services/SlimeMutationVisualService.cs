using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace taums_slime_ranching.Services;

/// <summary>Draws small animated mutation accents after the native Domesticated Slime sprite.</summary>
internal sealed class SlimeMutationVisualService
{
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

        foreach (FarmAnimal animal in this.mutatedSlimes)
        {
            if (!house.animals.TryGetValue(animal.myID.Value, out FarmAnimal? current)
                || !ReferenceEquals(current, animal))
                continue;

            string? trait = registry.GetMutationTrait(animal.myID.Value);
            if (!MutagenicPoolRules.IsApprovedMutation(trait))
                continue;

            this.DrawMutation(batch, animal, trait!, registry.GetGenomeSize(animal.myID.Value),
                FarmAnimalContent.GetVisualTint(registry.GetGenomeColor(animal.myID.Value)), now);
        }
    }

    private void RefreshCandidates(AnimalHouse house, SlimeRegistry registry, double now)
    {
        this.cachedHouse = house;
        this.refreshCandidatesAt = now + CandidateRefreshSeconds;
        this.mutatedSlimes.Clear();
        foreach (FarmAnimal animal in house.animals.Values)
        {
            if (animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId
                && MutagenicPoolRules.IsApprovedMutation(registry.GetMutationTrait(animal.myID.Value)))
                this.mutatedSlimes.Add(animal);
        }
        this.mutatedSlimes.Sort((a, b) => a.myID.Value.CompareTo(b.myID.Value));
    }

    private void DrawMutation(SpriteBatch batch, FarmAnimal animal, string trait, string? size, Color slimeTint, double now)
    {
        Rectangle frame = animal.Sprite.SourceRect;
        if (frame.Width <= 0 || frame.Height <= 0)
            return;

        float sizeScale = FarmAnimalContent.GetSizeScale(size);
        float bodyWidth = 16f * sizeScale * SpriteScale;
        float bodyHeight = 24f * sizeScale * SpriteScale;
        Vector2 spriteWorldTop = animal.Position + new Vector2(0, -24f + animal.yJumpOffset);
        Vector2 spriteScreenTop = Utility.snapDrawPosition(Game1.GlobalToLocal(Game1.viewport, spriteWorldTop));
        float bodyX = spriteScreenTop.X + (frame.Width - 16f * sizeScale) * SpriteScale / 2f;
        float bodyY = spriteScreenTop.Y + (frame.Height - 24f * sizeScale) * SpriteScale;
        float centerX = bodyX + bodyWidth / 2f;
        float pixel = SpriteScale * sizeScale;
        float phase = (float)(now + (animal.myID.Value % 1009) * 0.071);

        switch (trait)
        {
            case "Starry":
                this.DrawStarry(batch, centerX, bodyY, bodyWidth, bodyHeight, pixel, phase);
                break;
            case "Spotted":
                this.DrawSpots(batch, bodyX, bodyY, bodyWidth, bodyHeight, pixel);
                break;
            case "Glowing":
                this.DrawGlow(batch, bodyX, bodyY, bodyWidth, bodyHeight, pixel, phase);
                break;
            case "Crystalline":
                this.DrawCrystalline(batch, bodyX, bodyY, bodyWidth, bodyHeight, pixel);
                break;
            case "Mossy":
                this.DrawMossy(batch, centerX, bodyY, pixel);
                break;
            case "Bubbly":
                this.DrawBubbles(batch, centerX, bodyY, bodyWidth, bodyHeight, pixel, phase);
                break;
            case "Mushroom":
                this.DrawMushroom(batch, centerX, bodyY, pixel);
                break;
            case "Blooming":
                this.DrawBlooming(batch, centerX, bodyY, pixel);
                break;
            case "Firefly":
                this.DrawFireflies(batch, centerX, bodyY, bodyWidth, bodyHeight, pixel, phase);
                break;
            case "Crowned":
                this.DrawCrown(batch, centerX, bodyY, pixel);
                break;
            case "Cowboy":
                this.DrawCowboyHat(batch, centerX, bodyY, pixel);
                break;
            case "Smelly":
                this.DrawSmell(batch, centerX, bodyY, bodyWidth, pixel, phase);
                break;
        }
    }

    private void DrawStarry(SpriteBatch batch, float centerX, float top, float width, float height, float pixel, float phase)
    {
        float pulse = 0.55f + 0.45f * MathF.Sin(phase * 3.4f);
        Color gold = WithAlpha(new Color(255, 245, 154), pulse);
        Color lilac = WithAlpha(new Color(221, 198, 255), 1f - pulse * 0.35f);
        DrawStar(batch, centerX - width * 0.22f, top + height * 0.40f, pixel, gold);
        DrawStar(batch, centerX + width * 0.23f, top + height * 0.62f, pixel * 0.78f, lilac);
        DrawStar(batch, centerX + width * 0.05f, top + height * 0.22f, pixel * 0.62f, gold);
    }

    private void DrawSpots(SpriteBatch batch, float x, float y, float width, float height, float pixel)
    {
        Color spot = new(57, 74, 91, 150);
        DrawRect(batch, x + width * 0.24f, y + height * 0.30f, pixel * 2.1f, pixel * 1.5f, spot);
        DrawRect(batch, x + width * 0.61f, y + height * 0.26f, pixel * 1.7f, pixel * 1.7f, spot);
        DrawRect(batch, x + width * 0.43f, y + height * 0.52f, pixel * 2.4f, pixel * 1.7f, spot);
        DrawRect(batch, x + width * 0.72f, y + height * 0.68f, pixel * 1.7f, pixel * 1.4f, spot);
        DrawRect(batch, x + width * 0.20f, y + height * 0.72f, pixel * 1.6f, pixel * 1.5f, spot);
    }

    private void DrawGlow(SpriteBatch batch, float x, float y, float width, float height, float pixel, float phase)
    {
        int time = Game1.timeOfDay;
        float night = time >= 2000 || time < 600 ? 1f : time >= 1700 ? 0.66f : 0.24f;
        float pulse = 0.82f + 0.18f * MathF.Sin(phase * 1.8f);
        byte alpha = (byte)(night * pulse * 74f);
        Color glow = new(136, 245, 255, (int)alpha);
        float pad = pixel * 1.35f;
        DrawRect(batch, x - pad, y + height * 0.22f, pad, height * 0.56f, glow);
        DrawRect(batch, x + width, y + height * 0.22f, pad, height * 0.56f, glow);
        DrawRect(batch, x + width * 0.18f, y - pad, width * 0.64f, pad, glow);
        DrawRect(batch, x + width * 0.18f, y + height, width * 0.64f, pad, glow);
        Color inner = new(176, 252, 255, (int)(alpha * 0.55f));
        DrawRect(batch, x - pixel * 0.45f, y + height * 0.38f, pixel * 0.45f, height * 0.24f, inner);
        DrawRect(batch, x + width, y + height * 0.38f, pixel * 0.45f, height * 0.24f, inner);
    }

    private void DrawCrystalline(SpriteBatch batch, float x, float y, float width, float height, float pixel)
    {
        Color facet = new(212, 251, 255, 188);
        Color blue = new(83, 199, 230, 174);
        DrawDiamond(batch, x + width * 0.25f, y + height * 0.35f, pixel * 1.2f, facet);
        DrawDiamond(batch, x + width * 0.72f, y + height * 0.58f, pixel * 0.9f, blue);
        DrawDiamond(batch, x + width * 0.48f, y + height * 0.76f, pixel * 0.68f, facet);
        DrawRect(batch, x + width * 0.61f, y + height * 0.18f, pixel * 1.3f, pixel * 0.65f, facet);
    }

    private void DrawMossy(SpriteBatch batch, float centerX, float top, float pixel)
    {
        Color stem = new(55, 130, 53);
        Color leaf = new(113, 196, 83);
        Color lightLeaf = new(169, 224, 102);
        DrawRect(batch, centerX - pixel * 0.3f, top - pixel * 2.4f, pixel * 0.6f, pixel * 2.5f, stem);
        DrawRect(batch, centerX - pixel * 1.9f, top - pixel * 2.6f, pixel * 1.7f, pixel * 0.9f, leaf);
        DrawRect(batch, centerX + pixel * 0.2f, top - pixel * 3.4f, pixel * 1.8f, pixel * 0.9f, lightLeaf);
        DrawRect(batch, centerX - pixel * 1.1f, top - pixel * 1.9f, pixel * 0.8f, pixel * 0.65f, lightLeaf);
    }

    private void DrawBubbles(SpriteBatch batch, float centerX, float top, float width, float height, float pixel, float phase)
    {
        for (int index = 0; index < 3; index++)
        {
            float cycle = (phase * 0.33f + index * 0.38f) % 1f;
            float rise = cycle * (height * 0.78f + pixel * 3f);
            float x = centerX + MathF.Sin(phase * 1.6f + index * 2.3f) * width * 0.28f;
            float y = top + height * 0.82f - rise;
            byte alpha = (byte)(105f * (1f - cycle * 0.45f));
            DrawBubble(batch, x, y, pixel * (0.8f + index * 0.12f), new Color(172, 238, 255, (int)alpha));
        }
    }

    private void DrawMushroom(SpriteBatch batch, float centerX, float top, float pixel)
    {
        Color cap = new(220, 82, 91);
        Color underside = new(255, 205, 157);
        Color stem = new(248, 235, 205);
        DrawRect(batch, centerX - pixel * 0.95f, top - pixel * 1.55f, pixel * 1.9f, pixel * 1.2f, stem);
        DrawRect(batch, centerX - pixel * 2.8f, top - pixel * 2.2f, pixel * 5.6f, pixel * 0.8f, underside);
        DrawRect(batch, centerX - pixel * 2.4f, top - pixel * 3.2f, pixel * 4.8f, pixel * 1.3f, cap);
        DrawRect(batch, centerX - pixel * 1.2f, top - pixel * 3.8f, pixel * 2.4f, pixel * 0.8f, cap);
        DrawRect(batch, centerX - pixel * 1.2f, top - pixel * 2.9f, pixel * 0.55f, pixel * 0.55f, Color.White);
        DrawRect(batch, centerX + pixel * 0.75f, top - pixel * 2.9f, pixel * 0.55f, pixel * 0.55f, Color.White);
    }

    private void DrawBlooming(SpriteBatch batch, float centerX, float top, float pixel)
    {
        Color petal = new(255, 161, 207);
        Color center = new(255, 225, 96);
        Color stem = new(64, 153, 72);
        float y = top - pixel * 2.1f;
        DrawRect(batch, centerX - pixel * 0.35f, top - pixel * 0.9f, pixel * 0.7f, pixel * 1.3f, stem);
        DrawRect(batch, centerX - pixel * 0.55f, y - pixel * 1.5f, pixel * 1.1f, pixel * 1.1f, petal);
        DrawRect(batch, centerX - pixel * 1.75f, y - pixel * 0.55f, pixel * 1.1f, pixel * 1.1f, petal);
        DrawRect(batch, centerX + pixel * 0.65f, y - pixel * 0.55f, pixel * 1.1f, pixel * 1.1f, petal);
        DrawRect(batch, centerX - pixel * 0.55f, y + pixel * 0.55f, pixel * 1.1f, pixel * 1.1f, petal);
        DrawRect(batch, centerX - pixel * 0.5f, y - pixel * 0.5f, pixel, pixel, center);
    }

    private void DrawFireflies(SpriteBatch batch, float centerX, float top, float width, float height, float pixel, float phase)
    {
        for (int index = 0; index < 2; index++)
        {
            float angle = phase * 1.4f + index * MathF.PI;
            float x = centerX + MathF.Cos(angle) * width * 0.58f;
            float y = top + height * 0.52f + MathF.Sin(angle) * height * 0.56f;
            float pulse = 0.7f + 0.3f * MathF.Sin(phase * 3f + index);
            byte alpha = (byte)(210f * pulse);
            Color halo = new(255, 225, 115, (int)(alpha * 0.36f));
            Color core = new(255, 247, 184, (int)alpha);
            DrawRect(batch, x - pixel * 1.7f, y - pixel * 1.7f, pixel * 3.4f, pixel * 3.4f, halo);
            DrawRect(batch, x - pixel * 0.48f, y - pixel * 0.48f, pixel * 0.96f, pixel * 0.96f, core);
        }
    }

    private void DrawCrown(SpriteBatch batch, float centerX, float top, float pixel)
    {
        Color gold = new(255, 203, 70);
        Color light = new(255, 239, 142);
        float y = top - pixel * 1.1f;
        DrawRect(batch, centerX - pixel * 2.8f, y, pixel * 5.6f, pixel * 1.1f, gold);
        DrawRect(batch, centerX - pixel * 2.2f, y - pixel * 1.1f, pixel * 1.1f, pixel * 1.2f, light);
        DrawRect(batch, centerX - pixel * 0.55f, y - pixel * 1.7f, pixel * 1.1f, pixel * 1.8f, light);
        DrawRect(batch, centerX + pixel * 1.1f, y - pixel * 1.1f, pixel * 1.1f, pixel * 1.2f, light);
        DrawRect(batch, centerX - pixel * 2.2f, y + pixel * 1.25f, pixel * 4.4f, pixel * 0.55f, new Color(225, 153, 43));
    }

    private void DrawCowboyHat(SpriteBatch batch, float centerX, float top, float pixel)
    {
        Color leather = new(130, 83, 47);
        Color highlight = new(185, 125, 67);
        float y = top - pixel * 1.4f;
        DrawRect(batch, centerX - pixel * 3.2f, y + pixel * 1.25f, pixel * 6.4f, pixel * 0.85f, leather);
        DrawRect(batch, centerX - pixel * 2.2f, y + pixel * 0.45f, pixel * 4.4f, pixel * 0.9f, highlight);
        DrawRect(batch, centerX - pixel * 1.55f, y - pixel * 0.75f, pixel * 3.1f, pixel * 1.3f, leather);
        DrawRect(batch, centerX - pixel * 0.45f, y - pixel * 1.35f, pixel * 0.9f, pixel * 0.6f, highlight);
        DrawRect(batch, centerX - pixel * 1.5f, y + pixel * 0.75f, pixel * 3f, pixel * 0.4f, new Color(211, 179, 99));
    }

    private void DrawSmell(SpriteBatch batch, float centerX, float top, float width, float pixel, float phase)
    {
        Color odor = new(151, 215, 108, 165);
        for (int lane = -1; lane <= 1; lane++)
        for (int segment = 0; segment < 5; segment++)
        {
            float rise = segment * pixel * 1.35f;
            float sway = MathF.Sin(phase * 1.7f + segment * 0.85f + lane) * pixel * 1.2f;
            float x = centerX + lane * width * 0.18f + sway;
            float y = top - pixel * 1.2f - rise;
            DrawRect(batch, x, y, pixel * 0.7f, pixel * 0.9f, WithAlpha(odor, 1f - segment * 0.1f));
        }
    }

    private static void DrawStar(SpriteBatch batch, float x, float y, float pixel, Color color)
    {
        DrawRect(batch, x - pixel * 0.3f, y - pixel * 1.4f, pixel * 0.6f, pixel * 2.8f, color);
        DrawRect(batch, x - pixel * 1.4f, y - pixel * 0.3f, pixel * 2.8f, pixel * 0.6f, color);
        DrawRect(batch, x - pixel * 0.6f, y - pixel * 0.6f, pixel * 1.2f, pixel * 1.2f, color);
    }

    private static void DrawDiamond(SpriteBatch batch, float x, float y, float pixel, Color color)
    {
        DrawRect(batch, x - pixel * 0.5f, y - pixel * 2f, pixel, pixel, color);
        DrawRect(batch, x - pixel, y - pixel, pixel * 2f, pixel, color);
        DrawRect(batch, x - pixel * 1.5f, y, pixel * 3f, pixel, color);
        DrawRect(batch, x - pixel, y + pixel, pixel * 2f, pixel, color);
        DrawRect(batch, x - pixel * 0.5f, y + pixel * 2f, pixel, pixel, color);
    }

    private static void DrawBubble(SpriteBatch batch, float centerX, float centerY, float pixel, Color color)
    {
        DrawRect(batch, centerX - pixel, centerY - pixel * 2f, pixel * 2f, pixel, color);
        DrawRect(batch, centerX - pixel * 1.5f, centerY - pixel, pixel, pixel * 2f, color);
        DrawRect(batch, centerX + pixel * 0.5f, centerY - pixel, pixel, pixel * 2f, color);
        DrawRect(batch, centerX - pixel, centerY + pixel, pixel * 2f, pixel, color);
    }

    private static void DrawRect(SpriteBatch batch, float x, float y, float width, float height, Color color)
    {
        int left = (int)MathF.Round(x);
        int top = (int)MathF.Round(y);
        int right = (int)MathF.Round(x + width);
        int bottom = (int)MathF.Round(y + height);
        if (right <= left || bottom <= top)
            return;
        batch.Draw(Game1.staminaRect, new Rectangle(left, top, right - left, bottom - top), color);
    }

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, (byte)Math.Clamp((int)(color.A * Math.Clamp(alpha, 0f, 1f)), 0, 255));
}
