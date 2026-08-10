using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalProfileDetailsAndExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkPermitStatus",
                table: "ProfessionalProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Languages",
                table: "ProfessionalProfiles",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "ProfessionalProfiles",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ProfessionalProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "ProfessionalProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "ProfessionalProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdOrPassport",
                table: "ProfessionalProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ProfessionalProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalAddress",
                table: "ProfessionalProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "ProfessionalProfiles",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExperienceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfessionalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrentRole = table.Column<bool>(type: "bit", nullable: false),
                    Responsibilities = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceRecords_ProfessionalId",
                table: "ExperienceRecords",
                column: "ProfessionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperienceRecords");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "County",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdOrPassport",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "PostalAddress",
                table: "ProfessionalProfiles");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "ProfessionalProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "WorkPermitStatus",
                table: "ProfessionalProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Languages",
                table: "ProfessionalProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true);
        }
    }
}
