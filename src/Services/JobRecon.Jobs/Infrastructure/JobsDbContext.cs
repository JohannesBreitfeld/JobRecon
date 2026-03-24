using JobRecon.Jobs.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Infrastructure;

public sealed class JobsDbContext : DbContext
{
    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobSource> JobSources => Set<JobSource>();
    public DbSet<JobTag> JobTags => Set<JobTag>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jobs");

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NormalizedName);

            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.LogoUrl).HasMaxLength(1000);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.Industry).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(500);

            entity.HasMany(e => e.Jobs)
                .WithOne(j => j.Company)
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobSource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => new { e.IsEnabled, e.LastFetchedAt });

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.BaseUrl).HasMaxLength(1000);
            entity.Property(e => e.ApiKey).HasMaxLength(500);
            entity.Property(e => e.Configuration).HasMaxLength(10000);
            entity.Property(e => e.LastFetchError).HasMaxLength(2000);

            entity.HasMany(e => e.Jobs)
                .WithOne(j => j.JobSource)
                .HasForeignKey(j => j.JobSourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NormalizedTitle);
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.Hash);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PostedAt);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.IsEnriched);
            entity.HasIndex(e => new { e.JobSourceId, e.ExternalId }).IsUnique();
            entity.HasIndex(e => new { e.Status, e.PostedAt });
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.JobSourceId, e.Hash });
            entity.HasIndex(e => new { e.IsEnriched, e.Status, e.CreatedAt });

            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NormalizedTitle).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(50000);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.WorkLocationType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.SalaryCurrency).HasMaxLength(10);
            entity.Property(e => e.SalaryPeriod).HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasMaxLength(500);
            entity.Property(e => e.ExternalUrl).HasMaxLength(2000);
            entity.Property(e => e.ApplicationUrl).HasMaxLength(2000);
            entity.Property(e => e.RequiredSkills).HasMaxLength(5000);
            entity.Property(e => e.Benefits).HasMaxLength(5000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Hash).HasMaxLength(64);
            entity.Property(e => e.EnrichmentError).HasMaxLength(500);

            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Job)
                .HasForeignKey(t => t.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SavedJobs)
                .WithOne(s => s.Job)
                .HasForeignKey(s => s.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NormalizedName);
            entity.HasIndex(e => new { e.JobId, e.NormalizedName }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(100);
        });

        modelBuilder.Entity<SavedJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.JobId }).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(5000);
        });
    }
}
