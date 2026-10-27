using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Feeds one eligible monster drop or fishing-trash item to a juvenile Ranch Slime.</summary>
internal sealed class CompostTroughMenu : IClickableMenu
{
    private const int RowHeight = 48;
    private const int VisibleRows = 5;
    private readonly SlimeRegistry registry;
    private readonly Func<long, int, (bool Success, bool Favorite, int NewAge)> feedSlime;
    private readonly Rectangle slimeBounds;
    private readonly Rectangle foodBounds;
    private readonly Rectangle actionButton;
    private List<FarmAnimal> juveniles = new();
    private List<(int InventoryIndex, string ItemId, string Name, int Stack)> foods = new();
    private long? selectedSlimeId;
    private int? selectedInventoryIndex;
    private int firstSlimeIndex;
    private int firstFoodIndex;
    private string? statusMessage;

    public CompostTroughMenu(SlimeRegistry registry, Func<long, int, (bool Success, bool Favorite, int NewAge)> feedSlime)
        : base(Game1.uiViewport.Width / 2 - 400, Game1.uiViewport.Height / 2 - 275, 800, 550)
    {
        this.registry = registry;
        this.feedSlime = feedSlime;
        this.slimeBounds = new Rectangle(this.xPositionOnScreen + 28, this.yPositionOnScreen + 142, 354, VisibleRows * RowHeight);
        this.foodBounds = new Rectangle(this.xPositionOnScreen + 418, this.yPositionOnScreen + 142, 354, VisibleRows * RowHeight);
        this.actionButton = new Rectangle(this.xPositionOnScreen + 28, this.yPositionOnScreen + 414, 300, 54);
        this.RefreshChoices();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.actionButton.Contains(x, y))
        {
            if (this.selectedSlimeId is not long slimeId || this.selectedInventoryIndex is not int inventoryIndex)
            {
                this.statusMessage = "Select one juvenile and one eligible item first.";
                Game1.playSound("cancel");
                return;
            }

            (bool success, bool favorite, int newAge) = this.feedSlime(slimeId, inventoryIndex);
            this.statusMessage = success
                ? $"Fed {(favorite ? "favorite" : "regular")} food. Growth: {newAge}/{FarmAnimalContent.RanchSlimeDaysToMature} days."
                : "That slime or item is no longer eligible. Nothing was consumed.";
            if (success)
            {
                this.selectedSlimeId = null;
                this.selectedInventoryIndex = null;
                this.firstSlimeIndex = 0;
                this.firstFoodIndex = 0;
                this.RefreshChoices();
            }
            Game1.playSound(success ? "dwop" : "cancel");
            return;
        }

