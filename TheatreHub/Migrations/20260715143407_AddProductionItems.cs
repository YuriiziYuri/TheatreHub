using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponsibleName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    StorageLocation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    NeededBy = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PerformanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActId = table.Column<int>(type: "INTEGER", nullable: true),
                    SceneId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionItems_Acts_ActId",
                        column: x => x.ActId,
                        principalTable: "Acts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionItems_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionItems_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_ActId",
                table: "ProductionItems",
                column: "ActId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_NeededBy",
                table: "ProductionItems",
                column: "NeededBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_PerformanceId",
                table: "ProductionItems",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_SceneId",
                table: "ProductionItems",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_Status",
                table: "ProductionItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionItems_Type",
                table: "ProductionItems",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionItems");
        }
    }
}
