using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shared.Data;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811160000_AddEmployerPhoneVisibility")]
public partial class AddEmployerPhoneVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsContactPhonePublic",
            table: "EmployerProfiles",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsContactPhonePublic", table: "EmployerProfiles");
    }
}
