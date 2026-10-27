namespace taums_slime_ranching.Models;

/// <summary>Inherited identity, separate from an individual slime's current state.</summary>
public sealed class SlimeGenome
{
    public Guid Id { get; set; }
    public Guid? ParentA { get; set; }
    public Guid? ParentB { get; set; }
    public string? ParentAName { get; set; }
    public string? ParentBName { get; set; }
    public int Generation { get; set; }
    public string PrimarySpecies { get; set; } = "Green Slime";
    public string? SecondarySpecies { get; set; }
    public string VisualColor { get; set; } = "#00FF00";
    public string Size { get; set; } = "Normal";
    public bool IsShiny { get; set; }
    public string RarityTier { get; set; } = "Common";
    public string? MutationTrait { get; set; }
    /// <summary>Stable identifiers for the base abilities carried by this species genome.</summary>
    public List<string> AbilityIds { get; set; } = new();
    /// <summary>Inherited favorite vanilla object, such as (O)168 for Trash.</summary>
    public string? LikedFoodItemId { get; set; }
}
