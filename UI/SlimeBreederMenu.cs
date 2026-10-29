using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
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
    private readonly int cycleDurationDays;
    private readonly int listTop;
    private readonly int listBottom;
    private int firstVisibleCandidate;
    private readonly Rectangle actionButton;
    private readonly Rectangle crystalSelector;
    private string? selectedCrystalColor;
    private bool pairReserved;
    private string? statusMessage;
    private string activeTab = "BREEDING";

    public SlimeBreederMenu(SlimeRegistry registry, string machineKey, Func<bool> saveExternalState, Func<Guid, Item> createEggItem,
        int cycleDurationDays = BreedingRules.BasicCycleDurationDays)
        : base(Game1.uiViewport.Width / 2 - 500, Game1.uiViewport.Height / 2 - 330, 1000, 660)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.saveExternalState = saveExternalState;
        this.createEggItem = createEggItem;
        this.cycleDurationDays = Math.Max(1, cycleDurationDays);
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
                if (parent is not null && !registry.IsReserved(parentId, machineKey)
                    && this.candidates.All(animal => animal.myID.Value != parentId))
                    this.candidates.Add(parent);
            }
        }
        this.listTop = this.yPositionOnScreen + 198;
        this.listBottom = this.yPositionOnScreen + 448;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 670, this.yPositionOnScreen + this.height - 86, 250, 58);
        this.crystalSelector = new Rectangle(this.xPositionOnScreen + 635, this.yPositionOnScreen + 466, 320, 62);
        if (pair?.ChromaticCrystalColor is string savedColor && ChromaticCrystalContent.IsColorName(savedColor))
            this.selectedCrystalColor = savedColor;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (mouseX < this.xPositionOnScreen + 620 || mouseX > this.xPositionOnScreen + 970
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
        if (this.TrySelectTab(x, y))
        {
            Game1.playSound("smallSelect");
            return;
        }

        if (this.activeTab != "BREEDING")
        {
            if (this.activeTab == "SETTINGS" && this.crystalSelector.Contains(x, y)
                && this.registry.GetBreedingPair(this.machineKey)?.CycleReadyOnDay <= 0)
                this.CycleCrystalSelection();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.crystalSelector.Contains(x, y) && this.registry.GetBreedingPair(this.machineKey)?.CycleReadyOnDay <= 0)
        {
            this.CycleCrystalSelection();
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
                            this.statusMessage = $"Breeding cycle started: {this.cycleDurationDays} days. Parents are reserved.";
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
                        if (this.registry.CancelBreedingCycle(this.machineKey))
                        {
                            if (this.saveExternalState())
                            {
                                this.selected.Clear();
                                this.pairReserved = false;
                                this.statusMessage = "Cycle cancelled; parents released to the Daycare.";
                            }
                            else
                            {
                                this.registry.RestoreBreedingCycle(pair, Game1.Date.TotalDays);
                                this.statusMessage = "Could not save cancellation; the pair remains reserved and the cycle continues.";
                            }
                        }
                        else
                            this.statusMessage = "Could not cancel the breeding cycle.";
                    }
                }
                else
                {
                    if (this.TryStartBreedingCycle())
                    {
                        this.statusMessage = $"Breeding cycle started: {this.cycleDurationDays} days.";
                    }
                    else
                        this.statusMessage = "Reserve two parents first.";
                }
            }
            Game1.playSound("smallSelect");
            return;
        }

        if (this.pairReserved || x < this.xPositionOnScreen + 635 || x > this.xPositionOnScreen + 955 || y < this.listTop || y >= this.listBottom)
            return;

        int visibleIndex = (y - this.listTop) / 46;
        int index = this.firstVisibleCandidate + visibleIndex;
        if (visibleIndex < 0 || visibleIndex >= this.GetVisibleCandidateCount() || index >= this.candidates.Count)
            return;

        long id = this.candidates[index].myID.Value;
        if (!this.registry.TryGetSlimeRecord(id, out SlimeRecord selectedRecord))
        {
            this.statusMessage = "This Ranch Slime has no saved genome and cannot breed.";
            Game1.playSound("cancel");
            return;
        }

        if (!BreedingRules.IsBreedable(selectedRecord.Genome))
        {
            this.statusMessage = "Rare and exceptional Ranch Slimes cannot breed.";
            Game1.playSound("cancel");
            return;
        }

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
        Rectangle outer = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        b.Draw(Game1.staminaRect, outer, new Color(41, 35, 71));
        DrawPanel(b, new Rectangle(outer.X + 8, outer.Y + 8, outer.Width - 16, outer.Height - 16), new Color(29, 43, 86), new Color(111, 157, 229));
        Rectangle header = new(outer.X + 16, outer.Y + 14, outer.Width - 32, 58);
        b.Draw(Game1.staminaRect, header, new Color(30, 91, 170));
        DrawPanel(b, header, new Color(30, 91, 170), new Color(108, 178, 250));
        b.DrawString(Game1.dialogueFont, "SLIME BREEDER", new Vector2(header.X + 24, header.Y + 12), new Color(207, 244, 255));
        b.DrawString(Game1.smallFont, "×", new Vector2(header.Right - 44, header.Y + 5), new Color(255, 155, 125));
        this.DrawTabs(b);

        if (this.activeTab == "BREEDING")
            this.DrawBreedingTab(b);
        else if (this.activeTab == "RECIPES")
            this.DrawRecipesTab(b);
        else
            this.DrawSettingsTab(b);

        this.drawMouse(b);
    }

    private void DrawBreedingTab(SpriteBatch b)
    {
        Rectangle pod = new(this.xPositionOnScreen + 30, this.yPositionOnScreen + 126, 570, 400);
        DrawPanel(b, pod, new Color(36, 52, 102), new Color(120, 163, 227));
        this.DrawChamber(b, pod);
        Rectangle select = new(this.xPositionOnScreen + 620, this.yPositionOnScreen + 126, 350, 320);
        DrawPanel(b, select, new Color(35, 49, 96), new Color(95, 137, 205));
        b.DrawString(Game1.smallFont, "SELECT SLIMES", new Vector2(select.X + 18, select.Y + 12), new Color(130, 231, 255));
        this.DrawCandidateRows(b);
        this.DrawCrystalSelector(b);

        BreedingPairRecord? pair = this.registry.GetBreedingPair(this.machineKey);
        this.DrawCompatibility(b, pair);
        DrawPanel(b, new Rectangle(pod.X + 10, pod.Bottom - 82, 314, 66), new Color(74, 105, 172), new Color(129, 169, 235));
        b.DrawString(Game1.smallFont, "CYCLE DURATION", new Vector2(pod.X + 32, pod.Bottom - 76), Color.White);
        b.DrawString(Game1.dialogueFont, $"{this.GetCycleDuration(pair)} Days", new Vector2(pod.X + 92, pod.Bottom - 52), new Color(242, 246, 255));
        DrawProgress(b, new Rectangle(pod.X + 20, pod.Bottom - 23, 286, 8), pair);

        bool ready = pair?.OutputEggItemId is not null || pair?.CycleReadyOnDay > 0 && Game1.Date.TotalDays >= pair.CycleReadyOnDay;
        string buttonText = !this.pairReserved ? "♥  START BREEDING"
            : ready ? "COLLECT EGG"
            : pair?.CycleReadyOnDay > 0 ? "CANCEL CYCLE" : "♥  START BREEDING";
        DrawPanel(b, this.actionButton, ready ? new Color(79, 153, 91) : new Color(68, 147, 80), new Color(151, 238, 137));
        DrawCentered(b, buttonText, Game1.smallFont, this.actionButton, Color.White);
        string status = this.statusMessage ?? GetCycleStatus(pair);
        b.DrawString(Game1.tinyFont, status, new Vector2(this.xPositionOnScreen + 34, this.yPositionOnScreen + this.height - 65), Color.White);
        this.DrawPairingPreview(b);
    }

    private void DrawCandidateRows(SpriteBatch b)
    {
        if (this.candidates.Count == 0)
        {
            b.DrawString(Game1.smallFont, $"No Ranch Slimes found ({this.registeredCount} registered).", new Vector2(this.xPositionOnScreen + 638, this.listTop + 8), Color.White);
            return;
        }

        int visibleCount = this.GetVisibleCandidateCount();
        int end = Math.Min(this.firstVisibleCandidate + visibleCount, this.candidates.Count);
        for (int i = this.firstVisibleCandidate; i < end; i++)
        {
            FarmAnimal animal = this.candidates[i];
            int rowY = this.listTop + (i - this.firstVisibleCandidate) * 46;
            bool isSelected = this.selected.Contains(animal.myID.Value);
            bool hasGenome = this.registry.TryGetSlimeRecord(animal.myID.Value, out SlimeRecord record);
            bool canBreed = hasGenome && BreedingRules.IsBreedable(record.Genome);
            int rest = this.registry.GetBreedingRestDays(animal.myID.Value, Game1.Date.TotalDays);
            int growth = this.GetGrowthDaysRemaining(animal);
            Rectangle row = new(this.xPositionOnScreen + 634, rowY, 322, 41);
            DrawPanel(b, row, !canBreed || growth > 0 || rest > 0 ? new Color(63, 66, 94) : isSelected ? new Color(74, 119, 161) : new Color(46, 67, 121), isSelected ? new Color(151, 220, 255) : new Color(87, 118, 177));
            string label = !canBreed ? $"{animal.Name} · cannot breed"
                : growth > 0 ? $"{animal.Name} · {growth}d to maturity"
                : rest > 0 ? $"{animal.Name} · resting {rest}d"
                : $"{(isSelected ? "♥ " : "＋ ")}{animal.Name}";
            b.DrawString(Game1.smallFont, label, new Vector2(row.X + 11, row.Y + 9), Color.White);
            string detail = hasGenome ? FormatCandidateGenome(record.Genome, canBreed) : "Cannot breed - genome unavailable";
            DrawFittedText(b, detail, new Rectangle(row.X + 8, row.Y + 25, row.Width - 16, 14));
        }
        if (this.candidates.Count > visibleCount)
            b.DrawString(Game1.tinyFont, $"Scroll · {this.firstVisibleCandidate + 1}-{end} / {this.candidates.Count}", new Vector2(this.xPositionOnScreen + 738, this.listBottom - 14), Color.LightBlue);
    }

    private void DrawCompatibility(SpriteBatch b, BreedingPairRecord? pair)
    {
        string[] hearts = { "♡", "♡", "♡", "♡" };
        int count = Math.Min(4, this.selected.Count * 2);
        for (int i = 0; i < hearts.Length; i++)
            b.DrawString(Game1.dialogueFont, i < count ? "♥" : hearts[i], new Vector2(this.xPositionOnScreen + 656 + i * 36, this.yPositionOnScreen + 418), i < count ? new Color(255, 115, 173) : new Color(135, 158, 206));
        string label = pair is not null
            ? pair.OutputEggItemId is null ? "PARENTS RESERVED" : "EGG READY"
            : this.selected.Count != 2 ? "CHOOSE TWO PARENTS"
            : this.HasEligibleSelectedPair() ? "PAIR ELIGIBLE" : "PAIR INELIGIBLE";
        Color labelColor = label == "PAIR INELIGIBLE" ? new Color(255, 160, 145) : new Color(135, 255, 176);
        b.DrawString(Game1.tinyFont, label, new Vector2(this.xPositionOnScreen + 802, this.yPositionOnScreen + 430), labelColor);
    }

    private bool HasEligibleSelectedPair()
    {
        if (this.selected.Count != 2)
            return false;

        SlimeGenome[] parents = this.selected
            .Select(id => this.registry.TryGetSlimeRecord(id, out SlimeRecord record) ? record.Genome : null)
            .Where(genome => genome is not null)
            .Cast<SlimeGenome>()
            .ToArray();
        return parents.Length == 2 && BreedingRules.GetPossibleOffspring(parents[0], parents[1]).Count > 0;
    }

    private void DrawPairingPreview(SpriteBatch b)
    {
        Rectangle panel = new(this.xPositionOnScreen + 634, this.yPositionOnScreen + 532, 322, 38);
        DrawPanel(b, panel, new Color(34, 46, 86), new Color(94, 130, 193));
        string resultLine;
        string abilityLine;
        if (this.selected.Count != 2)
        {
            resultLine = this.selected.Count == 0 ? "Choose two parents to preview." : "Choose one more parent to preview.";
            abilityLine = "Rare and exceptional slimes cannot breed.";
        }
        else
        {
            SlimeGenome[] parents = this.selected
                .Select(id => this.registry.TryGetSlimeRecord(id, out SlimeRecord record) ? record.Genome : null)
                .Where(genome => genome is not null)
                .Cast<SlimeGenome>()
                .ToArray();
            IReadOnlyList<BreedingRules.Offspring> outcomes = parents.Length == 2
                ? BreedingRules.GetPossibleOffspring(parents[0], parents[1])
                : Array.Empty<BreedingRules.Offspring>();
            if (outcomes.Count > 0 && this.selectedCrystalColor is string crystalColor)
                outcomes = new[] { new BreedingRules.Offspring($"{crystalColor} Slime", null, FarmAnimalContent.GetReferenceHex(crystalColor)) };
            if (outcomes.Count == 0)
            {
                resultLine = "This pairing is not eligible.";
                abilityLine = "Rare and exceptional slimes cannot breed.";
            }
            else
            {
                var groups = outcomes.GroupBy(outcome => (outcome.PrimarySpecies, outcome.SecondarySpecies)).ToArray();
                resultLine = "Offspring: " + string.Join("; ", groups.Select(group =>
                    $"{group.Count() * 100 / outcomes.Count}% {BreedingRules.Describe(group.First())}"));
                abilityLine = "Abilities: " + string.Join("; ", groups.Select(group =>
                {
                    BreedingRules.Offspring outcome = group.First();
                    string abilities = string.Join(" + ", SlimeAbilityCatalog.GetAbilityNames(
                        outcome.PrimarySpecies, outcome.SecondarySpecies, outcome.VisualColor));
                    return $"{group.Count() * 100 / outcomes.Count}% {(abilities.Length == 0 ? "none" : abilities)}";
                }));
            }
        }

        DrawFittedText(b, resultLine, new Rectangle(panel.X + 6, panel.Y + 3, panel.Width - 12, 16));
        DrawFittedText(b, abilityLine, new Rectangle(panel.X + 6, panel.Y + 19, panel.Width - 12, 16));
    }

    private void DrawRecipesTab(SpriteBatch b)
    {
        Rectangle panel = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 126, this.width - 64, 420);
        DrawPanel(b, panel, new Color(35, 49, 96), new Color(95, 137, 205));
        b.DrawString(Game1.dialogueFont, "OFFSPRING TRAITS", new Vector2(panel.X + 30, panel.Y + 26), new Color(130, 231, 255));
        string[] lines = { "Colour: inherited from the parents, unless a Chromatic Crystal is used.", "Abilities follow the offspring species and colour.", "Other offspring traits start at Normal, Common, and non-shiny, with no mutation.", "A finished egg keeps its generated genome when you close or reopen the menu." };
        for (int i = 0; i < lines.Length; i++)
            b.DrawString(Game1.smallFont, lines[i], new Vector2(panel.X + 32, panel.Y + 96 + i * 58), Color.White);
    }

    private void DrawSettingsTab(SpriteBatch b)
    {
        Rectangle panel = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 126, this.width - 64, 420);
        DrawPanel(b, panel, new Color(35, 49, 96), new Color(95, 137, 205));
        b.DrawString(Game1.dialogueFont, "BREEDING SETTINGS", new Vector2(panel.X + 30, panel.Y + 26), new Color(130, 231, 255));
        b.DrawString(Game1.smallFont, "Breeding cycle", new Vector2(panel.X + 36, panel.Y + 104), Color.White);
        b.DrawString(Game1.dialogueFont, $"{this.cycleDurationDays} Days", new Vector2(panel.X + 36, panel.Y + 137), new Color(249, 210, 104));
        b.DrawString(Game1.smallFont, "Breeding mode", new Vector2(panel.X + 36, panel.Y + 215), Color.White);
        b.DrawString(Game1.smallFont, "Normal · inherited traits with a chance of variation", new Vector2(panel.X + 36, panel.Y + 250), Color.White);
        b.DrawString(Game1.smallFont, "Colour focus (consumes one Chromatic Crystal)", new Vector2(panel.X + 36, panel.Y + 320), Color.White);
        Rectangle selector = this.crystalSelector;
        DrawPanel(b, selector, new Color(46, 67, 121), new Color(103, 145, 212));
        string value = this.selectedCrystalColor is null ? "Inherited colour · click to select crystal" : $"{this.selectedCrystalColor} · click to change";
        DrawCentered(b, value, Game1.smallFont, selector, Color.White);
    }

    private int GetCycleDuration(BreedingPairRecord? pair) => pair is not null
        && pair.CycleStartedOnDay >= 0
        && pair.CycleReadyOnDay > pair.CycleStartedOnDay
            ? pair.CycleReadyOnDay - pair.CycleStartedOnDay
            : this.cycleDurationDays;

    private void DrawTabs(SpriteBatch b)
    {
        string[] labels = { "BREEDING", "RECIPES", "SETTINGS" };
        int[] widths = { 300, 300, 300 };
        int x = this.xPositionOnScreen + 32;
        for (int i = 0; i < labels.Length; i++)
        {
            Rectangle tab = new(x + i * 312, this.yPositionOnScreen + 82, widths[i], 36);
            bool active = this.activeTab == labels[i];
            DrawPanel(b, tab, active ? new Color(201, 143, 61) : new Color(63, 72, 128), active ? new Color(255, 204, 100) : new Color(112, 132, 191));
            DrawCentered(b, labels[i], Game1.smallFont, tab, active ? new Color(48, 41, 72) : Color.White);
        }
    }

    private bool TrySelectTab(int x, int y)
    {
        if (y < this.yPositionOnScreen + 82 || y >= this.yPositionOnScreen + 118)
            return false;
        if (x >= this.xPositionOnScreen + 32 && x < this.xPositionOnScreen + 332) this.activeTab = "BREEDING";
        else if (x >= this.xPositionOnScreen + 344 && x < this.xPositionOnScreen + 644) this.activeTab = "RECIPES";
        else if (x >= this.xPositionOnScreen + 656 && x < this.xPositionOnScreen + 956) this.activeTab = "SETTINGS";
        else return false;
        return true;
    }

    private static void DrawPanel(SpriteBatch b, Rectangle rect, Color fill, Color border)
    {
        b.Draw(Game1.staminaRect, rect, border);
        if (rect.Width > 4 && rect.Height > 4)
            b.Draw(Game1.staminaRect, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), fill);
    }

    private static void DrawCentered(SpriteBatch b, string text, SpriteFont font, Rectangle rect, Color color)
    {
        Vector2 measured = font.MeasureString(text);
        float scale = Math.Min(1f, (rect.Width - 12) / Math.Max(1f, measured.X));
        b.DrawString(font, text, new Vector2(rect.Center.X - measured.X * scale / 2f, rect.Center.Y - measured.Y * scale / 2f), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private static void DrawProgress(SpriteBatch b, Rectangle bounds, BreedingPairRecord? pair)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(19, 29, 61));
        int duration = pair is null ? 0 : pair.CycleReadyOnDay - pair.CycleStartedOnDay;
        float progress = pair is null || pair.CycleReadyOnDay <= 0 || duration <= 0
            ? 0f
            : Math.Clamp(1f - (pair.CycleReadyOnDay - Game1.Date.TotalDays) / (float)duration, 0f, 1f);
        int width = (int)(bounds.Width * progress);
        if (width > 0) b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), new Color(255, 79, 159));
    }

    private void DrawChamber(SpriteBatch b, Rectangle pod)
    {
        Rectangle chamber = new(pod.X + 18, pod.Y + 18, pod.Width - 36, pod.Height - 116);
        DrawPanel(b, chamber, new Color(27, 39, 83), new Color(84, 131, 205));
        b.DrawString(Game1.smallFont, "BREEDING POD", new Vector2(chamber.X + 18, chamber.Y + 12), new Color(130, 231, 255));
        Rectangle glass = new(chamber.X + 18, chamber.Y + 44, chamber.Width - 36, chamber.Height - 60);
        DrawPanel(b, glass, new Color(36, 62, 109), new Color(138, 177, 224));
        // Metal rails, control lamps and a central divider echo the reference machine silhouette.
        b.Draw(Game1.staminaRect, new Rectangle(glass.X + 8, glass.Bottom - 18, glass.Width - 16, 12), new Color(87, 101, 144));
        b.Draw(Game1.staminaRect, new Rectangle(glass.Center.X - 3, glass.Y + 8, 6, glass.Height - 28), new Color(91, 103, 162));
        b.Draw(Game1.staminaRect, new Rectangle(glass.Center.X - 18, glass.Y + 12, 36, 12), new Color(232, 112, 180));
        BreedingPairRecord? currentPair = this.registry.GetBreedingPair(this.machineKey);
        if (currentPair?.OutputEggItemId is not null)
        {
            b.DrawString(Game1.dialogueFont, "♥", new Vector2(glass.Center.X - 20, glass.Y + 20), new Color(255, 116, 178));
            b.DrawString(Game1.dialogueFont, "EGG READY", new Vector2(glass.Center.X - 88, glass.Center.Y + 8), new Color(255, 224, 140));
            b.DrawString(Game1.smallFont, "Parents returned to the Daycare.", new Vector2(glass.Center.X - 150, glass.Center.Y + 48), Color.White);
            return;
        }

        FarmAnimal[] parents = this.candidates.Where(animal => this.selected.Contains(animal.myID.Value)).Take(2).ToArray();
        if (currentPair is not null && parents.Length == 0)
            parents = new[] { currentPair.ParentAId, currentPair.ParentBId }
                .Select(id => this.candidates.FirstOrDefault(animal => animal.myID.Value == id))
                .Where(animal => animal is not null).Cast<FarmAnimal>().ToArray();
        if (parents.Length == 0)
        {
            b.DrawString(Game1.smallFont, "SELECT TWO PARENTS", new Vector2(glass.Center.X - 112, glass.Center.Y - 10), Color.White);
            b.DrawString(Game1.tinyFont, "Choose slimes from the selector panel", new Vector2(glass.Center.X - 140, glass.Center.Y + 24), new Color(185, 208, 246));
            return;
        }

        double time = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        for (int i = 0; i < parents.Length; i++)
        {
            FarmAnimal parent = parents[i];
            int centerX = glass.X + (i == 0 ? glass.Width / 4 : glass.Width * 3 / 4);
            float bob = (float)Math.Sin(time * 2.4 + i * Math.PI) * 3f;
            Rectangle spriteBounds = new(centerX - 42, glass.Y + 42 + (int)bob, 84, 104);
            b.Draw(parent.Sprite.Texture, spriteBounds, parent.Sprite.SourceRect, Color.White);
            string name = parent.Name;
            float nameWidth = Game1.smallFont.MeasureString(name).X;
            b.DrawString(Game1.smallFont, name, new Vector2(centerX - nameWidth / 2, glass.Bottom - 43), Color.White);

            var species = this.registry.GetGenomeColorState(parent.myID.Value);
            string speciesName = species?.SecondarySpecies is string secondary
                ? $"{species.Value.PrimarySpecies} / {secondary}"
                : species?.PrimarySpecies ?? "Ranch Slime";
            string size = this.registry.GetGenomeSize(parent.myID.Value) ?? "Normal";
            string shiny = this.registry.GetGenomeIsShiny(parent.myID.Value) ? " · Shiny" : "";
            string mutation = this.registry.GetMutationTrait(parent.myID.Value) is string trait ? $" · {trait}" : "";
            DrawCenteredFittedText(b, speciesName, centerX, glass.Bottom - 23);
            string traits = $"{size}{shiny}{mutation}";
            DrawCenteredFittedText(b, traits, centerX, glass.Bottom - 8);
        }
        b.DrawString(Game1.dialogueFont, "♥", new Vector2(glass.Center.X - 18, glass.Y + 22), new Color(255, 105, 174));
    }

    private static void DrawCenteredFittedText(SpriteBatch batch, string text, int centerX, int y)
    {
        Vector2 measured = Game1.tinyFont.MeasureString(text);
        float scale = Math.Min(1f, 148f / Math.Max(1f, measured.X));
        batch.DrawString(Game1.tinyFont, text,
            new Vector2(centerX - measured.X * scale / 2f, y), Game1.textColor,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private static void DrawFittedText(SpriteBatch batch, string text, Rectangle bounds)
    {
        Vector2 measured = Game1.tinyFont.MeasureString(text);
        float scale = Math.Min(1f, Math.Min((bounds.Width - 8) / Math.Max(1f, measured.X), (bounds.Height - 2) / Math.Max(1f, measured.Y)));
        batch.DrawString(Game1.tinyFont, text,
            new Vector2(bounds.Center.X - measured.X * scale / 2f, bounds.Center.Y - measured.Y * scale / 2f), Game1.textColor,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private static string FormatCandidateGenome(SlimeGenome genome, bool canBreed)
    {
        string primary = RemoveSlimeSuffix(genome.PrimarySpecies);
        string species = genome.SecondarySpecies is string secondary
            ? $"{primary}/{RemoveSlimeSuffix(secondary)} Hybrid"
            : genome.PrimarySpecies;
        if (!canBreed)
            return $"Cannot breed - {species}";

        IReadOnlyList<string> abilities = SlimeAbilityCatalog.GetAbilityNames(
            genome.PrimarySpecies, genome.SecondarySpecies, genome.VisualColor);
        return abilities.Count == 0 ? $"{species} - no ability" : $"{species} - {string.Join(" + ", abilities)}";
    }

    private static string RemoveSlimeSuffix(string species) =>
        species.EndsWith(" Slime", StringComparison.OrdinalIgnoreCase) ? species[..^6] : species;

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

        if (!this.registry.StartBreedingCycle(this.machineKey, Game1.Date.TotalDays, this.cycleDurationDays, this.selectedCrystalColor))
            return false;

        bool firstBreedingAlreadyRecorded = this.registry.IsSanctuaryMilestoneCompleted(SanctuaryQuestProgression.FirstBreedingStarted);
        bool recordedFirstBreeding = this.registry.TryCompleteSanctuaryMilestone(SanctuaryQuestProgression.FirstBreedingStarted);

        if (crystalItem is not null)
        {
            crystalItem.Stack--;
            if (crystalItem.Stack <= 0)
                Game1.player.Items[crystalSlot] = null;
        }

        if (this.saveExternalState())
            return true;

        this.registry.CancelBreedingCycle(this.machineKey);
        if (recordedFirstBreeding && !firstBreedingAlreadyRecorded)
            this.registry.RemoveSanctuaryMilestone(SanctuaryQuestProgression.FirstBreedingStarted);
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
            string? parentAName = this.candidates.FirstOrDefault(animal => animal.myID.Value == pair.ParentAId)?.Name;
            string? parentBName = this.candidates.FirstOrDefault(animal => animal.myID.Value == pair.ParentBId)?.Name;
            SlimeEggRecord? egg = this.registry.GetOrCreateBreedingEgg(
                this.machineKey, Game1.Date.TotalDays, parentAName, parentBName, out _);
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
