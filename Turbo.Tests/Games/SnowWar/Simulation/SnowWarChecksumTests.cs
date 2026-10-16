using System.Collections.Immutable;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarChecksumTests
{
    private static readonly SnowWarGameObjectSnapshot PILE = Snapshot(
        SnowWarGameObjectType.Pile,
        1,
        9600,
        3200,
        12,
        12,
        2
    );

    private static readonly SnowWarGameObjectSnapshot BALL = Snapshot(
        SnowWarGameObjectType.Snowball,
        7,
        12421,
        16000,
        3000,
        90,
        0,
        7,
        3,
        5,
        2000
    );

    [Fact]
    public void Compute_MatchesTheReferenceVectorsOfTheSpecification()
    {
        Assert.Equal(CHECKSUM_EMPTY_TURN_0, SnowWarChecksum.Compute(0, []));
        Assert.Equal(CHECKSUM_EMPTY_TURN_1, SnowWarChecksum.Compute(1, []));
        Assert.Equal(CHECKSUM_PILE_AND_BALL_TURN_7, SnowWarChecksum.Compute(7, [PILE, BALL]));
    }

    [Fact]
    public void Compute_DependsOnTheTurn() =>
        Assert.NotEqual(
            SnowWarChecksum.Compute(7, [PILE, BALL]),
            SnowWarChecksum.Compute(8, [PILE, BALL])
        );

    [Fact]
    public void Compute_DependsOnTheObjectOrder() =>
        Assert.NotEqual(
            SnowWarChecksum.Compute(7, [PILE, BALL]),
            SnowWarChecksum.Compute(7, [BALL, PILE])
        );

    [Fact]
    public void Compute_DependsOnEveryVariable()
    {
        var reference = SnowWarChecksum.Compute(7, [PILE, BALL]);

        for (var index = 0; index < BALL.Variables.Length; index++)
        {
            var changed = BALL with
            {
                Variables = BALL.Variables.SetItem(index, BALL.Variables[index] + 1),
            };
            Assert.NotEqual(reference, SnowWarChecksum.Compute(7, [PILE, changed]));
        }

        Assert.NotEqual(reference, SnowWarChecksum.Compute(7, [PILE, BALL with { Id = 8 }]));
    }

    [Fact]
    public void Compute_IgnoresTheHumanStrings()
    {
        var human = Snapshot(SnowWarGameObjectType.Human, 3, new int[17]) with
        {
            Strings = ["a", "b", "c", "d"],
        };

        Assert.Equal(
            SnowWarChecksum.Compute(2, [human]),
            SnowWarChecksum.Compute(2, [human with { Strings = ["e", "f", "g", "h"] }])
        );
    }

    [Fact]
    public void RunTurn_ReturnsTheChecksumOfTheBroadcastObjects()
    {
        var arena = new SnowWarArena(Open(20, 10, Furni("snst_ballpile", 9, 9)), 1);
        var human = AddHuman(arena, 1, 2, 5);
        ThrowAt(arena, human, 12, 5);

        for (var i = 0; i < 4; i++)
        {
            var result = arena.RunTurn();
            Assert.Equal(i, result.Turn);
            Assert.Equal(SnowWarChecksum.Compute(i, arena.GetFullStatus()), result.Checksum);
        }
    }

    private const int CHECKSUM_EMPTY_TURN_0 = -133030364;
    private const int CHECKSUM_EMPTY_TURN_1 = -92070181;
    private const int CHECKSUM_PILE_AND_BALL_TURN_7 = 629429088;

    private static SnowWarGameObjectSnapshot Snapshot(
        SnowWarGameObjectType type,
        int id,
        params int[] variables
    ) =>
        new()
        {
            Type = type,
            Id = id,
            Variables = [.. variables],
            Strings = ImmutableArray<string>.Empty,
        };
}
