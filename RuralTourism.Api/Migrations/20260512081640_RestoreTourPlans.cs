using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuralTourism.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestoreTourPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TourPlanId",
                table: "Notifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelPlanId",
                table: "ChatRooms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TourPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsGroupPlan = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoRouteData = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPlans_AppUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPlanMembers",
                columns: table => new
                {
                    TourPlanId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLeader = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExitedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPlanMembers", x => new { x.TourPlanId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TourPlanMembers_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TourPlanMembers_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourPlanMembers_TourPlans_TourPlanId",
                        column: x => x.TourPlanId,
                        principalTable: "TourPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPlanWaypoints",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TourPlanId = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPlanWaypoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPlanWaypoints_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourPlanWaypoints_TourPlans_TourPlanId",
                        column: x => x.TourPlanId,
                        principalTable: "TourPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TourPlanId",
                table: "Notifications",
                column: "TourPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_TravelPlanId",
                table: "ChatRooms",
                column: "TravelPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanMembers_AppUserId",
                table: "TourPlanMembers",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanMembers_UserId",
                table: "TourPlanMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlans_CreatedById",
                table: "TourPlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanWaypoints_ResourceId",
                table: "TourPlanWaypoints",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanWaypoints_TourPlanId",
                table: "TourPlanWaypoints",
                column: "TourPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_TourPlans_TravelPlanId",
                table: "ChatRooms",
                column: "TravelPlanId",
                principalTable: "TourPlans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TourPlans_TourPlanId",
                table: "Notifications",
                column: "TourPlanId",
                principalTable: "TourPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_TourPlans_TravelPlanId",
                table: "ChatRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TourPlans_TourPlanId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "TourPlanMembers");

            migrationBuilder.DropTable(
                name: "TourPlanWaypoints");

            migrationBuilder.DropTable(
                name: "TourPlans");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TourPlanId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_TravelPlanId",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "TourPlanId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TravelPlanId",
                table: "ChatRooms");
        }
    }
}
