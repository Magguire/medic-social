using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shared.Data
{
    /// <summary>
    /// Design-time DbContext factory for EF Core tooling (migrations).
    /// Used by 'dotnet ef' commands to instantiate ApplicationDbContext without DI.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // For local development with SQL Server LocalDB by default
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MedicSocial;Trusted_Connection=true;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
