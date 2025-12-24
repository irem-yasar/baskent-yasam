using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientUserIdToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_UserId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_UserId1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UserId1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "RecipientUserId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            // Mevcut kayıtlar için default değer (email'den user bulunacak)
            // Migration sonrası manuel olarak doldurulmalı veya kod tarafında email'den bulunacak
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId",
                table: "Appointments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UserId1",
                table: "Appointments",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_UserId",
                table: "Appointments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_UserId1",
                table: "Appointments",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
