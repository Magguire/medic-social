using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobEngagementTypesAndLandingBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                table: "LandingPageContents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandTagline",
                table: "LandingPageContents",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EngagementType",
                table: "Jobs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Permanent");

            migrationBuilder.AddColumn<string>(
                name: "ShiftPattern",
                table: "Jobs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobEngagementTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    AllowsShiftPattern = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEngagementTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobEngagementTypes_Slug",
                table: "JobEngagementTypes",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobEngagementTypes");

            migrationBuilder.DropColumn(
                name: "BrandName",
                table: "LandingPageContents");

            migrationBuilder.DropColumn(
                name: "BrandTagline",
                table: "LandingPageContents");

            migrationBuilder.DropColumn(
                name: "EngagementType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ShiftPattern",
                table: "Jobs");
        }
    }
}
