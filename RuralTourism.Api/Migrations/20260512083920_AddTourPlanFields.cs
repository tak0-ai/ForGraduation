using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuralTourism.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTourPlanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoRouteData",
                table: "TourPlans",
                newName: "WaypointsJson");

            migrationBuilder.AddColumn<bool>(
                name: "ReturnToStart",
                table: "TourPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RouteMode",
                table: "TourPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StartAddress",
                table: "TourPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnToStart",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "RouteMode",
                table: "TourPlans");

            migrationBuilder.DropColumn(
                name: "StartAddress",
                table: "TourPlans");

            migrationBuilder.RenameColumn(
                name: "WaypointsJson",
                table: "TourPlans",
                newName: "AutoRouteData");
        }
    }
}
