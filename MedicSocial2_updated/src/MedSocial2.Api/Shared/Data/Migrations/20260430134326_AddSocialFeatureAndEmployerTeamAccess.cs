using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialFeatureAndEmployerTeamAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployerTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CanManageProfile = table.Column<bool>(type: "bit", nullable: false),
                    CanManageSettings = table.Column<bool>(type: "bit", nullable: false),
                    CanCreateJobs = table.Column<bool>(type: "bit", nullable: false),
                    CanPublishJobs = table.Column<bool>(type: "bit", nullable: false),
                    CanViewApplications = table.Column<bool>(type: "bit", nullable: false),
                    CanVerifyApplications = table.Column<bool>(type: "bit", nullable: false),
                    CanInviteProfessionals = table.Column<bool>(type: "bit", nullable: false),
                    CanMessageProfessionals = table.Column<bool>(type: "bit", nullable: false),
                    CanManageTeam = table.Column<bool>(type: "bit", nullable: false),
                    IsOwner = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerTeamMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformFeatureConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisabledMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeatureConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerTeamMembers_EmployerId_UserId",
                table: "EmployerTeamMembers",
                columns: new[] { "EmployerId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployerTeamMembers_TenantId_UserId",
                table: "EmployerTeamMembers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeatureConfigs_FeatureKey",
                table: "PlatformFeatureConfigs",
                column: "FeatureKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerTeamMembers");

            migrationBuilder.DropTable(
                name: "PlatformFeatureConfigs");
        }
    }
}
