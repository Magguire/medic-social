using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalPageDocumentPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentContentType",
                table: "ContentPages",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "ContentPages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocumentSizeBytes",
                table: "ContentPages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "ContentPages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "ContentPages",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "DocumentSizeBytes",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "ContentPages");
        }
    }
}
