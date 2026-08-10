using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBillingAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxCandidateInvitesPerPeriod",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxMessagesPerPeriod",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxTeamMembers",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployerSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    ProvisioningSource = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BusinessShortCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PassKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReceiverAccount = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CallbackUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CallbackVerificationToken = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    PromptFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CheckoutReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PayerDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PeriodStartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerSubscriptions_EmployerId_Status_EndsAt",
                table: "EmployerSubscriptions",
                columns: new[] { "EmployerId", "Status", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerSubscriptions_TenantId",
                table: "EmployerSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigs_Provider",
                table: "PaymentProviderConfigs",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_EmployerId_CreatedAt",
                table: "PaymentTransactions",
                columns: new[] { "EmployerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ExternalReference",
                table: "PaymentTransactions",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionUsages_EmployerId_EmployerSubscriptionId_MetricKey_PeriodStartsAt",
                table: "SubscriptionUsages",
                columns: new[] { "EmployerId", "EmployerSubscriptionId", "MetricKey", "PeriodStartsAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerSubscriptions");

            migrationBuilder.DropTable(
                name: "PaymentProviderConfigs");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "SubscriptionUsages");

            migrationBuilder.DropColumn(
                name: "MaxCandidateInvitesPerPeriod",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxMessagesPerPeriod",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxTeamMembers",
                table: "SubscriptionPlans");
        }
    }
}
