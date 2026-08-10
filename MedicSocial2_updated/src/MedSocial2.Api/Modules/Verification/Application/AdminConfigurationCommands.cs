using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Employer.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Data;
using Shared.Kernel;
using Verification.Domain;

namespace Verification.Application.Commands
{
    public record CreateProfessionalCategoryCommand(string Name, string Slug) : IRequest<Result<ProfessionalCategoryAdminDto>>;
    public record CreateSubscriptionPlanCommand(string Name, string Slug, string Description, decimal PriceAmount, string Currency, string BillingInterval, int MaxPublishedJobs, int MaxTeamMembers, int MaxCandidateInvitesPerPeriod, int MaxMessagesPerPeriod, bool CanAccessJobPostingModule, bool CanAccessApplicantReviewModule, bool CanAccessTalentSearchModule, bool CanAccessReportsModule, bool CanAccessCommunicationsModule, bool CanViewProfessionalProfiles, bool CanViewProfessionalContactDetails, bool CanViewProfessionalDocuments, bool CanViewProfessionalVerificationStatus, bool CanInviteCandidates, bool CanMessageCandidates, bool CanUseEmailCommunications, bool CanUseSmsCommunications, bool CanUseWhatsAppCommunications, bool RequiresEmployerVerificationToPublishJobs, bool IsDefault) : IRequest<Result<SubscriptionPlanDto>>;
    public record CreateRequiredDocumentRuleCommand(DocumentTargetType TargetType, string? AppliesToCategoryOrFacilityType, string DocumentType, bool IsMandatory) : IRequest<Result<RequiredDocumentRuleDto>>;
    public record CreateVerificationPolicyCommand(string Name, VerificationSubjectType SubjectType, VerificationStage Stage, string ActionKey, VerificationPolicyMode PolicyMode, string? DocumentType, string? FieldName, Guid? IntegrationConfigId, bool RequireVerifiedStatusForAction, bool RequireAllMandatoryDocuments, bool BlockOnPending, bool BlockOnFailure, bool BypassWhenIntegrationMissing, bool AllowManualOverride, string? Notes) : IRequest<Result<VerificationPolicyDto>>;
    public record CreateDocumentTypeCommand(string Name, string Slug, DocumentTargetType TargetType, string Description, string AllowedExtensions, int MaxFileSizeMb, bool IsActive) : IRequest<Result<DocumentTypeDto>>;
    public record CreateVerificationIntegrationCommand(string Name, string Subject, string DocumentType, string? FieldName, VerificationTransportMode TransportMode, string? EndpointUrl, string? HttpMethod, string? ApiKeySecret, string? AuthenticationType, string? RequestHeadersJson, string? QueryParametersJson, string? RequestBodyTemplate, string? RequestFieldMapJson, string? SuccessConditionsJson, string? FailureConditionsJson, string? ResponseMapJson, int TimeoutSeconds, int RetryCount, int RetryDelaySeconds, bool RetryOnTimeout, bool RetryOn5xx, bool ParseJsonResponse, bool StoreRawRequestResponse, bool IsEnabled, bool AllowManualOverride) : IRequest<Result<VerificationIntegrationDto>>;
    public record GetAdminConfigurationQuery() : IRequest<Result<AdminConfigurationDto>>;

