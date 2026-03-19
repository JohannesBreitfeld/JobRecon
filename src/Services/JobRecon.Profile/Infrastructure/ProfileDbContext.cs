using JobRecon.Profile.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Profile.Infrastructure;

public sealed class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<DesiredJobTitle> DesiredJobTitles => Set<DesiredJobTitle>();
    public DbSet<JobPreference> JobPreferences => Set<JobPreference>();
    public DbSet<CVDocument> CVDocuments => Set<CVDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("profile");

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.CurrentJobTitle).HasMaxLength(200);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.GitHubUrl).HasMaxLength(500);
            entity.Property(e => e.PortfolioUrl).HasMaxLength(500);

            entity.HasMany(e => e.Skills)
                .WithOne(s => s.UserProfile)
                .HasForeignKey(s => s.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DesiredJobTitles)
                .WithOne(d => d.UserProfile)
                .HasForeignKey(d => d.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.JobPreference)
                .WithOne(j => j.UserProfile)
                .HasForeignKey<JobPreference>(j => j.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CVDocuments)
                .WithOne(c => c.UserProfile)
                .HasForeignKey(c => c.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserProfileId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Level).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<DesiredJobTitle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserProfileId, e.Title }).IsUnique();

            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<JobPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserProfileId).IsUnique();

            entity.Property(e => e.PreferredLocations).HasMaxLength(1000);
            entity.Property(e => e.PreferredIndustries).HasMaxLength(1000);
            entity.Property(e => e.ExcludedCompanies).HasMaxLength(1000);
            entity.Property(e => e.PreferredEmploymentType).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<CVDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserProfileId);

            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
        });
    }
}
