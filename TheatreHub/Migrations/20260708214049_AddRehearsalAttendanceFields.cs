using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRehearsalAttendanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbsenceReason",
                table: "RehearsalParticipants",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceStatus",
                table: "RehearsalParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "RehearsalParticipants",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "RehearsalParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResponseStatus",
                table: "RehearsalParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsenceReason",
                table: "RehearsalParticipants");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "RehearsalParticipants");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "RehearsalParticipants");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "RehearsalParticipants");

            migrationBuilder.DropColumn(
                name: "ResponseStatus",
                table: "RehearsalParticipants");
        }
    }
}
