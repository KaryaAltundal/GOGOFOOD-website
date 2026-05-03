using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GOGOFOOD.Migrations
{
    /// <inheritdoc />
    public partial class ConnectCaretakerToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Caretakers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Caretakers_UserId",
                table: "Caretakers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Caretakers_Users_UserId",
                table: "Caretakers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caretakers_Users_UserId",
                table: "Caretakers");

            migrationBuilder.DropIndex(
                name: "IX_Caretakers_UserId",
                table: "Caretakers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Caretakers");
        }
    }
}
