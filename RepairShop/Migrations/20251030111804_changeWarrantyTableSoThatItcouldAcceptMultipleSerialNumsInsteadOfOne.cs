using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class changeWarrantyTableSoThatItcouldAcceptMultipleSerialNumsInsteadOfOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_SerialNumbers_SerialNumberId",
                table: "Warranties");

            migrationBuilder.DropIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "SerialNumberId",
                table: "Warranties");

            migrationBuilder.AddColumn<int>(
                name: "WarrantyId",
                table: "SerialNumbers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumbers_WarrantyId",
                table: "SerialNumbers",
                column: "WarrantyId");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumbers_Warranties_WarrantyId",
                table: "SerialNumbers",
                column: "WarrantyId",
                principalTable: "Warranties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumbers_Warranties_WarrantyId",
                table: "SerialNumbers");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumbers_WarrantyId",
                table: "SerialNumbers");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                table: "SerialNumbers");

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberId",
                table: "Warranties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties",
                column: "SerialNumberId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_SerialNumbers_SerialNumberId",
                table: "Warranties",
                column: "SerialNumberId",
                principalTable: "SerialNumbers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
