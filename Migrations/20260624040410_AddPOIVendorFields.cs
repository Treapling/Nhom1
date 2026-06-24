using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nhom1.Migrations
{
    /// <inheritdoc />
    public partial class AddPOIVendorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "POIs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "POIs",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "POIs");
        }
    }
}
