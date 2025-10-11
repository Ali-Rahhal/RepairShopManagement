using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class RemovedSerialNumberAndModelFromTransactionHeaderAndAddedDefectiveUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Model",
                table: "TransactionHeaders");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "TransactionHeaders");

            migrationBuilder.AddColumn<int>(
                name: "DefectiveUnitId",
                table: "TransactionHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHeaders_DefectiveUnitId",
                table: "TransactionHeaders",
                column: "DefectiveUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeaders_DefectiveUnits_DefectiveUnitId",
                table: "TransactionHeaders",
                column: "DefectiveUnitId",
                principalTable: "DefectiveUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeaders_DefectiveUnits_DefectiveUnitId",
                table: "TransactionHeaders");

            migrationBuilder.DropIndex(
                name: "IX_TransactionHeaders_DefectiveUnitId",
                table: "TransactionHeaders");

            migrationBuilder.DropColumn(
                name: "DefectiveUnitId",
                table: "TransactionHeaders");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "TransactionHeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "TransactionHeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
