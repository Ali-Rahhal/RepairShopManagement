using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Migrations
{
    /// <inheritdoc />
    public partial class addedCategoryColumnTOModelsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Models",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Models");
        }
    }
}
