using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>First breeder slice: choose exactly two Ranch Slimes housed in the Daycare.</summary>
internal sealed class SlimeBreederMenu : IClickableMenu
{
    private const int RowHeight = 48;
    private readonly List<FarmAnimal> candidates;
    private readonly int registeredCount;
    private readonly HashSet<long> selected = new();
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly Func<bool> saveExternalState;
    private readonly Func<Guid, Item> createEggItem;
    private readonly int listTop;
    private readonly int listBottom;
    private int firstVisibleCandidate;
    private readonly Rectangle actionButton;
    private readonly Rectangle releaseButton;
    private readonly Rectangle crystalSelector;
    private string? selectedCrystalColor;
    private bool pairReserved;
    private string? statusMessage;

    public SlimeBreederMenu(SlimeRegistry registry, string machineKey, Func<bool> saveExternalState, Func<Guid, Item> createEggItem)
        : base(Game1.uiViewport.Width / 2 - 400, Game1.uiViewport.Height / 2 - 280, 800, 560)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.saveExternalState = saveExternalState;
        this.createEggItem = createEggItem;
        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        StardewValley.AnimalHouse? interior = daycare?.GetIndoors() as StardewValley.AnimalHouse;

        // Stardew moves indoor animals from Farm.animals into AnimalHouse.animals.
        // Match the room's actual resident objects against our external IDs.
        HashSet<long> registeredIds = registry.RanchSlimeFarmAnimalIds.ToHashSet();
        this.registeredCount = registeredIds.Count;
        BreedingPairRecord? pair = registry.GetBreedingPair(machineKey);
        if (pair is not null)
        {
            if (pair.OutputEggItemId is null)
            {
                this.selected.Add(pair.ParentAId);
                this.selected.Add(pair.ParentBId);
            }
            this.pairReserved = true;
        }
        this.candidates = (interior?.animals.Values.AsEnumerable() ?? Enumerable.Empty<FarmAnimal>())
            .Where(animal => registeredIds.Contains(animal.myID.Value) || animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
            .Where(animal => !registry.IsReserved(animal.myID.Value, machineKey))
            .OrderBy(animal => animal.myID.Value)
            .ToList();
        if (pair is not null)
        {
            foreach (long parentId in new[] { pair.ParentAId, pair.ParentBId })
            {
                FarmAnimal? parent = interior?.animals.Values.FirstOrDefault(animal => animal.myID.Value == parentId);
                if (parent is not null && this.candidates.All(animal => animal.myID.Value != parentId))
                    this.candidates.Add(parent);
            }
        }
        this.listTop = this.yPositionOnScreen + 112;
        this.listBottom = this.yPositionOnScreen + 326;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 34, this.yPositionOnScreen + this.height - 90, 190, 56);
        this.releaseButton = new Rectangle(this.xPositionOnScreen + 236, this.yPositionOnScreen + this.height - 90, 180, 56);
        this.crystalSelector = new Rectangle(this.xPositionOnScreen + 430, this.listBottom + 12, 336, 72);
        if (pair?.ChromaticCrystalColor is string savedColor && ChromaticCrystalContent.IsColorName(savedColor))
            this.selectedCrystalColor = savedColor;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (mouseX < this.xPositionOnScreen + 24 || mouseX > this.xPositionOnScreen + 406
            || mouseY < this.listTop || mouseY >= this.listBottom)
            return;

