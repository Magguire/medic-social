using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientThemeConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientThemeConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SurfaceColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MutedTextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DarkBackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DarkSurfaceColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DarkTextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DarkMutedTextColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientThemeConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientThemeConfigs_Key",
                table: "ClientThemeConfigs",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientThemeConfigs");
        }
    }
}