    public record ProfessionalCategoryAdminDto(Guid Id, string Name, string Slug, bool IsActive);
    public record SubscriptionPlanDto(Guid Id, string Name, string Slug, string Description, decimal PriceAmount, string Currency, string BillingInterval, int MaxPublishedJobs, int MaxTeamMembers, int MaxCandidateInvitesPerPeriod, int MaxMessagesPerPeriod, bool CanAccessJobPostingModule, bool CanAccessApplicantReviewModule, bool CanAccessTalentSearchModule, bool CanAccessReportsModule, bool CanAccessCommunicationsModule, bool CanViewProfessionalProfiles, bool CanViewProfessionalContactDetails, bool CanViewProfessionalDocuments, bool CanViewProfessionalVerificationStatus, bool CanInviteCandidates, bool CanMessageCandidates, bool CanUseEmailCommunications, bool CanUseSmsCommunications, bool CanUseWhatsAppCommunications, bool RequiresEmployerVerificationToPublishJobs, bool IsDefault);
    public record RequiredDocumentRuleDto(Guid Id, DocumentTargetType TargetType, string? AppliesToCategoryOrFacilityType, string DocumentType, bool IsMandatory);
    public record VerificationPolicyDto(Guid Id, string Name, VerificationSubjectType SubjectType, VerificationStage Stage, string ActionKey, VerificationPolicyMode PolicyMode, string? DocumentType, string? FieldName, Guid? IntegrationConfigId, bool RequireVerifiedStatusForAction, bool RequireAllMandatoryDocuments, bool BlockOnPending, bool BlockOnFailure, bool BypassWhenIntegrationMissing, bool AllowManualOverride, string? Notes);
    public record DocumentTypeDto(Guid Id, string Name, string Slug, DocumentTargetType TargetType, string Description, string AllowedExtensions, int MaxFileSizeMb, bool IsActive);
    public record VerificationIntegrationDto(Guid Id, string Name, string Subject, string DocumentType, string? FieldName, VerificationTransportMode TransportMode, string? EndpointUrl, string? HttpMethod, string? AuthenticationType, string? RequestHeadersJson, string? QueryParametersJson, string? RequestBodyTemplate, string? RequestFieldMapJson, string? SuccessConditionsJson, string? FailureConditionsJson, string? ResponseMapJson, int TimeoutSeconds, int RetryCount, int RetryDelaySeconds, bool RetryOnTimeout, bool RetryOn5xx, bool ParseJsonResponse, bool StoreRawRequestResponse, bool IsEnabled, bool AllowManualOverride);
    public record JobEngagementTypeAdminDto(Guid Id, string Name, string Slug, string Description, bool AllowsShiftPattern, bool IsActive, int DisplayOrder);
    public record AdminConfigurationDto(List<ProfessionalCategoryAdminDto> Categories, List<SubscriptionPlanDto> SubscriptionPlans, List<RequiredDocumentRuleDto> RequiredDocumentRules, List<VerificationPolicyDto> VerificationPolicies, List<DocumentTypeDto> DocumentTypes, List<VerificationIntegrationDto> VerificationIntegrations, List<JobEngagementTypeAdminDto> JobEngagementTypes);

