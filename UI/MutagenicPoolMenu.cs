using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Assign one mature Domesticated Slime to a two-day safe-failure mutation attempt.</summary>
internal sealed class MutagenicPoolMenu : IClickableMenu
{
    private const int SlimeItemId = 766;
    private static Texture2D? slimePortraitTexture;
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly MutagenicPoolRules rules;
    private readonly Func<string, bool> contributeSlime;
    private readonly List<FarmAnimal> candidates;
    private readonly Rectangle actionButton;
    private readonly Rectangle cancelButton;
    private readonly Rectangle mutationsButton;
    private readonly Rectangle selectedSlimePanel;
    private readonly Rectangle previousSlimeButton;
    private readonly Rectangle nextSlimeButton;
    private readonly Rectangle closeButton;
    private long? selectedSlimeId;
    private string? statusMessage;
    private bool showMutations;

    public MutagenicPoolMenu(SlimeRegistry registry, string machineKey, MutagenicPoolRules rules,
        Func<string, bool> contributeSlime)
        : base(Game1.uiViewport.Width / 2 - 500, Game1.uiViewport.Height / 2 - 310, 1000, 620)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.rules = rules;
        this.contributeSlime = contributeSlime;
        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        StardewValley.AnimalHouse? interior = daycare?.GetIndoors() as StardewValley.AnimalHouse;
        this.candidates = (interior?.animals.Values.AsEnumerable() ?? Enumerable.Empty<FarmAnimal>())
            .Where(animal => animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId)
            .OrderBy(animal => animal.myID.Value)
            .ToList();
        this.selectedSlimePanel = new Rectangle(this.xPositionOnScreen + 56, this.yPositionOnScreen + 166, 270, 76);
        this.previousSlimeButton = new Rectangle(this.xPositionOnScreen + 28, this.yPositionOnScreen + 176, 38, 52);
        this.nextSlimeButton = new Rectangle(this.xPositionOnScreen + 326, this.yPositionOnScreen + 176, 38, 52);
        this.actionButton = new Rectangle(this.xPositionOnScreen + 70, this.yPositionOnScreen + 550, 245, 52);
        this.cancelButton = new Rectangle(this.xPositionOnScreen + 292, this.yPositionOnScreen + 476, 72, 30);
        this.mutationsButton = new Rectangle(this.xPositionOnScreen + 438, this.yPositionOnScreen + 550, 350, 52);
        this.closeButton = new Rectangle(this.xPositionOnScreen + this.width - 52, this.yPositionOnScreen + 16, 36, 34);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.closeButton.Contains(x, y) && !this.showMutations)
        {
            Game1.activeClickableMenu = null;
            Game1.playSound("bigDeSelect");
            return;
        }
        if (this.showMutations)
        {
            this.showMutations = false;
            Game1.playSound("bigDeSelect");
            return;
        }
        if (this.mutationsButton.Contains(x, y))
        {
            this.showMutations = true;
            Game1.playSound("smallSelect");
            return;
        }
        if (this.cancelButton.Contains(x, y) && this.registry.GetMutagenicAttempt(this.machineKey) is not null)
        {
            if (this.registry.CancelMutagenicAttempt(this.machineKey))
                this.statusMessage = "Attempt cancelled. The slime and all chance percentages were preserved.";
            Game1.playSound("bigDeSelect");
            return;
        }
        if (this.previousSlimeButton.Contains(x, y))
        {
            this.SelectAdjacentSlime(-1);
            return;
        }
        if (this.nextSlimeButton.Contains(x, y))
        {
            this.SelectAdjacentSlime(1);
            return;
        }
        if (this.actionButton.Contains(x, y))
        {
            if (this.registry.GetMutagenicAttempt(this.machineKey) is null)
                this.StartAttempt();
            else
                this.ContributeToAttempt();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.selectedSlimePanel.Contains(x, y) && this.registry.GetMutagenicAttempt(this.machineKey) is null)
        {
            this.SelectAdjacentSlime(1);
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        DrawMachineFrame(b, new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height));
        MutagenicAttemptRecord? active = this.registry.GetMutagenicAttempt(this.machineKey);
        DrawInset(b, new Rectangle(this.xPositionOnScreen + 20, this.yPositionOnScreen + 12, this.width - 40, 54), new Color(154, 93, 51));
        Utility.drawTextWithShadow(b, "MUTAGENIC POOL", Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + 42, this.yPositionOnScreen + 20), Color.White);
        DrawButton(b, this.closeButton, "X", new Color(205, 62, 57));

        int left = this.xPositionOnScreen + 20;
        int top = this.yPositionOnScreen + 78;
        DrawInset(b, new Rectangle(left, top, 360, 462), new Color(238, 224, 190));
        b.DrawString(Game1.smallFont, "1. Select Adult Slime", new Vector2(left + 24, top + 20), Color.White);
        DrawInset(b, this.selectedSlimePanel, new Color(224, 196, 147));
        DrawButton(b, this.previousSlimeButton, "‹", new Color(198, 145, 87));
        DrawButton(b, this.nextSlimeButton, "›", new Color(198, 145, 87));
        FarmAnimal? selected = this.GetSelectedSlime()
            ?? (active is null ? null : this.candidates.FirstOrDefault(animal => animal.myID.Value == active.SlimeId));
        if (selected is null)
        {
            b.DrawString(Game1.smallFont, this.candidates.Count == 0 ? "No Domesticated Slimes" : "Select a slime",
                new Vector2(this.selectedSlimePanel.X + 22, this.selectedSlimePanel.Y + 24), Color.White);
        }
        else
        {
            b.DrawString(Game1.smallFont, selected.Name, new Vector2(this.selectedSlimePanel.X + 14, this.selectedSlimePanel.Y + 10), Color.White);
            int age = this.registry.GetMaturityAgeDays(selected.myID.Value, selected.age.Value);
            b.DrawString(Game1.tinyFont, $"Adult · age {age} days", new Vector2(this.selectedSlimePanel.X + 14, this.selectedSlimePanel.Y + 42), Color.White);
            Rectangle slimeBounds = new(this.selectedSlimePanel.Right - 58, this.selectedSlimePanel.Y + 5, 48, 64);
            b.Draw(selected.Sprite.Texture, slimeBounds, selected.Sprite.SourceRect, Color.White);
        }
        b.DrawString(Game1.tinyFont, selected is null ? "Use the arrows to choose a resident" : "‹ / › to change selected slime",
            new Vector2(left + 32, top + 258), Color.White);

        b.DrawString(Game1.smallFont, "2. Add Slime", new Vector2(left + 24, top + 302), Color.White);
        DrawInset(b, new Rectangle(left + 22, top + 330, 316, 60), new Color(224, 196, 147));
        DrawSlimeIcon(b, new Rectangle(left + 32, top + 336, 48, 48));
        b.DrawString(Game1.smallFont, $"{CountSlimeItems()} vanilla Slime", new Vector2(left + 92, top + 340), Color.White);
        b.DrawString(Game1.tinyFont, "10 Slime adds +1% during an attempt", new Vector2(left + 92, top + 366), Color.White);

        b.DrawString(Game1.smallFont, "3. Mutation Attempt", new Vector2(left + 24, top + 402), Color.White);
        if (active is not null)
            DrawButton(b, this.cancelButton, "CANCEL", new Color(159, 74, 69));
        if (active is null)
        {
            b.DrawString(Game1.tinyFont, $"Cycle: {MutagenicPoolRules.DurationDays} in-game days",
                new Vector2(left + 24, top + 418), Color.White);
            DrawBar(b, new Rectangle(left + 24, top + 442, 306, 20), 0f);
        }
        else
        {
            int duration = Math.Max(1, active.ReadyOnDay - active.StartedOnDay);
            float progress = Math.Clamp((Game1.Date.TotalDays - active.StartedOnDay) / (float)duration, 0f, 1f);
            b.DrawString(Game1.tinyFont, $"Attempt in progress · ready day {active.ReadyOnDay}", new Vector2(left + 24, top + 418), Color.White);
            DrawBar(b, new Rectangle(left + 24, top + 442, 306, 20), progress);
        }
        string actionLabel = active is null ? "START · 2 DAYS"
            : this.GetCurrentChance(selected, active) >= MutagenicPoolRules.MaximumChance ? "MAXIMUM CHANCE"
            : CountSlimeItems() >= MutagenicPoolRules.SlimePerContributionPoint ? "ADD 10 SLIME"
            : "NEED 10 SLIME";
        DrawButton(b, this.actionButton, actionLabel,
            active is null ? new Color(40, 119, 207)
            : actionLabel == "ADD 10 SLIME" ? new Color(94, 120, 192) : new Color(68, 76, 119));

        int center = this.xPositionOnScreen + 394;
        DrawInset(b, new Rectangle(center, top, 390, 462), new Color(238, 224, 190));
        DrawInset(b, new Rectangle(center + 8, top + 8, 374, 46), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "Possible Mutation Examples", new Vector2(center + 82, top + 21), Color.White);
        string[] mutationNames = MutagenicPoolRules.MutationTraits.ToArray();
        Color[] mutationColors = { new(148, 116, 214), new(224, 126, 162), new(148, 199, 92), new(119, 211, 227),
            new(92, 142, 66), new(90, 169, 221), new(168, 105, 74), new(236, 137, 168),
            new(242, 218, 108), new(233, 196, 83), new(153, 129, 91), new(141, 192, 109) };
        for (int i = 0; i < mutationNames.Length; i++)
        {
            int column = i % 4;
            int row = i / 4;
            Rectangle tile = new(center + 18 + column * 88, top + 78 + row * 106, 78, 94);
            DrawInset(b, tile, new Color(249, 231, 201));
            DrawSlimePortrait(b, new Rectangle(tile.X + 12, tile.Y + 4, 54, 58), mutationColors[i]);
            b.DrawString(Game1.tinyFont, mutationNames[i], new Vector2(tile.X + 5, tile.Y + 65), Color.White);
        }
        b.DrawString(Game1.tinyFont, "Approved cosmetic mutation outcomes.", new Vector2(center + 18, top + 404), Color.White);
        DrawButton(b, this.mutationsButton, "POSSIBLE MUTATIONS", new Color(31, 94, 178));

        int right = this.xPositionOnScreen + 804;
        DrawInset(b, new Rectangle(right, top, 176, 462), new Color(238, 224, 190));
        DrawInset(b, new Rectangle(right + 6, top + 6, 164, 46), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "PITY SYSTEM", new Vector2(right + 34, top + 20), Color.White);
        b.DrawString(Game1.tinyFont, "Each slime has", new Vector2(right + 14, top + 72), Color.White);
        b.DrawString(Game1.tinyFont, "its own counter.", new Vector2(right + 14, top + 92), Color.White);
        int failures = selected is null ? 0 : this.registry.GetMutagenicPoolFailures(selected.myID.Value);
        double contribution = selected is null ? 0 : this.registry.GetMutagenicPoolContribution(selected.myID.Value);
        double pityChance = this.rules.GetPityChance(failures);
        double contributionChance = this.rules.GetContributionChance(failures, contribution);
        double chance = active?.ChanceAtStart ?? this.rules.GetChance(failures, contribution);
        b.DrawString(Game1.smallFont, $"Chance {chance:P0}", new Vector2(right + 14, top + 132), Color.White);
        DrawChanceBar(b, new Rectangle(right + 14, top + 162, 148, 22), MutagenicPoolRules.BaseChance, pityChance, contributionChance);
        b.DrawString(Game1.tinyFont, $"Base {MutagenicPoolRules.BaseChance:P0}", new Vector2(right + 14, top + 196), new Color(112, 218, 255));
        b.DrawString(Game1.tinyFont, $"Pity +{pityChance:P0}", new Vector2(right + 14, top + 216), new Color(255, 201, 112));
        b.DrawString(Game1.tinyFont, $"You +{contributionChance:P0}", new Vector2(right + 14, top + 236), new Color(201, 153, 255));
        b.DrawString(Game1.tinyFont, $"Previous misses: {failures}", new Vector2(right + 14, top + 270), Color.White);
        b.DrawString(Game1.tinyFont, "Failure never harms", new Vector2(right + 14, top + 302), Color.White);
        b.DrawString(Game1.tinyFont, "the slime. Reaching", new Vector2(right + 14, top + 322), Color.White);
        b.DrawString(Game1.tinyFont, "100% guarantees a", new Vector2(right + 14, top + 342), Color.White);
        b.DrawString(Game1.tinyFont, "cosmetic mutation.", new Vector2(right + 14, top + 362), Color.White);
        if (active is not null)
        {
            b.DrawString(Game1.tinyFont, "Chance locked:", new Vector2(right + 14, top + 390), new Color(208, 219, 255));
            b.DrawString(Game1.tinyFont, $"{active.ChanceAtStart:P0} this attempt", new Vector2(right + 14, top + 410), new Color(208, 219, 255));
        }

        if (!string.IsNullOrEmpty(this.statusMessage))
            b.DrawString(Game1.tinyFont, this.statusMessage.Length > 72 ? this.statusMessage[..69] + "..." : this.statusMessage,
                new Vector2(this.xPositionOnScreen + 500, this.yPositionOnScreen + 34), Color.White);
        b.DrawString(Game1.tinyFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 26), Color.White);
        if (this.showMutations)
            this.DrawMutationsModal(b);
        this.drawMouse(b);
    }

    private void DrawMutationsModal(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect, new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height), new Color(0, 0, 0, 160));
        int width = 570;
        int height = 330;
        int x = this.xPositionOnScreen + (this.width - width) / 2;
        int y = this.yPositionOnScreen + (this.height - height) / 2;
        DrawMachineFrame(b, new Rectangle(x, y, width, height));
        b.DrawString(Game1.dialogueFont, "POSSIBLE MUTATIONS", new Vector2(x + 28, y + 22), Color.White);
        b.DrawString(Game1.smallFont, "APPROVED COSMETIC OUTCOMES", new Vector2(x + 32, y + 86), new Color(92, 220, 255));
        b.DrawString(Game1.smallFont, "One trait per slime; visual effects are cosmetic only.", new Vector2(x + 32, y + 122), Color.White);
        b.DrawString(Game1.tinyFont, "Starry, Spotted, Glowing, Crystalline, Mossy, Bubbly", new Vector2(x + 32, y + 174), Color.White);
        b.DrawString(Game1.tinyFont, "Mushroom, Blooming, Firefly, Crowned, Cowboy, Smelly", new Vector2(x + 32, y + 196), Color.White);
        b.DrawString(Game1.smallFont, "Failure is safe; pity rises by 1%.", new Vector2(x + 32, y + 248), Color.White);
        b.DrawString(Game1.smallFont, "Player contribution carries forward, with a 1% reduction only above 2%.", new Vector2(x + 32, y + 278), Color.White);
        b.DrawString(Game1.tinyFont, "Click anywhere to close", new Vector2(x + width - 174, y + height - 24), Color.White);
    }

    private FarmAnimal? GetSelectedSlime() => this.selectedSlimeId is long id
        ? this.candidates.FirstOrDefault(animal => animal.myID.Value == id)
        : null;

    private void SelectAdjacentSlime(int direction)
    {
        if (this.registry.GetMutagenicAttempt(this.machineKey) is not null)
            return;

        FarmAnimal[] eligible = this.candidates.Where(animal =>
        {
            int age = this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value);
            return age >= FarmAnimalContent.DomesticatedSlimeDaysToMature
                && !this.registry.IsReserved(animal.myID.Value, this.machineKey)
                && string.IsNullOrWhiteSpace(this.registry.GetMutationTrait(animal.myID.Value));
        }).ToArray();
        if (eligible.Length == 0)
        {
            this.statusMessage = "No eligible mature Domesticated Slime is available.";
            Game1.playSound("cancel");
            return;
        }

        int current = Array.FindIndex(eligible, animal => animal.myID.Value == this.selectedSlimeId);
        int next = current < 0 ? (direction > 0 ? 0 : eligible.Length - 1)
            : (current + direction + eligible.Length) % eligible.Length;
        FarmAnimal selected = eligible[next];
        this.selectedSlimeId = selected.myID.Value;
        this.statusMessage = $"Selected {selected.Name}.";
        Game1.playSound("smallSelect");
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, Color fill)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(17, 22, 49));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), new Color(112, 111, 171));
        Color interior = fill.R > 180 && fill.G < 100 ? fill
            : fill.R >= 220 ? new Color(47, 55, 98)
            : fill.R >= 145 ? new Color(11, 83, 184)
            : fill.R >= 100 ? new Color(45, 72, 129) : fill;
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10), interior);
    }

    private static void DrawMachineFrame(SpriteBatch b, Rectangle bounds)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(11, 15, 34));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8), new Color(91, 86, 151));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, bounds.Height - 16), new Color(35, 41, 79));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 12, bounds.Y + bounds.Height - 20, bounds.Width - 24, 5), new Color(116, 114, 173));
        for (int i = 0; i < 8; i++)
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 24 + i * ((bounds.Width - 56) / 7), bounds.Y + bounds.Height - 13, 28, 5), new Color(73, 79, 126));
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string label, Color fill)
    {
        DrawInset(b, bounds, fill);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label,
            new Vector2(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2), Color.White);
    }

    private static void DrawSlimeIcon(SpriteBatch b, Rectangle bounds)
    {
        Rectangle source = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, SlimeItemId, 16, 16);
        b.Draw(Game1.objectSpriteSheet, bounds, source, Color.White);
    }

    private static void DrawSlimePortrait(SpriteBatch b, Rectangle bounds, Color tint)
    {
        slimePortraitTexture ??= Game1.content.Load<Texture2D>("Characters/Monsters/Green Slime");
        b.Draw(slimePortraitTexture, bounds, new Rectangle(32, 120, 16, 24), tint);
    }

    private static void DrawBar(SpriteBatch batch, Rectangle bounds, float value)
    {
        batch.Draw(Game1.staminaRect, bounds, new Color(17, 26, 57));
        int width = (int)((bounds.Width - 4) * Math.Clamp(value, 0f, 1f));
        if (width > 0)
            batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), new Color(35, 198, 242));
    }

    private static void DrawChanceBar(SpriteBatch batch, Rectangle bounds, double baseChance, double pityChance, double contributionChance)
    {
        batch.Draw(Game1.staminaRect, bounds, new Color(17, 26, 57));
        int totalWidth = bounds.Width - 4;
        int baseWidth = (int)Math.Round(totalWidth * Math.Clamp(baseChance, 0, 1));
        int pityWidth = Math.Clamp((int)Math.Round(totalWidth * Math.Clamp(pityChance, 0, 1)), 0, totalWidth - baseWidth);
        int contributionWidth = Math.Clamp((int)Math.Round(totalWidth * Math.Clamp(contributionChance, 0, 1)),
            0, totalWidth - baseWidth - pityWidth);
        int x = bounds.X + 2;
        int y = bounds.Y + 2;
        int height = bounds.Height - 4;
        if (baseWidth > 0)
            batch.Draw(Game1.staminaRect, new Rectangle(x, y, baseWidth, height), new Color(84, 194, 239));
        x += baseWidth;
        if (pityWidth > 0)
            batch.Draw(Game1.staminaRect, new Rectangle(x, y, pityWidth, height), new Color(247, 179, 91));
        x += pityWidth;
        if (contributionWidth > 0)
            batch.Draw(Game1.staminaRect, new Rectangle(x, y, contributionWidth, height), new Color(186, 131, 238));
    }

    private void StartAttempt()
    {
        if (this.registry.GetMutagenicAttempt(this.machineKey) is not null)
        {
            this.statusMessage = "This pool is already processing an attempt.";
            return;
        }
        if (this.selectedSlimeId is not long slimeId)
        {
            this.statusMessage = "Select an eligible mature Domesticated Slime first.";
            return;
        }
        FarmAnimal? animal = this.candidates.FirstOrDefault(candidate => candidate.myID.Value == slimeId);
        int age = animal is null ? 0 : this.registry.GetMaturityAgeDays(slimeId, animal.age.Value);
        if (!this.registry.StartMutagenicAttempt(this.machineKey, slimeId, Game1.Date.TotalDays, age, Game1.random.NextDouble(), this.rules))
        {
            this.statusMessage = "The selected slime is no longer eligible.";
            return;
        }

        this.statusMessage = "The two-day attempt started at the displayed chance. Add Slime during processing to raise it.";
    }

    private void ContributeToAttempt()
    {
        MutagenicAttemptRecord? active = this.registry.GetMutagenicAttempt(this.machineKey);
        if (active is null || !this.candidates.Any(animal => animal.myID.Value == active.SlimeId))
        {
            this.statusMessage = "The slime assigned to this Pool is no longer available here.";
            return;
        }
        if (active.ChanceAtStart >= MutagenicPoolRules.MaximumChance)
        {
            this.statusMessage = "This attempt is already guaranteed at 100%.";
            return;
        }
        if (CountSlimeItems() < MutagenicPoolRules.SlimePerContributionPoint)
        {
            this.statusMessage = "Bring 10 vanilla Slime to add 1 percentage point during an attempt.";
            return;
        }

        if (!this.contributeSlime(this.machineKey))
        {
            this.statusMessage = "The contribution could not be saved; your Slime and prior chance were restored.";
            return;
        }

        double chance = this.registry.GetMutagenicAttempt(this.machineKey)?.ChanceAtStart ?? active.ChanceAtStart;
        this.statusMessage = $"Added 10 Slime. This attempt is now at {chance:P0}.";
    }

    private double GetCurrentChance(FarmAnimal? selected, MutagenicAttemptRecord? active) => active?.ChanceAtStart
        ?? (selected is null ? this.rules.GetChance(0)
            : this.rules.GetChance(this.registry.GetMutagenicPoolFailures(selected.myID.Value),
                this.registry.GetMutagenicPoolContribution(selected.myID.Value)));

    private static int CountSlimeItems() => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == "(O)" + SlimeItemId)
        .Sum(item => item!.Stack);

}
