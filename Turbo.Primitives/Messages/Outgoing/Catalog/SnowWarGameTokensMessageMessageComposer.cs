using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record SnowWarGameTokensMessageMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SnowWarTokenOfferSnapshot> Offers { get; init; }
}
