using Microsoft.EntityFrameworkCore;
using Job.Domain;
using Shared.Data;

namespace Job.Infrastructure
{
    public class JobDbContext : ApplicationDbContext
    {
        public DbSet<Job.Domain.Job> Jobs { get; set; }
        public DbSet<JobApplication> Applications { get; set; }

        public JobDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job.Domain.Job>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.TenantId, x.EmployerId });
                e.HasIndex(x => x.TenantId);
                e.Property(x => x.Title).HasMaxLength(200);
                e.Property(x => x.Department).HasMaxLength(100);
                e.Property(x => x.Location).HasMaxLength(200);
            });

            modelBuilder.Entity<JobApplication>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.TenantId, x.JobId });
                e.HasIndex(x => new { x.TenantId, x.ProfessionalId });
                e.HasIndex(x => x.TenantId);
            });
        }
    }
}
