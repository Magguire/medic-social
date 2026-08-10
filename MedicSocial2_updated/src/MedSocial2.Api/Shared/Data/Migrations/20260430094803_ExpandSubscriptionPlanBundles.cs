using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedSocial2.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSubscriptionPlanBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubscriptionPlans",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BillingInterval",
                table: "SubscriptionPlans",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessApplicantReviewModule",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessCommunicationsModule",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessJobPostingModule",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessReportsModule",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessTalentSearchModule",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanUseEmailCommunications",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanUseSmsCommunications",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanUseWhatsAppCommunications",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewProfessionalContactDetails",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewProfessionalDocuments",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewProfessionalProfiles",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewProfessionalVerificationStatus",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SubscriptionPlans",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAmount",
                table: "SubscriptionPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Slug",
                table: "SubscriptionPlans",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_Slug",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "BillingInterval",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanAccessApplicantReviewModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanAccessCommunicationsModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanAccessJobPostingModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanAccessReportsModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanAccessTalentSearchModule",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanUseEmailCommunications",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanUseSmsCommunications",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanUseWhatsAppCommunications",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanViewProfessionalContactDetails",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanViewProfessionalDocuments",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanViewProfessionalProfiles",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CanViewProfessionalVerificationStatus",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "SubscriptionPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
