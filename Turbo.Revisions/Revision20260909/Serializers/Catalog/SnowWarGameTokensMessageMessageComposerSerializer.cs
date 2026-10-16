using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Catalog;

internal class SnowWarGameTokensMessageMessageComposerSerializer(int header)
    : AbstractSerializer<SnowWarGameTokensMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SnowWarGameTokensMessageMessageComposer message
    )
    {
        packet.WriteInteger(message.Offers.Length);

        foreach (var offer in message.Offers)
        {
            packet
                .WriteInteger(offer.Id)
                .WriteString(offer.LocalizationId)
                .WriteInteger(offer.PriceCredits)
                .WriteInteger(offer.PricePoints)
                .WriteInteger(offer.PointsType);
        }
    }
}
