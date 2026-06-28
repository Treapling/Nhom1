using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nhom1.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Audios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Audios");
        }
    }
}
