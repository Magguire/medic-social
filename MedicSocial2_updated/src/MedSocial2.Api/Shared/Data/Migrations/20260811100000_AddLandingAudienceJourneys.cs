using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Data;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811100000_AddLandingAudienceJourneys")]
public partial class AddLandingAudienceJourneys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "JourneySectionTitle", table: "LandingPageContents", type: "nvarchar(300)", maxLength: 300, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "JourneySectionBody", table: "LandingPageContents", type: "nvarchar(800)", maxLength: 800, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ProfessionalJourneyTitle", table: "LandingPageContents", type: "nvarchar(240)", maxLength: 240, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ProfessionalJourneyBody", table: "LandingPageContents", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "EmployerJourneyTitle", table: "LandingPageContents", type: "nvarchar(240)", maxLength: 240, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "EmployerJourneyBody", table: "LandingPageContents", type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "FreeAccessTitle", table: "LandingPageContents", type: "nvarchar(240)", maxLength: 240, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "FreeAccessBody", table: "LandingPageContents", type: "nvarchar(800)", maxLength: 800, nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "JourneySectionTitle", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "JourneySectionBody", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "ProfessionalJourneyTitle", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "ProfessionalJourneyBody", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "EmployerJourneyTitle", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "EmployerJourneyBody", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "FreeAccessTitle", table: "LandingPageContents");
        migrationBuilder.DropColumn(name: "FreeAccessBody", table: "LandingPageContents");
    }
}
