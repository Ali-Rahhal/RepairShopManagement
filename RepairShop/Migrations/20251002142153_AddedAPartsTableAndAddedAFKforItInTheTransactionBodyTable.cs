using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class AddedAPartsTableAndAddedAFKforItInTheTransactionBodyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartId",
                table: "TransactionBodies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Part",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Part", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionBodies_PartId",
                table: "TransactionBodies",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionBodies_Part_PartId",
                table: "TransactionBodies",
                column: "PartId",
                principalTable: "Part",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionBodies_Part_PartId",
                table: "TransactionBodies");

            migrationBuilder.DropTable(
                name: "Part");

            migrationBuilder.DropIndex(
                name: "IX_TransactionBodies_PartId",
                table: "TransactionBodies");

            migrationBuilder.DropColumn(
                name: "PartId",
                table: "TransactionBodies");
        }
    }
}
