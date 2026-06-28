using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nhom1.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLangDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionJa",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionKo",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionZh",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "DescriptionJa",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "DescriptionKo",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "DescriptionZh",
                table: "POIs");
        }
    }
}
