using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiresAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_ExpiresAt",
                schema: "jobs",
                table: "Jobs",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_ExpiresAt",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
