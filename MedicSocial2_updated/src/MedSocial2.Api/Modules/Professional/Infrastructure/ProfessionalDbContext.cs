using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Data;

namespace Professional.Infrastructure
{
    public class ProfessionalDbContext : ApplicationDbContext
    {
        public DbSet<ProfessionalProfile> ProfessionalProfiles { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<ProfessionalCategory> Categories { get; set; }
        public DbSet<EducationRecord> EducationRecords { get; set; }
        public DbSet<QualificationRecord> QualificationRecords { get; set; }

        public ProfessionalDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
