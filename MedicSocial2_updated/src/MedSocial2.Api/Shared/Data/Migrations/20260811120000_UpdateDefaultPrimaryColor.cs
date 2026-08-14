using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Data;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811120000_UpdateDefaultPrimaryColor")]
public partial class UpdateDefaultPrimaryColor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE ClientThemeConfigs SET PrimaryColor = '#50b998' WHERE [Key] = 'default' AND LOWER(PrimaryColor) = '#607f75'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE ClientThemeConfigs SET PrimaryColor = '#607f75' WHERE [Key] = 'default' AND LOWER(PrimaryColor) = '#50b998'");
    }
}
