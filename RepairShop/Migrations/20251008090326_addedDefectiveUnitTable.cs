using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class addedDefectiveUnitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefectiveUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SerialNumberId = table.Column<int>(type: "int", nullable: false),
                    WarrantyId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceContractId = table.Column<int>(type: "int", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectiveUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefectiveUnits_MaintenanceContracts_MaintenanceContractId",
                        column: x => x.MaintenanceContractId,
                        principalTable: "MaintenanceContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DefectiveUnits_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefectiveUnits_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveUnits_MaintenanceContractId",
                table: "DefectiveUnits",
                column: "MaintenanceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveUnits_SerialNumberId",
                table: "DefectiveUnits",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_DefectiveUnits_WarrantyId",
                table: "DefectiveUnits",
                column: "WarrantyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefectiveUnits");
        }
    }
}
