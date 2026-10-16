using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarWorld
{
    private readonly List<SnowWarGameObject> _objects = [];
    private readonly Dictionary<int, SnowWarGameObject> _objectsById = [];
    private readonly List<SnowWarFuseObject> _fuseObjects = [];
    private readonly List<SnowWarRayGun> _rayGuns = [];
    private readonly int[] _teamScores = new int[SnowWarConstants.TEAM_COUNT + 1];
    private readonly List<int> _variables = [];
    private int _nextObjectId = 1;

    public SnowWarWorld(SnowWarLevelData level)
    {
        Grid = new SnowWarGrid(level.Width, level.Height, level.HeightMap);

        foreach (var data in level.Objects)
        {
            _fuseObjects.Add(
                new SnowWarFuseObject(
                    _fuseObjects.Count + 1,
                    SnowWarFurniCatalog.Get(data.Name),
                    data.X,
                    data.Y,
                    data.Direction,
                    data.Altitude
                )
            );
        }

        if (level.Backdrop is { ImageUrl.Length: > 0 } backdrop)
        {
            _fuseObjects.Add(
                new SnowWarFuseObject(
                    _fuseObjects.Count + 1,
                    SnowWarFurniCatalog.BACKDROP,
                    backdrop.X,
                    backdrop.Y,
                    1,
                    0
                )
            );
        }

        foreach (var fuse in _fuseObjects)
        {
            RegisterOnTiles(fuse);
        }

        foreach (var fuse in _fuseObjects)
        {
            CreateGameObject(fuse);
        }
    }

    public SnowWarGrid Grid { get; }

    public IReadOnlyList<SnowWarGameObject> Objects => _objects;

    public IReadOnlyList<SnowWarFuseObject> FuseObjects => _fuseObjects;

    public IReadOnlyList<SnowWarRayGun> RayGuns => _rayGuns;

    public int AllocateObjectId() => _nextObjectId++;

    public bool Contains(int id) => _objectsById.ContainsKey(id);

    public T? FindActive<T>(int id)
        where T : SnowWarGameObject =>
        _objectsById.TryGetValue(id, out var found) && found is T typed && typed.IsActive
            ? typed
            : null;

    public SnowWarRayGun? FindRayGun(int fuseId) =>
        _rayGuns.FirstOrDefault(gun => gun.FuseId == fuseId);

    public void Add(SnowWarGameObject gameObject)
    {
        _objects.Add(gameObject);
        _objectsById[gameObject.Id] = gameObject;

        if (gameObject.Id >= _nextObjectId)
        {
            _nextObjectId = gameObject.Id + 1;
        }
    }

    public SnowWarHuman AddHuman(SnowWarHumanEntry entry, int tileX, int tileY)
    {
        var tile =
            Grid.Get(tileX, tileY)
            ?? throw new ArgumentOutOfRangeException(nameof(tileX), "The tile does not exist.");
        var facing = SnowWarMath.Dir360ToDir8(
            SnowWarMath.Angle(
                SnowWarConstants.SPAWN_FACING_X - tileX,
                SnowWarConstants.SPAWN_FACING_Y - tileY
            )
        );
        var human = new SnowWarHuman(AllocateObjectId(), entry, tile, facing);
        Add(human);
        return human;
    }

    public void AwardPoints(SnowWarHuman human, int points)
    {
        human.AddScore(points);
        _teamScores[human.Team] += points;
    }

    public int GetTeamScore(int team) =>
        team >= 1 && team <= SnowWarConstants.TEAM_COUNT ? _teamScores[team] : 0;

    public void ForgetThrower(int humanId)
    {
        foreach (var gameObject in _objects)
        {
            if (gameObject is SnowWarSnowball snowball && snowball.ThrowerId == humanId)
            {
                snowball.ForgetThrower();
            }
        }
    }

    public void UpdateObjects()
    {
        for (var i = 0; i < _objects.Count; i++)
        {
            var gameObject = _objects[i];

            if (gameObject.IsActive)
            {
                gameObject.Update(this);
            }
        }
    }

    public void RemoveInactive() =>
        _objects.RemoveAll(gameObject =>
        {
            if (gameObject.IsActive)
            {
                return false;
            }

            _objectsById.Remove(gameObject.Id);
            return true;
        });

    public int ComputeChecksum(int turn)
    {
        var active = 0;

        foreach (var gameObject in _objects)
        {
            if (gameObject.IsActive)
            {
                active++;
            }
        }

        var checksum = new SnowWarChecksumBuilder();
        checksum.Add(turn);
        checksum.Add(active);

        foreach (var gameObject in _objects)
        {
            if (!gameObject.IsActive)
            {
                continue;
            }

            _variables.Clear();
            gameObject.AppendVariables(_variables);
            checksum.Add((int)gameObject.Type);
            checksum.Add(gameObject.Id);

            foreach (var variable in _variables)
            {
                checksum.Add(variable);
            }
        }

        return checksum.Finish();
    }

    public ImmutableArray<SnowWarGameObjectSnapshot> Snapshot() =>
        [.. _objects.Where(gameObject => gameObject.IsActive).Select(ToSnapshot)];

    private static SnowWarGameObjectSnapshot ToSnapshot(SnowWarGameObject gameObject)
    {
        var variables = new List<int>();
        gameObject.AppendVariables(variables);

        return new SnowWarGameObjectSnapshot
        {
            Type = gameObject.Type,
            Id = gameObject.Id,
            Variables = [.. variables],
            Strings = [.. gameObject.Strings],
        };
    }

    private void RegisterOnTiles(SnowWarFuseObject fuse)
    {
        var anchor = Grid.Get(fuse.X, fuse.Y);

        if (anchor is null)
        {
            return;
        }

        anchor.RegisterAnchor(fuse);

        for (var k = 1; k < fuse.FootprintWidth; k++)
        {
            Grid.Get(fuse.X + k, fuse.Y)?.RegisterFootprint(fuse);
        }

        for (var k = 1; k < fuse.FootprintDepth; k++)
        {
            Grid.Get(fuse.X, fuse.Y + k)?.RegisterFootprint(fuse);
        }
    }

    private void CreateGameObject(SnowWarFuseObject fuse)
    {
        var tile = Grid.Get(fuse.X, fuse.Y);

        if (tile is null)
        {
            return;
        }

        switch (fuse.Definition.Role)
        {
            case SnowWarFurniRole.Tree:
                Add(new SnowWarTree(AllocateObjectId(), fuse, tile));
                break;
            case SnowWarFurniRole.Pile:
                Add(new SnowWarPile(AllocateObjectId(), fuse, tile));
                break;
            case SnowWarFurniRole.Machine:
                Add(new SnowWarMachine(AllocateObjectId(), fuse, tile));
                break;
            case SnowWarFurniRole.RayGun:
                if (SnowWarRayGun.TryCreate(fuse) is { } gun)
                {
                    _rayGuns.Add(gun);
                }

                break;
            case SnowWarFurniRole.Obstacle:
            case SnowWarFurniRole.Backdrop:
                break;
        }
    }
}
