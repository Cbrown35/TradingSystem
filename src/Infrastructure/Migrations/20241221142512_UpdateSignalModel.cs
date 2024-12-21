using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using TradingSystem.Common.Models;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSignalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    QuoteVolume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    OpenInterest = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    BidPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    AskPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    BidSize = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    AskSize = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    VWAP = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    NumberOfTrades = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    Indicators = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false),
                    CustomMetrics = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false),
                    MarketCondition = table.Column<int>(type: "integer", nullable: false),
                    ImbalanceRatio = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    TakerBuyVolume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    TakerSellVolume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    CumulativeDelta = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Volatility = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    RelativeVolume = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    SpreadPercentage = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    LiquidityScore = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    OrderBook = table.Column<List<OrderBookLevel>>(type: "jsonb", nullable: false),
                    RecentTrades = table.Column<List<Trade>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => new { x.Symbol, x.Timestamp });
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientOrderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExchangeOrderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StopPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    LimitPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    AverageFilledPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    FilledQuantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Commission = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    CommissionAsset = table.Column<string>(type: "text", nullable: false),
                    TimeInForce = table.Column<int>(type: "integer", nullable: false),
                    IsReduceOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsClosePosition = table.Column<bool>(type: "boolean", nullable: false),
                    StrategyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TradeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Slippage = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TriggerType = table.Column<int>(type: "integer", nullable: true),
                    TriggerCondition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Signals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Expression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Strength = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    MetricsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StrategyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    ExitPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    StopLoss = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    TakeProfit = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RealizedPnL = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    UnrealizedPnL = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Commission = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Slippage = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    DrawdownFromPeak = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    RiskRewardRatio = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    ReturnOnInvestment = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MarketCondition = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    SetupType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Indicators = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false),
                    Tags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Signals = table.Column<List<Signal>>(type: "jsonb", nullable: false),
                    RiskMetrics = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false),
                    ParentTradeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trades_Trades_ParentTradeId",
                        column: x => x.ParentTradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalConditions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Expression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SignalId = table.Column<string>(type: "text", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalConditions_Signals_SignalId",
                        column: x => x.SignalId,
                        principalTable: "Signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Theories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbols = table.Column<List<string>>(type: "jsonb", nullable: false),
                    OpenSignalId = table.Column<string>(type: "text", nullable: false),
                    CloseSignalId = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Theories", x => x.Name);
                    table.ForeignKey(
                        name: "FK_Theories_Signals_CloseSignalId",
                        column: x => x.CloseSignalId,
                        principalTable: "Signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Theories_Signals_OpenSignalId",
                        column: x => x.OpenSignalId,
                        principalTable: "Signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    ValuesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    DependenciesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    TheoryName = table.Column<string>(type: "character varying(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicators_Theories_TheoryName",
                        column: x => x.TheoryName,
                        principalTable: "Theories",
                        principalColumn: "Name");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Name",
                table: "Indicators",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_TheoryName",
                table: "Indicators",
                column: "TheoryName");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Type",
                table: "Indicators",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Interval",
                table: "MarketData",
                column: "Interval");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_MarketCondition",
                table: "MarketData",
                column: "MarketCondition");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Symbol_Timestamp",
                table: "MarketData",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_Timestamp",
                table: "MarketData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientOrderId",
                table: "Orders",
                column: "ClientOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreateTime",
                table: "Orders",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExchangeOrderId",
                table: "Orders",
                column: "ExchangeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StrategyName",
                table: "Orders",
                column: "StrategyName");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Symbol",
                table: "Orders",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TradeId",
                table: "Orders",
                column: "TradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalConditions_SignalId",
                table: "SignalConditions",
                column: "SignalId");

            migrationBuilder.CreateIndex(
                name: "IX_Theories_CloseSignalId",
                table: "Theories",
                column: "CloseSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_Theories_OpenSignalId",
                table: "Theories",
                column: "OpenSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_CloseTime",
                table: "Trades",
                column: "CloseTime");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_OpenTime",
                table: "Trades",
                column: "OpenTime");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ParentTradeId",
                table: "Trades",
                column: "ParentTradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Status",
                table: "Trades",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_StrategyName",
                table: "Trades",
                column: "StrategyName");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Symbol",
                table: "Trades",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "SignalConditions");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Theories");

            migrationBuilder.DropTable(
                name: "Signals");
        }
    }
}
