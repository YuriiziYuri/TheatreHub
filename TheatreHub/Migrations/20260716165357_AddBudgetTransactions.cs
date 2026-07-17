using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheatreHub.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PlannedBudget",
                table: "Performances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PerformanceId = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformanceShowId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductionItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PlannedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ActualAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAutoCalculated = table.Column<bool>(type: "INTEGER", nullable: false),
                    AudienceCount = table.Column<int>(type: "INTEGER", nullable: true),
                    TicketPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ResponsibleName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_PerformanceShows_PerformanceShowId",
                        column: x => x.PerformanceShowId,
                        principalTable: "PerformanceShows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_Performances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "Performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_ProductionItems_ProductionItemId",
                        column: x => x.ProductionItemId,
                        principalTable: "ProductionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Category",
                table: "BudgetTransactions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Currency",
                table: "BudgetTransactions",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_PerformanceId",
                table: "BudgetTransactions",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_PerformanceShowId",
                table: "BudgetTransactions",
                column: "PerformanceShowId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_ProductionItemId",
                table: "BudgetTransactions",
                column: "ProductionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Status",
                table: "BudgetTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionDate",
                table: "BudgetTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_Type",
                table: "BudgetTransactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "PlannedBudget",
                table: "Performances");
        }
    }
}
