using JobRecon.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Notifications.Infrastructure;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<DigestQueueItem> DigestQueue => Set<DigestQueueItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        ConfigureNotification(modelBuilder);
        ConfigureNotificationPreference(modelBuilder);
        ConfigureDigestQueueItem(modelBuilder);
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.HasKey(n => n.Id);

            entity.Property(n => n.Id)
                .HasColumnName("id");

            entity.Property(n => n.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(n => n.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(n => n.Channel)
                .HasColumnName("channel")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(n => n.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(n => n.Body)
                .HasColumnName("body")
                .IsRequired();

            entity.Property(n => n.Data)
                .HasColumnName("data")
                .HasColumnType("jsonb");

            entity.Property(n => n.IsRead)
                .HasColumnName("is_read")
                .HasDefaultValue(false);

            entity.Property(n => n.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(n => n.ReadAt)
                .HasColumnName("read_at");

            entity.Property(n => n.SentAt)
                .HasColumnName("sent_at");

            entity.Property(n => n.EventId)
                .HasColumnName("event_id");

            entity.HasIndex(n => n.UserId)
                .HasDatabaseName("ix_notifications_user_id");

            entity.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("ix_notifications_created_at");

            entity.HasIndex(n => new { n.UserId, n.IsRead })
                .HasDatabaseName("ix_notifications_user_id_is_read");

            entity.HasIndex(n => n.EventId)
                .HasDatabaseName("ix_notifications_event_id")
                .IsUnique()
                .HasFilter("event_id IS NOT NULL");
        });
    }

    private static void ConfigureNotificationPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.ToTable("notification_preferences");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id");

            entity.Property(p => p.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(p => p.EmailEnabled)
                .HasColumnName("email_enabled")
                .HasDefaultValue(true);

            entity.Property(p => p.InAppEnabled)
                .HasColumnName("in_app_enabled")
                .HasDefaultValue(true);

            entity.Property(p => p.DigestEnabled)
                .HasColumnName("digest_enabled")
                .HasDefaultValue(true);

            entity.Property(p => p.DigestFrequency)
                .HasColumnName("digest_frequency")
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(DigestFrequency.Daily);

            entity.Property(p => p.DigestTime)
                .HasColumnName("digest_time")
                .IsRequired();

            entity.Property(p => p.MinMatchScoreForRealtime)
                .HasColumnName("min_match_score_for_realtime")
                .HasDefaultValue(0.8);

            entity.Property(p => p.OverrideEmail)
                .HasColumnName("override_email")
                .HasMaxLength(256);

            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("ix_notification_preferences_user_id")
                .IsUnique();
        });
    }

    private static void ConfigureDigestQueueItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DigestQueueItem>(entity =>
        {
            entity.ToTable("digest_queue");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
                .HasColumnName("id");

            entity.Property(d => d.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(d => d.JobId)
                .HasColumnName("job_id")
                .IsRequired();

            entity.Property(d => d.JobTitle)
                .HasColumnName("job_title")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(d => d.CompanyName)
                .HasColumnName("company_name")
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(d => d.Location)
                .HasColumnName("location")
                .HasMaxLength(256);

            entity.Property(d => d.MatchScore)
                .HasColumnName("match_score")
                .IsRequired();

            entity.Property(d => d.TopMatchFactors)
                .HasColumnName("top_match_factors")
                .HasColumnType("jsonb");

            entity.Property(d => d.JobUrl)
                .HasColumnName("job_url")
                .HasMaxLength(2048);

            entity.Property(d => d.QueuedAt)
                .HasColumnName("queued_at")
                .IsRequired();

            entity.Property(d => d.IsProcessed)
                .HasColumnName("is_processed")
                .HasDefaultValue(false);

            entity.Property(d => d.ProcessedAt)
                .HasColumnName("processed_at");

            entity.HasIndex(d => new { d.UserId, d.IsProcessed })
                .HasDatabaseName("ix_digest_queue_user_id_is_processed");

            entity.HasIndex(d => d.QueuedAt)
                .HasDatabaseName("ix_digest_queue_queued_at");
        });
    }
}
