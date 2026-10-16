using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;
using Turbo.Primitives.Messages.Outgoing.Catalog;

namespace Turbo.PacketHandlers.Catalog;

public class GetSnowWarGameTokensOfferMessageHandler
    : IMessageHandler<GetSnowWarGameTokensOfferMessage>
{
    public async ValueTask HandleAsync(
        GetSnowWarGameTokensOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        await ctx.SendComposerAsync(new SnowWarGameTokensMessageMessageComposer { Offers = [] }, ct)
            .ConfigureAwait(false);
    }
}
