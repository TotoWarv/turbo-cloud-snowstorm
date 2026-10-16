using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Incoming.Game.Directory;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.PacketHandlers.Game.Directory;

public class Game2QuickJoinGameMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2QuickJoinGameMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2QuickJoinGameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var summary = await _grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(false);

        if (summary is null)
            return;

        var totals = await _grainFactory
            .GetSnowWarScoreGrain()
            .GetPlayerTotalsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await _grainFactory
            .GetSnowWarDirectoryGrain()
            .QuickJoinAsync(
                ctx.PlayerId,
                new SnowWarPlayerSnapshot
                {
                    UserId = ctx.PlayerId.Value,
                    Name = summary.Name,
                    Figure = summary.Figure,
                    Gender = summary.Gender.ToLegacyString(),
                    TeamId = SnowWarTeams.NONE,
                    SkillLevel = totals.SkillLevel,
                    TotalScore = (int)Math.Min(int.MaxValue, totals.TotalScore),
                    ScoreToNextLevel = totals.ScoreToNextLevel,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
