using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobModerationAndDeclarationConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedByUserId",
                table: "Jobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousStatusBeforeModeration",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeclarationConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlowKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclarationConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_EmployerId_Status",
                table: "Jobs",
                columns: new[] { "EmployerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status_CreatedAt",
                table: "Jobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeclarationConfigs_FlowKey_IsActive_DisplayOrder",
                table: "DeclarationConfigs",
                columns: new[] { "FlowKey", "IsActive", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeclarationConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_EmployerId_Status",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_Status_CreatedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PreviousStatusBeforeModeration",
                table: "Jobs");
        }
    }
}
