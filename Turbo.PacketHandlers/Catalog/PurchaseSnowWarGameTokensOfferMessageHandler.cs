using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Messages.Registry;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Messages.Incoming.Catalog;

namespace Turbo.PacketHandlers.Catalog;

public class PurchaseSnowWarGameTokensOfferMessageHandler(
    ILogger<PurchaseSnowWarGameTokensOfferMessageHandler> logger
) : IMessageHandler<PurchaseSnowWarGameTokensOfferMessage>
{
    private readonly ILogger<PurchaseSnowWarGameTokensOfferMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        PurchaseSnowWarGameTokensOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        _logger.LogWarning(
            "Player {PlayerId} tried to buy SnowWar game tokens offer {OfferId}; games are free",
            ctx.PlayerId,
            message.OfferId
        );

        await ctx.SendPurchaseErrorAsync(CatalogPurchaseErrorType.OfferNotFound, ct)
            .ConfigureAwait(false);
    }
}
