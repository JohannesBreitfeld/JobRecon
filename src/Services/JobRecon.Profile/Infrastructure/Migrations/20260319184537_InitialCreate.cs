using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Profile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profile");

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentJobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GitHubUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CVDocuments",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ParsedContent = table.Column<string>(type: "text", nullable: true),
                    IsParsed = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVDocuments_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalSchema: "profile",
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DesiredJobTitles",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesiredJobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesiredJobTitles_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalSchema: "profile",
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobPreferences",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinSalary = table.Column<int>(type: "integer", nullable: true),
                    MaxSalary = table.Column<int>(type: "integer", nullable: true),
                    PreferredLocations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsRemotePreferred = table.Column<bool>(type: "boolean", nullable: false),
                    IsHybridAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnSiteAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredEmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreferredIndustries = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExcludedCompanies = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActivelyLooking = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoticePeriodDays = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPreferences_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalSchema: "profile",
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalSchema: "profile",
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVDocuments_UserProfileId",
                schema: "profile",
                table: "CVDocuments",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DesiredJobTitles_UserProfileId_Title",
                schema: "profile",
                table: "DesiredJobTitles",
                columns: new[] { "UserProfileId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPreferences_UserProfileId",
                schema: "profile",
                table: "JobPreferences",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_UserProfileId_Name",
                schema: "profile",
                table: "Skills",
                columns: new[] { "UserProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                schema: "profile",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CVDocuments",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "DesiredJobTitles",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "JobPreferences",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "Skills",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "UserProfiles",
                schema: "profile");
        }
    }
}
