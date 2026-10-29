using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;

namespace taums_slime_ranching.UI;

/// <summary>Runs the provisional Broken Glasses recycling recipe inside the Slime Daycare.</summary>
internal sealed class GelRefineryMenu : IClickableMenu
{
    private const int SlimeItemId = 766;
    private const int BrokenGlassesItemId = 170;
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly GelRefineryRules rules;
    private readonly Func<bool> canStartBatch;
    private readonly Func<bool> startBatch;
    private readonly Func<bool> collectBatch;
    private readonly Rectangle actionButton;
    private readonly Rectangle recipesButton;
    private readonly Rectangle closeButton;
    private bool showRecipes;
    private string? statusMessage;

    public GelRefineryMenu(SlimeRegistry registry, string machineKey, GelRefineryRules rules,
        Func<bool> canStartBatch, Func<bool> startBatch, Func<bool> collectBatch)
        : base(Game1.uiViewport.Width / 2 - 450, Game1.uiViewport.Height / 2 - 290, 900, 580)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.rules = rules;
        this.canStartBatch = canStartBatch;
        this.startBatch = startBatch;
        this.collectBatch = collectBatch;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 220, this.yPositionOnScreen + 474, 230, 54);
        this.recipesButton = new Rectangle(this.xPositionOnScreen + 630, this.yPositionOnScreen + 474, 230, 54);
        this.closeButton = new Rectangle(this.xPositionOnScreen + this.width - 52, this.yPositionOnScreen + 16, 36, 34);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.closeButton.Contains(x, y) && !this.showRecipes)
        {
            Game1.activeClickableMenu = null;
            Game1.playSound("bigDeSelect");
            return;
        }
        if (this.showRecipes)
        {
            this.showRecipes = false;
            Game1.playSound("bigDeSelect");
            return;
        }
        if (this.recipesButton.Contains(x, y))
        {
            this.showRecipes = true;
            Game1.playSound("smallSelect");
            return;
        }
        if (!this.actionButton.Contains(x, y))
            return;

        RefineryJobRecord? job = this.registry.GetRefineryJob(this.machineKey);
        if (job is null)
        {
            if (!this.canStartBatch())
            {
                this.statusMessage = "The Gel Refinery unlocks at the final Daycare tier.";
                Game1.playSound("cancel");
                return;
            }

            if (!this.HasBatchInputs())
            {
                this.statusMessage = $"Need 1 Broken Glasses and {this.rules.FuelCost} vanilla Slime to start.";
                Game1.playSound("cancel");
                return;
            }

            bool started = this.startBatch();
            this.statusMessage = started
                ? $"Batch started. Sleep for {this.rules.DurationDays} in-game day(s) to finish it."
                : "Bring 1 Broken Glasses and enough vanilla Slime, then try again.";
            Game1.playSound(started ? "smallSelect" : "cancel");
        }
        else if (Game1.Date.TotalDays >= job.ReadyOnDay)
        {
            bool collected = this.collectBatch();
            this.statusMessage = collected
                ? "Refined Quartz collected."
                : "Make room in your inventory before collecting the Refined Quartz.";
            Game1.playSound(collected ? "coin" : "cancel");
        }
    }

    public override void draw(SpriteBatch b)
    {
        DrawMachineFrame(b, new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height));
        DrawInset(b, new Rectangle(this.xPositionOnScreen + 20, this.yPositionOnScreen + 12, this.width - 80, 54), new Color(154, 93, 51));
        b.DrawString(Game1.dialogueFont, "SLIME GEL REFINERY", new Vector2(this.xPositionOnScreen + 42, this.yPositionOnScreen + 22), Color.White);
        DrawButton(b, this.closeButton, "X", new Color(205, 62, 57));

        int left = this.xPositionOnScreen + 30;
        int top = this.yPositionOnScreen + 78;
        Rectangle workstation = new(left, top, 570, 378);
        DrawInset(b, workstation, new Color(238, 224, 190));
        DrawInset(b, new Rectangle(left + 12, top + 12, 546, 54), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "PROCESSING CHAMBER", new Vector2(left + 200, top + 21), Color.White);

        b.DrawString(Game1.smallFont, "Fuel (Slime)", new Vector2(left + 26, top + 82), Color.White);
        DrawInset(b, new Rectangle(left + 24, top + 110, 350, 82), new Color(224, 196, 147));
        DrawSlimeIcon(b, new Rectangle(left + 38, top + 119, 56, 56));
        int slimeCount = CountItem("(O)" + SlimeItemId);
        b.DrawString(Game1.smallFont, $"{slimeCount} / {this.rules.FuelCost}", new Vector2(left + 112, top + 123), Color.White);
        b.DrawString(Game1.tinyFont, "in inventory; consumed when batch starts", new Vector2(left + 112, top + 151), Color.White);
        DrawProgressBar(b, new Rectangle(left + 215, top + 174, 145, 12), Math.Min(1f, slimeCount / (float)this.rules.FuelCost), "");

        b.DrawString(Game1.smallFont, "Input", new Vector2(left + 28, top + 214), Color.White);
        b.DrawString(Game1.smallFont, "Output", new Vector2(left + 410, top + 214), Color.White);
        DrawInset(b, new Rectangle(left + 24, top + 242, 205, 92), new Color(224, 196, 147));
        DrawObjectIcon(b, 170, new Rectangle(left + 38, top + 252, 64, 64));
        int brokenGlassesCount = CountItem("(O)" + BrokenGlassesItemId);
        b.DrawString(Game1.tinyFont, $"Owned: {brokenGlassesCount} / 1", new Vector2(left + 106, top + 292),
            brokenGlassesCount > 0 ? new Color(160, 255, 180) : new Color(255, 168, 148));
        b.DrawString(Game1.smallFont, "Broken Glasses ×1", new Vector2(left + 106, top + 268), Color.White);
        b.DrawString(Game1.dialogueFont, "→", new Vector2(left + 250, top + 260), Color.White);
        DrawInset(b, new Rectangle(left + 316, top + 242, 230, 92), new Color(224, 196, 147));
        DrawObjectIcon(b, 338, new Rectangle(left + 328, top + 252, 64, 64));
        b.DrawString(Game1.smallFont, "Refined Quartz ×1", new Vector2(left + 396, top + 268), Color.White);

        RefineryJobRecord? job = this.registry.GetRefineryJob(this.machineKey);
        if (job is null)
        {
            bool unlocked = this.canStartBatch();
            bool hasInputs = this.HasBatchInputs();
            b.DrawString(Game1.tinyFont, $"Prototype time: {this.rules.DurationDays} in-game day(s)",
                new Vector2(left + 28, top + 342), Color.White);
            DrawProgressBar(b, new Rectangle(left + 260, top + 346, 270, 22), 0f, "Ready");
            DrawButton(b, this.actionButton, !unlocked ? "LOCKED" : hasInputs ? "START BATCH" : "NEED INPUTS",
                !unlocked || !hasInputs ? new Color(74, 80, 117) : new Color(55, 133, 217));
            if (!unlocked)
                b.DrawString(Game1.tinyFont, "Unlocks at the final Daycare tier.", new Vector2(left + 28, top + 372), Color.White);
        }
        else
        {
            bool ready = Game1.Date.TotalDays >= job.ReadyOnDay;
            int duration = Math.Max(1, job.ReadyOnDay - job.StartedOnDay);
            int elapsed = Math.Clamp(Game1.Date.TotalDays - job.StartedOnDay, 0, duration);
            float progress = elapsed / (float)duration;
            b.DrawString(Game1.smallFont, ready ? "Batch ready" : $"Processing · ready day {job.ReadyOnDay}",
                new Vector2(left + 28, top + 342), Color.White);
            DrawProgressBar(b, new Rectangle(left + 260, top + 346, 270, 22), progress,
                ready ? "Complete" : $"{elapsed} / {duration} days");
            b.DrawString(Game1.tinyFont, ready ? "Output reserved until collected." : $"Configured processing time: {duration} day(s)",
                new Vector2(left + 28, top + 370), Color.White);
            bool buttonEnabled = ready;
            DrawButton(b, this.actionButton, buttonEnabled ? "COLLECT OUTPUT" : "PROCESSING",
                buttonEnabled ? new Color(45, 176, 106) : new Color(74, 80, 117));
        }

        int examplesX = this.xPositionOnScreen + 620;
        DrawInset(b, new Rectangle(examplesX, top, 250, 282), new Color(238, 224, 190));
        DrawInset(b, new Rectangle(examplesX + 6, top + 6, 238, 42), new Color(154, 93, 51));
        b.DrawString(Game1.smallFont, "EXAMPLE USES", new Vector2(examplesX + 62, top + 17), Color.White);
        DrawExample(b, examplesX + 18, top + 62, 400, "Strawberry", "Strawberry Jam");
        DrawExample(b, examplesX + 18, top + 112, 176, "Egg", "Mayonnaise");
        DrawExample(b, examplesX + 18, top + 162, 184, "Milk", "Cheese");
        DrawExample(b, examplesX + 18, top + 212, 258, "Blueberry", "Blueberry Wine");
        DrawInset(b, new Rectangle(examplesX, top + 294, 250, 84), new Color(238, 224, 190));
        b.DrawString(Game1.smallFont, "NOTES", new Vector2(examplesX + 96, top + 302), Color.White);
        b.DrawString(Game1.tinyFont, "Reference recipes are not active yet. The prototype uses existing items and Slime fuel.",
            new Vector2(examplesX + 12, top + 330), Color.White);

        DrawButton(b, this.recipesButton, "INPUT / OUTPUT GUIDE", new Color(55, 104, 180));
        if (!string.IsNullOrEmpty(this.statusMessage))
            b.DrawString(Game1.tinyFont, this.statusMessage, new Vector2(this.xPositionOnScreen + 30, this.yPositionOnScreen + 458), Color.White);
        b.DrawString(Game1.tinyFont, "Esc: close",
            new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 24), Color.White);
        if (this.showRecipes)
            this.DrawRecipesModal(b);
        this.drawMouse(b);
    }

    private static void DrawInset(SpriteBatch b, Rectangle bounds, Color fill)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(17, 22, 49));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), new Color(112, 111, 171));
        Color interior = fill.R > 180 && fill.G < 100 ? fill
            : fill.R >= 220 ? new Color(47, 55, 98)
            : fill.R >= 145 ? new Color(11, 83, 184)
            : fill.R >= 100 ? new Color(45, 72, 129) : fill;
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10), interior);
    }

    private static void DrawMachineFrame(SpriteBatch b, Rectangle bounds)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(11, 15, 34));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8), new Color(91, 86, 151));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, bounds.Height - 16), new Color(35, 41, 79));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 12, bounds.Y + bounds.Height - 20, bounds.Width - 24, 5), new Color(116, 114, 173));
        for (int i = 0; i < 8; i++)
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 24 + i * ((bounds.Width - 56) / 7), bounds.Y + bounds.Height - 13, 28, 5), new Color(73, 79, 126));
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string label, Color fill)
    {
        b.Draw(Game1.staminaRect, bounds, new Color(12, 17, 39));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), new Color(117, 120, 180));
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 10), fill);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label,
            new Vector2(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2), Color.White);
    }

    private static void DrawSlimeIcon(SpriteBatch b, Rectangle bounds)
    {
        DrawObjectIcon(b, SlimeItemId, bounds);
    }

    private static void DrawObjectIcon(SpriteBatch b, int index, Rectangle target)
    {
        Rectangle source = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, index, 16, 16);
        b.Draw(Game1.objectSpriteSheet, target, source, Color.White);
    }

    private static void DrawExample(SpriteBatch b, int x, int y, int itemIndex, string input, string output)
    {
        DrawObjectIcon(b, itemIndex, new Rectangle(x, y, 38, 38));
        b.DrawString(Game1.tinyFont, input, new Vector2(x + 42, y + 10), Color.White);
        b.DrawString(Game1.tinyFont, "→", new Vector2(x + 106, y + 10), Color.White);
        b.DrawString(Game1.tinyFont, output, new Vector2(x + 130, y + 10), Color.White);
    }


    private void DrawRecipesModal(SpriteBatch b)
    {
        Rectangle shade = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        b.Draw(Game1.staminaRect, shade, new Color(0, 0, 0, 160));
        int width = 570;
        int height = 360;
        int x = this.xPositionOnScreen + (this.width - width) / 2;
        int y = this.yPositionOnScreen + (this.height - height) / 2;
        DrawMachineFrame(b, new Rectangle(x, y, width, height));
        b.DrawString(Game1.dialogueFont, "REFINERY INPUT / OUTPUT", new Vector2(x + 28, y + 22), Color.White);
        b.DrawString(Game1.smallFont, this.canStartBatch() ? "PROTOTYPE RECIPE ENABLED" : "RECIPE UNLOCKS AT FINAL TIER",
            new Vector2(x + 32, y + 78), new Color(92, 220, 255));
        b.DrawString(Game1.smallFont, $"1 Broken Glasses + {this.rules.FuelCost} Slime fuel  →  1 Refined Quartz",
            new Vector2(x + 32, y + 108), Color.White);
        b.DrawString(Game1.smallFont, "Design examples shown in the reference", new Vector2(x + 32, y + 158), Color.White);
        b.DrawString(Game1.smallFont, "Strawberry → Strawberry Jam", new Vector2(x + 46, y + 190), Color.White);
        b.DrawString(Game1.smallFont, "Egg → Mayonnaise", new Vector2(x + 46, y + 220), Color.White);
        b.DrawString(Game1.smallFont, "Milk → Cheese", new Vector2(x + 46, y + 250), Color.White);
        b.DrawString(Game1.smallFont, "Blueberry → Blueberry Wine", new Vector2(x + 46, y + 280), Color.White);
        b.DrawString(Game1.tinyFont, "These four examples are reference-only; the prototype recipe is the only one enabled. Slime fuel amount is configurable.",
            new Vector2(x + 32, y + 320), Color.White);
        b.DrawString(Game1.tinyFont, "Click anywhere to close", new Vector2(x + width - 174, y + height - 24), Color.White);
    }

    private static void DrawProgressBar(SpriteBatch b, Rectangle bar, float progress, string label)
    {
        b.Draw(Game1.staminaRect, bar, new Color(17, 26, 57));
        if (progress > 0)
            b.Draw(Game1.staminaRect, new Rectangle(bar.X + 3, bar.Y + 3,
                Math.Max(1, (int)((bar.Width - 6) * Math.Clamp(progress, 0f, 1f))), bar.Height - 6), new Color(35, 198, 242));
        b.DrawString(Game1.tinyFont, label, new Vector2(bar.X + 8, bar.Y + 4), Color.White);
    }

    private static int CountItem(string qualifiedItemId) => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == qualifiedItemId)
        .Sum(item => item!.Stack);

    private bool HasBatchInputs() => CountItem("(O)" + BrokenGlassesItemId) >= 1
        && CountItem("(O)" + SlimeItemId) >= this.rules.FuelCost;
}
