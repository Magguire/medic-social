using Microsoft.EntityFrameworkCore;
using Verification.Domain;
using Shared.Data;

namespace Verification.Infrastructure
{
    public class VerificationDbContext : ApplicationDbContext
    {
        public DbSet<VerificationRequest> VerificationRequests { get; set; }
        public DbSet<VerificationPolicy> VerificationPolicies { get; set; }
        public DbSet<RequiredDocumentRule> RequiredDocumentRules { get; set; }

        public VerificationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VerificationRequest>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.TenantId);
                e.HasIndex(x => x.SubjectId);
                e.HasIndex(x => new { x.SubjectType, x.SubjectId });
            });

            modelBuilder.Entity<VerificationPolicy>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(160);
                e.Property(x => x.ActionKey).HasMaxLength(80);
                e.Property(x => x.DocumentType).HasMaxLength(120);
                e.Property(x => x.FieldName).HasMaxLength(120);
                e.Property(x => x.Notes).HasMaxLength(1000);
            });

            modelBuilder.Entity<RequiredDocumentRule>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.DocumentType).HasMaxLength(100);
                e.Property(x => x.AppliesToCategoryOrFacilityType).HasMaxLength(120);
            });
        }
    }
}
