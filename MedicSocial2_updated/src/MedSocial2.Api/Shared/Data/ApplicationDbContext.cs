using Employer.Domain;
using Communication.Domain;
using Identity.Domain;
using Job.Domain;
using Matching.Domain;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Audit;
using Shared.Data.Entities;
using Verification.Domain;

namespace Shared.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<TestFeature> TestFeatures { get; set; } = null!;

        public DbSet<Identity.Domain.Tenant> Tenants { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;
        public DbSet<PasswordPolicyConfig> PasswordPolicies { get; set; } = null!;

        public DbSet<EmployerProfile> EmployerProfiles { get; set; } = null!;
        public DbSet<EmployerDocument> EmployerDocuments { get; set; } = null!;
        public DbSet<EmployerTeamMember> EmployerTeamMembers { get; set; } = null!;
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<EmployerSubscription> EmployerSubscriptions { get; set; } = null!;
        public DbSet<SubscriptionUsage> SubscriptionUsages { get; set; } = null!;
        public DbSet<PaymentProviderConfig> PaymentProviderConfigs { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<PlatformFeatureConfig> PlatformFeatureConfigs { get; set; } = null!;

        public DbSet<Job.Domain.Job> Jobs { get; set; } = null!;
        public DbSet<JobApplication> JobApplications { get; set; } = null!;
        public DbSet<JobRequiredDocument> JobRequiredDocuments { get; set; } = null!;
        public DbSet<JobPoster> JobPosters { get; set; } = null!;
        public DbSet<JobEngagementType> JobEngagementTypes { get; set; } = null!;

        public DbSet<ProfessionalProfile> ProfessionalProfiles { get; set; } = null!;
        public DbSet<ProfessionalCategory> ProfessionalCategories { get; set; } = null!;
        public DbSet<EducationRecord> EducationRecords { get; set; } = null!;
        public DbSet<QualificationRecord> QualificationRecords { get; set; } = null!;
        public DbSet<ExperienceRecord> ExperienceRecords { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;

        public DbSet<VerificationRequest> VerificationRequests { get; set; } = null!;
        public DbSet<VerificationPolicy> VerificationPolicies { get; set; } = null!;
        public DbSet<RequiredDocumentRule> RequiredDocumentRules { get; set; } = null!;
        public DbSet<DocumentTypeCatalog> DocumentTypes { get; set; } = null!;
        public DbSet<VerificationIntegrationConfig> VerificationIntegrationConfigs { get; set; } = null!;
        public DbSet<MatchInvitation> MatchInvitations { get; set; } = null!;

        public DbSet<AuditLog> AuditLog { get; set; } = null!;
        public DbSet<CommunicationProviderConfig> CommunicationProviderConfigs { get; set; } = null!;
        public DbSet<CommunicationMessage> CommunicationMessages { get; set; } = null!;
        public DbSet<InAppNotification> InAppNotifications { get; set; } = null!;
        public DbSet<JobWatch> JobWatches { get; set; } = null!;
        public DbSet<DeclarationConfig> DeclarationConfigs { get; set; } = null!;
        public DbSet<PayAsYouGoRule> PayAsYouGoRules { get; set; } = null!;
        public DbSet<PayAsYouGoCharge> PayAsYouGoCharges { get; set; } = null!;
        public DbSet<ContentPage> ContentPages { get; set; } = null!;
        public DbSet<LandingPageContent> LandingPageContents { get; set; } = null!;
        public DbSet<ClientThemeConfig> ClientThemeConfigs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<Professional.Domain.Professional>();

            modelBuilder.Entity<ProfessionalProfile>(entity =>
            {
                entity.ToTable("ProfessionalProfiles");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => new { p.TenantId, p.UserId }).IsUnique();
                entity.HasIndex(p => p.TenantId);
                entity.Property(p => p.ExpectedSalary).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Specialty).HasMaxLength(100);
                entity.Property(p => p.LicenseNumber).HasMaxLength(50);
                entity.Property(p => p.LicenseBoard).HasMaxLength(50);
                entity.Property(p => p.PhoneNumber).HasMaxLength(30);
                entity.Property(p => p.EmailAddress).HasMaxLength(200);
                entity.Property(p => p.NationalIdOrPassport).HasMaxLength(50);
                entity.Property(p => p.AddressLine).HasMaxLength(250);
                entity.Property(p => p.City).HasMaxLength(120);
                entity.Property(p => p.County).HasMaxLength(120);
                entity.Property(p => p.PostalAddress).HasMaxLength(120);
                entity.Property(p => p.CurrentPosition).HasMaxLength(150);
                entity.Property(p => p.CurrentEmployer).HasMaxLength(150);
                entity.Property(p => p.Skills).HasMaxLength(1500);
                entity.Property(p => p.Languages).HasMaxLength(400);
                entity.Property(p => p.WorkPermitStatus).HasMaxLength(120);
            });

            modelBuilder.Entity<PasswordPolicyConfig>(entity =>
            {
                entity.ToTable("PasswordPolicies");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.MinLength).HasDefaultValue(8);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(user => user.LastActivityAt);
                entity.Property(user => user.SessionsInvalidatedAt);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLog");
                entity.HasKey(log => log.Id);
                entity.HasIndex(log => new { log.IsArchived, log.Timestamp });
                entity.HasIndex(log => new { log.UserId, log.Timestamp });
            });

            modelBuilder.Entity<InAppNotification>(entity =>
            {
                entity.ToTable("InAppNotifications");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.UserId, item.ReadAt, item.CreatedAt });
                entity.Property(item => item.Type).HasMaxLength(80);
                entity.Property(item => item.Title).HasMaxLength(180);
                entity.Property(item => item.Message).HasMaxLength(1000);
                entity.Property(item => item.ActionUrl).HasMaxLength(500);
                entity.Property(item => item.EntityType).HasMaxLength(80);
            });

            modelBuilder.Entity<JobWatch>(entity =>
            {
                entity.ToTable("JobWatches");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.JobId, item.UserId }).IsUnique();
                entity.HasIndex(item => item.UserId);
            });

            modelBuilder.Entity<DeclarationConfig>(entity =>
            {
                entity.ToTable("DeclarationConfigs");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.FlowKey, item.IsActive, item.DisplayOrder });
                entity.Property(item => item.FlowKey).HasMaxLength(80);
                entity.Property(item => item.Title).HasMaxLength(180);
                entity.Property(item => item.Body).HasMaxLength(2000);
            });

            modelBuilder.Entity<PayAsYouGoRule>(entity =>
            {
                entity.ToTable("PayAsYouGoRules");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Action).IsUnique();
                entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(item => item.Currency).HasMaxLength(12);
                entity.Property(item => item.PeriodKey).HasMaxLength(40);
                entity.Property(item => item.Description).HasMaxLength(600);
            });

            modelBuilder.Entity<PayAsYouGoCharge>(entity =>
            {
                entity.ToTable("PayAsYouGoCharges");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.Action, item.UserId, item.PeriodKey });
                entity.HasIndex(item => new { item.EmployerId, item.Action, item.PeriodKey });
                entity.HasIndex(item => item.RelatedEntityId);
                entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(item => item.Amount).HasColumnType("decimal(18,2)");
                entity.Property(item => item.Currency).HasMaxLength(12);
                entity.Property(item => item.PeriodKey).HasMaxLength(80);
                entity.Property(item => item.ExternalReference).HasMaxLength(250);
                entity.Property(item => item.CheckoutReference).HasMaxLength(250);
                entity.Property(item => item.FailureReason).HasMaxLength(1000);
            });

            modelBuilder.Entity<ContentPage>(entity =>
            {
                entity.ToTable("ContentPages");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Slug).IsUnique();
                entity.Property(item => item.Slug).HasMaxLength(120);
                entity.Property(item => item.Title).HasMaxLength(180);
                entity.Property(item => item.SourceType).HasMaxLength(40);
                entity.Property(item => item.DocumentFileName).HasMaxLength(255);
                entity.Property(item => item.DocumentContentType).HasMaxLength(120);
                entity.Property(item => item.DocumentUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<LandingPageContent>(entity =>
            {
                entity.ToTable("LandingPageContents");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Key).IsUnique();
                entity.Property(item => item.Key).HasMaxLength(80);
                entity.Property(item => item.BrandName).HasMaxLength(120);
                entity.Property(item => item.BrandTagline).HasMaxLength(180);
                entity.Property(item => item.BadgeText).HasMaxLength(180);
                entity.Property(item => item.Headline).HasMaxLength(500);
                entity.Property(item => item.HighlightText).HasMaxLength(180);
                entity.Property(item => item.Subheading).HasMaxLength(1000);
                entity.Property(item => item.PrimaryCallToActionText).HasMaxLength(120);
                entity.Property(item => item.PrimaryCallToActionUrl).HasMaxLength(500);
                entity.Property(item => item.SecondaryCallToActionText).HasMaxLength(120);
                entity.Property(item => item.SecondaryCallToActionUrl).HasMaxLength(500);
                entity.Property(item => item.EmployerCalloutTitle).HasMaxLength(500);
                entity.Property(item => item.EmployerCalloutBody).HasMaxLength(1200);
                entity.Property(item => item.JourneySectionTitle).HasMaxLength(300);
                entity.Property(item => item.JourneySectionBody).HasMaxLength(800);
                entity.Property(item => item.ProfessionalJourneyTitle).HasMaxLength(240);
                entity.Property(item => item.ProfessionalJourneyBody).HasMaxLength(1000);
                entity.Property(item => item.EmployerJourneyTitle).HasMaxLength(240);
                entity.Property(item => item.EmployerJourneyBody).HasMaxLength(1000);
                entity.Property(item => item.FreeAccessTitle).HasMaxLength(240);
                entity.Property(item => item.FreeAccessBody).HasMaxLength(800);
            });

            modelBuilder.Entity<JobEngagementType>(entity =>
            {
                entity.ToTable("JobEngagementTypes");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Slug).IsUnique();
                entity.Property(item => item.Name).HasMaxLength(120);
                entity.Property(item => item.Slug).HasMaxLength(120);
                entity.Property(item => item.Description).HasMaxLength(600);
            });

            modelBuilder.Entity<ClientThemeConfig>(entity =>
            {
                entity.ToTable("ClientThemeConfigs");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Key).IsUnique();
                entity.Property(item => item.Key).HasMaxLength(80);
                entity.Property(item => item.PrimaryColor).HasMaxLength(20);
                entity.Property(item => item.SecondaryColor).HasMaxLength(20);
                entity.Property(item => item.AccentColor).HasMaxLength(20);
                entity.Property(item => item.BackgroundColor).HasMaxLength(20);
                entity.Property(item => item.SurfaceColor).HasMaxLength(20);
                entity.Property(item => item.TextColor).HasMaxLength(20);
                entity.Property(item => item.MutedTextColor).HasMaxLength(20);
                entity.Property(item => item.DarkBackgroundColor).HasMaxLength(20);
                entity.Property(item => item.DarkSurfaceColor).HasMaxLength(20);
                entity.Property(item => item.DarkTextColor).HasMaxLength(20);
                entity.Property(item => item.DarkMutedTextColor).HasMaxLength(20);
            });

            modelBuilder.Entity<RefreshTokenEntity>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(token => token.Id);
                entity.HasIndex(token => token.HashedToken).IsUnique();
                entity.HasIndex(token => new { token.UserId, token.DeviceId });
                entity.HasIndex(token => new { token.RevokedAt, token.Expiry });
                entity.Property(token => token.HashedToken).IsRequired();
                entity.Property(token => token.DeviceId).IsRequired().HasMaxLength(160);
                entity.Property(token => token.Ip).HasMaxLength(64);
                entity.Property(token => token.UserAgent).HasMaxLength(1000);
            });

            modelBuilder.Entity<ProfessionalCategory>(entity =>
            {
                entity.ToTable("ProfessionalCategories");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Slug).IsUnique();
                entity.Property(c => c.Name).HasMaxLength(100);
                entity.Property(c => c.Slug).HasMaxLength(120);
            });

            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("SubscriptionPlans");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.Slug).IsUnique();
                entity.Property(p => p.Name).HasMaxLength(120);
                entity.Property(p => p.Slug).HasMaxLength(120);
                entity.Property(p => p.Description).HasMaxLength(600);
                entity.Property(p => p.Currency).HasMaxLength(12);
                entity.Property(p => p.BillingInterval).HasMaxLength(40);
                entity.Property(p => p.PriceAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<EmployerSubscription>(entity =>
            {
                entity.ToTable("EmployerSubscriptions");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.EmployerId, item.Status, item.EndsAt });
                entity.HasIndex(item => item.TenantId);
                entity.Property(item => item.ProvisioningSource).HasMaxLength(80);
                entity.Property(item => item.Notes).HasMaxLength(1000);
            });

            modelBuilder.Entity<SubscriptionUsage>(entity =>
            {
                entity.ToTable("SubscriptionUsages");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.EmployerId, item.EmployerSubscriptionId, item.MetricKey, item.PeriodStartsAt }).IsUnique();
                entity.Property(item => item.MetricKey).HasMaxLength(100);
            });

            modelBuilder.Entity<PaymentProviderConfig>(entity =>
            {
                entity.ToTable("PaymentProviderConfigs");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => item.Provider).IsUnique();
                entity.Property(item => item.DisplayName).HasMaxLength(120);
                entity.Property(item => item.ApiBaseUrl).HasMaxLength(500);
                entity.Property(item => item.ClientId).HasMaxLength(500);
                entity.Property(item => item.ClientSecret).HasMaxLength(1000);
                entity.Property(item => item.BusinessShortCode).HasMaxLength(100);
                entity.Property(item => item.PassKey).HasMaxLength(1000);
                entity.Property(item => item.ReceiverAccount).HasMaxLength(250);
                entity.Property(item => item.CallbackUrl).HasMaxLength(1000);
                entity.Property(item => item.CallbackVerificationToken).HasMaxLength(250);
                entity.Property(item => item.Currency).HasMaxLength(12);
            });

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.ToTable("PaymentTransactions");
                entity.HasKey(item => item.Id);
                entity.HasIndex(item => new { item.EmployerId, item.CreatedAt });
                entity.HasIndex(item => item.ExternalReference);
                entity.Property(item => item.Amount).HasColumnType("decimal(18,2)");
                entity.Property(item => item.Currency).HasMaxLength(12);
                entity.Property(item => item.ExternalReference).HasMaxLength(250);
                entity.Property(item => item.CheckoutReference).HasMaxLength(250);
                entity.Property(item => item.FailureReason).HasMaxLength(1000);
            });

            modelBuilder.Entity<PlatformFeatureConfig>(entity =>
            {
                entity.ToTable("PlatformFeatureConfigs");
                entity.HasKey(f => f.Id);
                entity.HasIndex(f => f.FeatureKey).IsUnique();
                entity.Property(f => f.FeatureKey).HasMaxLength(80);
                entity.Property(f => f.DisabledMessage).HasMaxLength(500);
            });

            modelBuilder.Entity<EmployerTeamMember>(entity =>
            {
                entity.ToTable("EmployerTeamMembers");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.EmployerId, m.UserId }).IsUnique();
                entity.HasIndex(m => new { m.TenantId, m.UserId });
                entity.Property(m => m.RoleName).HasMaxLength(120);
            });

            modelBuilder.Entity<EducationRecord>(entity =>
            {
                entity.ToTable("EducationRecords");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProfessionalId);
                entity.Property(e => e.Institution).HasMaxLength(200);
                entity.Property(e => e.Award).HasMaxLength(150);
                entity.Property(e => e.FieldOfStudy).HasMaxLength(150);
            });

            modelBuilder.Entity<QualificationRecord>(entity =>
            {
                entity.ToTable("QualificationRecords");
                entity.HasKey(q => q.Id);
                entity.HasIndex(q => q.ProfessionalId);
                entity.Property(q => q.Title).HasMaxLength(150);
                entity.Property(q => q.IssuingBody).HasMaxLength(150);
                entity.Property(q => q.LicenseNumber).HasMaxLength(80);
            });

            modelBuilder.Entity<ExperienceRecord>(entity =>
            {
                entity.ToTable("ExperienceRecords");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProfessionalId);
                entity.Property(e => e.EmployerName).HasMaxLength(180);
                entity.Property(e => e.JobTitle).HasMaxLength(180);
                entity.Property(e => e.EmploymentType).HasMaxLength(80);
                entity.Property(e => e.Location).HasMaxLength(160);
                entity.Property(e => e.Responsibilities).HasMaxLength(2000);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.ProfessionalId);
                entity.HasIndex(d => new { d.TenantId, d.ProfessionalId });
                entity.Property(d => d.DocumentTypeName).HasMaxLength(160);
                entity.Property(d => d.FileName).HasMaxLength(255);
                entity.Property(d => d.StoragePath).HasMaxLength(500);
                entity.Property(d => d.ContentType).HasMaxLength(100);
            });

            modelBuilder.Entity<JobRequiredDocument>(entity =>
            {
                entity.ToTable("JobRequiredDocuments");
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.JobId);
                entity.Property(d => d.DocumentType).HasMaxLength(160);
                entity.Property(d => d.VerificationMode).HasMaxLength(40);
            });

            modelBuilder.Entity<JobPoster>(entity =>
            {
                entity.ToTable("JobPosters");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.JobId);
                entity.HasIndex(p => p.TenantId);
                entity.Property(p => p.FileName).HasMaxLength(255);
                entity.Property(p => p.ContentType).HasMaxLength(120);
                entity.Property(p => p.StoragePath).HasMaxLength(700);
                entity.Property(p => p.PublicUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<CommunicationProviderConfig>(entity =>
            {
                entity.ToTable("CommunicationProviderConfigs");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Channel).IsUnique();
                entity.Property(c => c.ProviderName).HasMaxLength(120);
                entity.Property(c => c.BaseUrl).HasMaxLength(500);
                entity.Property(c => c.SenderId).HasMaxLength(160);
                entity.Property(c => c.ApiKeySecret).HasMaxLength(1000);
                entity.Property(c => c.AccountSid).HasMaxLength(240);
                entity.Property(c => c.TemplateNamespace).HasMaxLength(240);
            });

            modelBuilder.Entity<CommunicationMessage>(entity =>
            {
                entity.ToTable("CommunicationMessages");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.Channel, m.CreatedAt });
                entity.HasIndex(m => m.TenantId);
                entity.Property(m => m.Recipient).HasMaxLength(320);
                entity.Property(m => m.Subject).HasMaxLength(300);
                entity.Property(m => m.TemplateKey).HasMaxLength(160);
                entity.Property(m => m.RelatedEntityName).HasMaxLength(160);
                entity.Property(m => m.RelatedEntityId).HasMaxLength(80);
                entity.Property(m => m.ProviderName).HasMaxLength(120);
                entity.Property(m => m.ProviderResponse).HasMaxLength(1000);
            });

            modelBuilder.Entity<DocumentTypeCatalog>(entity =>
            {
                entity.ToTable("DocumentTypes");
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.Slug).IsUnique();
                entity.Property(d => d.Name).HasMaxLength(120);
                entity.Property(d => d.Slug).HasMaxLength(140);
                entity.Property(d => d.Description).HasMaxLength(500);
                entity.Property(d => d.AllowedExtensions).HasMaxLength(400);
            });

            modelBuilder.Entity<VerificationIntegrationConfig>(entity =>
            {
                entity.ToTable("VerificationIntegrationConfigs");
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => new { v.Subject, v.DocumentType, v.FieldName });
                entity.Property(v => v.Name).HasMaxLength(160);
                entity.Property(v => v.Subject).HasMaxLength(80);
                entity.Property(v => v.DocumentType).HasMaxLength(120);
                entity.Property(v => v.FieldName).HasMaxLength(120);
                entity.Property(v => v.EndpointUrl).HasMaxLength(700);
                entity.Property(v => v.HttpMethod).HasMaxLength(12);
                entity.Property(v => v.ApiKeySecret).HasMaxLength(1000);
                entity.Property(v => v.AuthenticationType).HasMaxLength(60);
                entity.Property(v => v.RequestHeadersJson).HasMaxLength(4000);
                entity.Property(v => v.QueryParametersJson).HasMaxLength(4000);
                entity.Property(v => v.RequestBodyTemplate).HasMaxLength(4000);
                entity.Property(v => v.RequestFieldMapJson).HasMaxLength(4000);
                entity.Property(v => v.SuccessConditionsJson).HasMaxLength(4000);
                entity.Property(v => v.FailureConditionsJson).HasMaxLength(4000);
                entity.Property(v => v.ResponseMapJson).HasMaxLength(4000);
            });

            modelBuilder.Entity<VerificationPolicy>(entity =>
            {
                entity.ToTable("VerificationPolicies");
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => new { v.SubjectType, v.Stage, v.ActionKey });
                entity.Property(v => v.Name).HasMaxLength(160);
                entity.Property(v => v.ActionKey).HasMaxLength(80);
                entity.Property(v => v.DocumentType).HasMaxLength(120);
                entity.Property(v => v.FieldName).HasMaxLength(120);
                entity.Property(v => v.Notes).HasMaxLength(1000);
            });

            modelBuilder.Entity<Job.Domain.Job>()
                .Property(j => j.SalaryMin)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Job.Domain.Job>()
                .Property(j => j.SalaryMax)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Job.Domain.Job>(entity =>
            {
                entity.Property(j => j.EngagementType).HasMaxLength(120).HasDefaultValue("Permanent");
                entity.Property(j => j.ShiftPattern).HasMaxLength(250);
                entity.Property(j => j.ModerationReason).HasMaxLength(1000);
                entity.HasIndex(j => new { j.Status, j.CreatedAt });
                entity.HasIndex(j => new { j.EmployerId, j.Status });
            });
        }
    }
}
