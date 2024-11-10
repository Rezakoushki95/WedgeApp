using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDataMonths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Month = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataMonths", x => x.Id);
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
                name: "MarketDataDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    MarketDataMonthId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketDataDays_MarketDataMonths_MarketDataMonthId",
                        column: x => x.MarketDataMonthId,
                        principalTable: "MarketDataMonths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessedMonths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MarketDataMonthId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessedMonths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessedMonths_MarketDataMonths_MarketDataMonthId",
                        column: x => x.MarketDataMonthId,
                        principalTable: "MarketDataMonths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessedMonths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Instrument = table.Column<string>(type: "TEXT", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "AccessedDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MarketDataDayId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessedDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessedDays_MarketDataDays_MarketDataDayId",
                        column: x => x.MarketDataDayId,
                        principalTable: "MarketDataDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessedDays_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiveMinuteBars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<string>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", nullable: false),
                    High = table.Column<decimal>(type: "TEXT", nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", nullable: false),
                    MarketDataDayId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiveMinuteBars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiveMinuteBars_MarketDataDays_MarketDataDayId",
                        column: x => x.MarketDataDayId,
                        principalTable: "MarketDataDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessedDays_MarketDataDayId",
                table: "AccessedDays",
                column: "MarketDataDayId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessedDays_UserId",
                table: "AccessedDays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessedMonths_MarketDataMonthId",
                table: "AccessedMonths",
                column: "MarketDataMonthId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessedMonths_UserId",
                table: "AccessedMonths",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FiveMinuteBars_MarketDataDayId",
                table: "FiveMinuteBars",
                column: "MarketDataDayId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataDays_MarketDataMonthId",
                table: "MarketDataDays",
                column: "MarketDataMonthId");

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
                name: "AccessedDays");

            migrationBuilder.DropTable(
                name: "AccessedMonths");

            migrationBuilder.DropTable(
                name: "FiveMinuteBars");

            migrationBuilder.DropTable(
                name: "TradingSessions");

            migrationBuilder.DropTable(
                name: "MarketDataDays");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MarketDataMonths");
        }
    }
}
