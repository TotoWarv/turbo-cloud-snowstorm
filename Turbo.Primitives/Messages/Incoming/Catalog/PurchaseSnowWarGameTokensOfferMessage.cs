using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Catalog;

public record PurchaseSnowWarGameTokensOfferMessage : IMessageEvent
{
    public required int OfferId { get; init; }
}
