using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nhom1.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServerSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShopName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxPOISlots = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "POIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Lat = table.Column<double>(type: "float", nullable: false),
                    Lng = table.Column<double>(type: "float", nullable: false),
                    Radius = table.Column<double>(type: "float", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POIs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VendorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFoodSafetyCertified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Audios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POI_Id = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audios_POIs_POI_Id",
                        column: x => x.POI_Id,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POI_Id = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_POIs_POI_Id",
                        column: x => x.POI_Id,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "POITour",
                columns: table => new
                {
                    POIsId = table.Column<int>(type: "int", nullable: false),
                    ToursId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POITour", x => new { x.POIsId, x.ToursId });
                    table.ForeignKey(
                        name: "FK_POITour_POIs_POIsId",
                        column: x => x.POIsId,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_POITour_Tours_ToursId",
                        column: x => x.ToursId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POI_Id = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_POIs_POI_Id",
                        column: x => x.POI_Id,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POI_Id = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingLogs_POIs_POI_Id",
                        column: x => x.POI_Id,
                        principalTable: "POIs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Audios_POI_Id",
                table: "Audios",
                column: "POI_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_POI_Id",
                table: "Menus",
                column: "POI_Id");

            migrationBuilder.CreateIndex(
                name: "IX_POIs_UserId",
                table: "POIs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_POITour_ToursId",
                table: "POITour",
                column: "ToursId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_POI_Id",
                table: "Reviews",
                column: "POI_Id");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLogs_POI_Id",
                table: "TrackingLogs",
                column: "POI_Id");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProfiles_UserId",
                table: "VendorProfiles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audios");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "POITour");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "TrackingLogs");

            migrationBuilder.DropTable(
                name: "VendorProfiles");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropTable(
                name: "POIs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
