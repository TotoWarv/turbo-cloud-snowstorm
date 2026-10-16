using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Game;

[Table("snowwar_scores")]
[PrimaryKey(nameof(PlayerId), nameof(WeekStart))]
[Index(nameof(WeekStart), nameof(Score))]
public sealed class SnowWarScoreEntity
{
    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("week_start", TypeName = "date")]
    public DateOnly WeekStart { get; set; }

    [Column("score")]
    public long Score { get; set; }

    [Column("matches")]
    public int Matches { get; set; }
}
