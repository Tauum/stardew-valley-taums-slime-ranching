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
    private readonly SlimeRegistry registry;
    private readonly string machineKey;
    private readonly GelRefineryRules rules;
    private readonly Func<bool> startBatch;
    private readonly Func<bool> collectBatch;
    private readonly Rectangle actionButton;
    private readonly Rectangle recipesButton;
    private bool showRecipes;
    private string? statusMessage;

    public GelRefineryMenu(SlimeRegistry registry, string machineKey, GelRefineryRules rules,
        Func<bool> startBatch, Func<bool> collectBatch)
        : base(Game1.uiViewport.Width / 2 - 390, Game1.uiViewport.Height / 2 - 270, 780, 540)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.rules = rules;
        this.startBatch = startBatch;
        this.collectBatch = collectBatch;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 34, this.yPositionOnScreen + 434, 230, 54);
        this.recipesButton = new Rectangle(this.xPositionOnScreen + 282, this.yPositionOnScreen + 434, 210, 54);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
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
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        const string title = "Gel Refinery";
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(this.xPositionOnScreen + (this.width - Game1.dialogueFont.MeasureString(title).X) / 2, this.yPositionOnScreen + 24),
            Game1.textColor);

        int left = this.xPositionOnScreen + 34;
        int top = this.yPositionOnScreen + 82;
        Rectangle recipePanel = new(left, top, this.width - 68, 106);
        b.Draw(Game1.staminaRect, recipePanel, new Color(238, 224, 190));
        b.DrawString(Game1.smallFont, "Provisional recycling recipe", new Vector2(left + 16, top + 12), Game1.textColor);
        b.DrawString(Game1.dialogueFont, "1 Broken Glasses + vanilla Slime", new Vector2(left + 16, top + 37), Game1.textColor);
        b.DrawString(Game1.smallFont, "= 1 Refined Quartz", new Vector2(left + 16, top + 76), Game1.textColor);

        RefineryJobRecord? job = this.registry.GetRefineryJob(this.machineKey);
        if (job is null)
        {
            b.DrawString(Game1.smallFont, $"In inventory: Broken Glasses {CountItem("(O)170")}/1   |   Slime {CountItem("(O)766")}/{this.rules.FuelCost}",
                new Vector2(left, top + 128), Game1.textColor);
            b.DrawString(Game1.smallFont, $"Cycle time: {this.rules.DurationDays} in-game day(s) (placeholder; tune later).",
                new Vector2(left, top + 158), Game1.textColor);
            DrawProgressBar(b, new Rectangle(left, top + 188, this.width - 68, 24), 0f, "Idle");
            b.Draw(Game1.staminaRect, this.actionButton, new Color(198, 177, 220));
            b.DrawString(Game1.smallFont, "Start Batch", new Vector2(this.actionButton.X + 66, this.actionButton.Y + 17), Game1.textColor);
        }
        else
        {
            bool ready = Game1.Date.TotalDays >= job.ReadyOnDay;
            int duration = Math.Max(1, job.ReadyOnDay - job.StartedOnDay);
            int elapsed = Math.Clamp(Game1.Date.TotalDays - job.StartedOnDay, 0, duration);
            float progress = elapsed / (float)duration;
            b.DrawString(Game1.smallFont, ready ? "Batch ready" : $"Batch processing  |  ready day {job.ReadyOnDay}",
                new Vector2(left, top + 128), Game1.textColor);
            DrawProgressBar(b, new Rectangle(left, top + 162, this.width - 68, 24), progress,
                ready ? "Complete" : $"{elapsed} / {duration} days");
            b.DrawString(Game1.tinyFont, ready ? "Output is reserved here until collected." : $"Placeholder cycle: {duration} in-game day(s).",
                new Vector2(left, top + 194), Game1.textColor);
            bool buttonEnabled = ready;
            b.Draw(Game1.staminaRect, this.actionButton, buttonEnabled ? new Color(198, 177, 220) : new Color(210, 210, 210));
            b.DrawString(Game1.smallFont, buttonEnabled ? "Collect Refined Quartz" : "Processing",
                new Vector2(this.actionButton.X + 18, this.actionButton.Y + 17), Game1.textColor);
        }

        b.Draw(Game1.staminaRect, this.recipesButton, new Color(224, 192, 137));
        b.DrawString(Game1.smallFont, "Inputs / Outputs", new Vector2(this.recipesButton.X + 20, this.recipesButton.Y + 17), Game1.textColor);
        b.DrawString(Game1.tinyFont, this.statusMessage ?? "The current test recipe is provisional.",
            new Vector2(this.xPositionOnScreen + 510, this.actionButton.Y + 10), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Esc: close",
            new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 24), Game1.textColor);
        if (this.showRecipes)
            this.DrawRecipesModal(b);
        this.drawMouse(b);
    }

    private void DrawRecipesModal(SpriteBatch b)
    {
        Rectangle shade = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        b.Draw(Game1.staminaRect, shade, new Color(0, 0, 0, 160));
        int width = 570;
        int height = 360;
        int x = this.xPositionOnScreen + (this.width - width) / 2;
        int y = this.yPositionOnScreen + (this.height - height) / 2;
        IClickableMenu.drawTextureBox(b, x, y, width, height, Color.White);
        Utility.drawTextWithShadow(b, "Refinery Inputs / Outputs", Game1.dialogueFont,
            new Vector2(x + 28, y + 22), Game1.textColor);
        b.DrawString(Game1.smallFont, "Prototype recipe currently enabled", new Vector2(x + 32, y + 78), Game1.textColor);
        b.DrawString(Game1.smallFont, "1 Broken Glasses + vanilla Slime fuel  →  1 Refined Quartz",
            new Vector2(x + 32, y + 108), Game1.textColor);
        b.DrawString(Game1.smallFont, "Design examples shown in the reference", new Vector2(x + 32, y + 158), Game1.textColor);
        b.DrawString(Game1.smallFont, "Strawberry → Strawberry Jam", new Vector2(x + 46, y + 190), Game1.textColor);
        b.DrawString(Game1.smallFont, "Egg → Mayonnaise", new Vector2(x + 46, y + 220), Game1.textColor);
        b.DrawString(Game1.smallFont, "Milk → Cheese", new Vector2(x + 46, y + 250), Game1.textColor);
        b.DrawString(Game1.smallFont, "Blueberry → Blueberry Wine", new Vector2(x + 46, y + 280), Game1.textColor);
        b.DrawString(Game1.tinyFont, "These four examples are reference-only; the prototype recipe is the only one enabled. Slime fuel amount is configurable.",
            new Vector2(x + 32, y + 320), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Click anywhere to close", new Vector2(x + width - 174, y + height - 24), Game1.textColor);
    }

    private static void DrawProgressBar(SpriteBatch b, Rectangle bar, float progress, string label)
    {
        b.Draw(Game1.staminaRect, bar, new Color(91, 75, 58));
        if (progress > 0)
            b.Draw(Game1.staminaRect, new Rectangle(bar.X + 3, bar.Y + 3,
                Math.Max(1, (int)((bar.Width - 6) * Math.Clamp(progress, 0f, 1f))), bar.Height - 6), new Color(184, 135, 204));
        b.DrawString(Game1.tinyFont, label, new Vector2(bar.X + 8, bar.Y + 4), Color.White);
    }

    private static int CountItem(string qualifiedItemId) => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == qualifiedItemId)
        .Sum(item => item!.Stack);
}
