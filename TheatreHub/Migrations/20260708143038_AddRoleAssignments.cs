using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),

                    CharacterRoleId = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    ActorId = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    CastType = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    StartDate = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: false),

                    EndDate = table.Column<DateTime>(
                        type: "TEXT",
                        nullable: true),

                    IsCurrent = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false),

                    IsPublic = table.Column<bool>(
                        type: "INTEGER",
                        nullable: false),

                    Status = table.Column<int>(
                        type: "INTEGER",
                        nullable: false),

                    Notes = table.Column<string>(
                        type: "TEXT",
                        maxLength: 1000,
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_RoleAssignments",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_RoleAssignments_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_RoleAssignments_CharacterRoles_CharacterRoleId",
                        column: x => x.CharacterRoleId,
                        principalTable: "CharacterRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_ActorId",
                table: "RoleAssignments",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_CharacterRoleId",
                table: "RoleAssignments",
                column: "CharacterRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_CharacterRoleId_ActorId_StartDate",
                table: "RoleAssignments",
                columns: new[]
                {
                    "CharacterRoleId",
                    "ActorId",
                    "StartDate"
                },
                unique: true);

            // Перенесення старих призначень із CharacterRoles.ActorId
            // до нової таблиці RoleAssignments.
            migrationBuilder.Sql(
                """
                INSERT INTO "RoleAssignments"
                (
                    "CharacterRoleId",
                    "ActorId",
                    "CastType",
                    "StartDate",
                    "EndDate",
                    "IsCurrent",
                    "IsPublic",
                    "Status",
                    "Notes"
                )
                SELECT
                    "Id",
                    "ActorId",
                    0,
                    CURRENT_TIMESTAMP,
                    NULL,
                    1,
                    0,
                    2,
                    'Перенесено з попередньої версії TheatreHub'
                FROM "CharacterRoles"
                WHERE "ActorId" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleAssignments");
        }
    }
}
