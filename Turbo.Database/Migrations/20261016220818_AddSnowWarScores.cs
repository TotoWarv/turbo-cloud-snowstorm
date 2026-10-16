using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    public partial class AddSnowWarScores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "snowwar_scores",
                    columns: table => new
                    {
                        player_id = table.Column<int>(type: "int", nullable: false),
                        week_start = table.Column<DateOnly>(type: "date", nullable: false),
                        score = table.Column<long>(type: "bigint", nullable: false),
                        matches = table.Column<int>(type: "int", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey(
                            "PK_snowwar_scores",
                            x => new { x.player_id, x.week_start }
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_snowwar_scores_week_start_score",
                table: "snowwar_scores",
                columns: new[] { "week_start", "score" }
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "snowwar_scores");
        }
    }
}
