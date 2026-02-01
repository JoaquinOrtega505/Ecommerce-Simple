using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl2",
                table: "Productos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl3",
                table: "Productos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenUrl2",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl3",
                table: "Productos");
        }
    }
}
