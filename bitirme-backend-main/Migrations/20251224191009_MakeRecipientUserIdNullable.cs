using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProject.Migrations
{
    /// <inheritdoc />
    public partial class MakeRecipientUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RecipientUserId'yi nullable yap
            migrationBuilder.AlterColumn<int>(
                name: "RecipientUserId",
                table: "Notifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri al: RecipientUserId'yi not-null yap (ama bu veri kaybına neden olabilir)
            migrationBuilder.AlterColumn<int>(
                name: "RecipientUserId",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
