using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthCrawlerApi.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "EthTransactions",
                schema: "public",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    TimeStampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    From = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    To = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValueEth = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    GasUsed = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    GasPriceGwei = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false),
                    isError = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthTransactions", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "InternalTransactions",
                schema: "public",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Hash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    From = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    To = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValueEth = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "TokenTransfers",
                schema: "public",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TxHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenSymbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TokenDecimals = table.Column<int>(type: "integer", nullable: false),
                    From = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    To = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(38,18)", precision: 38, scale: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransfers", x => x.UniqueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EthTransactions_BlockNumber",
                schema: "public",
                table: "EthTransactions",
                column: "BlockNumber");

            migrationBuilder.CreateIndex(
                name: "IX_EthTransactions_From",
                schema: "public",
                table: "EthTransactions",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_EthTransactions_TimeStampUtc",
                schema: "public",
                table: "EthTransactions",
                column: "TimeStampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EthTransactions_To",
                schema: "public",
                table: "EthTransactions",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransactions_BlockNumber",
                schema: "public",
                table: "InternalTransactions",
                column: "BlockNumber");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransactions_From",
                schema: "public",
                table: "InternalTransactions",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransactions_Hash",
                schema: "public",
                table: "InternalTransactions",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransactions_To",
                schema: "public",
                table: "InternalTransactions",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_BlockNumber",
                schema: "public",
                table: "TokenTransfers",
                column: "BlockNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ContractAddress",
                schema: "public",
                table: "TokenTransfers",
                column: "ContractAddress");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_ContractAddress_TokenSymbol",
                schema: "public",
                table: "TokenTransfers",
                columns: new[] { "ContractAddress", "TokenSymbol" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_From",
                schema: "public",
                table: "TokenTransfers",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_To",
                schema: "public",
                table: "TokenTransfers",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransfers_TxHash",
                schema: "public",
                table: "TokenTransfers",
                column: "TxHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EthTransactions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InternalTransactions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TokenTransfers",
                schema: "public");
        }
    }
}
