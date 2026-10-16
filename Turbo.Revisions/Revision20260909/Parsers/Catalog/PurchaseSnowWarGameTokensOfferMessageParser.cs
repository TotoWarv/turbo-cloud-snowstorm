using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Parsers.Catalog;

internal class PurchaseSnowWarGameTokensOfferMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PurchaseSnowWarGameTokensOfferMessage { OfferId = packet.PopInt() };
}
