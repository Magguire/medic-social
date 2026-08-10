using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionOperationsAndAuditArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionsInvalidatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "AuditLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                table: "AuditLog",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "AuditLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_IsArchived_Timestamp",
                table: "AuditLog",
                columns: new[] { "IsArchived", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_Timestamp",
                table: "AuditLog",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLog_IsArchived_Timestamp",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_UserId_Timestamp",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SessionsInvalidatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "AuditLog");
        }
    }
}
