using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Employer.Domain;

namespace Employer.Infrastructure
{
    public class EmployerDbContext : ApplicationDbContext
    {
        public DbSet<EmployerProfile> Employers { get; set; }
        public DbSet<EmployerDocument> Documents { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public EmployerDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EmployerProfile>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.TenantId);
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.OrganizationSlug).HasMaxLength(160);
                e.Property(x => x.FacilityType).HasMaxLength(100);
                e.Property(x => x.ContactEmail).HasMaxLength(150);
                e.Property(x => x.SubscriptionTier).HasMaxLength(50);
                e.Property(x => x.VerificationStatus).HasMaxLength(50);
            });

            modelBuilder.Entity<EmployerDocument>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.EmployerId);
                e.HasIndex(x => x.TenantId);
                e.Property(x => x.DocumentType).HasMaxLength(100);
                e.Property(x => x.FileName).HasMaxLength(255);
                e.Property(x => x.StoragePath).HasMaxLength(500);
                e.Property(x => x.Status).HasMaxLength(50);
            });

            modelBuilder.Entity<SubscriptionPlan>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.Name).HasMaxLength(100);
                e.Property(x => x.Slug).HasMaxLength(100);
            });
        }
    }
}
