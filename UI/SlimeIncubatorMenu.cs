using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Starts a two-day hatch job from a genome-specific egg in the player's inventory.</summary>
internal sealed class SlimeIncubatorMenu : IClickableMenu
{
    private const int RowHeight = 52;
    private const int VisibleRows = 4;
    private const string EggItemPrefix = "(O)Taum.SlimeRanching_Egg_";

    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly Func<int, bool> startIncubation;
    private readonly List<(int InventoryIndex, string Name, int Stack)> candidates;
    private readonly int listTop;
    private readonly int listBottom;
    private int firstVisible;
    private string? statusMessage;

    public SlimeIncubatorMenu(SlimeRegistry registry, string machineKey, Func<int, bool> startIncubation)
        : base(Game1.uiViewport.Width / 2 - 350, Game1.uiViewport.Height / 2 - 240, 700, 480)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.startIncubation = startIncubation;
        this.listTop = this.yPositionOnScreen + 132;
        this.listBottom = this.listTop + VisibleRows * RowHeight;
        this.candidates = Game1.player.Items
            .Select((item, index) => (item, index))
            .Where(pair => pair.item is not null
                && (pair.item.QualifiedItemId == ConverterContent.CommonEggQualifiedId
                    || (pair.item.QualifiedItemId.StartsWith(EggItemPrefix, StringComparison.Ordinal)
                        && registry.CanIncubate(pair.item.QualifiedItemId[3..]))))
            .Select(pair => (pair.index, pair.item!.DisplayName, pair.item.Stack))
            .ToList();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        if (mouseX < this.xPositionOnScreen + 30 || mouseX > this.xPositionOnScreen + 390
            || mouseY < this.listTop || mouseY >= this.listBottom)
            return;

        this.firstVisible = Math.Clamp(this.firstVisible + (direction < 0 ? 1 : -1),
            0, Math.Max(0, this.candidates.Count - VisibleRows));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.registry.GetIncubation(this.machineKey) is not null
            || x < this.xPositionOnScreen + 24 || x > this.xPositionOnScreen + 396
            || y < this.listTop || y >= this.listBottom)
            return;

        int visibleIndex = (y - this.listTop) / RowHeight;
        int index = this.firstVisible + visibleIndex;
        if (visibleIndex < 0 || visibleIndex >= VisibleRows || index >= this.candidates.Count)
            return;

        if (this.startIncubation(this.candidates[index].InventoryIndex))
            this.statusMessage = "Egg inserted. It will hatch in two in-game days.";
        else
            this.statusMessage = "Could not start incubation; the egg remains in your inventory.";
        Game1.playSound(this.statusMessage.StartsWith("Egg inserted", StringComparison.Ordinal) ? "smallSelect" : "cancel");
    }

    public override void draw(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        const string title = "Slime Incubator";
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 28),
            Game1.textColor);
        b.DrawString(Game1.smallFont, "Choose a Common or genome-specific Ranch Slime Egg. Hatching takes two days.",
            new Vector2(this.xPositionOnScreen + 34, this.yPositionOnScreen + 82), Game1.textColor);

        IncubationRecord? active = this.registry.GetIncubation(this.machineKey);
        if (active is not null)
        {
            int daysLeft = Math.Max(0, active.ReadyOnDay - Game1.Date.TotalDays);
            int duration = Math.Max(1, active.ReadyOnDay - active.StartedOnDay);
            int elapsed = Math.Clamp(Game1.Date.TotalDays - active.StartedOnDay, 0, duration);
            float progress = (float)elapsed / duration;
            int panelX = this.xPositionOnScreen + 36;
            int panelY = this.listTop + 8;
            int panelWidth = this.width - 72;
            int panelHeight = 190;
            Rectangle panel = new(panelX, panelY, panelWidth, panelHeight);
            b.Draw(Game1.staminaRect, panel, new Color(238, 224, 190));

            string eggName = "Ranch Slime Egg";
            if (this.registry.TryGetEgg(active.EggItemId, out SlimeEggRecord egg))
                eggName = $"{egg.Genome.PrimarySpecies} Egg  •  {egg.Genome.RarityTier}  •  Gen {egg.Genome.Generation}";
            b.DrawString(Game1.dialogueFont, eggName,
                new Vector2(panelX + 18, panelY + 16), Game1.textColor);

            string state = active.HatchAnimalId is not null
                ? "Hatch reserved; confirming the saved animal."
                : daysLeft > 0 ? $"Incubating  •  {daysLeft} day(s) remaining"
                : "Ready to hatch  •  waiting for Daycare space";
            b.DrawString(Game1.smallFont, state,
                new Vector2(panelX + 18, panelY + 55), Game1.textColor);

            int barX = panelX + 18;
            int barY = panelY + 94;
            int barWidth = panelWidth - 36;
            Rectangle barBack = new(barX, barY, barWidth, 24);
            b.Draw(Game1.staminaRect, barBack, new Color(91, 75, 58));
            if (progress > 0)
                b.Draw(Game1.staminaRect, new Rectangle(barX + 3, barY + 3,
                    Math.Max(1, (int)((barWidth - 6) * progress)), 18), new Color(112, 158, 89));

            string progressText = $"{elapsed} / {duration} days  ({(int)(progress * 100)}%)";
            Vector2 progressSize = Game1.smallFont.MeasureString(progressText);
            b.DrawString(Game1.smallFont, progressText,
                new Vector2(panelX + (panelWidth - progressSize.X) / 2, barY + 31), Game1.textColor);
        }
        else if (this.candidates.Count == 0)
        {
            b.DrawString(Game1.smallFont, "No unhatched genome-specific eggs in your inventory.",
                new Vector2(this.xPositionOnScreen + 40, this.listTop + 12), Game1.textColor);
        }
        else
        {
            int end = Math.Min(this.firstVisible + VisibleRows, this.candidates.Count);
            for (int i = this.firstVisible; i < end; i++)
            {
                int rowY = this.listTop + (i - this.firstVisible) * RowHeight;
                Rectangle row = new(this.xPositionOnScreen + 26, rowY, 365, RowHeight - 4);
                b.Draw(Game1.staminaRect, row, new Color(238, 224, 190));
                var candidate = this.candidates[i];
                b.DrawString(Game1.smallFont, candidate.Name,
                    new Vector2(row.X + 12, row.Y + 8), Game1.textColor);
                b.DrawString(Game1.tinyFont, $"Inventory stack: {candidate.Stack}",
                    new Vector2(row.X + 12, row.Y + 30), Game1.textColor);
            }

            if (this.candidates.Count > VisibleRows)
            {
                string range = $"{this.firstVisible + 1}-{end} of {this.candidates.Count}  (scroll)";
                b.DrawString(Game1.tinyFont, range,
                    new Vector2(this.xPositionOnScreen + 250, this.listBottom + 4), Game1.textColor);
            }
        }

        if (this.statusMessage is not null)
            b.DrawString(Game1.smallFont, this.statusMessage,
                new Vector2(this.xPositionOnScreen + 34, this.yPositionOnScreen + this.height - 62), Game1.textColor);
        b.DrawString(Game1.smallFont, "Esc: close",
            new Vector2(this.xPositionOnScreen + this.width - 120, this.yPositionOnScreen + this.height - 36), Game1.textColor);
        this.drawMouse(b);
    }
}
