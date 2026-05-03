using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOGOFOOD.Migrations
{
    /// <inheritdoc />
    public partial class Week4InitialModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Caretakers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Caretakers",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaretakerRatings",
                columns: table => new
                {
                    CaretakerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatingValue = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CaretakerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaretakerRatings", x => x.CaretakerRatingId);
                    table.ForeignKey(
                        name: "FK_CaretakerRatings_Caretakers_CaretakerId",
                        column: x => x.CaretakerId,
                        principalTable: "Caretakers",
                        principalColumn: "CaretakerId");
                    table.ForeignKey(
                        name: "FK_CaretakerRatings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_CaretakerRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "MenuItemRatings",
                columns: table => new
                {
                    MenuItemRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatingValue = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRatings", x => x.MenuItemRatingId);
                    table.ForeignKey(
                        name: "FK_MenuItemRatings_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId");
                    table.ForeignKey(
                        name: "FK_MenuItemRatings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_MenuItemRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    SystemLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.SystemLogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaretakerRatings_CaretakerId",
                table: "CaretakerRatings",
                column: "CaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_CaretakerRatings_OrderId",
                table: "CaretakerRatings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CaretakerRatings_UserId",
                table: "CaretakerRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRatings_MenuItemId",
                table: "MenuItemRatings",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRatings_OrderId",
                table: "MenuItemRatings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRatings_UserId",
                table: "MenuItemRatings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId",
                principalTable: "Caretakers",
                principalColumn: "CaretakerId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems");

            migrationBuilder.DropTable(
                name: "CaretakerRatings");

            migrationBuilder.DropTable(
                name: "MenuItemRatings");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Caretakers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Caretakers");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId",
                principalTable: "Caretakers",
                principalColumn: "CaretakerId");
        }
    }
}
