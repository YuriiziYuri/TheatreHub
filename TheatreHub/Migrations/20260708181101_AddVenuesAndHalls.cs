using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuesAndHalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterRoles_PerformanceId",
                table: "CharacterRoles");

            migrationBuilder.AddColumn<int>(
                name: "HallId",
                table: "Rehearsals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Halls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VenueId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    RentalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HasStage = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasCurtains = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDressingRooms = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasLighting = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMicrophones = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasProjector = table.Column<bool>(type: "INTEGER", nullable: false),
                    EquipmentNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Halls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Halls_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rehearsals_HallId",
                table: "Rehearsals",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_Rehearsals_StartDateTime",
                table: "Rehearsals",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRoles_PerformanceId_Name",
                table: "CharacterRoles",
                columns: new[] { "PerformanceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Halls_VenueId_Name",
                table: "Halls",
                columns: new[] { "VenueId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Name",
                table: "Venues",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Rehearsals_Halls_HallId",
                table: "Rehearsals",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rehearsals_Halls_HallId",
                table: "Rehearsals");

            migrationBuilder.DropTable(
                name: "Halls");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Rehearsals_HallId",
                table: "Rehearsals");

            migrationBuilder.DropIndex(
                name: "IX_Rehearsals_StartDateTime",
                table: "Rehearsals");

            migrationBuilder.DropIndex(
                name: "IX_CharacterRoles_PerformanceId_Name",
                table: "CharacterRoles");

            migrationBuilder.DropColumn(
                name: "HallId",
                table: "Rehearsals");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRoles_PerformanceId",
                table: "CharacterRoles",
                column: "PerformanceId");
        }
    }
}
