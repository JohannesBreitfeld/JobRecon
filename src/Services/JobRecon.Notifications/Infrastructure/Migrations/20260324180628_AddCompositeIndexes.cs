using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_is_read_created",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_channel_created",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "user_id", "channel", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_digest_schedule",
                schema: "notifications",
                table: "notification_preferences",
                columns: new[] { "digest_enabled", "digest_frequency", "digest_time" });

            migrationBuilder.CreateIndex(
                name: "ix_digest_queue_is_processed_processed_at",
                schema: "notifications",
                table: "digest_queue",
                columns: new[] { "is_processed", "processed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_is_read_created",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notifications_user_channel_created",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_notification_preferences_digest_schedule",
                schema: "notifications",
                table: "notification_preferences");

            migrationBuilder.DropIndex(
                name: "ix_digest_queue_is_processed_processed_at",
                schema: "notifications",
                table: "digest_queue");
        }
    }
}
