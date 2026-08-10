using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationsAndSearchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RelatedEntityName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SenderId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ApiKeySecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AccountSid = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    TemplateNamespace = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    SimulateWhenDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_Channel_CreatedAt",
                table: "CommunicationMessages",
                columns: new[] { "Channel", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMessages_TenantId",
                table: "CommunicationMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationProviderConfigs_Channel",
                table: "CommunicationProviderConfigs",
                column: "Channel",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunicationMessages");

            migrationBuilder.DropTable(
                name: "CommunicationProviderConfigs");
        }
    }
}
