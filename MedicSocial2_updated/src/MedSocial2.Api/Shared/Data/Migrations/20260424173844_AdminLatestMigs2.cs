using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminLatestMigs2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VerificationPolicies",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ActionKey",
                table: "VerificationPolicies",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowManualOverride",
                table: "VerificationPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlockOnFailure",
                table: "VerificationPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlockOnPending",
                table: "VerificationPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BypassWhenIntegrationMissing",
                table: "VerificationPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "VerificationPolicies",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "VerificationPolicies",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IntegrationConfigId",
                table: "VerificationPolicies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "VerificationPolicies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyMode",
                table: "VerificationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "VerificationPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationPolicies_SubjectType_Stage_ActionKey",
                table: "VerificationPolicies",
                columns: new[] { "SubjectType", "Stage", "ActionKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationPolicies_SubjectType_Stage_ActionKey",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "ActionKey",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "AllowManualOverride",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "BlockOnFailure",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "BlockOnPending",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "BypassWhenIntegrationMissing",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "IntegrationConfigId",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyMode",
                table: "VerificationPolicies");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "VerificationPolicies");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VerificationPolicies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160);
        }
    }
}
