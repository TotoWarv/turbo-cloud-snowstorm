using System;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarRandom(int seed)
{
    private uint _state = unchecked((uint)seed);

    public int Next(int exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMax);
        unchecked
        {
            _state += 0x6D2B79F5;
            var value = _state;
            value = (value ^ (value >> 15)) * (value | 1);
            value ^= value + ((value ^ (value >> 7)) * (value | 61));
            value ^= value >> 14;
            return (int)(value % (uint)exclusiveMax);
        }
    }
}
