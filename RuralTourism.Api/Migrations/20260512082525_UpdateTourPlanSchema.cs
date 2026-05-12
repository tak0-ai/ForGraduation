using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuralTourism.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTourPlanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TourPlanMembers");

            migrationBuilder.DropTable(
                name: "TourPlanWaypoints");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "IsGroupPlan",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TourPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TourPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TourPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "TourPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGroupPlan",
                table: "TourPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "TourPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "TourPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TourPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TourPlanMembers",
                columns: table => new
                {
                    TourPlanId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AppUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ExitedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsLeader = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
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
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    TourPlanId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "IX_TourPlanMembers_AppUserId",
                table: "TourPlanMembers",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanMembers_UserId",
                table: "TourPlanMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanWaypoints_ResourceId",
                table: "TourPlanWaypoints",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPlanWaypoints_TourPlanId",
                table: "TourPlanWaypoints",
                column: "TourPlanId");
        }
    }
}
