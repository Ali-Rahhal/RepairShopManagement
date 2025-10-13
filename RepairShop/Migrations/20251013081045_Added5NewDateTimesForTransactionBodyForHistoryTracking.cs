using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class Added5NewDateTimesForTransactionBodyForHistoryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FixedDate",
                table: "TransactionBodies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotRepairableDate",
                table: "TransactionBodies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotReplaceableDate",
                table: "TransactionBodies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReplacedDate",
                table: "TransactionBodies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaitingPartDate",
                table: "TransactionBodies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedDate",
                table: "TransactionBodies");

            migrationBuilder.DropColumn(
                name: "NotRepairableDate",
                table: "TransactionBodies");

            migrationBuilder.DropColumn(
                name: "NotReplaceableDate",
                table: "TransactionBodies");

            migrationBuilder.DropColumn(
                name: "ReplacedDate",
                table: "TransactionBodies");

            migrationBuilder.DropColumn(
                name: "WaitingPartDate",
                table: "TransactionBodies");
        }
    }
}
