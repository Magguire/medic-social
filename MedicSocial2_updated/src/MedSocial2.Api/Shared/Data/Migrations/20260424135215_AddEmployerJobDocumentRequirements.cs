using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerJobDocumentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeName",
                table: "Documents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobRequiredDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    VerificationMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AllowAdminOverride = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRequiredDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobRequiredDocuments_JobId",
                table: "JobRequiredDocuments",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRequiredDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentTypeName",
                table: "Documents");
        }
    }
}
