using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuralTourism.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTriggerUserToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerUserId",
                table: "Notifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TriggerUserId",
                table: "Notifications",
                column: "TriggerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AppUsers_TriggerUserId",
                table: "Notifications",
                column: "TriggerUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AppUsers_TriggerUserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TriggerUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TriggerUserId",
                table: "Notifications");
        }
    }
}
