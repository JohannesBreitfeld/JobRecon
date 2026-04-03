using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalitiesAndJobCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                schema: "jobs",
                table: "Jobs",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocalityId",
                schema: "jobs",
                table: "Jobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                schema: "jobs",
                table: "Jobs",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Localities",
                schema: "jobs",
                columns: table => new
                {
                    GeoNameId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AsciiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AlternateNames = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    FeatureCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Admin2Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Population = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localities", x => x.GeoNameId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_LocalityId",
                schema: "jobs",
                table: "Jobs",
                column: "LocalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Localities_AsciiName",
                schema: "jobs",
                table: "Localities",
                column: "AsciiName");

            migrationBuilder.CreateIndex(
                name: "IX_Localities_Population",
                schema: "jobs",
                table: "Localities",
                column: "Population");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Localities_LocalityId",
                schema: "jobs",
                table: "Jobs",
                column: "LocalityId",
                principalSchema: "jobs",
                principalTable: "Localities",
                principalColumn: "GeoNameId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Localities_LocalityId",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "Localities",
                schema: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_LocalityId",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LocalityId",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
