using Microsoft.Xna.Framework;

namespace taums_slime_ranching.Services;

internal enum EnrichmentActivityKind
{
    Bounce,
    Rest,
    Swing,
    PairedSwing,
    BallPit,
    LampGathering
}

internal sealed record EnrichmentStationDefinition(
    string QualifiedItemId,
    EnrichmentActivityKind ActivityKind,
    float BounceHeightPixels = 0f,
    float CompressionHeightPixels = 0f,
    float HorizontalTravelPixels = 0f,
    bool IsResting = false,
    float SwingDistancePixels = 0f,
    bool UsesGenomeSizeBounce = false,
    int? MaximumParticipants = 1,
    IReadOnlyList<Point>? ApproachOffsets = null,
    int MinimumParticipants = 1,
    int MinimumDurationMinutes = 120,
    int MaximumDurationMinutes = 180)
{
    public IReadOnlyList<Point> ApproachTileOffsets =>
        this.ApproachOffsets ?? EnrichmentStationCatalog.DefaultApproachTileOffsets;
}

/// <summary>Station behavior data kept separate from activity scheduling and animation cleanup.</summary>
internal static class EnrichmentStationCatalog
{
    public static readonly IReadOnlyList<Point> DefaultApproachTileOffsets = Array.AsReadOnly(new[]
    {
        new Point(0, -1),
        new Point(1, 0),
        new Point(0, 1),
        new Point(-1, 0)
    });

    private static readonly IReadOnlyDictionary<string, EnrichmentStationDefinition> Stations = new EnrichmentStationDefinition[]
    {
        new(FurnitureContent.TrampolineQualifiedId, EnrichmentActivityKind.Bounce, UsesGenomeSizeBounce: true),
        new(FurnitureContent.SlimeCushionQualifiedId, EnrichmentActivityKind.Bounce, BounceHeightPixels: 9f, CompressionHeightPixels: 10f),
        new(FurnitureContent.SlimeSlideQualifiedId, EnrichmentActivityKind.Bounce, BounceHeightPixels: 8f, CompressionHeightPixels: 3f, HorizontalTravelPixels: 30f),
        new(FurnitureContent.SlimeSeesawQualifiedId, EnrichmentActivityKind.PairedSwing,
            SwingDistancePixels: 8f, MaximumParticipants: 2, MinimumParticipants: 2),
        new(FurnitureContent.SlimeBallPitQualifiedId, EnrichmentActivityKind.BallPit),
        new(FurnitureContent.SlimeTunnelQualifiedId, EnrichmentActivityKind.Bounce, BounceHeightPixels: 5f, CompressionHeightPixels: 2f, HorizontalTravelPixels: 42f),
        new(FurnitureContent.HeatedSlimeStoneQualifiedId, EnrichmentActivityKind.Rest, CompressionHeightPixels: 6f, IsResting: true),
        new(FurnitureContent.SlimePondQualifiedId, EnrichmentActivityKind.Bounce, BounceHeightPixels: 4f),
        new(FurnitureContent.SlimeSwingQualifiedId, EnrichmentActivityKind.Swing, SwingDistancePixels: 8f),
        new(FurnitureContent.SlimeLampQualifiedId, EnrichmentActivityKind.LampGathering,
            MaximumParticipants: null)
    }.ToDictionary(station => station.QualifiedItemId, StringComparer.Ordinal);

    public static bool TryGet(string qualifiedItemId, out EnrichmentStationDefinition station) =>
        Stations.TryGetValue(qualifiedItemId, out station!);
}
