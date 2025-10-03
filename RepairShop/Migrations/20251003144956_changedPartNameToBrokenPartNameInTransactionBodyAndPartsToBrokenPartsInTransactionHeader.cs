using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class changedPartNameToBrokenPartNameInTransactionBodyAndPartsToBrokenPartsInTransactionHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartName",
                table: "TransactionBodies",
                newName: "BrokenPartName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BrokenPartName",
                table: "TransactionBodies",
                newName: "PartName");
        }
    }
}
