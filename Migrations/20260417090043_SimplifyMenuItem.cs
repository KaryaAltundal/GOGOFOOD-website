using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOGOFOOD.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MenuItems");

            migrationBuilder.AlterColumn<int>(
                name: "CaretakerId",
                table: "MenuItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId",
                principalTable: "Caretakers",
                principalColumn: "CaretakerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<int>(
                name: "CaretakerId",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Caretakers_CaretakerId",
                table: "MenuItems",
                column: "CaretakerId",
                principalTable: "Caretakers",
                principalColumn: "CaretakerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
