using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTheatreTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TheatreTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ResponsibleName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PerformanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActId = table.Column<int>(type: "INTEGER", nullable: true),
                    SceneId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheatreTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheatreTasks_Acts_ActId",
                        column: x => x.ActId,
                        principalTable: "Acts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreTasks_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreTasks_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTasks_ActId",
                table: "TheatreTasks",
                column: "ActId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTasks_Deadline",
                table: "TheatreTasks",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTasks_PerformanceId",
                table: "TheatreTasks",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTasks_SceneId",
                table: "TheatreTasks",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTasks_Status",
                table: "TheatreTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TheatreTasks");
        }
    }
}
