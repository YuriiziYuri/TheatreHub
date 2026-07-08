using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePerformanceStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
        UPDATE "Performances"
        SET "Status" =
            CASE "Status"
                WHEN 4 THEN 5
                WHEN 5 THEN 6
                WHEN 6 THEN 7
                WHEN 7 THEN 8
                ELSE "Status"
            END;
        """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
        UPDATE "Performances"
        SET "Status" =
            CASE "Status"
                WHEN 4 THEN 3
                WHEN 5 THEN 4
                WHEN 6 THEN 5
                WHEN 7 THEN 6
                WHEN 8 THEN 7
                ELSE "Status"
            END;
        """);
        }
    }
}
