using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiveMinuteBars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: true),
                    Time = table.Column<string>(type: "TEXT", nullable: true),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiveMinuteBars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    TotalProfit = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalOrders = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTradingDays = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
                    TradingDay = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentBarIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    HasOpenOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CurrentProfitLoss = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalProfitLoss = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalOrders = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "TotalOrders", "TotalProfit", "TotalTradingDays", "Username" },
                values: new object[] { 5, "75K3eLr+dx6JJFuJ7LwIpEpOFmwGZZkRiB84PURz6U8=", 0, 0m, 0, "testuser5" });

            migrationBuilder.InsertData(
                table: "TradingSessions",
                columns: new[] { "Id", "CurrentBarIndex", "CurrentProfitLoss", "EntryPrice", "HasOpenOrder", "Instrument", "TotalOrders", "TotalProfitLoss", "TradingDay", "UserId" },
                values: new object[] { 1, 0, null, null, false, "S&P 500", 0, 0m, "2024-01-01", 5 });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSessions_UserId",
                table: "TradingSessions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiveMinuteBars");

            migrationBuilder.DropTable(
                name: "TradingSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
