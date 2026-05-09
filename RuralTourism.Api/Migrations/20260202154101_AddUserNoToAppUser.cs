using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuralTourism.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNoToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notification_AppUsers_UserId",
                table: "Notification");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_ChatRooms_ChatRoomId",
                table: "Notification");

            migrationBuilder.DropForeignKey(
                name: "FK_Notification_TourPlans_TourPlanId",
                table: "Notification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notification",
                table: "Notification");

            migrationBuilder.RenameTable(
                name: "Notification",
                newName: "Notifications");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_UserId",
                table: "Notifications",
                newName: "IX_Notifications_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_TourPlanId",
                table: "Notifications",
                newName: "IX_Notifications_TourPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Notification_ChatRoomId",
                table: "Notifications",
                newName: "IX_Notifications_ChatRoomId");

            migrationBuilder.AddColumn<int>(
                name: "UserNo",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Populating UserNo with unique values for existing rows to satisfy validity checks
            migrationBuilder.Sql("UPDATE AppUsers SET UserNo = rowid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_UserNo",
                table: "AppUsers",
                column: "UserNo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AppUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ChatRooms_ChatRoomId",
                table: "Notifications",
                column: "ChatRoomId",
                principalTable: "ChatRooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TourPlans_TourPlanId",
                table: "Notifications",
                column: "TourPlanId",
                principalTable: "TourPlans",
                principalColumn: "Id");

            // SQLite trigger to simulate Identity/AutoIncrement for UserNo
            migrationBuilder.Sql(@"
                CREATE TRIGGER IF NOT EXISTS AppUsers_UserNo_AutoInc
                AFTER INSERT ON AppUsers
                FOR EACH ROW
                WHEN NEW.UserNo = 0 OR NEW.UserNo IS NULL
                BEGIN
                    UPDATE AppUsers
                    SET UserNo = (SELECT IFNULL(MAX(UserNo), 0) + 1 FROM AppUsers)
                    WHERE Id = NEW.Id;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AppUsers_UserNo_AutoInc");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AppUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ChatRooms_ChatRoomId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TourPlans_TourPlanId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_UserNo",
                table: "AppUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserNo",
                table: "AppUsers");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notification");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_UserId",
                table: "Notification",
                newName: "IX_Notification_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_TourPlanId",
                table: "Notification",
                newName: "IX_Notification_TourPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_ChatRoomId",
                table: "Notification",
                newName: "IX_Notification_ChatRoomId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notification",
                table: "Notification",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_AppUsers_UserId",
                table: "Notification",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_ChatRooms_ChatRoomId",
                table: "Notification",
                column: "ChatRoomId",
                principalTable: "ChatRooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notification_TourPlans_TourPlanId",
                table: "Notification",
                column: "TourPlanId",
                principalTable: "TourPlans",
                principalColumn: "Id");
        }
    }
}
