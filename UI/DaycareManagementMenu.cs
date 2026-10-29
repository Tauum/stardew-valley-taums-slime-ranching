using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Read-only tester preview for the Registry, Progression, and Slime Wiki desk sections.</summary>
internal sealed class DaycareManagementMenu : IClickableMenu
{
    private const int RowHeight = 42;
    private const int VisibleRows = 8;
    private static readonly string[] BaseSpecies =
    {
        "Red Slime", "Yellow Slime", "Blue Slime", "Green Slime", "Orange Slime", "Purple Slime", "Pink Slime"
    };
    private static readonly string[] WikiCategories = { "BASE", "HYBRID", "RARE", "RULES", "LIFE" };
    private static readonly IReadOnlyDictionary<string, string> AbilityDirections = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Fighter"] = "May occasionally increase the chance of double monster drops.",
        ["Revealer"] = "May find artifact spots or other supported hidden clues.",
        ["Irrigator"] = "Blue's saved genome label is Irrigator. Its approved direction is an occasional double of fishing trash or treasure-chest catches only; it does not improve fish quality or irrigate crops.",
        ["Collector"] = "Green's saved genome label is Collector. Its approved direction is to randomly fertilize eligible existing crops and remove weeds. Role/name, fertilizer source/type, eligible states, timing, chance, and limits remain under review.",
        ["Nurturer"] = "May occasionally double furnace output and cooked-food output.",
        ["Prospector"] = "May occasionally double ore from mining.",
        ["Healer"] = "May very slowly restore player health and energy."
    };
    private static readonly IReadOnlyList<WikiEntry> WikiEntries = BuildWikiEntries();

    private readonly AnimalHouse daycare;
    private readonly SlimeRegistry registry;
    private readonly int buildingTier;
    private readonly Func<long, string?> getActiveActivitySummary;
    private readonly List<FarmAnimal> residents;
    private Rectangle registryRows;
    private Rectangle registryDetails;
    private Rectangle wikiRows;
    private Rectangle wikiDetails;
    private Rectangle[] sectionButtons = Array.Empty<Rectangle>();
    private Rectangle[] wikiCategoryButtons = Array.Empty<Rectangle>();
    private string activeSection = "REGISTRY";
    private string activeWikiCategory = "BASE";
    private int firstResident;
    private int selectedResidentIndex = -1;
    private int firstWikiEntry;
    private int selectedWikiIndex;
    private float registryDetailScroll;
    private float registryDetailMaxScroll;
    private float wikiDetailScroll;
    private float wikiDetailMaxScroll;

    public DaycareManagementMenu(AnimalHouse daycare, SlimeRegistry registry, int buildingTier,
        Func<long, string?> getActiveActivitySummary)
        : base(Game1.uiViewport.Width / 2 - 540, Game1.uiViewport.Height / 2 - 330, 1080, 660)
    {
        this.daycare = daycare;
        this.registry = registry;
        this.buildingTier = Math.Clamp(buildingTier, 0, 3);
        this.getActiveActivitySummary = getActiveActivitySummary;
        this.residents = daycare.animals.Values
            .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
            .OrderBy(animal => animal.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(animal => animal.myID.Value)
            .ToList();
        if (this.residents.Count > 0)
            this.selectedResidentIndex = 0;

        this.LayoutControls();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        this.xPositionOnScreen = Math.Clamp(this.xPositionOnScreen, 0,
            Math.Max(0, Game1.uiViewport.Width - this.width));
        this.yPositionOnScreen = Math.Clamp(this.yPositionOnScreen, 0,
            Math.Max(0, Game1.uiViewport.Height - this.height));
        this.LayoutControls();
    }

    private void LayoutControls()
    {
        this.sectionButtons = new[]
        {
            new Rectangle(this.xPositionOnScreen + 34, this.yPositionOnScreen + 82, 326, 48),
            new Rectangle(this.xPositionOnScreen + 377, this.yPositionOnScreen + 82, 326, 48),
            new Rectangle(this.xPositionOnScreen + 720, this.yPositionOnScreen + 82, 326, 48)
        };
        this.registryRows = new Rectangle(this.xPositionOnScreen + 48, this.yPositionOnScreen + 198, 382, 360);
        this.registryDetails = new Rectangle(this.xPositionOnScreen + 466, this.yPositionOnScreen + 146, 582, 432);
        this.wikiRows = new Rectangle(this.xPositionOnScreen + 48, this.yPositionOnScreen + 220, 382, 330);
        const int wikiCategoryGap = 6;
        int wikiCategoryWidth = (this.wikiRows.Width - (WikiCategories.Length - 1) * wikiCategoryGap) / WikiCategories.Length;
        this.wikiCategoryButtons = Enumerable.Range(0, WikiCategories.Length)
            .Select(index => new Rectangle(this.wikiRows.X + index * (wikiCategoryWidth + wikiCategoryGap),
                this.yPositionOnScreen + 166, wikiCategoryWidth, 42))
            .ToArray();
        this.wikiDetails = this.registryDetails;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        string[] sections = { "REGISTRY", "PROGRESSION", "SLIME WIKI" };
        for (int index = 0; index < this.sectionButtons.Length; index++)
        {
            if (!this.sectionButtons[index].Contains(x, y))
                continue;

            this.activeSection = sections[index];
            Game1.playSound("smallSelect");
            return;
        }

        if (this.activeSection == "REGISTRY" && this.registryRows.Contains(x, y))
        {
            int row = (y - this.registryRows.Y) / RowHeight;
            int selected = this.firstResident + row;
            if (row >= 0 && row < VisibleRows && selected < this.residents.Count)
            {
                this.selectedResidentIndex = selected;
                this.registryDetailScroll = 0;
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (this.activeSection != "SLIME WIKI")
            return;

        for (int index = 0; index < this.wikiCategoryButtons.Length; index++)
        {
            if (!this.wikiCategoryButtons[index].Contains(x, y))
                continue;

            this.activeWikiCategory = WikiCategories[index];
            this.firstWikiEntry = 0;
            this.selectedWikiIndex = 0;
            this.wikiDetailScroll = 0;
            Game1.playSound("smallSelect");
            return;
        }

        if (this.wikiRows.Contains(x, y))
        {
            int row = (y - this.wikiRows.Y) / RowHeight;
            int selected = this.firstWikiEntry + row;
            if (row >= 0 && row < VisibleRows && selected < this.GetVisibleWikiEntries().Count)
            {
                this.selectedWikiIndex = selected;
                this.wikiDetailScroll = 0;
                Game1.playSound("smallSelect");
            }
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
        if (Game1.activeClickableMenu != this)
            return;

        int sectionIndex = key switch
        {
            Keys.D1 or Keys.NumPad1 => 0,
            Keys.D2 or Keys.NumPad2 => 1,
            Keys.D3 or Keys.NumPad3 => 2,
            _ => -1
        };
        if (sectionIndex >= 0)
        {
            this.SetSection(sectionIndex);
            return;
        }

        if (key is Keys.Up or Keys.Down)
        {
            int delta = key == Keys.Up ? -1 : 1;
            this.MoveCurrentSelection(delta);
        }
        else if (key is Keys.PageUp or Keys.PageDown)
        {
            this.MoveCurrentSelectionByPage(key == Keys.PageUp ? -1 : 1);
        }
        else if (key is Keys.Home or Keys.End)
        {
            this.MoveCurrentSelectionToBoundary(key == Keys.End);
        }
        else if (key is Keys.Left or Keys.Right && this.activeSection == "SLIME WIKI")
        {
            this.ChangeWikiCategory(key == Keys.Left ? -1 : 1);
        }
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void receiveGamePadButton(Buttons button)
    {
        base.receiveGamePadButton(button);
        switch (button)
        {
            case Buttons.B:
                this.exitThisMenu();
                break;
            case Buttons.LeftTrigger:
                this.ChangeSection(-1);
                break;
            case Buttons.RightTrigger:
                this.ChangeSection(1);
                break;
            case Buttons.LeftShoulder:
                this.MoveCurrentSelectionByPage(-1);
                break;
            case Buttons.RightShoulder:
                this.MoveCurrentSelectionByPage(1);
                break;
            case Buttons.LeftThumbstickUp:
                if (!this.TryScrollCurrentDetails(-1))
                    this.MoveCurrentSelection(-1);
                break;
            case Buttons.LeftThumbstickDown:
                if (!this.TryScrollCurrentDetails(1))
                    this.MoveCurrentSelection(1);
                break;
            case Buttons.DPadUp:
                this.MoveCurrentSelection(-1);
                break;
            case Buttons.DPadDown:
                this.MoveCurrentSelection(1);
                break;
            case Buttons.DPadLeft:
                if (this.activeSection == "SLIME WIKI")
                    this.ChangeWikiCategory(-1);
                else
                    this.ChangeSection(-1);
                break;
            case Buttons.DPadRight:
                if (this.activeSection == "SLIME WIKI")
                    this.ChangeWikiCategory(1);
                else
                    this.ChangeSection(1);
                break;
        }
    }

    private bool TryScrollCurrentDetails(int direction)
    {
        if (this.activeSection == "REGISTRY" && this.registryDetailMaxScroll > 0)
        {
            float next = Math.Clamp(this.registryDetailScroll + direction * 36f, 0, this.registryDetailMaxScroll);
            if (next == this.registryDetailScroll)
                return false;

            this.registryDetailScroll = next;
            return true;
        }

        if (this.activeSection == "SLIME WIKI" && this.wikiDetailMaxScroll > 0)
        {
            float next = Math.Clamp(this.wikiDetailScroll + direction * 36f, 0, this.wikiDetailMaxScroll);
            if (next == this.wikiDetailScroll)
                return false;

            this.wikiDetailScroll = next;
            return true;
        }

        return false;
    }

    private void SetSection(int index)
    {
        this.activeSection = index switch
        {
            0 => "REGISTRY",
            1 => "PROGRESSION",
            _ => "SLIME WIKI"
        };
        Game1.playSound("smallSelect");
    }

    private void ChangeSection(int delta)
    {
        string[] sections = { "REGISTRY", "PROGRESSION", "SLIME WIKI" };
        int current = Array.IndexOf(sections, this.activeSection);
        this.SetSection((current + delta + sections.Length) % sections.Length);
    }

    private void ChangeWikiCategory(int delta)
    {
        int current = Array.IndexOf(WikiCategories, this.activeWikiCategory);
        this.activeWikiCategory = WikiCategories[(current + delta + WikiCategories.Length) % WikiCategories.Length];
        this.firstWikiEntry = 0;
        this.selectedWikiIndex = 0;
        this.wikiDetailScroll = 0;
        Game1.playSound("smallSelect");
    }

    private void MoveCurrentSelection(int delta)
    {
        if (this.activeSection == "REGISTRY")
        {
            if (this.residents.Count == 0)
                return;

            this.selectedResidentIndex = Math.Clamp(this.selectedResidentIndex + delta, 0, this.residents.Count - 1);
            this.registryDetailScroll = 0;
            if (this.selectedResidentIndex < this.firstResident)
                this.firstResident = this.selectedResidentIndex;
            else if (this.selectedResidentIndex >= this.firstResident + VisibleRows)
                this.firstResident = this.selectedResidentIndex - VisibleRows + 1;
            Game1.playSound("smallSelect");
        }
        else if (this.activeSection == "SLIME WIKI")
        {
            List<WikiEntry> entries = this.GetVisibleWikiEntries();
            this.selectedWikiIndex = Math.Clamp(this.selectedWikiIndex + delta, 0, Math.Max(0, entries.Count - 1));
            this.wikiDetailScroll = 0;
            if (this.selectedWikiIndex < this.firstWikiEntry)
                this.firstWikiEntry = this.selectedWikiIndex;
            else if (this.selectedWikiIndex >= this.firstWikiEntry + VisibleRows)
                this.firstWikiEntry = this.selectedWikiIndex - VisibleRows + 1;
            Game1.playSound("smallSelect");
        }
    }

    private void MoveCurrentSelectionToBoundary(bool last)
    {
        if (this.activeSection == "REGISTRY")
        {
            if (this.residents.Count == 0)
                return;

            this.selectedResidentIndex = last ? this.residents.Count - 1 : 0;
            this.firstResident = last ? Math.Max(0, this.residents.Count - VisibleRows) : 0;
            this.registryDetailScroll = 0;
            Game1.playSound("smallSelect");
        }
        else if (this.activeSection == "SLIME WIKI")
        {
            List<WikiEntry> entries = this.GetVisibleWikiEntries();
            if (entries.Count == 0)
                return;

            this.selectedWikiIndex = last ? entries.Count - 1 : 0;
            this.firstWikiEntry = last ? Math.Max(0, entries.Count - VisibleRows) : 0;
            this.wikiDetailScroll = 0;
            Game1.playSound("smallSelect");
        }
    }

    private void MoveCurrentSelectionByPage(int pageDelta)
    {
        if (pageDelta == 0)
            return;

        if (this.activeSection == "REGISTRY")
        {
            if (this.residents.Count == 0)
                return;

            (this.firstResident, this.selectedResidentIndex) = GetPagedSelection(
                this.firstResident, this.selectedResidentIndex, this.residents.Count, pageDelta);
            this.registryDetailScroll = 0;
            Game1.playSound("smallSelect");
        }
        else if (this.activeSection == "SLIME WIKI")
        {
            int count = this.GetVisibleWikiEntries().Count;
            if (count == 0)
                return;

            (this.firstWikiEntry, this.selectedWikiIndex) = GetPagedSelection(
                this.firstWikiEntry, this.selectedWikiIndex, count, pageDelta);
            this.wikiDetailScroll = 0;
            Game1.playSound("smallSelect");
        }
    }

    private static (int FirstIndex, int SelectedIndex) GetPagedSelection(
        int firstIndex, int selectedIndex, int count, int pageDelta)
    {
        int maximumFirstIndex = Math.Max(0, count - VisibleRows);
        int newFirstIndex = Math.Clamp(firstIndex + pageDelta * VisibleRows, 0, maximumFirstIndex);
        int newSelectedIndex = Math.Clamp(selectedIndex + pageDelta * VisibleRows, 0, count - 1);
        if (newSelectedIndex < newFirstIndex)
            newFirstIndex = newSelectedIndex;
        else if (newSelectedIndex >= newFirstIndex + VisibleRows)
            newFirstIndex = newSelectedIndex - VisibleRows + 1;

        return (newFirstIndex, newSelectedIndex);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (this.activeSection == "REGISTRY" && this.registryDetails.Contains(mouseX, mouseY))
        {
            this.registryDetailScroll = Math.Clamp(this.registryDetailScroll + (direction < 0 ? 36f : -36f),
                0, this.registryDetailMaxScroll);
        }
        else if (this.activeSection == "SLIME WIKI" && this.wikiDetails.Contains(mouseX, mouseY))
        {
            this.wikiDetailScroll = Math.Clamp(this.wikiDetailScroll + (direction < 0 ? 36f : -36f),
                0, this.wikiDetailMaxScroll);
        }
        else if (this.activeSection == "REGISTRY" && this.registryRows.Contains(mouseX, mouseY))
        {
            this.firstResident = Math.Clamp(this.firstResident + (direction < 0 ? 1 : -1),
                0, Math.Max(0, this.residents.Count - VisibleRows));
            if (this.selectedResidentIndex < this.firstResident
                || this.selectedResidentIndex >= this.firstResident + VisibleRows)
            {
                this.selectedResidentIndex = this.firstResident;
                this.registryDetailScroll = 0;
            }
        }
        else if (this.activeSection == "SLIME WIKI" && this.wikiRows.Contains(mouseX, mouseY))
        {
            List<WikiEntry> entries = this.GetVisibleWikiEntries();
            this.firstWikiEntry = Math.Clamp(this.firstWikiEntry + (direction < 0 ? 1 : -1),
                0, Math.Max(0, entries.Count - VisibleRows));
            if (this.selectedWikiIndex < this.firstWikiEntry
                || this.selectedWikiIndex >= this.firstWikiEntry + VisibleRows)
            {
                this.selectedWikiIndex = this.firstWikiEntry;
                this.wikiDetailScroll = 0;
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        Rectangle panel = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        DrawPanel(b, panel, new Color(30, 42, 79), new Color(135, 178, 230));
        b.DrawString(Game1.dialogueFont, "DAYCARE MANAGEMENT DESK", new Vector2(panel.X + 28, panel.Y + 18), new Color(208, 238, 255));
        b.DrawString(Game1.tinyFont, $"{this.daycare.NameOrUniqueName} | read-only tester preview", new Vector2(panel.X + 30, panel.Y + 56), new Color(170, 204, 242));
        this.DrawSectionButtons(b);

        if (this.activeSection == "REGISTRY")
            this.DrawRegistrySection(b);
        else if (this.activeSection == "PROGRESSION")
            this.DrawProgressionSection(b);
        else
            this.DrawWikiSection(b);

        b.DrawString(Game1.tinyFont, "Wheel: hovered list/details | L-stick: details/rows | D-pad/Up/Down: rows | PgUp/PgDn or LB/RB: pages | Home/End: list ends | Left/Right: Wiki", new Vector2(panel.X + 30, panel.Bottom - 24), Color.White);
        b.DrawString(Game1.tinyFont, "Esc: close", new Vector2(panel.Right - 86, panel.Bottom - 24), Color.White);
        this.drawMouse(b);
    }

    private void DrawSectionButtons(SpriteBatch b)
    {
        string[] sections = { "SLIME REGISTRY", "PROGRESSION", "SLIME WIKI" };
        for (int index = 0; index < this.sectionButtons.Length; index++)
        {
            bool selected = sections[index] switch
            {
                "SLIME REGISTRY" => this.activeSection == "REGISTRY",
                _ => this.activeSection == sections[index]
            };
            DrawPanel(b, this.sectionButtons[index], selected ? new Color(68, 112, 166) : new Color(42, 58, 102),
                selected ? new Color(178, 224, 255) : new Color(90, 126, 182));
            DrawCentered(b, sections[index], Game1.smallFont, this.sectionButtons[index], Color.White);
        }
    }

    private void DrawRegistrySection(SpriteBatch b)
    {
        Rectangle listPanel = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 146, 416, 432);
        Rectangle detailPanel = this.registryDetails;
        DrawPanel(b, listPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        DrawPanel(b, detailPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        b.DrawString(Game1.tinyFont, $"RANCH SLIMES: {this.residents.Count}",
            new Vector2(listPanel.X + 16, listPanel.Y + 10), new Color(139, 226, 255));
        b.DrawString(Game1.tinyFont, $"LIVING CAPACITY: {this.daycare.animalsThatLiveHere.Count}/{this.daycare.animalLimit.Value}",
            new Vector2(listPanel.X + 16, listPanel.Y + 29), new Color(178, 214, 249));

        int end = Math.Min(this.firstResident + VisibleRows, this.residents.Count);
        for (int index = this.firstResident; index < end; index++)
        {
            FarmAnimal animal = this.residents[index];
            Rectangle row = new(this.registryRows.X, this.registryRows.Y + (index - this.firstResident) * RowHeight,
                this.registryRows.Width, RowHeight - 3);
            bool selected = index == this.selectedResidentIndex;
            DrawPanel(b, row, selected ? new Color(65, 99, 151) : new Color(43, 62, 108),
                selected ? new Color(174, 220, 255) : new Color(83, 115, 171));
            string species = this.registry.TryGetSlimeRecord(animal.myID.Value, out SlimeRecord record)
                ? record.Genome.PrimarySpecies
                : "Ranch Slime";
            DrawFittedText(b, $"{animal.Name} | {species} | ID {animal.myID.Value}",
                new Rectangle(row.X + 8, row.Y + 3, row.Width - 16, row.Height - 6), Game1.tinyFont, Color.White);
        }

        if (this.residents.Count > VisibleRows)
            b.DrawString(Game1.tinyFont, $"Scroll | {this.firstResident + 1}-{end} / {this.residents.Count}",
                new Vector2(listPanel.X + 16, listPanel.Bottom - 26), new Color(178, 214, 249));

        if (this.selectedResidentIndex < 0 || this.selectedResidentIndex >= this.residents.Count)
        {
            this.registryDetailScroll = 0;
            this.registryDetailMaxScroll = 0;
            DrawCentered(b, "No Ranch Slime residents are registered in this Daycare.", Game1.smallFont,
                new Rectangle(detailPanel.X + 24, detailPanel.Y + 48, detailPanel.Width - 48, 100), Color.White);
            return;
        }

        FarmAnimal selectedAnimal = this.residents[this.selectedResidentIndex];
        b.DrawString(Game1.smallFont, selectedAnimal.Name, new Vector2(detailPanel.X + 20, detailPanel.Y + 16), new Color(139, 226, 255));
        if (!this.registry.TryGetSlimeRecord(selectedAnimal.myID.Value, out SlimeRecord selectedRecord))
        {
            this.registryDetailScroll = 0;
            this.registryDetailMaxScroll = 0;
            b.DrawString(Game1.smallFont, "Genome record unavailable.", new Vector2(detailPanel.X + 20, detailPanel.Y + 58), Color.White);
            return;
        }

        SlimeGenome genome = selectedRecord.Genome;
        string speciesName = genome.SecondarySpecies is null
            ? genome.PrimarySpecies
            : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
        int ageDays = selectedRecord.AgeClockInitialized ? selectedRecord.AgeDays : selectedAnimal.age.Value;
        string parentA = FormatParent(genome.ParentAName, genome.ParentA);
        string parentB = FormatParent(genome.ParentBName, genome.ParentB);
        string[] details =
        {
            $"FarmAnimal ID: {selectedAnimal.myID.Value}",
            $"Species: {speciesName}",
            $"Genome ID: {genome.Id:N}",
            $"Parents: {parentA} + {parentB}",
            $"Generation {genome.Generation} | abilities: {(genome.AbilityIds.Count == 0 ? "none" : string.Join(" + ", genome.AbilityIds))}",
            $"Color {genome.VisualColor} | size {genome.Size} | rarity {genome.RarityTier} | shiny {genome.IsShiny}",
            $"Mutation: {genome.MutationTrait ?? "none"}",
            $"Favorite food: {FoodLikeCatalog.GetDisplayName(genome.LikedFoodItemId)}",
            $"Age: {ageDays}/{FarmAnimalContent.RanchSlimeDaysToMature} days",
            $"Friendship {selectedAnimal.friendshipTowardFarmer.Value}/1000 | happiness {selectedAnimal.happiness.Value}/255 | fullness {selectedAnimal.fullness.Value}/255",
            $"Assignment: {SlimeRegistry.GetResidentStatusLabel(this.registry.GetResidentStatus(selectedAnimal.myID.Value))}",
            $"Current activity: {this.getActiveActivitySummary(selectedAnimal.myID.Value) ?? "none"}"
        };
        Rectangle detailBody = new(detailPanel.X + 20, detailPanel.Y + 56, detailPanel.Width - 40, detailPanel.Height - 68);
        this.registryDetailScroll = DrawScrollableParagraphs(b,
            details.Select(text => (Text: text, Color: Color.White)), detailBody, Game1.tinyFont,
            18f, 4f, this.registryDetailScroll, out this.registryDetailMaxScroll);
        if (this.registryDetailMaxScroll > 0)
            b.DrawString(Game1.tinyFont, "Mouse wheel: scroll details", new Vector2(detailPanel.X + 20, detailPanel.Y + 39), new Color(178, 214, 249));
    }

    private void DrawProgressionSection(SpriteBatch b)
    {
        Rectangle content = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 146, this.width - 64, 432);
        DrawPanel(b, content, new Color(35, 51, 92), new Color(90, 128, 190));
        string tierName = this.buildingTier switch
        {
            0 => "Basic Daycare",
            1 => "First upgrade",
            2 => "Second upgrade",
            _ => "Final upgrade"
        };
        int residents = this.daycare.animalsThatLiveHere.Count;
        string prefix = this.daycare.NameOrUniqueName + ":";
        string rareLabStatus = this.buildingTier == 0
            ? "CryoLab / Rare Labs: locked until first upgrade"
            : $"CryoLab / Rare Labs: unlocked | { (this.buildingTier >= 3 ? 5 : 1)}% base chance";
        string finalFacilityStatus = this.buildingTier >= 3
            ? "Second breeder / incubator and Gel Refinery: unlocked"
            : "Second breeder / incubator and Gel Refinery: locked until final upgrade";
        string[] status =
        {
            $"Building tier: {tierName} ({this.buildingTier}/3)",
            $"Living residents: {residents}/{this.daycare.animalLimit.Value}",
            $"Breeder pens currently placed: {this.CountPlaced(RanchingItemContent.BreederQualifiedId)}",
            $"Incubators currently placed: {this.CountPlaced(RanchingItemContent.IncubatorQualifiedId)}",
            $"Egg CryoLab tester chests: {this.daycare.Objects.Values.OfType<Chest>().Count(EggCryoLabContent.IsEggCryoLab)}",
            $"Rare Lab records: {this.registry.RareLabs.Count(lab => lab.MachineKey.StartsWith(prefix, StringComparison.Ordinal))}",
            $"Mutagenic Pool facilities: {this.CountPlaced(MutagenicPoolContent.QualifiedItemId)}",
            $"Gel Refinery facilities: {this.CountPlaced(GelRefineryContent.QualifiedItemId)}",
            rareLabStatus,
            finalFacilityStatus
        };
        b.DrawString(Game1.smallFont, "CURRENT DAYCARE", new Vector2(content.X + 24, content.Y + 22), new Color(139, 226, 255));
        int y = content.Y + 64;
        foreach (string line in status)
        {
            b.DrawString(Game1.tinyFont, line, new Vector2(content.X + 30, y), Color.White);
            y += 26;
        }

        Rectangle guidance = new(content.X + 20, content.Y + 320, content.Width - 40, 92);
        DrawPanel(b, guidance, new Color(47, 66, 111), new Color(95, 134, 194));
        b.DrawString(Game1.tinyFont, "PROGRESSION GUIDE", new Vector2(guidance.X + 12, guidance.Y + 5), new Color(139, 226, 255));
        int currentCycleDays = this.buildingTier == 0 ? 18 : 14;
        string guidanceText = $"New breeder cycles: {currentCycleDays} days at this tier (14 days from the first upgrade onward). "
            + "First upgrade unlocks the Egg CryoLab and Rare Labs at 1% base chance. Final upgrade adds a second breeder and incubator, "
            + "raises Rare Lab base chance to 5%, and unlocks the Gel Refinery. Robin handles upgrades; Marnie sells furniture recipes.";
        DrawWrappedText(b, guidanceText, new Vector2(guidance.X + 12, guidance.Y + 21), guidance.Width - 24,
            Game1.tinyFont, new Color(230, 239, 255), 14f);
    }

    private void DrawWikiSection(SpriteBatch b)
    {
        Rectangle listPanel = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 146, 416, 432);
        Rectangle detailPanel = this.wikiDetails;
        DrawPanel(b, listPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        DrawPanel(b, detailPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        for (int index = 0; index < this.wikiCategoryButtons.Length; index++)
        {
            bool selected = WikiCategories[index] == this.activeWikiCategory;
            DrawPanel(b, this.wikiCategoryButtons[index], selected ? new Color(68, 112, 166) : new Color(42, 58, 102),
                selected ? new Color(178, 224, 255) : new Color(90, 126, 182));
            DrawCentered(b, WikiCategories[index], Game1.tinyFont, this.wikiCategoryButtons[index], Color.White);
        }

        List<WikiEntry> entries = this.GetVisibleWikiEntries();
        int end = Math.Min(this.firstWikiEntry + VisibleRows, entries.Count);
        for (int index = this.firstWikiEntry; index < end; index++)
        {
            Rectangle row = new(this.wikiRows.X, this.wikiRows.Y + (index - this.firstWikiEntry) * RowHeight,
                this.wikiRows.Width, RowHeight - 3);
            bool selected = index == this.selectedWikiIndex;
            DrawPanel(b, row, selected ? new Color(65, 99, 151) : new Color(43, 62, 108),
                selected ? new Color(174, 220, 255) : new Color(83, 115, 171));
            DrawFittedText(b, entries[index].Name, new Rectangle(row.X + 8, row.Y + 3, row.Width - 16, row.Height - 6),
                Game1.tinyFont, Color.White);
        }
        if (entries.Count > VisibleRows)
            b.DrawString(Game1.tinyFont, $"Scroll | {this.firstWikiEntry + 1}-{end} / {entries.Count}",
                new Vector2(listPanel.X + 16, listPanel.Bottom - 26), new Color(178, 214, 249));

        if (entries.Count == 0)
        {
            this.wikiDetailScroll = 0;
            this.wikiDetailMaxScroll = 0;
            return;
        }
        this.selectedWikiIndex = Math.Clamp(this.selectedWikiIndex, 0, entries.Count - 1);
        WikiEntry entry = entries[this.selectedWikiIndex];
        b.DrawString(Game1.smallFont, entry.Name, new Vector2(detailPanel.X + 20, detailPanel.Y + 18), new Color(139, 226, 255));
        string abilityList = entry.Abilities.Count == 0 ? "No base ability" : string.Join(" + ", entry.Abilities);
        string abilityLabel = entry.Category switch
        {
            "RARE" => "Current base-color ability: none; rare companion direction is planned.",
            "RULES" => "Approved Ranch Slime breeding reference.",
            "LIFE" => "Ranch Slime life cycle reference.",
            _ => $"Current genome ability label: {abilityList}"
        };
        var details = new List<(string Text, Color Color)>
        {
            (abilityLabel, Color.White),
            (entry.Summary, Color.White)
        };
        foreach (string ability in entry.Abilities.Distinct(StringComparer.Ordinal))
        {
            string description = AbilityDirections.TryGetValue(ability, out string? direction)
                ? $"{ability}: {direction}"
                : $"{ability}: details are still being written.";
            details.Add((description, new Color(217, 231, 253)));
        }
        bool showAbilityDisclaimer = entry.Category is "BASE" or "HYBRID" or "RARE";
        int reservedFooterHeight = showAbilityDisclaimer ? 158 : 90;
        Rectangle detailBody = new(detailPanel.X + 20, detailPanel.Y + 58, detailPanel.Width - 40,
            detailPanel.Height - reservedFooterHeight);
        this.wikiDetailScroll = DrawScrollableParagraphs(b, details, detailBody, Game1.tinyFont,
            18f, 8f, this.wikiDetailScroll, out this.wikiDetailMaxScroll);
        if (this.wikiDetailMaxScroll > 0)
            b.DrawString(Game1.tinyFont, "Mouse wheel: scroll entry", new Vector2(detailPanel.X + 20, detailPanel.Y + 40), new Color(178, 214, 249));
        if (showAbilityDisclaimer)
        {
            DrawWrappedText(b, "These are planned companion abilities. They do not currently activate during normal play; exact rates, cooldowns, and limits remain under review.",
                new Vector2(detailPanel.X + 20, detailPanel.Bottom - 86), detailPanel.Width - 40,
                Game1.tinyFont, new Color(198, 213, 241), 18f);
        }
    }

    private int CountPlaced(string qualifiedItemId) =>
        this.daycare.Objects.Values.Count(obj => obj.QualifiedItemId == qualifiedItemId);

    private static string FormatParent(string? name, Guid? genomeId)
    {
        if (genomeId is Guid id && id != Guid.Empty)
            return string.IsNullOrWhiteSpace(name) ? $"ID {id:N}" : $"{name} [{id:N}]";

        return string.IsNullOrWhiteSpace(name) ? "unrecorded" : name;
    }

    private List<WikiEntry> GetVisibleWikiEntries() =>
        WikiEntries.Where(entry => entry.Category == this.activeWikiCategory).ToList();

    private static List<WikiEntry> BuildWikiEntries()
    {
        var entries = new List<WikiEntry>();
        foreach (string species in BaseSpecies)
        {
            IReadOnlyList<string> abilities = SlimeAbilityCatalog.GetAbilityNames(species, null, null);
            entries.Add(new WikiEntry("BASE", species,
                $"Base-color Ranch Slime. Its ability label is {string.Join(" + ", abilities)}.", abilities));
        }

        for (int first = 0; first < BaseSpecies.Length; first++)
        {
            for (int second = first + 1; second < BaseSpecies.Length; second++)
            {
                string firstSpecies = BaseSpecies[first];
                string secondSpecies = BaseSpecies[second];
                IReadOnlyList<string> abilities = SlimeAbilityCatalog.GetAbilityNames(firstSpecies, secondSpecies, null);
                entries.Add(new WikiEntry("HYBRID", $"{firstSpecies} / {secondSpecies} Hybrid",
                    "A two-ability hybrid created by pairing distinct base species. Matching hybrids retain the same two abilities; different hybrids follow the contributor-slot breeding rule.",
                    abilities));
            }
        }

        entries.Add(new WikiEntry("RARE", "Gold Slime", "Gold is an active core rare offspring type. Its planned companion direction is a strict, capped shop discount; the effect is not implemented.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RARE", "Void Slime", "Void is an active core rare offspring type. Its planned companion direction stores an additional row of collected items if inventory is full; the effect is not implemented.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RARE", "Prismatic Slime", "Prismatic is an active core rare offspring type. Its planned companion direction allows riding at any size and selecting its other base-role ability; the effect is not implemented.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RULES", "Base pairings",
            "Two parents with the same base species produce that base species. Two different base species produce a two-ability hybrid containing both base abilities.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RULES", "Hybrid pairings",
            "Two parents with the same exact hybrid type produce that hybrid again; abilities do not stack. A hybrid paired with a base is eligible and has equal 50% outcomes: the base parent's type or the same hybrid type. Two different hybrids choose uniformly among their four base-contributor slots. Repeated species keep their separate slots: Red/Blue × Red/Yellow gives Red 50%, Blue 25%, Yellow 25%.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RULES", "Rare slimes and crystals",
            "Rare slimes cannot breed. A Chromatic Crystal can focus an offspring on one of the seven pure base identities, but cannot force a hybrid or rare result. Crystals are currently a tester input; normal acquisition and cost are not set.", Array.Empty<string>()));
        entries.Add(new WikiEntry("RULES", "Fixed egg outcomes",
            "The Breeder saves the offspring's non-rarity genome when its cycle completes. The Incubator's Rare Lab resolves rarity once at incubation completion and saves the result before hatching; reloading cannot reroll it. If the Daycare is full, a ready hatch waits safely for room.", Array.Empty<string>()));
        entries.Add(new WikiEntry("LIFE", "Incubation and hatching",
            "Each Ranch Slime Egg has a unique identity and does not stack with other eggs. Incubation takes seven days. An unlocked Rare Lab fixes its result when incubation completes; if the Daycare is full, the ready hatch waits with that same result.", Array.Empty<string>()));
        entries.Add(new WikiEntry("LIFE", "Growing up",
            "A Ranch Slime reaches maturity seven in-game days after hatching. This growth period is separate from the Incubator's seven-day timer, and visual size is a separate trait from age.", Array.Empty<string>()));
        return entries;
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
        b.DrawString(font, text, new Vector2(rect.Center.X - measured.X * scale / 2f, rect.Center.Y - measured.Y * scale / 2f),
            color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private static void DrawFittedText(SpriteBatch b, string text, Rectangle bounds, SpriteFont font, Color color)
    {
        Vector2 measured = font.MeasureString(text);
        float scale = Math.Min(1f, Math.Min((bounds.Width - 8) / Math.Max(1f, measured.X), (bounds.Height - 2) / Math.Max(1f, measured.Y)));
        b.DrawString(font, text, new Vector2(bounds.Center.X - measured.X * scale / 2f, bounds.Center.Y - measured.Y * scale / 2f),
            color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.999f);
    }

    private static int DrawWrappedText(SpriteBatch b, string text, Vector2 position, int maxWidth, SpriteFont font, Color color, float lineHeight)
    {
        string line = string.Empty;
        float y = position.Y;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.MeasureString(candidate).X > maxWidth)
            {
                b.DrawString(font, line, new Vector2(position.X, y), color);
                y += lineHeight;
                line = word;
            }
            else
                line = candidate;
        }

        if (line.Length > 0)
        {
            b.DrawString(font, line, new Vector2(position.X, y), color);
            y += lineHeight;
        }
        return (int)y;
    }

    private static float DrawScrollableParagraphs(SpriteBatch b, IEnumerable<(string Text, Color Color)> paragraphs,
        Rectangle bounds, SpriteFont font, float lineHeight, float paragraphSpacing, float scrollOffset, out float maxScroll)
    {
        var paragraphList = paragraphs.ToList();
        var lines = new List<(string Text, float Y, Color Color)>();
        float contentHeight = 0;
        for (int paragraphIndex = 0; paragraphIndex < paragraphList.Count; paragraphIndex++)
        {
            (string paragraph, Color paragraphColor) = paragraphList[paragraphIndex];
            string line = string.Empty;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(candidate).X > bounds.Width)
                {
                    lines.Add((line, contentHeight, paragraphColor));
                    contentHeight += lineHeight;
                    line = word;
                }
                else
                    line = candidate;
            }

            if (line.Length > 0)
            {
                lines.Add((line, contentHeight, paragraphColor));
                contentHeight += lineHeight;
            }
            if (paragraphIndex < paragraphList.Count - 1)
                contentHeight += paragraphSpacing;
        }

        maxScroll = Math.Max(0, contentHeight - bounds.Height);
        scrollOffset = Math.Clamp(scrollOffset, 0, maxScroll);
        foreach ((string text, float y, Color color) in lines)
        {
            float drawY = bounds.Y + y - scrollOffset;
            if (drawY < bounds.Y || drawY + lineHeight > bounds.Bottom)
                continue;

            b.DrawString(font, text, new Vector2(bounds.X, drawY), color);
        }
        return scrollOffset;
    }

    private sealed record WikiEntry(string Category, string Name, string Summary, IReadOnlyList<string> Abilities);
}
