using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTheatreTaskComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TheatreTaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TheatreTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheatreTaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheatreTaskComments_TheatreTasks_TheatreTaskId",
                        column: x => x.TheatreTaskId,
                        principalTable: "TheatreTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTaskComments_CreatedAt",
                table: "TheatreTaskComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreTaskComments_TheatreTaskId",
                table: "TheatreTaskComments",
                column: "TheatreTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TheatreTaskComments");
        }
    }
}
