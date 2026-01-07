using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class CreatedNewPartStockHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartStockHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PartId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityChange = table.Column<int>(type: "int", nullable: false),
                    QuantityAfter = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TransactionBodyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartStockHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartStockHistory_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartStockHistory_TransactionBodies_TransactionBodyId",
                        column: x => x.TransactionBodyId,
                        principalTable: "TransactionBodies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartStockHistory_PartId",
                table: "PartStockHistory",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_PartStockHistory_TransactionBodyId",
                table: "PartStockHistory",
                column: "TransactionBodyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartStockHistory");
        }
    }
}
