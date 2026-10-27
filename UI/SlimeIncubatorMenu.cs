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
    private const int GridColumns = 3;
    private const int VisibleEggs = 6;
    private const string EggItemPrefix = "(O)Taum.SlimeRanching_Egg_";

    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly Func<int, bool> startIncubation;
    private readonly Func<bool> takeHatchedSlime;
    private readonly Texture2D incubatorTexture;
    private readonly List<(int InventoryIndex, string Name, int Stack)> candidates;
    private readonly int listTop;
    private readonly int listBottom;
    private readonly Rectangle actionButton;
    private int firstVisible;
    private int? selectedInventoryIndex;
    private string? statusMessage;

    public SlimeIncubatorMenu(SlimeRegistry registry, string machineKey, Func<int, bool> startIncubation, Func<bool> takeHatchedSlime, Texture2D incubatorTexture)
        : base(Game1.uiViewport.Width / 2 - 450, Game1.uiViewport.Height / 2 - 280, 900, 560)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.startIncubation = startIncubation;
        this.takeHatchedSlime = takeHatchedSlime;
        this.incubatorTexture = incubatorTexture;
        this.listTop = this.yPositionOnScreen + 166;
        this.listBottom = this.listTop + 184;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 660, this.yPositionOnScreen + this.height - 88, 204, 58);
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
            this.statusMessage = this.takeHatchedSlime()
                ? "Slime added to the Daycare. Incubator ready."
                : "Could not save the hatch; it remains ready to collect.";
            Game1.playSound(this.statusMessage.StartsWith("Slime added", StringComparison.Ordinal) ? "smallSelect" : "cancel");
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
                this.statusMessage = "Egg inserted. It will hatch in two in-game days.";
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
            string eggName = "Ranch Slime Egg";
            if (this.registry.TryGetEgg(active.EggItemId, out SlimeEggRecord egg))
                eggName = $"{egg.Genome.PrimarySpecies} Egg · {egg.Genome.RarityTier} · Gen {egg.Genome.Generation}";

            bool hatched = active.HatchAnimalId is not null;
            b.DrawString(Game1.smallFont, hatched ? "HATCHED!" : "HATCHING...", new Vector2(panel.X + 24, panel.Y + 22), new Color(130, 231, 255));
            if (hatched)
            {
                DrawPanel(b, new Rectangle(panel.X + 18, panel.Y + 62, panel.Width - 36, 236), new Color(198, 215, 245), new Color(235, 204, 255));
                DrawCentered(b, "A new slime has hatched!", Game1.dialogueFont, new Rectangle(panel.X + 32, panel.Y + 82, panel.Width - 64, 50), new Color(42, 50, 88));
                DrawCentered(b, "♥   ♥   ♥", Game1.dialogueFont, new Rectangle(panel.X + 72, panel.Y + 140, panel.Width - 144, 58), new Color(238, 91, 157));
                DrawCentered(b, eggName, Game1.smallFont, new Rectangle(panel.X + 24, panel.Y + 208, panel.Width - 48, 36), new Color(42, 50, 88));
                DrawPanel(b, this.actionButton, new Color(46, 153, 78), new Color(145, 239, 134));
                DrawCentered(b, "TAKE SLIME", Game1.smallFont, this.actionButton, Color.White);
            }
            else
            {
                DrawPanel(b, new Rectangle(panel.X + 18, panel.Y + 62, panel.Width - 36, 236), new Color(48, 65, 128), new Color(105, 140, 209));
                DrawCentered(b, eggName, Game1.dialogueFont, new Rectangle(panel.X + 30, panel.Y + 82, panel.Width - 60, 54), Color.White);
                DrawCentered(b, daysLeft > 0 ? $"{daysLeft} Day{(daysLeft == 1 ? "" : "s")} Left" : "Ready to hatch · waiting for Daycare space", Game1.smallFont, new Rectangle(panel.X + 30, panel.Y + 134, panel.Width - 60, 38), Color.White);
                DrawProgress(b, new Rectangle(panel.X + 32, panel.Y + 194, panel.Width - 64, 28), progress);
                DrawCentered(b, $"{elapsed} / {duration} days  ({(int)(progress * 100)}%)", Game1.smallFont, new Rectangle(panel.X + 25, panel.Y + 232, panel.Width - 50, 34), Color.White);
                DrawPanel(b, new Rectangle(panel.X + 18, panel.Y + 316, panel.Width - 36, 54), new Color(55, 72, 134), new Color(101, 138, 205));
                b.DrawString(Game1.smallFont, "TEMPERATURE", new Vector2(panel.X + 30, panel.Y + 325), Color.White);
                DrawProgress(b, new Rectangle(panel.X + 173, panel.Y + 335, panel.Width - 216, 16), progress);
            }
        }
        else if (this.candidates.Count == 0)
        {
            b.DrawString(Game1.smallFont, "SELECT EGG", new Vector2(panel.X + 24, panel.Y + 22), new Color(130, 231, 255));
            DrawCentered(b, "No eligible eggs in your inventory.", Game1.smallFont, new Rectangle(panel.X + 20, panel.Y + 100, panel.Width - 40, 100), Color.White);
            DrawIncubationControls(b, panel);
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
        }

        if (this.statusMessage is not null)
            b.DrawString(Game1.smallFont, this.statusMessage, new Vector2(this.xPositionOnScreen + 32, this.yPositionOnScreen + this.height - 46), Color.White);
        b.DrawString(Game1.smallFont, "Esc: close", new Vector2(this.xPositionOnScreen + this.width - 142, this.yPositionOnScreen + this.height - 34), Color.White);
        this.drawMouse(b);
    }

    private void DrawIncubationControls(SpriteBatch b, Rectangle panel)
    {
        b.DrawString(Game1.smallFont, "INCUBATION TIME", new Vector2(panel.X + 24, panel.Y + 294), Color.White);
        DrawCentered(b, "2 Days", Game1.dialogueFont, new Rectangle(panel.X + 160, panel.Y + 282, 135, 44), new Color(188, 235, 255));
        DrawProgress(b, new Rectangle(panel.X + 305, panel.Y + 300, 160, 14), 0f);
        DrawPanel(b, this.actionButton, this.selectedInventoryIndex is null ? new Color(65, 79, 105) : new Color(43, 151, 73), this.selectedInventoryIndex is null ? new Color(117, 131, 159) : new Color(139, 240, 127));
        DrawCentered(b, "▶  START", Game1.smallFont, this.actionButton, Color.White);
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

    private static void DrawProgress(SpriteBatch b, Rectangle rect, float progress)
    {
        b.Draw(Game1.staminaRect, rect, new Color(18, 30, 62));
        int width = (int)((rect.Width - 4) * Math.Clamp(progress, 0f, 1f));
        if (width > 0)
            b.Draw(Game1.staminaRect, new Rectangle(rect.X + 2, rect.Y + 2, width, Math.Max(1, rect.Height - 4)), new Color(46, 213, 235));
    }
}
