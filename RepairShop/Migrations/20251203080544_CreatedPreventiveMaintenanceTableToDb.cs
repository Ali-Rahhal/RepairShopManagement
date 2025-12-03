using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class CreatedPreventiveMaintenanceTableToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreventiveMaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SerialNumberId = table.Column<long>(type: "bigint", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Problem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Solution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheckupDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceRecords_SerialNumbers_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_ClientId",
                table: "PreventiveMaintenanceRecords",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_SerialNumberId",
                table: "PreventiveMaintenanceRecords",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceRecords_UserId",
                table: "PreventiveMaintenanceRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreventiveMaintenanceRecords");
        }
    }
}
