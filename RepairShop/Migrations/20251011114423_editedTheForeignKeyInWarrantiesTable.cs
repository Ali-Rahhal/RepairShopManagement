using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class editedTheForeignKeyInWarrantiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties",
                column: "SerialNumberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_SerialNumberId",
                table: "Warranties",
                column: "SerialNumberId");
        }
    }
}
