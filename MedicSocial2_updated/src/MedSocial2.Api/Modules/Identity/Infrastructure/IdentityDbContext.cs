using Microsoft.EntityFrameworkCore;
using Identity.Domain;
using Shared.Data;

namespace Identity.Infrastructure
{
    public class IdentityDbContext : ApplicationDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
        public DbSet<Tenant> Tenants { get; set; }

        public IdentityDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
                e.HasIndex(x => x.TenantId);
                e.Property(x => x.Email).IsRequired().HasMaxLength(256);
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
                e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
                e.Property(x => x.PhoneNumber).HasMaxLength(20);
                e.Property(x => x.SubscriptionTier).HasMaxLength(50);
                e.Property(x => x.VerificationStatus).HasMaxLength(50);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshTokenEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.UserId, x.DeviceId });
                e.Property(x => x.HashedToken).IsRequired();
                e.Property(x => x.DeviceId).IsRequired().HasMaxLength(160);
                e.Property(x => x.Ip).HasMaxLength(64);
                e.Property(x => x.UserAgent).HasMaxLength(1000);
            });

            // Tenant configuration
            modelBuilder.Entity<Tenant>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.Name).IsRequired().HasMaxLength(256);
                e.Property(x => x.Slug).IsRequired().HasMaxLength(100);
                e.Property(x => x.SubscriptionTier).HasMaxLength(50);
                e.Property(x => x.RegionCode).HasMaxLength(10);
            });
        }
    }
}
