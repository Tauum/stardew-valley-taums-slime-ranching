using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Starts a fixed seven-day hatch job from an eligible egg in the player's inventory.</summary>
internal sealed class SlimeIncubatorMenu : IClickableMenu
{
    private const int GridColumns = 3;
    private const int VisibleEggs = 6;
    private const string EggItemPrefix = "(O)Taum.SlimeRanching_Egg_";

    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly Func<int, bool> startIncubation;
    private readonly Func<string, bool> takeHatchedSlime;
    private readonly Action requestIncubationCancellation;
    private readonly Func<string, bool> contributeSlime;
    private readonly Func<string, bool> addGoldCatalyst;
    private readonly Texture2D incubatorTexture;
    private readonly AnimalHouse? daycare;
    private readonly FarmAnimal? hatchedAnimal;
    private readonly List<(int InventoryIndex, string Name, int Stack)> candidates;
    private readonly int ineligibleEggCount;
    private readonly int listTop;
    private readonly int listBottom;
    private readonly Rectangle actionButton;
    private readonly Rectangle contributionButton;
    private readonly Rectangle catalystButton;
    private int firstVisible;
    private int? selectedInventoryIndex;
    private string? statusMessage;

    public SlimeIncubatorMenu(SlimeRegistry registry, string machineKey, Func<int, bool> startIncubation, Func<string, bool> takeHatchedSlime, Action requestIncubationCancellation, Func<string, bool> contributeSlime, Func<string, bool> addGoldCatalyst, Texture2D incubatorTexture)
        : base(Game1.uiViewport.Width / 2 - 450, Game1.uiViewport.Height / 2 - 280, 900, 560)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.startIncubation = startIncubation;
        this.takeHatchedSlime = takeHatchedSlime;
        this.requestIncubationCancellation = requestIncubationCancellation;
        this.contributeSlime = contributeSlime;
        this.addGoldCatalyst = addGoldCatalyst;
        this.incubatorTexture = incubatorTexture;
        this.daycare = Game1.currentLocation as AnimalHouse;
        if (registry.GetIncubation(machineKey)?.HatchAnimalId is long animalId
            && this.daycare is AnimalHouse animalHouse)
            this.hatchedAnimal = animalHouse.animals.Values.FirstOrDefault(animal => animal.myID.Value == animalId);
        this.listTop = this.yPositionOnScreen + 166;
        this.listBottom = this.listTop + 184;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 710, this.yPositionOnScreen + this.height - 88, 148, 58);
        this.contributionButton = new Rectangle(this.xPositionOnScreen + 400, this.yPositionOnScreen + this.height - 88, 142, 58);
        this.catalystButton = new Rectangle(this.xPositionOnScreen + 550, this.yPositionOnScreen + this.height - 88, 145, 58);
        int ineligibleEggCount = 0;
        this.candidates = Game1.player.Items
            .Select((item, index) => (item, index))
            .Where(pair =>
            {
                Item? item = pair.item;
                if (item is null
                    || !(item.QualifiedItemId.StartsWith(EggItemPrefix, StringComparison.Ordinal)
                        || item.modData.ContainsKey(SlimeEggContent.EggIdModDataKey)))
                    return false;

                if (!SlimeEggContent.TryGetEggId(item, out string eggId))
                {
                    ineligibleEggCount++;
                    return false;
                }

                if (SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
                    && !registry.RegisterEggItem(egg))
                {
                    ineligibleEggCount++;
                    return false;
                }

                bool eligible = registry.CanIncubate(eggId);
                if (!eligible)
                    ineligibleEggCount++;
                return eligible;
            })
            .Select(pair => (pair.index, pair.item!.DisplayName, pair.item.Stack))
            .ToList();
        this.ineligibleEggCount = ineligibleEggCount;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (mouseX < this.xPositionOnScreen + 392 || mouseX > this.xPositionOnScreen + 868
            || mouseY < this.listTop || mouseY >= this.listBottom)
            return;

        this.firstVisible = Math.Clamp(this.firstVisible + (direction < 0 ? 1 : -1),
            0, Math.Max(0, this.candidates.Count - VisibleEggs));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        IncubationRecord? active = this.registry.GetIncubation(this.machineKey);
        if (active?.HatchAnimalId is not null && this.actionButton.Contains(x, y))
        {
            string suggestedName = this.hatchedAnimal?.Name ?? "Domesticated Slime";
            Game1.activeClickableMenu = new NamingMenu(name =>
            {
                if (!this.takeHatchedSlime(name))
                {
                    this.statusMessage = "Could not save the name; the slime remains ready in the incubator.";
                    Game1.activeClickableMenu = this;
                    Game1.playSound("cancel");
                }
                else
                    Game1.playSound("smallSelect");
            }, "Name your new slime", suggestedName);
            return;
        }

        if (active?.HatchAnimalId is null && this.catalystButton.Contains(x, y))
        {
            RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
            if (lab is not { IsUnlocked: true })
                this.statusMessage = "The Rare Lab unlocks after the first Daycare upgrade.";
            else if (lab.CatalystSpecies is not null)
                this.statusMessage = "This Rare Lab already holds a Gold catalyst.";
            else if (active is { RareOutcomeResolved: true })
                this.statusMessage = "This egg's outcome is fixed; load a catalyst for the next egg.";
            else if (active is not null && Game1.Date.TotalDays >= active.ReadyOnDay)
                this.statusMessage = "This egg is ready; its Rare Lab result will resolve before hatching.";
            else if (CountInventoryItem(RareLabRules.GoldBarQualifiedItemId) < RareLabRules.GoldBarsPerCatalyst)
                this.statusMessage = $"Bring {RareLabRules.GoldBarsPerCatalyst} Gold Bars to load a Gold catalyst.";
            else if (this.addGoldCatalyst(this.machineKey))
                this.statusMessage = "Gold catalyst loaded; retained on failed roll, used on success.";
            else
                this.statusMessage = "Could not save the Gold catalyst; your bars were restored.";
            Game1.playSound(this.statusMessage.StartsWith("Gold catalyst loaded", StringComparison.Ordinal) ? "coin" : "cancel");
            return;
        }

        if (active is { HatchAnimalId: null } && this.contributionButton.Contains(x, y))
        {
            RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
            if (lab is not { IsUnlocked: true })
                this.statusMessage = "The Rare Lab unlocks after the first Daycare upgrade.";
            else if (Game1.Date.TotalDays >= active.ReadyOnDay)
                this.statusMessage = "This egg's incubation is complete; no more Slime can be contributed.";
            else if (this.registry.GetRareLabChancePoints(this.machineKey) >= RareLabRules.MaximumChancePoints)
                this.statusMessage = "This Rare Lab chance is already at 100%.";
            else if (CountVanillaSlimeItems() < RareLabRules.SlimeUnitsPerContributionPoint)
                this.statusMessage = $"Bring {RareLabRules.SlimeUnitsPerContributionPoint} vanilla Slime to contribute.";
            else if (this.contributeSlime(this.machineKey))
                this.statusMessage = $"Contributed {RareLabRules.SlimeUnitsPerContributionPoint} Slime. Rare Lab chance is now {this.registry.GetRareLabChancePoints(this.machineKey)}%.";
            else
                this.statusMessage = "Could not save the contribution; your Slime was restored.";
            Game1.playSound(this.statusMessage.StartsWith("Contributed", StringComparison.Ordinal) ? "coin" : "cancel");
            return;
        }

        if (active?.HatchAnimalId is null && active is not null && this.actionButton.Contains(x, y))
        {
            if (active.RareOutcomeResolved)
            {
                this.statusMessage = "This egg's outcome is fixed; make room in the Daycare for it to hatch.";
                Game1.playSound("cancel");
            }
            else
            {
                this.requestIncubationCancellation();
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (active is not null)
            return;

        if (this.actionButton.Contains(x, y))
        {
            if (this.selectedInventoryIndex is not int inventoryIndex)
                this.statusMessage = "Select an egg first.";
            else if (this.startIncubation(inventoryIndex))
            {
                this.statusMessage = $"Egg inserted. It will hatch in {IncubatorRules.CycleDurationDays} in-game days.";
                this.selectedInventoryIndex = null;
            }
            else
                this.statusMessage = "Could not start incubation; the egg remains in your inventory.";
            Game1.playSound(this.statusMessage.StartsWith("Egg inserted", StringComparison.Ordinal) ? "smallSelect" : "cancel");
            return;
        }

        if (x < this.xPositionOnScreen + 392 || x > this.xPositionOnScreen + 868
            || y < this.listTop || y >= this.listBottom)
            return;

        int column = Math.Clamp((x - (this.xPositionOnScreen + 400)) / 151, 0, GridColumns - 1);
        int row = (y - this.listTop) / 92;
        int visibleIndex = row * GridColumns + column;
        int index = this.firstVisible + visibleIndex;
        if (visibleIndex < 0 || visibleIndex >= VisibleEggs || index >= this.candidates.Count)
            return;

        this.selectedInventoryIndex = this.candidates[index].InventoryIndex;
        this.statusMessage = $"Selected {this.candidates[index].Name}. Press Start to begin incubation.";
        Game1.playSound("smallSelect");
    }

    public override void draw(SpriteBatch b)
    {
        Rectangle outer = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        DrawPanel(b, outer, new Color(37, 37, 72), new Color(135, 153, 222));
        Rectangle header = new(outer.X + 10, outer.Y + 10, outer.Width - 20, 48);
        DrawPanel(b, header, new Color(30, 77, 164), new Color(114, 173, 255));
        IncubationRecord? active = this.registry.GetIncubation(this.machineKey);
        b.DrawString(Game1.dialogueFont, active is null ? "STASIS INCUBATOR" : "SLIME INCUBATOR", new Vector2(header.X + 18, header.Y + 7), new Color(185, 239, 255));
        Rectangle machineFrame = new(this.xPositionOnScreen + 22, this.yPositionOnScreen + 72, 340, 408);
        DrawPanel(b, machineFrame, new Color(61, 54, 91), new Color(131, 150, 215));
        Rectangle machineArea = new(machineFrame.X + 12, machineFrame.Y + 12, machineFrame.Width - 24, machineFrame.Height - 24);
        b.Draw(this.incubatorTexture, machineArea, new Rectangle(0, 0, this.incubatorTexture.Width, this.incubatorTexture.Height), Color.White);

        Rectangle panel = new(this.xPositionOnScreen + 378, this.yPositionOnScreen + 72, 500, 408);
        DrawPanel(b, panel, new Color(43, 51, 105), new Color(112, 143, 214));
        if (active is not null)
        {
            int daysLeft = Math.Max(0, active.ReadyOnDay - Game1.Date.TotalDays);
            int duration = Math.Max(1, active.ReadyOnDay - active.StartedOnDay);
            int elapsed = Math.Clamp(Game1.Date.TotalDays - active.StartedOnDay, 0, duration);
            float progress = (float)elapsed / duration;
            string eggName = "Domesticated Slime Egg";
            if (this.registry.TryGetEgg(active.EggItemId, out SlimeEggRecord egg))
            {
                SlimeGenome displayGenome = active.HatchGenome ?? egg.Genome;
                string species = displayGenome.SecondarySpecies is string secondary
                    ? $"{displayGenome.PrimarySpecies} / {secondary} Hybrid"
                    : displayGenome.PrimarySpecies;
                eggName = $"{species} Egg · {displayGenome.RarityTier} · Gen {displayGenome.Generation}";
            }

            bool hatched = active.HatchAnimalId is not null;
            b.DrawString(Game1.smallFont, hatched ? "HATCHED!" : "HATCHING...", new Vector2(panel.X + 24, panel.Y + 22), new Color(130, 231, 255));
            if (hatched)
            {
                DrawPanel(b, new Rectangle(panel.X + 18, panel.Y + 62, panel.Width - 36, 236), new Color(198, 215, 245), new Color(235, 204, 255));
                DrawCentered(b, "A new slime has hatched!", Game1.dialogueFont, new Rectangle(panel.X + 32, panel.Y + 82, panel.Width - 64, 50), new Color(42, 50, 88));
                if (this.hatchedAnimal is not null)
                    b.Draw(this.hatchedAnimal.Sprite.Texture, new Rectangle(panel.Center.X - 44, panel.Y + 132, 88, 82), this.hatchedAnimal.Sprite.SourceRect, Color.White);
                DrawCentered(b, "♥   ♥   ♥", Game1.dialogueFont, new Rectangle(panel.X + 72, panel.Y + 211, panel.Width - 144, 38), new Color(238, 91, 157));
                DrawCentered(b, eggName, Game1.smallFont, new Rectangle(panel.X + 24, panel.Y + 252, panel.Width - 48, 34), new Color(42, 50, 88));
                DrawPanel(b, this.actionButton, new Color(46, 153, 78), new Color(145, 239, 134));
                DrawCentered(b, "NAME & TAKE", Game1.smallFont, this.actionButton, Color.White);
            }
            else
            {
                DrawPanel(b, new Rectangle(panel.X + 18, panel.Y + 62, panel.Width - 36, 236), new Color(48, 65, 128), new Color(105, 140, 209));
                DrawCentered(b, eggName, Game1.dialogueFont, new Rectangle(panel.X + 30, panel.Y + 82, panel.Width - 60, 54), Color.White);
                bool daycareIsFull = this.daycare?.isFull() ?? false;
                string hatchStatus = daysLeft > 0
                    ? $"{daysLeft} Day{(daysLeft == 1 ? "" : "s")} Left"
                    : daycareIsFull ? "Waiting for Daycare space" : "Ready to hatch";
                DrawCentered(b, hatchStatus, Game1.smallFont, new Rectangle(panel.X + 30, panel.Y + 134, panel.Width - 60, 30), Color.White);
                if (this.daycare is not null)
                    DrawCentered(b, $"Daycare residents: {this.daycare.animalsThatLiveHere.Count}/{this.daycare.animalLimit.Value}",
                        Game1.tinyFont, new Rectangle(panel.X + 30, panel.Y + 164, panel.Width - 60, 24), new Color(198, 219, 255));
                DrawProgress(b, new Rectangle(panel.X + 32, panel.Y + 194, panel.Width - 64, 28), progress);
                DrawCentered(b, $"{elapsed} / {duration} days  ({(int)(progress * 100)}%)", Game1.smallFont, new Rectangle(panel.X + 25, panel.Y + 232, panel.Width - 50, 34), Color.White);
                DrawPanel(b, this.contributionButton,
                    this.CanContribute(active) ? new Color(39, 135, 105) : new Color(65, 73, 103),
                    this.CanContribute(active) ? new Color(118, 228, 183) : new Color(110, 122, 160));
                DrawCentered(b, this.GetContributionButtonLabel(active), Game1.tinyFont, this.contributionButton, Color.White);
                DrawPanel(b, this.catalystButton,
                    this.CanAddGoldCatalyst(active) ? new Color(152, 111, 40) : new Color(75, 74, 74),
                    this.CanAddGoldCatalyst(active) ? new Color(255, 211, 95) : new Color(135, 131, 120));
                DrawCentered(b, this.GetGoldCatalystButtonLabel(active), Game1.tinyFont, this.catalystButton, Color.White);
                DrawPanel(b, this.actionButton,
                    active.RareOutcomeResolved ? new Color(65, 79, 105) : new Color(146, 63, 66),
                    active.RareOutcomeResolved ? new Color(117, 131, 159) : new Color(242, 142, 137));
                DrawCentered(b, active.RareOutcomeResolved ? "OUTCOME FIXED" : "CANCEL", Game1.tinyFont, this.actionButton, Color.White);
            }
        }
        else if (this.candidates.Count == 0)
        {
            b.DrawString(Game1.smallFont, "SELECT EGG", new Vector2(panel.X + 24, panel.Y + 22), new Color(130, 231, 255));
            Rectangle emptyState = new(panel.X + 20, panel.Y + 88, panel.Width - 40, 112);
            if (this.ineligibleEggCount == 0)
                DrawCentered(b, "No eligible eggs in your inventory.", Game1.smallFont, emptyState, Color.White);
            else
            {
                DrawCentered(b, "No eligible eggs in your inventory.", Game1.smallFont,
                    new Rectangle(emptyState.X, emptyState.Y, emptyState.Width, 48), Color.White);
                string eggCount = this.ineligibleEggCount == 1
                    ? "1 Domesticated Slime Egg copy is used or unavailable."
                    : $"{this.ineligibleEggCount} Domesticated Slime Egg copies are used or unavailable.";
                DrawCentered(b, eggCount, Game1.tinyFont,
                    new Rectangle(emptyState.X, emptyState.Y + 48, emptyState.Width, 40), new Color(210, 220, 245));
            }
            DrawIncubationControls(b, panel);
            this.DrawGoldCatalystButton(b);
        }
        else
        {
            b.DrawString(Game1.smallFont, "SELECT EGG", new Vector2(panel.X + 24, panel.Y + 22), new Color(130, 231, 255));
            int end = Math.Min(this.firstVisible + VisibleEggs, this.candidates.Count);
            for (int i = this.firstVisible; i < end; i++)
            {
                int local = i - this.firstVisible;
                int column = local % GridColumns;
                int row = local / GridColumns;
                Rectangle card = new(panel.X + 22 + column * 151, this.listTop + row * 92, 140, 82);
                var candidate = this.candidates[i];
                bool selected = this.selectedInventoryIndex == candidate.InventoryIndex;
                DrawPanel(b, card, selected ? new Color(80, 117, 172) : new Color(48, 63, 123), selected ? new Color(156, 223, 255) : new Color(99, 135, 206));
                DrawCentered(b, candidate.Name, Game1.tinyFont, new Rectangle(card.X + 6, card.Y + 12, card.Width - 12, 38), Color.White);
                DrawCentered(b, $"× {candidate.Stack}", Game1.tinyFont, new Rectangle(card.X + 8, card.Bottom - 27, card.Width - 16, 20), new Color(180, 224, 255));
            }
            if (this.candidates.Count > VisibleEggs)
                b.DrawString(Game1.tinyFont, $"Scroll · {this.firstVisible + 1}-{end} / {this.candidates.Count}", new Vector2(panel.X + 178, this.listBottom + 6), Color.White);
            DrawIncubationControls(b, panel);
            this.DrawGoldCatalystButton(b);
        }

        this.DrawRareLabMeter(b, panel, this.registry.GetRareLab(this.machineKey));
        if (this.statusMessage is not null)
            b.DrawString(Game1.tinyFont, this.statusMessage.Length > 72 ? this.statusMessage[..69] + "..." : this.statusMessage,
                new Vector2(this.xPositionOnScreen + 32, this.yPositionOnScreen + this.height - 22), Color.White);
        b.DrawString(Game1.tinyFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 90, this.yPositionOnScreen + this.height - 22), Color.White);
        this.drawMouse(b);
    }

    private void DrawIncubationControls(SpriteBatch b, Rectangle panel)
    {
        b.DrawString(Game1.smallFont, "INCUBATION TIME", new Vector2(panel.X + 24, panel.Y + 294), Color.White);
        DrawCentered(b, $"{IncubatorRules.CycleDurationDays} Days", Game1.dialogueFont, new Rectangle(panel.X + 160, panel.Y + 282, 135, 44), new Color(188, 235, 255));
        DrawProgress(b, new Rectangle(panel.X + 305, panel.Y + 300, 160, 14), 0f);
        DrawPanel(b, this.actionButton, this.selectedInventoryIndex is null ? new Color(65, 79, 105) : new Color(43, 151, 73), this.selectedInventoryIndex is null ? new Color(117, 131, 159) : new Color(139, 240, 127));
        DrawCentered(b, "▶  START", Game1.smallFont, this.actionButton, Color.White);
    }

    private bool CanContribute(IncubationRecord active)
    {
        RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
        return !active.RareOutcomeResolved
            && active.HatchAnimalId is null
            && Game1.Date.TotalDays < active.ReadyOnDay
            && lab is { IsUnlocked: true }
            && this.registry.GetRareLabChancePoints(this.machineKey) < RareLabRules.MaximumChancePoints
            && CountVanillaSlimeItems() >= RareLabRules.SlimeUnitsPerContributionPoint;
    }

    private bool CanAddGoldCatalyst(IncubationRecord? active)
    {
        RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
        return lab is { IsUnlocked: true, CatalystSpecies: null }
            && (active is null || (active.HatchAnimalId is null && !active.RareOutcomeResolved && Game1.Date.TotalDays < active.ReadyOnDay))
            && CountInventoryItem(RareLabRules.GoldBarQualifiedItemId) >= RareLabRules.GoldBarsPerCatalyst;
    }

    private string GetGoldCatalystButtonLabel(IncubationRecord? active)
    {
        RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
        if (lab is not { IsUnlocked: true })
            return "RARE LAB LOCKED";
        if (lab.CatalystSpecies is not null)
            return "GOLD HELD";
        if (active is { RareOutcomeResolved: true })
            return "OUTCOME FIXED";
        if (active is not null && Game1.Date.TotalDays >= active.ReadyOnDay)
            return "INCUBATION DONE";
        if (CountInventoryItem(RareLabRules.GoldBarQualifiedItemId) < RareLabRules.GoldBarsPerCatalyst)
            return $"NEED {RareLabRules.GoldBarsPerCatalyst} GOLD";
        return $"ADD {RareLabRules.GoldBarsPerCatalyst} GOLD";
    }

    private void DrawGoldCatalystButton(SpriteBatch b)
    {
        IncubationRecord? active = this.registry.GetIncubation(this.machineKey);
        DrawPanel(b, this.catalystButton,
            this.CanAddGoldCatalyst(active) ? new Color(152, 111, 40) : new Color(75, 74, 74),
            this.CanAddGoldCatalyst(active) ? new Color(255, 211, 95) : new Color(135, 131, 120));
        DrawCentered(b, this.GetGoldCatalystButtonLabel(active), Game1.tinyFont, this.catalystButton, Color.White);
    }

    private string GetContributionButtonLabel(IncubationRecord active)
    {
        RareLabRecord? lab = this.registry.GetRareLab(this.machineKey);
        if (active.RareOutcomeResolved)
            return "RARE OUTCOME FIXED";
        if (lab is not { IsUnlocked: true })
            return "RARE LAB LOCKED";
        if (Game1.Date.TotalDays >= active.ReadyOnDay)
            return "INCUBATION COMPLETE";
        if (this.registry.GetRareLabChancePoints(this.machineKey) >= RareLabRules.MaximumChancePoints)
            return "CHANCE MAXED";
        if (CountVanillaSlimeItems() < RareLabRules.SlimeUnitsPerContributionPoint)
            return $"NEED {RareLabRules.SlimeUnitsPerContributionPoint} SLIME";
        return $"ADD {RareLabRules.SlimeUnitsPerContributionPoint} SLIME (+1%)";
    }

    private void DrawRareLabMeter(SpriteBatch b, Rectangle panel, RareLabRecord? lab)
    {
        Rectangle bounds = new(panel.X + 18, panel.Y + 316, panel.Width - 36, 70);
        DrawPanel(b, bounds, new Color(55, 72, 134), new Color(101, 138, 205));
        bool unlocked = lab is { IsUnlocked: true };
        int total = RareLabRules.GetChancePoints(lab);
        IncubationRecord? incubation = this.registry.GetIncubation(this.machineKey);
        string chanceLabel = incubation is { RareOutcomeResolved: false } ? "CURRENT EGG CHANCE" : "NEXT CHANCE";
        string outcomeText = incubation is not { RareOutcomeResolved: true }
            ? "RARE LAB"
            : incubation.RareOffspringProduced
                ? $"RESULT FIXED · {incubation.HatchGenome?.PrimarySpecies ?? "RARE"}"
                : "RESULT FIXED · REGULAR";
        b.DrawString(Game1.tinyFont,
            unlocked ? $"{outcomeText} · {chanceLabel} {total}%" : "RARE LAB LOCKED · TOTAL 0%",
            new Vector2(bounds.X + 12, bounds.Y + 4), new Color(213, 227, 255));

        Rectangle bar = new(bounds.X + 12, bounds.Y + 19, bounds.Width - 24, 12);
        b.Draw(Game1.staminaRect, bar, new Color(18, 30, 62));
        if (unlocked && lab is not null)
        {
            int cursor = 0;
            DrawChanceSegment(b, bar, cursor, lab.BaseChancePoints, new Color(83, 207, 255));
            cursor += lab.BaseChancePoints;
            DrawChanceSegment(b, bar, cursor, lab.PityPoints, new Color(255, 198, 91));
            cursor += lab.PityPoints;
            DrawChanceSegment(b, bar, cursor, lab.ContributionPoints, new Color(200, 150, 255));
        }

        string detail = unlocked && lab is not null
            ? $"Base {lab.BaseChancePoints}%  ·  Pity +{lab.PityPoints}%  ·  You +{lab.ContributionPoints}%"
            : "Unlocks after the first Daycare upgrade.";
        b.DrawString(Game1.tinyFont, detail, new Vector2(bounds.X + 12, bounds.Y + 34), Color.White);
        string catalystDetail = lab is not { IsUnlocked: true }
            ? "Catalyst: unlocks after the first Daycare upgrade"
            : lab.CatalystSpecies is not null
                ? $"Catalyst: {lab.CatalystSpecies} (kept if roll fails)"
                : $"Catalyst: none · {RareLabRules.GoldBarsPerCatalyst} Gold Bars loads Gold";
        b.DrawString(Game1.tinyFont, catalystDetail, new Vector2(bounds.X + 12, bounds.Y + 50), new Color(255, 223, 158));
    }

    private static void DrawChanceSegment(SpriteBatch b, Rectangle bar, int startPoints, int points, Color color)
    {
        if (points <= 0)
            return;

        int availableWidth = bar.Width - 4;
        int left = bar.X + 2 + availableWidth * startPoints / RareLabRules.MaximumChancePoints;
        int right = bar.X + 2 + availableWidth * Math.Min(RareLabRules.MaximumChancePoints, startPoints + points) / RareLabRules.MaximumChancePoints;
        if (right <= left)
            right = left + 1;
        right = Math.Min(right, bar.Right - 2);
        if (right > left)
            b.Draw(Game1.staminaRect, new Rectangle(left, bar.Y + 2, right - left, bar.Height - 4), color);
    }

    private static int CountVanillaSlimeItems() => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == "(O)766")
        .Sum(item => item!.Stack);

    private static int CountInventoryItem(string qualifiedItemId) => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == qualifiedItemId)
        .Sum(item => item!.Stack);

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

    private static void DrawProgress(SpriteBatch b, Rectangle rect, float progress)
    {
        b.Draw(Game1.staminaRect, rect, new Color(18, 30, 62));
        int width = (int)((rect.Width - 4) * Math.Clamp(progress, 0f, 1f));
        if (width > 0)
            b.Draw(Game1.staminaRect, new Rectangle(rect.X + 2, rect.Y + 2, width, Math.Max(1, rect.Height - 4)), new Color(46, 213, 235));
    }
}
