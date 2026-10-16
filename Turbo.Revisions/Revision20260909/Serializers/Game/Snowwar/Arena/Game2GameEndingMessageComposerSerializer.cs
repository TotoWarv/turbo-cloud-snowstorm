using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Arena;

internal class Game2GameEndingMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameEndingMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameEndingMessageComposer message)
    {
        packet
            .WriteInteger(message.TimeToNextState)
            .WriteBoolean(message.IsDeathMatch)
            .WriteInteger((int)message.ResultType)
            .WriteInteger(message.WinnerId)
            .WriteInteger(message.Teams.Length);

        foreach (var team in message.Teams)
        {
            packet
                .WriteInteger(team.TeamId)
                .WriteInteger(team.Score)
                .WriteInteger(team.Players.Length);

            foreach (var player in team.Players)
            {
                packet
                    .WriteString(player.UserName)
                    .WriteInteger(player.UserId)
                    .WriteString(player.Figure)
                    .WriteString(player.Gender)
                    .WriteInteger(player.Score)
                    .WriteInteger(player.Stats.Score)
                    .WriteInteger(player.Stats.Kills)
                    .WriteInteger(player.Stats.Deaths)
                    .WriteInteger(player.Stats.SnowballHits)
                    .WriteInteger(player.Stats.SnowballHitsTaken)
                    .WriteInteger(player.Stats.SnowballsThrown)
                    .WriteInteger(player.Stats.SnowballsCreated)
                    .WriteInteger(player.Stats.SnowballsFromMachine)
                    .WriteInteger(player.Stats.FriendlyHits)
                    .WriteInteger(player.Stats.FriendlyKills);
            }
        }

        packet.WriteInteger(message.PlayerWithMostKills).WriteInteger(message.PlayerWithMostHits);
    }
}
