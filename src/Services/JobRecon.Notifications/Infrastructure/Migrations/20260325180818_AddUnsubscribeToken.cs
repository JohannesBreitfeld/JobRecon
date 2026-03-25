using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnsubscribeToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unsubscribe_token",
                schema: "notifications",
                table: "notification_preferences",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_unsubscribe_token",
                schema: "notifications",
                table: "notification_preferences",
                column: "unsubscribe_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notification_preferences_unsubscribe_token",
                schema: "notifications",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "unsubscribe_token",
                schema: "notifications",
                table: "notification_preferences");
        }
    }
}
