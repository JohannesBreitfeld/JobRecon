using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobRecon.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "digest_queue",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    company_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    match_score = table.Column<double>(type: "double precision", nullable: false),
                    top_match_factors = table.Column<string>(type: "jsonb", nullable: true),
                    job_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digest_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    digest_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    digest_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Daily"),
                    digest_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    min_match_score_for_realtime = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.80000000000000004),
                    override_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digest_queue_queued_at",
                schema: "notifications",
                table: "digest_queue",
                column: "queued_at");

            migrationBuilder.CreateIndex(
                name: "ix_digest_queue_user_id_is_processed",
                schema: "notifications",
                table: "digest_queue",
                columns: new[] { "user_id", "is_processed" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_user_id",
                schema: "notifications",
                table: "notification_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                schema: "notifications",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_event_id",
                schema: "notifications",
                table: "notifications",
                column: "event_id",
                unique: true,
                filter: "event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id",
                schema: "notifications",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digest_queue",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");
        }
    }
}
