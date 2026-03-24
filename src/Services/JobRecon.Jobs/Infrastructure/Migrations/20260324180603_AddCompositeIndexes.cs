using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnrichedAt",
                schema: "jobs",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentError",
                schema: "jobs",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnriched",
                schema: "jobs",
                table: "Jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_JobSources_IsEnabled_LastFetchedAt",
                schema: "jobs",
                table: "JobSources",
                columns: new[] { "IsEnabled", "LastFetchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsEnriched",
                schema: "jobs",
                table: "Jobs",
                column: "IsEnriched");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsEnriched_Status_CreatedAt",
                schema: "jobs",
                table: "Jobs",
                columns: new[] { "IsEnriched", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobSourceId_Hash",
                schema: "jobs",
                table: "Jobs",
                columns: new[] { "JobSourceId", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_CreatedAt",
                schema: "jobs",
                table: "Jobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_PostedAt",
                schema: "jobs",
                table: "Jobs",
                columns: new[] { "Status", "PostedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobSources_IsEnabled_LastFetchedAt",
                schema: "jobs",
                table: "JobSources");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_IsEnriched",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_IsEnriched_Status_CreatedAt",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobSourceId_Hash",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_CreatedAt",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_PostedAt",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EnrichedAt",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EnrichmentError",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsEnriched",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
