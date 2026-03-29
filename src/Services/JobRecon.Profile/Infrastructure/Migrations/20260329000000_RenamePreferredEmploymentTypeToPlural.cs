using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Profile.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RenamePreferredEmploymentTypeToPlural : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PreferredEmploymentType",
            table: "JobPreferences",
            newName: "PreferredEmploymentTypes");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PreferredEmploymentTypes",
            table: "JobPreferences",
            newName: "PreferredEmploymentType");
    }
}