    public class CreateProfessionalCategoryHandler : IRequestHandler<CreateProfessionalCategoryCommand, Result<ProfessionalCategoryAdminDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateProfessionalCategoryHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<ProfessionalCategoryAdminDto>> Handle(CreateProfessionalCategoryCommand request, CancellationToken cancellationToken)
        {
            if (await _db.ProfessionalCategories.AnyAsync(c => c.Slug == request.Slug || c.Name == request.Name, cancellationToken))
                return Result<ProfessionalCategoryAdminDto>.Failure("Professional category already exists");
            var entity = new ProfessionalCategory { Id = Guid.NewGuid(), Name = request.Name, Slug = request.Slug, IsActive = true, CreatedAt = DateTime.UtcNow };
            _db.ProfessionalCategories.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<ProfessionalCategoryAdminDto>.Success(new ProfessionalCategoryAdminDto(entity.Id, entity.Name, entity.Slug, entity.IsActive));
        }
    }

    public class CreateSubscriptionPlanHandler : IRequestHandler<CreateSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateSubscriptionPlanHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<SubscriptionPlanDto>> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            if (request.IsDefault)
            {
                var currentDefaults = await _db.SubscriptionPlans.Where(p => p.IsDefault).ToListAsync(cancellationToken);
                foreach (var plan in currentDefaults) plan.IsDefault = false;
            }
            var entity = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                PriceAmount = request.PriceAmount,
                Currency = request.Currency,
                BillingInterval = request.BillingInterval,
                MaxPublishedJobs = request.MaxPublishedJobs,
                MaxTeamMembers = request.MaxTeamMembers,
                MaxCandidateInvitesPerPeriod = request.MaxCandidateInvitesPerPeriod,
                MaxMessagesPerPeriod = request.MaxMessagesPerPeriod,
                CanAccessJobPostingModule = request.CanAccessJobPostingModule,
                CanAccessApplicantReviewModule = request.CanAccessApplicantReviewModule,
                CanAccessTalentSearchModule = request.CanAccessTalentSearchModule,
                CanAccessReportsModule = request.CanAccessReportsModule,
                CanAccessCommunicationsModule = request.CanAccessCommunicationsModule,
                CanViewProfessionalProfiles = request.CanViewProfessionalProfiles,
                CanViewProfessionalContactDetails = request.CanViewProfessionalContactDetails,
                CanViewProfessionalDocuments = request.CanViewProfessionalDocuments,
                CanViewProfessionalVerificationStatus = request.CanViewProfessionalVerificationStatus,
                CanInviteCandidates = request.CanInviteCandidates,
                CanMessageCandidates = request.CanMessageCandidates,
                CanUseEmailCommunications = request.CanUseEmailCommunications,
                CanUseSmsCommunications = request.CanUseSmsCommunications,
                CanUseWhatsAppCommunications = request.CanUseWhatsAppCommunications,
                RequiresEmployerVerificationToPublishJobs = request.RequiresEmployerVerificationToPublishJobs,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow
            };
            _db.SubscriptionPlans.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<SubscriptionPlanDto>.Success(AdminConfigurationMappings.MapPlan(entity));
        }
    }

    public class CreateRequiredDocumentRuleHandler : IRequestHandler<CreateRequiredDocumentRuleCommand, Result<RequiredDocumentRuleDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateRequiredDocumentRuleHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<RequiredDocumentRuleDto>> Handle(CreateRequiredDocumentRuleCommand request, CancellationToken cancellationToken)
        {
            var entity = new RequiredDocumentRule
            {
                Id = Guid.NewGuid(),
                TargetType = request.TargetType,
                AppliesToCategoryOrFacilityType = request.AppliesToCategoryOrFacilityType,
                DocumentType = request.DocumentType,
                IsMandatory = request.IsMandatory,
                CreatedAt = DateTime.UtcNow
            };
            _db.RequiredDocumentRules.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<RequiredDocumentRuleDto>.Success(new RequiredDocumentRuleDto(entity.Id, entity.TargetType, entity.AppliesToCategoryOrFacilityType, entity.DocumentType, entity.IsMandatory));
        }
    }

    public class CreateVerificationPolicyHandler : IRequestHandler<CreateVerificationPolicyCommand, Result<VerificationPolicyDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateVerificationPolicyHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<VerificationPolicyDto>> Handle(CreateVerificationPolicyCommand request, CancellationToken cancellationToken)
        {
            var entity = new VerificationPolicy
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                SubjectType = request.SubjectType,
                Stage = request.Stage,
                ActionKey = request.ActionKey,
                PolicyMode = request.PolicyMode,
                DocumentType = request.DocumentType,
                FieldName = request.FieldName,
                IntegrationConfigId = request.IntegrationConfigId,
                RequireVerifiedStatusForAction = request.RequireVerifiedStatusForAction,
                RequireAllMandatoryDocuments = request.RequireAllMandatoryDocuments,
                BlockOnPending = request.BlockOnPending,
                BlockOnFailure = request.BlockOnFailure,
                BypassWhenIntegrationMissing = request.BypassWhenIntegrationMissing,
                AllowManualOverride = request.AllowManualOverride,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            _db.VerificationPolicies.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VerificationPolicyDto>.Success(new VerificationPolicyDto(entity.Id, entity.Name, entity.SubjectType, entity.Stage, entity.ActionKey, entity.PolicyMode, entity.DocumentType, entity.FieldName, entity.IntegrationConfigId, entity.RequireVerifiedStatusForAction, entity.RequireAllMandatoryDocuments, entity.BlockOnPending, entity.BlockOnFailure, entity.BypassWhenIntegrationMissing, entity.AllowManualOverride, entity.Notes));
        }
    }

    public class CreateDocumentTypeHandler : IRequestHandler<CreateDocumentTypeCommand, Result<DocumentTypeDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateDocumentTypeHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<DocumentTypeDto>> Handle(CreateDocumentTypeCommand request, CancellationToken cancellationToken)
        {
            if (await _db.DocumentTypes.AnyAsync(d => d.Slug == request.Slug, cancellationToken))
                return Result<DocumentTypeDto>.Failure("Document type already exists");

            var entity = new DocumentTypeCatalog { Id = Guid.NewGuid(), Name = request.Name, Slug = request.Slug, TargetType = request.TargetType, Description = request.Description, AllowedExtensions = request.AllowedExtensions, MaxFileSizeMb = request.MaxFileSizeMb, IsActive = request.IsActive, CreatedAt = DateTime.UtcNow };
            _db.DocumentTypes.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<DocumentTypeDto>.Success(new DocumentTypeDto(entity.Id, entity.Name, entity.Slug, entity.TargetType, entity.Description, entity.AllowedExtensions, entity.MaxFileSizeMb, entity.IsActive));
        }
    }

    public class CreateVerificationIntegrationHandler : IRequestHandler<CreateVerificationIntegrationCommand, Result<VerificationIntegrationDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateVerificationIntegrationHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<VerificationIntegrationDto>> Handle(CreateVerificationIntegrationCommand request, CancellationToken cancellationToken)
        {
            var entity = new VerificationIntegrationConfig
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Subject = request.Subject,
                DocumentType = request.DocumentType,
                FieldName = request.FieldName,
                TransportMode = request.TransportMode,
                EndpointUrl = request.EndpointUrl,
                HttpMethod = request.HttpMethod,
                ApiKeySecret = request.ApiKeySecret,
                AuthenticationType = request.AuthenticationType,
                RequestHeadersJson = request.RequestHeadersJson,
                QueryParametersJson = request.QueryParametersJson,
                RequestBodyTemplate = request.RequestBodyTemplate,
                RequestFieldMapJson = request.RequestFieldMapJson,
                SuccessConditionsJson = request.SuccessConditionsJson,
                FailureConditionsJson = request.FailureConditionsJson,
                ResponseMapJson = request.ResponseMapJson,
                TimeoutSeconds = request.TimeoutSeconds,
                RetryCount = request.RetryCount,
                RetryDelaySeconds = request.RetryDelaySeconds,
                RetryOnTimeout = request.RetryOnTimeout,
                RetryOn5xx = request.RetryOn5xx,
                ParseJsonResponse = request.ParseJsonResponse,
                StoreRawRequestResponse = request.StoreRawRequestResponse,
                IsEnabled = request.IsEnabled,
                AllowManualOverride = request.AllowManualOverride,
                CreatedAt = DateTime.UtcNow
            };
            _db.VerificationIntegrationConfigs.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VerificationIntegrationDto>.Success(new VerificationIntegrationDto(entity.Id, entity.Name, entity.Subject, entity.DocumentType, entity.FieldName, entity.TransportMode, entity.EndpointUrl, entity.HttpMethod, entity.AuthenticationType, entity.RequestHeadersJson, entity.QueryParametersJson, entity.RequestBodyTemplate, entity.RequestFieldMapJson, entity.SuccessConditionsJson, entity.FailureConditionsJson, entity.ResponseMapJson, entity.TimeoutSeconds, entity.RetryCount, entity.RetryDelaySeconds, entity.RetryOnTimeout, entity.RetryOn5xx, entity.ParseJsonResponse, entity.StoreRawRequestResponse, entity.IsEnabled, entity.AllowManualOverride));
        }
    }

    public class GetAdminConfigurationHandler : IRequestHandler<GetAdminConfigurationQuery, Result<AdminConfigurationDto>>
    {
        private readonly ApplicationDbContext _db;
        public GetAdminConfigurationHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<AdminConfigurationDto>> Handle(GetAdminConfigurationQuery request, CancellationToken cancellationToken)
        {
            var categories = await _db.ProfessionalCategories.OrderBy(c => c.Name).Select(c => new ProfessionalCategoryAdminDto(c.Id, c.Name, c.Slug, c.IsActive)).ToListAsync(cancellationToken);
            var plans = await _db.SubscriptionPlans.OrderBy(c => c.Name).Select(p => new SubscriptionPlanDto(p.Id, p.Name, p.Slug, p.Description, p.PriceAmount, p.Currency, p.BillingInterval, p.MaxPublishedJobs, p.MaxTeamMembers, p.MaxCandidateInvitesPerPeriod, p.MaxMessagesPerPeriod, p.CanAccessJobPostingModule, p.CanAccessApplicantReviewModule, p.CanAccessTalentSearchModule, p.CanAccessReportsModule, p.CanAccessCommunicationsModule, p.CanViewProfessionalProfiles, p.CanViewProfessionalContactDetails, p.CanViewProfessionalDocuments, p.CanViewProfessionalVerificationStatus, p.CanInviteCandidates, p.CanMessageCandidates, p.CanUseEmailCommunications, p.CanUseSmsCommunications, p.CanUseWhatsAppCommunications, p.RequiresEmployerVerificationToPublishJobs, p.IsDefault)).ToListAsync(cancellationToken);
            var rules = await _db.RequiredDocumentRules.OrderBy(r => r.DocumentType).Select(r => new RequiredDocumentRuleDto(r.Id, r.TargetType, r.AppliesToCategoryOrFacilityType, r.DocumentType, r.IsMandatory)).ToListAsync(cancellationToken);
            var policies = await _db.VerificationPolicies.OrderBy(p => p.Name).Select(p => new VerificationPolicyDto(p.Id, p.Name, p.SubjectType, p.Stage, p.ActionKey, p.PolicyMode, p.DocumentType, p.FieldName, p.IntegrationConfigId, p.RequireVerifiedStatusForAction, p.RequireAllMandatoryDocuments, p.BlockOnPending, p.BlockOnFailure, p.BypassWhenIntegrationMissing, p.AllowManualOverride, p.Notes)).ToListAsync(cancellationToken);
            var documentTypes = await _db.DocumentTypes.OrderBy(d => d.Name).Select(d => new DocumentTypeDto(d.Id, d.Name, d.Slug, d.TargetType, d.Description, d.AllowedExtensions, d.MaxFileSizeMb, d.IsActive)).ToListAsync(cancellationToken);
            var integrations = await _db.VerificationIntegrationConfigs.OrderBy(v => v.Name).Select(v => new VerificationIntegrationDto(v.Id, v.Name, v.Subject, v.DocumentType, v.FieldName, v.TransportMode, v.EndpointUrl, v.HttpMethod, v.AuthenticationType, v.RequestHeadersJson, v.QueryParametersJson, v.RequestBodyTemplate, v.RequestFieldMapJson, v.SuccessConditionsJson, v.FailureConditionsJson, v.ResponseMapJson, v.TimeoutSeconds, v.RetryCount, v.RetryDelaySeconds, v.RetryOnTimeout, v.RetryOn5xx, v.ParseJsonResponse, v.StoreRawRequestResponse, v.IsEnabled, v.AllowManualOverride)).ToListAsync(cancellationToken);
            var engagementTypes = await _db.JobEngagementTypes.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).Select(item => new JobEngagementTypeAdminDto(item.Id, item.Name, item.Slug, item.Description, item.AllowsShiftPattern, item.IsActive, item.DisplayOrder)).ToListAsync(cancellationToken);
            return Result<AdminConfigurationDto>.Success(new AdminConfigurationDto(categories, plans, rules, policies, documentTypes, integrations, engagementTypes));
        }
    }

    internal static class AdminConfigurationMappings
    {
        internal static SubscriptionPlanDto MapPlan(SubscriptionPlan entity) =>
            new(entity.Id, entity.Name, entity.Slug, entity.Description, entity.PriceAmount, entity.Currency, entity.BillingInterval, entity.MaxPublishedJobs, entity.MaxTeamMembers, entity.MaxCandidateInvitesPerPeriod, entity.MaxMessagesPerPeriod, entity.CanAccessJobPostingModule, entity.CanAccessApplicantReviewModule, entity.CanAccessTalentSearchModule, entity.CanAccessReportsModule, entity.CanAccessCommunicationsModule, entity.CanViewProfessionalProfiles, entity.CanViewProfessionalContactDetails, entity.CanViewProfessionalDocuments, entity.CanViewProfessionalVerificationStatus, entity.CanInviteCandidates, entity.CanMessageCandidates, entity.CanUseEmailCommunications, entity.CanUseSmsCommunications, entity.CanUseWhatsAppCommunications, entity.RequiresEmployerVerificationToPublishJobs, entity.IsDefault);
    }
}