        int lastFirstVisible = Math.Max(0, this.candidates.Count - this.GetVisibleCandidateCount());
        this.firstVisibleCandidate = Math.Clamp(
            this.firstVisibleCandidate + (direction < 0 ? 1 : -1),
            0,
            lastFirstVisible);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.crystalSelector.Contains(x, y) && this.registry.GetBreedingPair(this.machineKey)?.CycleReadyOnDay <= 0)
        {
            this.CycleCrystalSelection();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.releaseButton.Contains(x, y))
        {
            BreedingPairRecord? pair = this.registry.GetBreedingPair(this.machineKey);
            if (pair?.OutputEggItemId is not null)
                this.statusMessage = "Collect the finished egg before clearing this breeder.";
            else if (pair is not null && pair.CycleReadyOnDay > 0)
                this.statusMessage = "Cancel the cycle before releasing the parents.";
            else if (this.pairReserved)
            {
                this.registry.ReleasePair(this.machineKey);
                this.selected.Clear();
                this.pairReserved = false;
                this.statusMessage = "Parents released back to the Daycare pool.";
                this.saveExternalState();
            }
            Game1.playSound("smallSelect");
            return;
        }

        if (this.actionButton.Contains(x, y))
        {
            if (!this.pairReserved)
            {
                if (this.selected.Count != 2)
                    this.statusMessage = "Select exactly two parents to start the cycle.";
                else
                {
                    long[] ids = this.selected.ToArray();
                    if (this.registry.ReservePair(this.machineKey, ids[0], ids[1], Game1.Date.TotalDays))
                    {
                        if (this.TryStartBreedingCycle())
                        {
                            this.pairReserved = true;
                            this.statusMessage = "Two-day cycle started; parents are reserved.";
                        }
                        else
                        {
                            this.registry.ReleasePair(this.machineKey);
                            this.statusMessage = "Could not start the cycle; try again.";
                        }
                    }
                    else
                        this.statusMessage = "A parent is reserved or still resting after its last cycle.";
                }
            }
            else
            {
                BreedingPairRecord? pair = this.registry.GetBreedingPair(this.machineKey);
                if (pair?.OutputEggItemId is not null)
                    this.CollectBreedingEgg(pair);
                else if (pair is not null && pair.CycleReadyOnDay > 0)
                {
                    if (Game1.Date.TotalDays >= pair.CycleReadyOnDay)
                        this.CollectBreedingEgg(pair);
                    else
                    {
                        this.registry.CancelBreedingCycle(this.machineKey);
                        this.statusMessage = "Breeding cycle cancelled; parents remain reserved.";
                        this.saveExternalState();
                    }
                }
                else
                {
                    if (this.TryStartBreedingCycle())
                    {
                        this.statusMessage = "Two-day breeding cycle started.";
                    }
                    else
                        this.statusMessage = "Reserve two parents first.";
                }
            }
            Game1.playSound("smallSelect");
            return;
        }

        if (this.pairReserved || x < this.xPositionOnScreen + 24 || x > this.xPositionOnScreen + 410 || y < this.listTop || y >= this.listBottom)
            return;

        int visibleIndex = (y - this.listTop) / RowHeight;
        int index = this.firstVisibleCandidate + visibleIndex;
        if (visibleIndex < 0 || visibleIndex >= this.GetVisibleCandidateCount() || index >= this.candidates.Count)
            return;

        long id = this.candidates[index].myID.Value;
        int growthDays = this.GetGrowthDaysRemaining(this.candidates[index]);
        if (growthDays > 0)
        {
            this.statusMessage = $"This young Ranch Slime needs {growthDays} more day(s) to grow before breeding.";
            Game1.playSound("cancel");
            return;
        }

        int restDays = this.registry.GetBreedingRestDays(id, Game1.Date.TotalDays);
        if (restDays > 0)
        {
            this.statusMessage = $"This Ranch Slime needs {restDays} more day(s) of rest.";
            Game1.playSound("cancel");
            return;
        }

        if (!this.selected.Add(id))
            this.selected.Remove(id);
        else if (this.selected.Count > 2)
        {
            this.selected.Remove(id);
            this.statusMessage = "Choose exactly two parents.";
            Game1.playSound("cancel");
            return;
        }

        this.statusMessage = $"Selected {this.selected.Count} of 2 parents.";
        Game1.playSound("smallSelect");
    }

    public override void draw(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        string title = "Slime Breeder";
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 28),
            Game1.textColor);

        string instructions = "Choose two parents, then start the cycle to reserve them. Scroll the list for more.";
        b.DrawString(Game1.smallFont, instructions, new Vector2(this.xPositionOnScreen + 40, this.yPositionOnScreen + 82), Game1.textColor);

        if (this.candidates.Count == 0)
        {
            b.DrawString(Game1.smallFont, $"No live Ranch Slime matched. External registry IDs: {this.registeredCount}.",
                new Vector2(this.xPositionOnScreen + 40, this.listTop + 8), Game1.textColor);
        }
        else
        {
            int visibleCount = this.GetVisibleCandidateCount();
            int visibleEnd = Math.Min(this.firstVisibleCandidate + visibleCount, this.candidates.Count);
            for (int i = this.firstVisibleCandidate; i < visibleEnd; i++)
            {
                FarmAnimal animal = this.candidates[i];
                int rowY = this.listTop + (i - this.firstVisibleCandidate) * RowHeight;
                bool isSelected = this.selected.Contains(animal.myID.Value);
                int restDays = this.registry.GetBreedingRestDays(animal.myID.Value, Game1.Date.TotalDays);
                int growthDays = this.GetGrowthDaysRemaining(animal);
                Rectangle row = new(this.xPositionOnScreen + 24, rowY, 382, RowHeight - 4);
                b.Draw(Game1.staminaRect, row, restDays > 0 || growthDays > 0 ? new Color(210, 210, 210) : isSelected ? new Color(180, 235, 170) : new Color(238, 224, 190));
                string label = growthDays > 0
                    ? $"[GROWING {growthDays}d] {animal.Name}"
                    : restDays > 0
                    ? $"[RESTING {restDays}d] {animal.Name}"
                    : $"{(isSelected ? "[SELECTED] " : "[  ] ")}{animal.Name}";
                b.DrawString(Game1.smallFont, label, new Vector2(row.X + 12, row.Y + 14), Game1.textColor);
            }

            if (this.candidates.Count > visibleCount)
            {
                string range = $"{this.firstVisibleCandidate + 1}-{visibleEnd} of {this.candidates.Count}";
                b.DrawString(Game1.tinyFont, range, new Vector2(this.xPositionOnScreen + 320, this.listBottom - 18), Game1.textColor);
            }
        }

        this.DrawChamber(b);
        this.DrawCrystalSelector(b);
        b.Draw(Game1.staminaRect, this.actionButton, new Color(180, 220, 177));
        BreedingPairRecord? currentPair = this.registry.GetBreedingPair(this.machineKey);
        bool cycleFinished = currentPair?.CycleReadyOnDay > 0 && Game1.Date.TotalDays >= currentPair.CycleReadyOnDay;
        string buttonText = !this.pairReserved ? "Start 2-day Cycle"
            : currentPair?.OutputEggItemId is not null || cycleFinished ? "Collect Egg"
            : currentPair?.CycleReadyOnDay > 0 ? "Cancel Cycle" : "Start 2-day Cycle";
        b.DrawString(Game1.smallFont, buttonText, new Vector2(this.actionButton.X + 14, this.actionButton.Y + 17), Game1.textColor);
        b.Draw(Game1.staminaRect, this.releaseButton, new Color(221, 196, 158));
        b.DrawString(Game1.smallFont, "Release Pair", new Vector2(this.releaseButton.X + 28, this.releaseButton.Y + 17), Game1.textColor);
        string status = this.statusMessage ?? GetCycleStatus(currentPair);
        b.DrawString(Game1.smallFont, status, new Vector2(this.xPositionOnScreen + 430, this.actionButton.Y + 17), Game1.textColor);
        b.DrawString(Game1.smallFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 130, this.actionButton.Y + 52), Game1.textColor);
        this.drawMouse(b);
    }

    private void DrawChamber(SpriteBatch b)
    {
        Rectangle chamber = new(this.xPositionOnScreen + 424, this.listTop, 342, this.listBottom - this.listTop);
        b.Draw(Game1.staminaRect, chamber, new Color(191, 222, 222));
        IClickableMenu.drawTextureBox(b, chamber.X, chamber.Y, chamber.Width, chamber.Height, Color.White);
        b.DrawString(Game1.smallFont, "BREEDING CHAMBER", new Vector2(chamber.X + 74, chamber.Y + 12), Game1.textColor);
        BreedingPairRecord? currentPair = this.registry.GetBreedingPair(this.machineKey);
        if (currentPair?.OutputEggItemId is not null)
        {
            b.DrawString(Game1.dialogueFont, "EGG READY", new Vector2(chamber.X + 88, chamber.Y + 82), Game1.textColor);
            b.DrawString(Game1.smallFont, "Parents returned to the Daycare.", new Vector2(chamber.X + 47, chamber.Y + 124), Game1.textColor);
            b.DrawString(Game1.smallFont, "Collect the egg to use this breeder again.", new Vector2(chamber.X + 12, chamber.Y + 154), Game1.textColor);
            return;
        }

        FarmAnimal[] parents = this.candidates.Where(animal => this.selected.Contains(animal.myID.Value)).Take(2).ToArray();
        if (parents.Length == 0)
        {
            b.DrawString(Game1.smallFont, "Select two parents", new Vector2(chamber.X + 84, chamber.Y + 112), Game1.textColor);
            return;
        }

        double time = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        for (int i = 0; i < parents.Length; i++)
        {
            FarmAnimal parent = parents[i];
            int centerX = chamber.X + (i == 0 ? 102 : 242);
            float bob = (float)Math.Sin(time * 4 + i * Math.PI) * 5f;
            Rectangle spriteBounds = new(centerX - 36, chamber.Y + 38 + (int)bob, 72, 96);
            b.Draw(parent.Sprite.Texture, spriteBounds, parent.Sprite.SourceRect, Color.White);
            string name = i == 0 ? "Parent A" : "Parent B";
            float nameWidth = Game1.smallFont.MeasureString(name).X;
            b.DrawString(Game1.smallFont, name, new Vector2(centerX - nameWidth / 2, chamber.Y + 136), Game1.textColor);

            var species = this.registry.GetGenomeColorState(parent.myID.Value);
            string speciesName = species?.SecondarySpecies is string secondary
                ? $"{species.Value.PrimarySpecies} / {secondary}"
                : species?.PrimarySpecies ?? "Ranch Slime";
            string size = this.registry.GetGenomeSize(parent.myID.Value) ?? "Normal";
            string shiny = this.registry.GetGenomeIsShiny(parent.myID.Value) ? " · Shiny" : "";
            string mutation = this.registry.GetMutationTrait(parent.myID.Value) is string trait ? $" · {trait}" : "";
            DrawCenteredFittedText(b, speciesName, centerX, chamber.Y + 156);
            string traits = $"{size}{shiny}{mutation}";
            DrawCenteredFittedText(b, traits, centerX, chamber.Y + 174);

            string likedFood = $"Likes: {FoodLikeCatalog.GetDisplayName(this.registry.GetGenomeLikedFood(parent.myID.Value))}";
            DrawCenteredFittedText(b, likedFood, centerX, chamber.Y + 192);
        }
    }

    private static void DrawCenteredFittedText(SpriteBatch batch, string text, int centerX, int y)
    {
        Vector2 measured = Game1.tinyFont.MeasureString(text);
        float scale = Math.Min(1f, 148f / Math.Max(1f, measured.X));
        batch.DrawString(Game1.tinyFont, text,
            new Vector2(centerX - measured.X * scale / 2f, y), Game1.textColor,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private void DrawCrystalSelector(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect, this.crystalSelector, new Color(244, 224, 176));
        IClickableMenu.drawTextureBox(b, this.crystalSelector.X, this.crystalSelector.Y, this.crystalSelector.Width, this.crystalSelector.Height, Color.White);
        string label = this.selectedCrystalColor is null
            ? "Offspring color: inherited (click to choose a crystal)"
            : $"Offspring color: {this.selectedCrystalColor} (crystal consumed on start)";
        b.DrawString(Game1.smallFont, label, new Vector2(this.crystalSelector.X + 12, this.crystalSelector.Y + 13), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Only common single-color offspring result; hybrid and exceptional traits are cleared.",
            new Vector2(this.crystalSelector.X + 12, this.crystalSelector.Y + 43), Game1.textColor);
    }

    private void CycleCrystalSelection()
    {
        List<string> available = ChromaticCrystalContent.QualifiedItemIds
            .Where(id => Game1.player.Items.Any(item => item is not null && item.QualifiedItemId == id))
            .Select(id => ChromaticCrystalContent.TryGetColor(id, out string color, out _) ? color : "")
            .Where(color => color.Length > 0)
            .ToList();
        if (available.Count == 0)
        {
            this.selectedCrystalColor = null;
            this.statusMessage = "No Chromatic Crystals in inventory; using inherited color.";
            return;
        }

        int current = this.selectedCrystalColor is null ? -1 : available.FindIndex(color => color.Equals(this.selectedCrystalColor, StringComparison.OrdinalIgnoreCase));
        this.selectedCrystalColor = current == available.Count - 1 ? null : available[Math.Max(0, current + 1)];
        this.statusMessage = this.selectedCrystalColor is null
            ? "Crystal focus disabled; offspring will inherit a parent color."
            : $"Crystal focus: {this.selectedCrystalColor}. Click again to cycle.";
    }

    private bool TryStartBreedingCycle()
    {
        Item? crystalItem = null;
        int crystalSlot = -1;
        if (this.selectedCrystalColor is not null)
        {
            string qualifiedId = "(O)" + ChromaticCrystalContent.GetItemId(this.selectedCrystalColor);
            for (int i = 0; i < Game1.player.Items.Count; i++)
            {
                Item? item = Game1.player.Items[i];
                if (item is not null && item.QualifiedItemId == qualifiedId && item.Stack > 0)
                {
                    crystalSlot = i;
                    break;
                }
            }
            if (crystalSlot < 0)
            {
                this.statusMessage = $"No {this.selectedCrystalColor} Chromatic Crystal remains; choose another focus.";
                return false;
            }
            crystalItem = Game1.player.Items[crystalSlot];
        }

        if (!this.registry.StartBreedingCycle(this.machineKey, Game1.Date.TotalDays, this.selectedCrystalColor))
            return false;

        if (crystalItem is not null)
        {
            crystalItem.Stack--;
            if (crystalItem.Stack <= 0)
                Game1.player.Items[crystalSlot] = null;
        }

        if (this.saveExternalState())
            return true;

        this.registry.CancelBreedingCycle(this.machineKey);
        if (crystalItem is not null)
        {
            if (crystalItem.Stack > 0)
                crystalItem.Stack++;
            else
                Game1.player.Items[crystalSlot] = crystalItem;
        }
        this.statusMessage = "Could not save the cycle; the crystal was returned.";
        return false;
    }

    private int GetVisibleCandidateCount() => Math.Max(1, (this.listBottom - this.listTop) / RowHeight);

    private int GetGrowthDaysRemaining(FarmAnimal animal) =>
        Math.Max(0, FarmAnimalContent.RanchSlimeDaysToMature
            - this.registry.GetMaturityAgeDays(animal.myID.Value, animal.age.Value));

    private string GetCycleStatus(BreedingPairRecord? pair)
    {
        if (pair is null)
            return $"Selected: {this.selected.Count}/2";
        if (pair.CycleReadyOnDay <= 0)
            return "Pair reserved; cycle not started.";

        int remainingDays = pair.CycleReadyOnDay - Game1.Date.TotalDays;
        return remainingDays > 0
            ? $"Cycle: {remainingDays} day(s) remaining."
            : pair.OutputEggItemId is null ? "Egg is forming; parents will return when it is ready." : "Egg ready; parents returned to the Daycare.";
    }

    private void CollectBreedingEgg(BreedingPairRecord pair)
    {
        if (Game1.player.Items.Count(item => item is null) == 0)
        {
            this.statusMessage = "Make one inventory slot free to collect the egg.";
            return;
        }

        try
        {
            SlimeEggRecord? egg = this.registry.GetOrCreateBreedingEgg(this.machineKey, Game1.Date.TotalDays, out _);
            if (egg is null || !this.saveExternalState())
            {
                this.statusMessage = "Could not save the fixed offspring genome; try again.";
                return;
            }

            Item item = this.createEggItem(egg.Genome.Id);
            if (!this.registry.FinishBreedingCycle(this.machineKey, egg.ItemId, Game1.Date.TotalDays) || !this.saveExternalState())
            {
                this.registry.RestoreBreedingCycle(pair, Game1.Date.TotalDays);
                this.statusMessage = "Could not save egg collection; the egg remains ready in the breeder.";
                return;
            }

            if (!Game1.player.addItemToInventoryBool(item))
            {
                this.registry.RestoreBreedingCycle(pair, Game1.Date.TotalDays);
                this.saveExternalState();
                this.statusMessage = "Inventory is full; the egg remains ready in the breeder.";
                return;
            }

            this.pairReserved = false;
            this.selected.Clear();
            this.statusMessage = $"Collected a fixed-genome egg (generation {egg.Genome.Generation}).";
            Game1.playSound("coin");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            this.statusMessage = $"Could not collect the egg: {exception.Message}";
        }
    }
}
