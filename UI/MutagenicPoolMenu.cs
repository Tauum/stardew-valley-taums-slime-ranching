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
    private const int RowHeight = 50;
    private const int SlimeItemId = 766;
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly MutagenicPoolRules rules;
    private readonly List<FarmAnimal> candidates;
    private readonly Rectangle actionButton;
    private readonly Rectangle listBounds;
    private long? selectedSlimeId;
    private string? statusMessage;
    private int firstVisibleIndex;

    public MutagenicPoolMenu(SlimeRegistry registry, string machineKey, MutagenicPoolRules rules)
        : base(Game1.uiViewport.Width / 2 - 360, Game1.uiViewport.Height / 2 - 270, 720, 540)
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
        this.listBounds = new Rectangle(this.xPositionOnScreen + 30, this.yPositionOnScreen + 124, 370, 286);
        this.actionButton = new Rectangle(this.xPositionOnScreen + 30, this.yPositionOnScreen + this.height - 84, 270, 52);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.actionButton.Contains(x, y))
        {
            this.StartAttempt();
            Game1.playSound("smallSelect");
            return;
        }

        if (!this.listBounds.Contains(x, y) || this.registry.GetMutagenicAttempt(this.machineKey) is not null)
            return;

        int index = this.firstVisibleIndex + (y - this.listBounds.Y) / RowHeight;
        if (index < 0 || index >= this.candidates.Count)
            return;

        FarmAnimal animal = this.candidates[index];
        int age = this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value);
        if (age < FarmAnimalContent.RanchSlimeDaysToMature)
        {
            this.statusMessage = $"{animal.Name} is still growing and cannot use the pool yet.";
            Game1.playSound("cancel");
            return;
        }
        if (this.registry.IsReserved(animal.myID.Value, this.machineKey))
        {
            this.statusMessage = $"{animal.Name} is already assigned to another ranching job.";
            Game1.playSound("cancel");
            return;
        }
        if (!string.IsNullOrWhiteSpace(this.registry.GetMutationTrait(animal.myID.Value)))
        {
            this.statusMessage = $"{animal.Name} already has a mutation trait.";
            Game1.playSound("cancel");
            return;
        }

        this.selectedSlimeId = animal.myID.Value;
        this.statusMessage = $"Selected {animal.Name}; failure history: {this.registry.GetMutagenicPoolFailures(animal.myID.Value)}.";
        Game1.playSound("smallSelect");
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int visibleRows = this.listBounds.Height / RowHeight;
        int maxFirst = Math.Max(0, this.candidates.Count - visibleRows);
        this.firstVisibleIndex = Math.Clamp(this.firstVisibleIndex + (direction < 0 ? 1 : -1), 0, maxFirst);
        base.receiveScrollWheelAction(direction);
    }

    public override void draw(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        string title = "Mutagenic Pool";
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 24),
            Game1.textColor);
        b.DrawString(Game1.smallFont, $"Choose one mature Ranch Slime. Cost: {this.rules.SlimeCost} vanilla Slime. Failure never harms the animal.",
            new Vector2(this.xPositionOnScreen + 32, this.yPositionOnScreen + 76), Game1.textColor);

        MutagenicAttemptRecord? active = this.registry.GetMutagenicAttempt(this.machineKey);
        int visibleRows = this.listBounds.Height / RowHeight;
        for (int rowIndex = 0; rowIndex < visibleRows && this.firstVisibleIndex + rowIndex < this.candidates.Count; rowIndex++)
        {
            int i = this.firstVisibleIndex + rowIndex;
            FarmAnimal animal = this.candidates[i];
            int age = this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value);
            int failures = this.registry.GetMutagenicPoolFailures(animal.myID.Value);
            bool eligible = age >= FarmAnimalContent.RanchSlimeDaysToMature
                && !this.registry.IsReserved(animal.myID.Value, this.machineKey)
                && string.IsNullOrWhiteSpace(this.registry.GetMutationTrait(animal.myID.Value))
                && active is null;
            Rectangle row = new(this.listBounds.X, this.listBounds.Y + rowIndex * RowHeight, this.listBounds.Width, RowHeight - 3);
            Color fill = this.selectedSlimeId == animal.myID.Value ? new Color(178, 225, 173)
                : eligible ? new Color(239, 224, 190) : new Color(211, 211, 211);
            b.Draw(Game1.staminaRect, row, fill);
            string label = age < FarmAnimalContent.RanchSlimeDaysToMature
                ? $"[GROWING] {animal.Name}"
                : !string.IsNullOrWhiteSpace(this.registry.GetMutationTrait(animal.myID.Value))
                    ? $"[MUTATED] {animal.Name}"
                : active is not null || this.registry.IsReserved(animal.myID.Value, this.machineKey)
                        ? $"[BUSY] {animal.Name}"
                : $"{animal.Name}  |  pity {failures}  |  next {this.rules.GetChance(failures):P0}";
            b.DrawString(Game1.smallFont, label, new Vector2(row.X + 10, row.Y + 14), Game1.textColor);
        }

        b.Draw(Game1.staminaRect, this.actionButton, active is null ? new Color(200, 177, 220) : new Color(210, 210, 210));
        b.DrawString(Game1.smallFont, active is null ? $"Start {this.rules.DurationDays}-day Attempt" : $"Processing until day {active.ReadyOnDay}",
            new Vector2(this.actionButton.X + 16, this.actionButton.Y + 16), Game1.textColor);
        string message = this.statusMessage ?? (active is null
            ? $"Slime in inventory: {CountSlimeItems()}/{this.rules.SlimeCost}. Shadow Clone is the current pool trait."
            : $"Chance locked at {active.ChanceAtStart:P0}; sleep to resolve the attempt.");
        b.DrawString(Game1.smallFont, message, new Vector2(this.xPositionOnScreen + 326, this.actionButton.Y + 12), Game1.textColor);
        if (this.candidates.Count > visibleRows)
            b.DrawString(Game1.tinyFont, $"Residents {this.firstVisibleIndex + 1}-{Math.Min(this.firstVisibleIndex + visibleRows, this.candidates.Count)} of {this.candidates.Count} (scroll)",
                new Vector2(this.listBounds.X, this.listBounds.Bottom + 4), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 26), Game1.textColor);
        this.drawMouse(b);
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
