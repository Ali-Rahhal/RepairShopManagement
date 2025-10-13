using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class Added2NewDateTimesForTransactionHeaderForHistoryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOrOutOfServiceDate",
                table: "TransactionHeaders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InProgressDate",
                table: "TransactionHeaders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedOrOutOfServiceDate",
                table: "TransactionHeaders");

            migrationBuilder.DropColumn(
                name: "InProgressDate",
                table: "TransactionHeaders");
        }
    }
}
