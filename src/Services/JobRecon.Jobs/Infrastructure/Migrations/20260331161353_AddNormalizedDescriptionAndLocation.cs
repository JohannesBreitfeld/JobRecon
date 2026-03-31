using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedDescriptionAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedDescription",
                schema: "jobs",
                table: "Jobs",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLocation",
                schema: "jobs",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_NormalizedDescription",
                schema: "jobs",
                table: "Jobs",
                column: "NormalizedDescription");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_NormalizedLocation",
                schema: "jobs",
                table: "Jobs",
                column: "NormalizedLocation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_NormalizedDescription",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_NormalizedLocation",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "NormalizedDescription",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "NormalizedLocation",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
