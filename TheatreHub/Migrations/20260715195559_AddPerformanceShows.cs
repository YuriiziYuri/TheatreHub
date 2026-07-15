using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceShows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceShows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PerformanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    HallId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExternalLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectedAudienceCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualAudienceCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceShows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceShows_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceShows_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceShows_HallId",
                table: "PerformanceShows",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceShows_PerformanceId",
                table: "PerformanceShows",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceShows_StartDateTime",
                table: "PerformanceShows",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceShows_Status",
                table: "PerformanceShows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceShows_Type",
                table: "PerformanceShows",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceShows");
        }
    }
}
