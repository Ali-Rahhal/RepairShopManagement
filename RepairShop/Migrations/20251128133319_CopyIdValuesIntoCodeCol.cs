using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class CopyIdValuesIntoCodeCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Parts SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE Warranties SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE TransactionBodies SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE TransactionHeaders SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE SerialNumbers SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE Models SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE MaintenanceContracts SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE DefectiveUnits SET Code = CAST(Id AS VARCHAR(50))");
            migrationBuilder.Sql("UPDATE Clients SET Code = CAST(Id AS VARCHAR(50))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
