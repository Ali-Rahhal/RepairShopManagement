using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class removedClientFKFromTransactionHeaderAndRemovedWarrantyAndMaintenanceContractFKsFromDefectiveUnitAndRemovedTransactionsFromClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DefectiveUnits_MaintenanceContracts_MaintenanceContractId",
                table: "DefectiveUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_DefectiveUnits_Warranties_WarrantyId",
                table: "DefectiveUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeaders_Clients_ClientId",
                table: "TransactionHeaders");

            migrationBuilder.DropIndex(
                name: "IX_TransactionHeaders_ClientId",
                table: "TransactionHeaders");

            migrationBuilder.DropIndex(
                name: "IX_DefectiveUnits_MaintenanceContractId",
                table: "DefectiveUnits");

            migrationBuilder.DropIndex(
                name: "IX_DefectiveUnits_WarrantyId",
                table: "DefectiveUnits");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "TransactionHeaders");

            migrationBuilder.DropColumn(
                name: "MaintenanceContractId",
                table: "DefectiveUnits");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                table: "DefectiveUnits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "TransactionHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceContractId",
                table: "DefectiveUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyId",
                table: "DefectiveUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHeaders_ClientId",
                table: "TransactionHeaders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveUnits_MaintenanceContractId",
                table: "DefectiveUnits",
                column: "MaintenanceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveUnits_WarrantyId",
                table: "DefectiveUnits",
                column: "WarrantyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DefectiveUnits_MaintenanceContracts_MaintenanceContractId",
                table: "DefectiveUnits",
                column: "MaintenanceContractId",
                principalTable: "MaintenanceContracts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DefectiveUnits_Warranties_WarrantyId",
                table: "DefectiveUnits",
                column: "WarrantyId",
                principalTable: "Warranties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeaders_Clients_ClientId",
                table: "TransactionHeaders",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
