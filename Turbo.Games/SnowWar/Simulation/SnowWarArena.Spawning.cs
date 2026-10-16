using System;
using System.Collections.Generic;

namespace Turbo.Games.SnowWar.Simulation;

public sealed partial class SnowWarArena
{
    public bool TryAddHuman(SnowWarHumanEntry entry, out int humanId)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var tile = PickTeamSpawn(entry.Team) ?? PickSpreadSpawn();

        if (tile is null)
        {
            humanId = 0;
            return false;
        }

        return TryAddHumanAt(entry, tile.X, tile.Y, out humanId);
    }

    internal bool TryAddHumanAt(SnowWarHumanEntry entry, int tileX, int tileY, out int humanId)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfLessThan(entry.Team, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entry.Team, SnowWarConstants.TEAM_COUNT);

        var tile = World.Grid.Get(tileX, tileY);

        if (tile is null || !tile.IsWalkable)
        {
            humanId = 0;
            return false;
        }

        _spawnTiles.Add(tile);
        humanId = World.AddHuman(entry, tileX, tileY).Id;
        return true;
    }

    private SnowWarTile? PickTeamSpawn(int team)
    {
        if (team < 1 || team > _level.TeamSpawns.Length)
        {
            return null;
        }

        var candidates = new List<SnowWarTile>();

        foreach (var position in _level.TeamSpawns[team - 1])
        {
            var tile = World.Grid.Get(position.X, position.Y);

            if (tile is not null && tile.IsWalkable && !_spawnTiles.Contains(tile))
            {
                candidates.Add(tile);
            }
        }

        return Pick(candidates);
    }

    private SnowWarTile? PickSpreadSpawn()
    {
        for (
            var distance = SnowWarConstants.SPAWN_FIRST_DISTANCE;
            distance >= 0;
            distance -= SnowWarConstants.SPAWN_DISTANCE_STEP
        )
        {
            var minimum = Math.Max(1, distance * distance);
            var candidates = new List<SnowWarTile>();

            foreach (var tile in World.Grid.Tiles)
            {
                if (tile.IsWalkable && IsFarFromEverySpawn(tile, minimum))
                {
                    candidates.Add(tile);
                }
            }

            if (candidates.Count > 0)
            {
                return Pick(candidates);
            }
        }

        return null;
    }

    private bool IsFarFromEverySpawn(SnowWarTile tile, int minimumSquared)
    {
        foreach (var spawn in _spawnTiles)
        {
            var dx = tile.X - spawn.X;
            var dy = tile.Y - spawn.Y;

            if ((dx * dx) + (dy * dy) < minimumSquared)
            {
                return false;
            }
        }

        return true;
    }

    private SnowWarTile? Pick(List<SnowWarTile> candidates) =>
        candidates.Count == 0 ? null : candidates[_random.Next(candidates.Count)];
}
