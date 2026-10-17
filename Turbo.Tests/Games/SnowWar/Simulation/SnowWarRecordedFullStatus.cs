namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarRecordedFullStatus(
    int Turn,
    int Checksum,
    IReadOnlyList<SnowWarRecordedObject> Objects,
    int[][][] Events
);
