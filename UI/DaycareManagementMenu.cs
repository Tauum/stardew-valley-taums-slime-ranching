using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private static readonly IReadOnlyDictionary<string, string> AbilityDirections = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Fighter"] = "May occasionally increase the chance of double monster drops.",
        ["Revealer"] = "May find artifact spots or other supported hidden clues.",
        ["Irrigator"] = "May occasionally double fishing trash or treasure-chest catches; it does not improve fish quality or irrigate crops.",
        ["Collector"] = "The current genome label is Collector. Green's recorded direction is crop care; its final role and name are still being reconciled.",
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
    private readonly Rectangle registryRows;
    private readonly Rectangle wikiRows;
    private readonly Rectangle[] sectionButtons;
    private readonly Rectangle[] wikiCategoryButtons;
    private string activeSection = "REGISTRY";
    private string activeWikiCategory = "BASE";
    private int firstResident;
    private int selectedResidentIndex = -1;
    private int firstWikiEntry;
    private int selectedWikiIndex;

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

        this.sectionButtons = new[]
        {
            new Rectangle(this.xPositionOnScreen + 34, this.yPositionOnScreen + 82, 326, 48),
            new Rectangle(this.xPositionOnScreen + 377, this.yPositionOnScreen + 82, 326, 48),
            new Rectangle(this.xPositionOnScreen + 720, this.yPositionOnScreen + 82, 326, 48)
        };
        this.registryRows = new Rectangle(this.xPositionOnScreen + 48, this.yPositionOnScreen + 190, 382, 360);
        this.wikiCategoryButtons = new[]
        {
            new Rectangle(this.xPositionOnScreen + 48, this.yPositionOnScreen + 166, 112, 42),
            new Rectangle(this.xPositionOnScreen + 170, this.yPositionOnScreen + 166, 112, 42),
            new Rectangle(this.xPositionOnScreen + 292, this.yPositionOnScreen + 166, 112, 42)
        };
        this.wikiRows = new Rectangle(this.xPositionOnScreen + 48, this.yPositionOnScreen + 220, 382, 330);
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
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (this.activeSection != "SLIME WIKI")
            return;

        string[] categories = { "BASE", "HYBRID", "RARE" };
        for (int index = 0; index < this.wikiCategoryButtons.Length; index++)
        {
            if (!this.wikiCategoryButtons[index].Contains(x, y))
                continue;

            this.activeWikiCategory = categories[index];
            this.firstWikiEntry = 0;
            this.selectedWikiIndex = 0;
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
                Game1.playSound("smallSelect");
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (this.activeSection == "REGISTRY" && this.registryRows.Contains(mouseX, mouseY))
        {
            this.firstResident = Math.Clamp(this.firstResident + (direction < 0 ? 1 : -1),
                0, Math.Max(0, this.residents.Count - VisibleRows));
        }
        else if (this.activeSection == "SLIME WIKI" && this.wikiRows.Contains(mouseX, mouseY))
        {
            List<WikiEntry> entries = this.GetVisibleWikiEntries();
            this.firstWikiEntry = Math.Clamp(this.firstWikiEntry + (direction < 0 ? 1 : -1),
                0, Math.Max(0, entries.Count - VisibleRows));
            if (this.selectedWikiIndex < this.firstWikiEntry
                || this.selectedWikiIndex >= this.firstWikiEntry + VisibleRows)
                this.selectedWikiIndex = this.firstWikiEntry;
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
        Rectangle detailPanel = new(this.xPositionOnScreen + 466, this.yPositionOnScreen + 146, 582, 432);
        DrawPanel(b, listPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        DrawPanel(b, detailPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        b.DrawString(Game1.smallFont, $"RESIDENTS | {this.residents.Count}/{this.daycare.animalLimit.Value}",
            new Vector2(listPanel.X + 16, listPanel.Y + 12), new Color(139, 226, 255));

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
            DrawCentered(b, "No Ranch Slime residents are registered in this Daycare.", Game1.smallFont,
                new Rectangle(detailPanel.X + 24, detailPanel.Y + 48, detailPanel.Width - 48, 100), Color.White);
            return;
        }

        FarmAnimal selectedAnimal = this.residents[this.selectedResidentIndex];
        b.DrawString(Game1.smallFont, selectedAnimal.Name, new Vector2(detailPanel.X + 20, detailPanel.Y + 16), new Color(139, 226, 255));
        if (!this.registry.TryGetSlimeRecord(selectedAnimal.myID.Value, out SlimeRecord selectedRecord))
        {
            b.DrawString(Game1.smallFont, "Genome record unavailable.", new Vector2(detailPanel.X + 20, detailPanel.Y + 58), Color.White);
            return;
        }

        SlimeGenome genome = selectedRecord.Genome;
        string speciesName = genome.SecondarySpecies is null
            ? genome.PrimarySpecies
            : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
        string[] details =
        {
            $"FarmAnimal ID: {selectedAnimal.myID.Value}",
            $"Species: {speciesName}",
            $"Genome ID: {genome.Id:N}",
            $"Generation {genome.Generation} | abilities: {(genome.AbilityIds.Count == 0 ? "none" : string.Join(" + ", genome.AbilityIds))}",
            $"Color {genome.VisualColor} | size {genome.Size} | rarity {genome.RarityTier} | shiny {genome.IsShiny}",
            $"Mutation: {genome.MutationTrait ?? "none"}",
            $"Favorite food: {FoodLikeCatalog.GetDisplayName(genome.LikedFoodItemId)}",
            $"Age: {selectedRecord.AgeDays}/{FarmAnimalContent.RanchSlimeDaysToMature} days",
            $"Friendship {selectedAnimal.friendshipTowardFarmer.Value}/1000 | happiness {selectedAnimal.happiness.Value}/255 | fullness {selectedAnimal.fullness.Value}/255",
            $"Assignment: {SlimeRegistry.GetResidentStatusLabel(this.registry.GetResidentStatus(selectedAnimal.myID.Value))}",
            $"Current activity: {this.getActiveActivitySummary(selectedAnimal.myID.Value) ?? "none"}"
        };
        int y = detailPanel.Y + 56;
        foreach (string line in details)
        {
            y = DrawWrappedText(b, line, new Vector2(detailPanel.X + 20, y), detailPanel.Width - 40,
                Game1.tinyFont, Color.White, 18f);
            y += 4;
        }
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
        string[] status =
        {
            $"Building tier: {tierName} ({this.buildingTier}/3)",
            $"Living residents: {residents}/{this.daycare.animalLimit.Value}",
            $"Breeder pens currently placed: {this.CountPlaced(RanchingItemContent.BreederQualifiedId)}",
            $"Incubators currently placed: {this.CountPlaced(RanchingItemContent.IncubatorQualifiedId)}",
            $"Egg CryoLab tester chests: {this.daycare.Objects.Values.OfType<Chest>().Count(EggCryoLabContent.IsEggCryoLab)}",
            $"Rare Lab records: {this.registry.RareLabs.Count(lab => lab.MachineKey.StartsWith(prefix, StringComparison.Ordinal))}",
            $"Mutagenic Pool facilities: {this.CountPlaced(MutagenicPoolContent.QualifiedItemId)}",
            $"Gel Refinery facilities: {this.CountPlaced(GelRefineryContent.QualifiedItemId)}"
        };
        b.DrawString(Game1.smallFont, "CURRENT DAYCARE", new Vector2(content.X + 24, content.Y + 22), new Color(139, 226, 255));
        int y = content.Y + 64;
        foreach (string line in status)
        {
            b.DrawString(Game1.tinyFont, line, new Vector2(content.X + 30, y), Color.White);
            y += 30;
        }

        Rectangle guidance = new(content.X + 20, content.Y + 320, content.Width - 40, 92);
        DrawPanel(b, guidance, new Color(47, 66, 111), new Color(95, 134, 194));
        string guidanceText = "Robin handles building upgrades. The first upgrade unlocks the Egg CryoLab and Rare Labs; the final upgrade adds the Gel Refinery and raises the Rare Lab base chance to 5%.";
        DrawWrappedText(b, guidanceText, new Vector2(guidance.X + 12, guidance.Y + 9), guidance.Width - 24,
            Game1.tinyFont, new Color(230, 239, 255), 17f);
    }

    private void DrawWikiSection(SpriteBatch b)
    {
        Rectangle listPanel = new(this.xPositionOnScreen + 32, this.yPositionOnScreen + 146, 416, 432);
        Rectangle detailPanel = new(this.xPositionOnScreen + 466, this.yPositionOnScreen + 146, 582, 432);
        DrawPanel(b, listPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        DrawPanel(b, detailPanel, new Color(35, 51, 92), new Color(90, 128, 190));
        string[] categories = { "BASE", "HYBRID", "RARE" };
        for (int index = 0; index < this.wikiCategoryButtons.Length; index++)
        {
            bool selected = categories[index] == this.activeWikiCategory;
            DrawPanel(b, this.wikiCategoryButtons[index], selected ? new Color(68, 112, 166) : new Color(42, 58, 102),
                selected ? new Color(178, 224, 255) : new Color(90, 126, 182));
            DrawCentered(b, categories[index], Game1.tinyFont, this.wikiCategoryButtons[index], Color.White);
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
            return;
        this.selectedWikiIndex = Math.Clamp(this.selectedWikiIndex, 0, entries.Count - 1);
        WikiEntry entry = entries[this.selectedWikiIndex];
        b.DrawString(Game1.smallFont, entry.Name, new Vector2(detailPanel.X + 20, detailPanel.Y + 18), new Color(139, 226, 255));
        string abilityList = entry.Abilities.Count == 0 ? "No base ability" : string.Join(" + ", entry.Abilities);
        int y = DrawWrappedText(b, $"Current genome ability label: {abilityList}", new Vector2(detailPanel.X + 20, detailPanel.Y + 58),
            detailPanel.Width - 40, Game1.tinyFont, Color.White, 18f);
        y = DrawWrappedText(b, entry.Summary, new Vector2(detailPanel.X + 20, y + 8),
            detailPanel.Width - 40, Game1.tinyFont, Color.White, 18f);
        y += 10;
        foreach (string ability in entry.Abilities.Distinct(StringComparer.Ordinal))
        {
            string description = AbilityDirections.TryGetValue(ability, out string? direction)
                ? $"{ability}: {direction}"
                : $"{ability}: details are still being written.";
            y = DrawWrappedText(b, description, new Vector2(detailPanel.X + 20, y),
                detailPanel.Width - 40, Game1.tinyFont, new Color(217, 231, 253), 18f);
            y += 6;
        }
        DrawWrappedText(b, "These are planned companion abilities. They do not currently activate during normal play; exact rates, cooldowns, and limits remain under review.",
            new Vector2(detailPanel.X + 20, detailPanel.Bottom - 86), detailPanel.Width - 40,
            Game1.tinyFont, new Color(198, 213, 241), 18f);
    }

    private int CountPlaced(string qualifiedItemId) =>
        this.daycare.Objects.Values.Count(obj => obj.QualifiedItemId == qualifiedItemId);

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

    private sealed record WikiEntry(string Category, string Name, string Summary, IReadOnlyList<string> Abilities);
}
