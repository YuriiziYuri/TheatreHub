using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyActorFromCharacterRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterRoles_Actors_ActorId",
                table: "CharacterRoles");

            migrationBuilder.DropIndex(
                name: "IX_CharacterRoles_ActorId",
                table: "CharacterRoles");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "CharacterRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActorId",
                table: "CharacterRoles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRoles_ActorId",
                table: "CharacterRoles",
                column: "ActorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterRoles_Actors_ActorId",
                table: "CharacterRoles",
                column: "ActorId",
                principalTable: "Actors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
