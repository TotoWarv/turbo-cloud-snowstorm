namespace Turbo.Games.SnowWar.Simulation;

internal struct SnowWarChecksumBuilder()
{
    private uint _hash = SnowWarChecksum.SEED;
    private int _count;

    public void Add(int word)
    {
        _hash = SnowWarChecksum.Mix(_hash, word);
        _count++;
    }

    public readonly int Finish() => SnowWarChecksum.Finish(_hash, _count);
}
