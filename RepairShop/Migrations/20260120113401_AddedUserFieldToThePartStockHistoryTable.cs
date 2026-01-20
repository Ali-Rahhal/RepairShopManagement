using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserFieldToThePartStockHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PartStockHistory",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartStockHistory_UserId",
                table: "PartStockHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartStockHistory_AspNetUsers_UserId",
                table: "PartStockHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartStockHistory_AspNetUsers_UserId",
                table: "PartStockHistory");

            migrationBuilder.DropIndex(
                name: "IX_PartStockHistory_UserId",
                table: "PartStockHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PartStockHistory");
        }
    }
}
