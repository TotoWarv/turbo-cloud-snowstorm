using System;
using System.Collections.Generic;
using System.Numerics;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

public static class SnowWarChecksum
{
    internal const uint SEED = 0x534E4F57;

    public static int Compute(int turn, IReadOnlyList<SnowWarGameObjectSnapshot> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        var checksum = new SnowWarChecksumBuilder();
        checksum.Add(turn);
        checksum.Add(objects.Count);

        foreach (var gameObject in objects)
        {
            checksum.Add((int)gameObject.Type);
            checksum.Add(gameObject.Id);

            foreach (var variable in gameObject.Variables)
            {
                checksum.Add(variable);
            }
        }

        return checksum.Finish();
    }

    internal static uint Mix(uint hash, int word)
    {
        var mixed = unchecked((uint)word * 0xCC9E2D51);
        mixed = BitOperations.RotateLeft(mixed, 15);
        mixed = unchecked(mixed * 0x1B873593);
        hash ^= mixed;
        hash = BitOperations.RotateLeft(hash, 13);
        return unchecked((hash * 5) + 0xE6546B64);
    }

    internal static int Finish(uint hash, int count)
    {
        hash ^= unchecked((uint)count);
        hash ^= hash >> 16;
        hash = unchecked(hash * 0x85EBCA6B);
        hash ^= hash >> 13;
        hash = unchecked(hash * 0xC2B2AE35);
        hash ^= hash >> 16;
        return unchecked((int)hash);
    }
}
