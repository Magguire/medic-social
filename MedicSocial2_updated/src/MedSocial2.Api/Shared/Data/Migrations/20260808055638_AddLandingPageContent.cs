using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingPageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandingPageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsHeroMediaVisible = table.Column<bool>(type: "bit", nullable: false),
                    BadgeText = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HighlightText = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Subheading = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PrimaryCallToActionText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PrimaryCallToActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecondaryCallToActionText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SecondaryCallToActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeroSlidesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeatureCardsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployerCalloutTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmployerCalloutBody = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPageContents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LandingPageContents_Key",
                table: "LandingPageContents",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LandingPageContents");
        }
    }
}
