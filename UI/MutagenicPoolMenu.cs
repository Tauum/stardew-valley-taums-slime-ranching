using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Assign one mature Ranch Slime to a one-day safe-failure mutation attempt.</summary>
internal sealed class MutagenicPoolMenu : IClickableMenu
{
    private const int SlimeItemId = 766;
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly MutagenicPoolRules rules;
    private readonly List<FarmAnimal> candidates;
    private readonly Rectangle actionButton;
    private readonly Rectangle mutationsButton;
    private readonly Rectangle selectedSlimePanel;
    private readonly Rectangle previousSlimeButton;
    private readonly Rectangle nextSlimeButton;
    private long? selectedSlimeId;
    private string? statusMessage;
    private bool showMutations;

    public MutagenicPoolMenu(SlimeRegistry registry, string machineKey, MutagenicPoolRules rules)
        : base(Game1.uiViewport.Width / 2 - 500, Game1.uiViewport.Height / 2 - 310, 1000, 620)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.rules = rules;
        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        StardewValley.AnimalHouse? interior = daycare?.GetIndoors() as StardewValley.AnimalHouse;
        this.candidates = (interior?.animals.Values.AsEnumerable() ?? Enumerable.Empty<FarmAnimal>())
            .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
            .OrderBy(animal => animal.myID.Value)
            .ToList();
        this.selectedSlimePanel = new Rectangle(this.xPositionOnScreen + 56, this.yPositionOnScreen + 166, 270, 76);
        this.previousSlimeButton = new Rectangle(this.xPositionOnScreen + 28, this.yPositionOnScreen + 176, 38, 52);
        this.nextSlimeButton = new Rectangle(this.xPositionOnScreen + 326, this.yPositionOnScreen + 176, 38, 52);
        this.actionButton = new Rectangle(this.xPositionOnScreen + 70, this.yPositionOnScreen + 550, 245, 52);
        this.mutationsButton = new Rectangle(this.xPositionOnScreen + 438, this.yPositionOnScreen + 550, 350, 52);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
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
            this.StartAttempt();
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
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        MutagenicAttemptRecord? active = this.registry.GetMutagenicAttempt(this.machineKey);
        DrawInset(b, new Rectangle(this.xPositionOnScreen + 20, this.yPositionOnScreen + 12, this.width - 40, 54), new Color(154, 93, 51));
        Utility.drawTextWithShadow(b, "Mutagenic Pool", Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + 42, this.yPositionOnScreen + 20), Color.White);

        int left = this.xPositionOnScreen + 20;
        int top = this.yPositionOnScreen + 78;
        DrawInset(b, new Rectangle(left, top, 360, 462), new Color(238, 224, 190));
        b.DrawString(Game1.smallFont, "1. Select Adult Slime", new Vector2(left + 24, top + 20), Game1.textColor);
        DrawInset(b, this.selectedSlimePanel, new Color(224, 196, 147));
        DrawButton(b, this.previousSlimeButton, "‹", new Color(198, 145, 87));
        DrawButton(b, this.nextSlimeButton, "›", new Color(198, 145, 87));
        FarmAnimal? selected = this.GetSelectedSlime()
            ?? (active is null ? null : this.candidates.FirstOrDefault(animal => animal.myID.Value == active.SlimeId));
        if (selected is null)
        {
            b.DrawString(Game1.smallFont, this.candidates.Count == 0 ? "No Ranch Slimes" : "Select a slime",
                new Vector2(this.selectedSlimePanel.X + 22, this.selectedSlimePanel.Y + 24), Game1.textColor);
        }
        else
        {
            b.DrawString(Game1.smallFont, selected.Name, new Vector2(this.selectedSlimePanel.X + 14, this.selectedSlimePanel.Y + 10), Game1.textColor);
            int age = this.registry.GetMaturityAgeDays(selected.myID.Value, selected.age.Value);
            b.DrawString(Game1.tinyFont, $"Adult · age {age} days", new Vector2(this.selectedSlimePanel.X + 14, this.selectedSlimePanel.Y + 42), Game1.textColor);
        }
        b.DrawString(Game1.tinyFont, selected is null ? "Use the arrows to choose a resident" : "‹ / › to change selected slime",
            new Vector2(left + 32, top + 258), Game1.textColor);

        b.DrawString(Game1.smallFont, "2. Add Slime (Fuel)", new Vector2(left + 24, top + 302), Game1.textColor);
        DrawInset(b, new Rectangle(left + 22, top + 330, 316, 60), new Color(224, 196, 147));
        DrawSlimeIcon(b, new Rectangle(left + 32, top + 336, 48, 48));
        b.DrawString(Game1.smallFont, $"{CountSlimeItems()} / {this.rules.SlimeCost}", new Vector2(left + 92, top + 340), Game1.textColor);
        b.DrawString(Game1.tinyFont, "vanilla Slime in inventory", new Vector2(left + 92, top + 366), Game1.textColor);

        b.DrawString(Game1.smallFont, "3. Mutation Attempt", new Vector2(left + 24, top + 402), Game1.textColor);
        if (active is null)
        {
            b.DrawString(Game1.tinyFont, $"Cycle: {this.rules.DurationDays} in-game day(s) · placeholder",
                new Vector2(left + 24, top + 418), Game1.textColor);
            DrawBar(b, new Rectangle(left + 24, top + 442, 306, 20), 0f, new Color(184, 135, 204));
        }
        else
        {
            int duration = Math.Max(1, active.ReadyOnDay - active.StartedOnDay);
            float progress = Math.Clamp((Game1.Date.TotalDays - active.StartedOnDay) / (float)duration, 0f, 1f);
            b.DrawString(Game1.tinyFont, $"Attempt in progress · ready day {active.ReadyOnDay}", new Vector2(left + 24, top + 418), Game1.textColor);
            DrawBar(b, new Rectangle(left + 24, top + 442, 306, 20), progress, new Color(184, 135, 204));
        }
        DrawButton(b, this.actionButton, active is null ? "Start" : "Processing", active is null ? new Color(103, 155, 206) : new Color(175, 175, 175));

        int center = this.xPositionOnScreen + 394;
        DrawInset(b, new Rectangle(center, top, 390, 462), new Color(238, 224, 190));
        DrawInset(b, new Rectangle(center + 8, top + 8, 374, 46), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "Possible Mutation Examples", new Vector2(center + 82, top + 21), Color.White);
        string[] mutationNames = { "Spotted", "Striped", "Starry", "Speckled", "Glowing", "Crystalline", "Mossy", "Metallic" };
        Color[] mutationColors = { new(224, 126, 162), new(90, 169, 221), new(148, 116, 214), new(232, 174, 75),
            new(148, 199, 92), new(119, 211, 227), new(92, 142, 66), new(153, 160, 168) };
        for (int i = 0; i < mutationNames.Length; i++)
        {
            int column = i % 4;
            int row = i / 4;
            Rectangle tile = new(center + 18 + column * 88, top + 78 + row * 106, 78, 94);
            DrawInset(b, tile, new Color(249, 231, 201));
            DrawSlimeSwatch(b, new Rectangle(tile.X + 18, tile.Y + 10, 42, 42), mutationColors[i]);
            b.DrawString(Game1.tinyFont, mutationNames[i], new Vector2(tile.X + 5, tile.Y + 65), Game1.textColor);
        }
        b.DrawString(Game1.tinyFont, "Design examples shown above.", new Vector2(center + 18, top + 300), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Only Shadow Clone is implemented.", new Vector2(center + 18, top + 320), Game1.textColor);
        DrawButton(b, this.mutationsButton, "Possible Mutations", new Color(198, 145, 87));

        int right = this.xPositionOnScreen + 804;
        DrawInset(b, new Rectangle(right, top, 176, 462), new Color(238, 224, 190));
        DrawInset(b, new Rectangle(right + 6, top + 6, 164, 46), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "Pity System", new Vector2(right + 34, top + 20), Color.White);
        b.DrawString(Game1.tinyFont, "Each slime has", new Vector2(right + 14, top + 72), Game1.textColor);
        b.DrawString(Game1.tinyFont, "its own counter.", new Vector2(right + 14, top + 92), Game1.textColor);
        double chance = active?.ChanceAtStart ?? (selected is null
            ? this.rules.GetChance(0)
            : this.rules.GetChance(this.registry.GetMutagenicPoolFailures(selected.myID.Value)));
        b.DrawString(Game1.smallFont, $"Chance {chance:P0}", new Vector2(right + 14, top + 132), Game1.textColor);
        DrawBar(b, new Rectangle(right + 14, top + 162, 148, 22), (float)chance, new Color(184, 135, 204));
        int failures = selected is null ? 0 : this.registry.GetMutagenicPoolFailures(selected.myID.Value);
        b.DrawString(Game1.tinyFont, $"Previous misses: {failures}", new Vector2(right + 14, top + 198), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Misses raise its", new Vector2(right + 14, top + 238), Game1.textColor);
        b.DrawString(Game1.tinyFont, "chance. Failure", new Vector2(right + 14, top + 258), Game1.textColor);
        b.DrawString(Game1.tinyFont, "never harms it.", new Vector2(right + 14, top + 278), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Success gives", new Vector2(right + 14, top + 316), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Shadow Clone.", new Vector2(right + 14, top + 336), Game1.textColor);
        if (active is not null)
            b.DrawString(Game1.tinyFont, $"Locked at {active.ChanceAtStart:P0} for this attempt.", new Vector2(right + 14, top + 360), Game1.textColor);

        if (!string.IsNullOrEmpty(this.statusMessage))
            b.DrawString(Game1.tinyFont, this.statusMessage.Length > 72 ? this.statusMessage[..69] + "..." : this.statusMessage,
                new Vector2(this.xPositionOnScreen + 500, this.yPositionOnScreen + 34), Color.White);
        b.DrawString(Game1.tinyFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 26), Game1.textColor);
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
        IClickableMenu.drawTextureBox(b, x, y, width, height, Color.White);
        Utility.drawTextWithShadow(b, "Possible Mutations", Game1.dialogueFont, new Vector2(x + 28, y + 22), Game1.textColor);
        b.DrawString(Game1.smallFont, "Currently implemented", new Vector2(x + 32, y + 86), Game1.textColor);
        b.DrawString(Game1.dialogueFont, "Shadow Clone", new Vector2(x + 32, y + 122), new Color(111, 83, 142));
        b.DrawString(Game1.smallFont, "Adds an external mutation trait and a visual shadow companion.", new Vector2(x + 32, y + 166), Game1.textColor);
        b.DrawString(Game1.smallFont, "Failure is safe: the slime returns unchanged and its next-attempt chance rises.",
            new Vector2(x + 32, y + 208), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Spotted, Striped, Starry, Speckled, Glowing, Crystalline, Mossy, and Metallic are design examples only; not implemented yet.",
            new Vector2(x + 32, y + 260), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Click anywhere to close", new Vector2(x + width - 174, y + height - 24), Game1.textColor);
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
            return age >= FarmAnimalContent.RanchSlimeDaysToMature
                && !this.registry.IsReserved(animal.myID.Value, this.machineKey)
                && string.IsNullOrWhiteSpace(this.registry.GetMutationTrait(animal.myID.Value));
        }).ToArray();
        if (eligible.Length == 0)
        {
            this.statusMessage = "No eligible mature Ranch Slime is available.";
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
        b.Draw(Game1.staminaRect, bounds, new Color(109, 72, 48));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6), fill);
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string label, Color fill)
    {
        DrawInset(b, bounds, fill);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label,
            new Vector2(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2), Game1.textColor);
    }

    private static void DrawSlimeIcon(SpriteBatch b, Rectangle bounds)
    {
        DrawSlimeSwatch(b, bounds, new Color(165, 93, 203));
    }

    private static void DrawSlimeSwatch(SpriteBatch b, Rectangle bounds, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 18, bounds.Width - 10, bounds.Height - 18), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 12, bounds.Y + 9, bounds.Width - 24, 12), color);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 13, bounds.Y + 28, 4, 4), Color.White);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + bounds.Width - 17, bounds.Y + 28, 4, 4), Color.White);
    }

    private static void DrawBar(SpriteBatch batch, Rectangle bounds, float value, Color fill)
    {
        batch.Draw(Game1.staminaRect, bounds, new Color(91, 75, 58));
        int width = (int)((bounds.Width - 4) * Math.Clamp(value, 0f, 1f));
        if (width > 0)
            batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), fill);
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
            this.statusMessage = "Select an eligible mature Ranch Slime first.";
            return;
        }
        if (CountSlimeItems() < this.rules.SlimeCost)
        {
            this.statusMessage = $"Bring {this.rules.SlimeCost} vanilla Slime to the Daycare.";
            return;
        }

        List<(int Slot, Item Item, int OldStack)> removed = ConsumeSlimeItems(this.rules.SlimeCost);
        FarmAnimal? animal = this.candidates.FirstOrDefault(candidate => candidate.myID.Value == slimeId);
        int age = animal is null ? 0 : this.registry.GetMaturityAgeDays(slimeId, animal.age.Value);
        if (!this.registry.StartMutagenicAttempt(this.machineKey, slimeId, Game1.Date.TotalDays, age, Game1.random.NextDouble(), this.rules))
        {
            RestoreItems(removed);
            this.statusMessage = "The selected slime is no longer eligible.";
            return;
        }

        this.statusMessage = "The pool is processing. Save by sleeping; failure is safe and improves this slime's odds next time.";
    }

    private static int CountSlimeItems() => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == "(O)" + SlimeItemId)
        .Sum(item => item!.Stack);

    private static List<(int Slot, Item Item, int OldStack)> ConsumeSlimeItems(int amount)
    {
        var removed = new List<(int Slot, Item Item, int OldStack)>();
        for (int i = 0; i < Game1.player.Items.Count && amount > 0; i++)
        {
            Item? item = Game1.player.Items[i];
            if (item is null || item.QualifiedItemId != "(O)" + SlimeItemId)
                continue;

            int oldStack = item.Stack;
            int consumed = Math.Min(amount, oldStack);
            removed.Add((i, item, oldStack));
            item.Stack -= consumed;
            amount -= consumed;
            if (item.Stack <= 0)
                Game1.player.Items[i] = null;
        }
        return removed;
    }

    private static void RestoreItems(IEnumerable<(int Slot, Item Item, int OldStack)> removed)
    {
        foreach ((int slot, Item item, int oldStack) in removed)
        {
            item.Stack = oldStack;
            Game1.player.Items[slot] = item;
        }
    }
}
