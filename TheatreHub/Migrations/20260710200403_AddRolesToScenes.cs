using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesToScenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SceneRoles",
                columns: table => new
                {
                    SceneId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterRoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneRoles", x => new { x.SceneId, x.CharacterRoleId });
                    table.ForeignKey(
                        name: "FK_SceneRoles_CharacterRoles_CharacterRoleId",
                        column: x => x.CharacterRoleId,
                        principalTable: "CharacterRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SceneRoles_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SceneRoles_CharacterRoleId",
                table: "SceneRoles",
                column: "CharacterRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SceneRoles");
        }
    }
}
