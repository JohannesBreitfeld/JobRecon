using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Profile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLocations",
                schema: "profile",
                table: "JobPreferences");

            migrationBuilder.CreateTable(
                name: "PreferredLocations",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPreferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalityId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<double>(type: "double precision", precision: 9, scale: 6, nullable: false),
                    MaxDistanceKm = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferredLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferredLocations_JobPreferences_JobPreferenceId",
                        column: x => x.JobPreferenceId,
                        principalSchema: "profile",
                        principalTable: "JobPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferredLocations_JobPreferenceId_LocalityId",
                schema: "profile",
                table: "PreferredLocations",
                columns: new[] { "JobPreferenceId", "LocalityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferredLocations",
                schema: "profile");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocations",
                schema: "profile",
                table: "JobPreferences",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
