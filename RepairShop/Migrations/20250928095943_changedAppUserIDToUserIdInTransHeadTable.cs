using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class changedAppUserIDToUserIdInTransHeadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeaders_AspNetUsers_AppUserId",
                table: "TransactionHeaders");

            migrationBuilder.RenameColumn(
                name: "AppUserId",
                table: "TransactionHeaders",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionHeaders_AppUserId",
                table: "TransactionHeaders",
                newName: "IX_TransactionHeaders_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeaders_AspNetUsers_UserId",
                table: "TransactionHeaders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeaders_AspNetUsers_UserId",
                table: "TransactionHeaders");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TransactionHeaders",
                newName: "AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionHeaders_UserId",
                table: "TransactionHeaders",
                newName: "IX_TransactionHeaders_AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeaders_AspNetUsers_AppUserId",
                table: "TransactionHeaders",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
