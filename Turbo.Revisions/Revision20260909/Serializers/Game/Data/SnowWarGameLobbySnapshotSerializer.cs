using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarGameLobbySnapshotSerializer
{
    public static void Serialize(IServerPacket packet, SnowWarGameLobbySnapshot lobby)
    {
        packet
            .WriteInteger(lobby.GameId)
            .WriteString(lobby.LevelName)
            .WriteInteger(lobby.GameType)
            .WriteInteger(lobby.FieldType)
            .WriteInteger(lobby.NumberOfTeams)
            .WriteInteger(lobby.MaximumPlayers)
            .WriteString(lobby.OwningPlayerName)
            .WriteInteger(lobby.LevelEntryId)
            .WriteInteger(lobby.Players.Length);

        foreach (var player in lobby.Players)
        {
            SnowWarPlayerSnapshotSerializer.Serialize(packet, player);
        }
    }
}
