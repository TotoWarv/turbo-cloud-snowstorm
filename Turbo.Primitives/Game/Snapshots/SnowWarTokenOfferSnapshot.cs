using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarTokenOfferSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string LocalizationId { get; init; }

    [Id(2)]
    public required int PriceCredits { get; init; }

    [Id(3)]
    public required int PricePoints { get; init; }

    [Id(4)]
    public required int PointsType { get; init; }
}
