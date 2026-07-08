using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class renamedTheInvoiceByBachirFieldToPaidInTheDUTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InvoiceByBachir",
                table: "DefectiveUnits",
                newName: "Paid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Paid",
                table: "DefectiveUnits",
                newName: "InvoiceByBachir");
        }
    }
}
