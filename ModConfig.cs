namespace taums_slime_ranching;

public sealed class ModConfig
{
    public bool LogRegistrySummary { get; set; } = true;
    public int TrampolineRecipePrice { get; set; } = 2000;
    public int SlimeCushionRecipePrice { get; set; } = 2000;
    public int SlimeSlideRecipePrice { get; set; } = 2000;
    public int SlimeSeesawRecipePrice { get; set; } = 2000;
    public bool ShowShadowCloneVisualEchoes { get; set; } = true;
    [Newtonsoft.Json.JsonProperty("GelRefinerySlimeFuelCost")]
    [System.Text.Json.Serialization.JsonPropertyName("GelRefinerySlimeFuelCost")]
    public int SlimePrinterFuelCost { get; set; } = 10;
    [Newtonsoft.Json.JsonProperty("GelRefineryDurationDays")]
    [System.Text.Json.Serialization.JsonPropertyName("GelRefineryDurationDays")]
    public int SlimePrinterDurationDays { get; set; } = 1;
    public int SlimeTunnelRecipePrice { get; set; } = 5000;
    public int HeatedSlimeStoneRecipePrice { get; set; } = 2000;
    public int SlimePondRecipePrice { get; set; } = 5000;
    public int SlimeSwingRecipePrice { get; set; } = 5000;
    public int SlimeLampRecipePrice { get; set; } = 5000;
    public int SlimeFairyLightsRecipePrice { get; set; } = 10000;
    public int SlimeBallPitRecipePrice { get; set; } = 10000;
    public int SlimeWashingMachineRecipePrice { get; set; } = 10000;
}
