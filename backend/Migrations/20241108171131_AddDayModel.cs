using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDayModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TradingSessions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "DayId",
                table: "FiveMinuteBars",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Days",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: true),
                    TradingSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Days_TradingSessions_TradingSessionId",
                        column: x => x.TradingSessionId,
                        principalTable: "TradingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiveMinuteBars_DayId",
                table: "FiveMinuteBars",
                column: "DayId");

            migrationBuilder.CreateIndex(
                name: "IX_Days_TradingSessionId",
                table: "Days",
                column: "TradingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_FiveMinuteBars_Days_DayId",
                table: "FiveMinuteBars",
                column: "DayId",
                principalTable: "Days",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiveMinuteBars_Days_DayId",
                table: "FiveMinuteBars");

            migrationBuilder.DropTable(
                name: "Days");

            migrationBuilder.DropIndex(
                name: "IX_FiveMinuteBars_DayId",
                table: "FiveMinuteBars");

            migrationBuilder.DropColumn(
                name: "DayId",
                table: "FiveMinuteBars");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "TotalOrders", "TotalProfit", "TotalTradingDays", "Username" },
                values: new object[] { 5, "75K3eLr+dx6JJFuJ7LwIpEpOFmwGZZkRiB84PURz6U8=", 0, 0m, 0, "testuser5" });

            migrationBuilder.InsertData(
                table: "TradingSessions",
                columns: new[] { "Id", "CurrentBarIndex", "CurrentProfitLoss", "EntryPrice", "HasOpenOrder", "Instrument", "TotalOrders", "TotalProfitLoss", "TradingDay", "UserId" },
                values: new object[] { 1, 0, null, null, false, "S&P 500", 0, 0m, "2024-01-01", 5 });
        }
    }
}
