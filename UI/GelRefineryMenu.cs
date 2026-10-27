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
    private string? statusMessage;

    public GelRefineryMenu(SlimeRegistry registry, string machineKey, GelRefineryRules rules,
        Func<bool> startBatch, Func<bool> collectBatch)
        : base(Game1.uiViewport.Width / 2 - 330, Game1.uiViewport.Height / 2 - 220, 660, 440)
    {
        this.registry = registry;
        this.machineKey = machineKey;
        this.rules = rules;
        this.startBatch = startBatch;
        this.collectBatch = collectBatch;
        this.actionButton = new Rectangle(this.xPositionOnScreen + 34, this.yPositionOnScreen + 326, 230, 54);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
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
            b.DrawString(Game1.smallFont, $"Processing time: {this.rules.DurationDays} in-game day(s). Start the batch, then sleep to advance it.",
                new Vector2(left, top + 158), Game1.textColor);
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
            Rectangle bar = new(left, top + 162, this.width - 68, 24);
            b.Draw(Game1.staminaRect, bar, new Color(91, 75, 58));
            if (progress > 0)
                b.Draw(Game1.staminaRect, new Rectangle(bar.X + 3, bar.Y + 3,
                    Math.Max(1, (int)((bar.Width - 6) * progress)), bar.Height - 6), new Color(184, 135, 204));
            b.DrawString(Game1.tinyFont, ready ? "Output is reserved here until collected." : $"{elapsed} / {duration} days",
                new Vector2(left, top + 194), Game1.textColor);
            bool buttonEnabled = ready;
            b.Draw(Game1.staminaRect, this.actionButton, buttonEnabled ? new Color(198, 177, 220) : new Color(210, 210, 210));
            b.DrawString(Game1.smallFont, buttonEnabled ? "Collect Refined Quartz" : "Processing",
                new Vector2(this.actionButton.X + 18, this.actionButton.Y + 17), Game1.textColor);
        }

        b.DrawString(Game1.tinyFont, this.statusMessage ?? "Recipe, fuel, duration, and machine price are early test values.",
            new Vector2(this.xPositionOnScreen + 282, this.actionButton.Y + 10), Game1.textColor);
        b.DrawString(Game1.tinyFont, "Esc: close",
            new Vector2(this.xPositionOnScreen + this.width - 100, this.yPositionOnScreen + this.height - 24), Game1.textColor);
        this.drawMouse(b);
    }

    private static int CountItem(string qualifiedItemId) => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == qualifiedItemId)
        .Sum(item => item!.Stack);
}