        if (this.slimeBounds.Contains(x, y))
        {
            int index = this.firstSlimeIndex + (y - this.slimeBounds.Y) / RowHeight;
            if (index >= 0 && index < this.juveniles.Count)
            {
                this.selectedSlimeId = this.juveniles[index].myID.Value;
                this.statusMessage = $"Selected {this.juveniles[index].Name}.";
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (this.foodBounds.Contains(x, y))
        {
            int index = this.firstFoodIndex + (y - this.foodBounds.Y) / RowHeight;
            if (index >= 0 && index < this.foods.Count)
            {
                this.selectedInventoryIndex = this.foods[index].InventoryIndex;
                this.statusMessage = $"Selected {this.foods[index].Name} x{this.foods[index].Stack}.";
                Game1.playSound("smallSelect");
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (this.slimeBounds.Contains(mouseX, mouseY))
            this.firstSlimeIndex = Math.Clamp(this.firstSlimeIndex + (direction < 0 ? 1 : -1), 0,
                Math.Max(0, this.juveniles.Count - VisibleRows));
        else if (this.foodBounds.Contains(mouseX, mouseY))
            this.firstFoodIndex = Math.Clamp(this.firstFoodIndex + (direction < 0 ? 1 : -1), 0,
                Math.Max(0, this.foods.Count - VisibleRows));
        base.receiveScrollWheelAction(direction);
    }

    public override void draw(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        const string title = "Compost Trough";
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 24),
            Game1.textColor);
        b.DrawString(Game1.smallFont, "Feed one eligible item to a juvenile per day. Their favorite food gives extra growth.",
            new Vector2(this.xPositionOnScreen + 30, this.yPositionOnScreen + 78), Game1.textColor);
        b.DrawString(Game1.smallFont, "Juveniles", new Vector2(this.slimeBounds.X, this.slimeBounds.Y - 30), Game1.textColor);
        b.DrawString(Game1.smallFont, "Eligible monster drops and fishing trash", new Vector2(this.foodBounds.X, this.foodBounds.Y - 30), Game1.textColor);
        this.DrawRows(b);
        b.Draw(Game1.staminaRect, this.actionButton, new Color(190, 208, 157));
        b.DrawString(Game1.smallFont, "Feed Selected Slime", new Vector2(this.actionButton.X + 52, this.actionButton.Y + 17), Game1.textColor);
        b.DrawString(Game1.tinyFont, this.statusMessage ?? "Favorite food: extra growth. Mature or busy slimes cannot be fed.",
            new Vector2(this.xPositionOnScreen + 350, this.actionButton.Y + 10), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Scroll each list separately  |  Esc: close",
            new Vector2(this.xPositionOnScreen + this.width - 245, this.yPositionOnScreen + this.height - 28), Game1.textColor);
        this.drawMouse(b);
    }

    private void DrawRows(SpriteBatch b)
    {
        int slimeEnd = Math.Min(this.firstSlimeIndex + VisibleRows, this.juveniles.Count);
        for (int i = this.firstSlimeIndex; i < slimeEnd; i++)
        {
            FarmAnimal animal = this.juveniles[i];
            int age = this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value);
            Rectangle row = new(this.slimeBounds.X, this.slimeBounds.Y + (i - this.firstSlimeIndex) * RowHeight,
                this.slimeBounds.Width, RowHeight - 3);
            bool selected = this.selectedSlimeId == animal.myID.Value;
            b.Draw(Game1.staminaRect, row, selected ? new Color(178, 225, 173) : new Color(239, 224, 190));
            b.DrawString(Game1.smallFont, animal.Name, new Vector2(row.X + 10, row.Y + 5), Game1.textColor);
            b.DrawString(Game1.tinyFont, $"Age {age}/{FarmAnimalContent.RanchSlimeDaysToMature} days",
                new Vector2(row.X + 10, row.Y + 27), Game1.textColor);
        }

        if (this.juveniles.Count == 0)
            b.DrawString(Game1.smallFont, "No eligible juveniles in the Daycare.",
                new Vector2(this.slimeBounds.X + 8, this.slimeBounds.Y + 8), Game1.textColor);

        int foodEnd = Math.Min(this.firstFoodIndex + VisibleRows, this.foods.Count);
        for (int i = this.firstFoodIndex; i < foodEnd; i++)
        {
            var food = this.foods[i];
            Rectangle row = new(this.foodBounds.X, this.foodBounds.Y + (i - this.firstFoodIndex) * RowHeight,
                this.foodBounds.Width, RowHeight - 3);
            bool selected = this.selectedInventoryIndex == food.InventoryIndex;
            b.Draw(Game1.staminaRect, row, selected ? new Color(178, 225, 173) : new Color(239, 224, 190));
            bool favorite = this.selectedSlimeId is long slimeId
                && string.Equals(this.registry.GetGenomeLikedFood(slimeId), food.ItemId, StringComparison.Ordinal);
            b.DrawString(Game1.smallFont, favorite ? $"{food.Name}  [FAVORITE]" : food.Name,
                new Vector2(row.X + 10, row.Y + 5), Game1.textColor);
            b.DrawString(Game1.tinyFont, $"Stack: {food.Stack}", new Vector2(row.X + 10, row.Y + 27), Game1.textColor);
        }

        if (this.foods.Count == 0)
            b.DrawString(Game1.smallFont, "No eligible items in your inventory.",
                new Vector2(this.foodBounds.X + 8, this.foodBounds.Y + 8), Game1.textColor);

    }

    private void RefreshChoices()
    {
        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        StardewValley.AnimalHouse? interior = daycare?.GetIndoors() as StardewValley.AnimalHouse;
        this.juveniles = (interior?.animals.Values.AsEnumerable() ?? Enumerable.Empty<FarmAnimal>())
            .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId
                && this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value) < FarmAnimalContent.RanchSlimeDaysToMature
                && !this.registry.IsReserved(animal.myID.Value))
            .OrderBy(animal => animal.myID.Value)
            .ToList();
        this.foods = Game1.player.Items
            .Select((item, index) => (item, index))
            .Where(pair => pair.item is not null && CompostTroughRules.IsEligibleFood(pair.item.QualifiedItemId))
            .Select(pair => (pair.index, pair.item!.QualifiedItemId, pair.item.DisplayName, pair.item.Stack))
            .ToList();
    }

}
