using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRehearsalStructureTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rehearsals_Performances_PerformanceId",
                table: "Rehearsals");

            migrationBuilder.DropIndex(
                name: "IX_Rehearsals_StartDateTime",
                table: "Rehearsals");

            migrationBuilder.AddColumn<int>(
                name: "ActId",
                table: "Rehearsals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SceneId",
                table: "Rehearsals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rehearsals_ActId",
                table: "Rehearsals",
                column: "ActId");

            migrationBuilder.CreateIndex(
                name: "IX_Rehearsals_SceneId",
                table: "Rehearsals",
                column: "SceneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rehearsals_Acts_ActId",
                table: "Rehearsals",
                column: "ActId",
                principalTable: "Acts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rehearsals_Performances_PerformanceId",
                table: "Rehearsals",
                column: "PerformanceId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rehearsals_Scenes_SceneId",
                table: "Rehearsals",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rehearsals_Acts_ActId",
                table: "Rehearsals");

            migrationBuilder.DropForeignKey(
                name: "FK_Rehearsals_Performances_PerformanceId",
                table: "Rehearsals");

            migrationBuilder.DropForeignKey(
                name: "FK_Rehearsals_Scenes_SceneId",
                table: "Rehearsals");

            migrationBuilder.DropIndex(
                name: "IX_Rehearsals_ActId",
                table: "Rehearsals");

            migrationBuilder.DropIndex(
                name: "IX_Rehearsals_SceneId",
                table: "Rehearsals");

            migrationBuilder.DropColumn(
                name: "ActId",
                table: "Rehearsals");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Rehearsals");

            migrationBuilder.CreateIndex(
                name: "IX_Rehearsals_StartDateTime",
                table: "Rehearsals",
                column: "StartDateTime");

            migrationBuilder.AddForeignKey(
                name: "FK_Rehearsals_Performances_PerformanceId",
                table: "Rehearsals",
                column: "PerformanceId",
                principalTable: "Performances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
