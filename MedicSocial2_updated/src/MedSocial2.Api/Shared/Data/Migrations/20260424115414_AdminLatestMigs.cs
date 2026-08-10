using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminLatestMigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationType",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureConditionsJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParseJsonResponse",
                table: "VerificationIntegrationConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QueryParametersJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestBodyTemplate",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFieldMapJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHeadersJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseMapJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "VerificationIntegrationConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryDelaySeconds",
                table: "VerificationIntegrationConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RetryOn5xx",
                table: "VerificationIntegrationConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RetryOnTimeout",
                table: "VerificationIntegrationConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StoreRawRequestResponse",
                table: "VerificationIntegrationConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SuccessConditionsJson",
                table: "VerificationIntegrationConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "VerificationIntegrationConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AllowedExtensions",
                table: "DocumentTypes",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxFileSizeMb",
                table: "DocumentTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticationType",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "FailureConditionsJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "ParseJsonResponse",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "QueryParametersJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RequestBodyTemplate",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RequestFieldMapJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RequestHeadersJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "ResponseMapJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RetryDelaySeconds",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RetryOn5xx",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "RetryOnTimeout",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "StoreRawRequestResponse",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "SuccessConditionsJson",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "VerificationIntegrationConfigs");

            migrationBuilder.DropColumn(
                name: "AllowedExtensions",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "MaxFileSizeMb",
                table: "DocumentTypes");
        }
    }
}
