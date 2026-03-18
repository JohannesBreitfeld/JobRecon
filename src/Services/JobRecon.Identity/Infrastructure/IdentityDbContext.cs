using JobRecon.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Identity.Infrastructure;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        // Rename Identity tables
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.ExternalLogins)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("roles");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.TokenHash)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(r => r.DeviceInfo)
                .HasMaxLength(500);

            entity.Property(r => r.ReplacedByTokenHash)
                .HasMaxLength(256);

            entity.HasIndex(r => r.TokenHash);
            entity.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt });
        });

        builder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("external_logins");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ProviderKey)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.DisplayName)
                .HasMaxLength(256);

            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
        });
    }
}
