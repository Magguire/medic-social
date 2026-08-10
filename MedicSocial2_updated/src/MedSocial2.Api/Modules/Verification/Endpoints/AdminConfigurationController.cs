using System;
using System.Threading.Tasks;
using Employer.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Data;
using Verification.Application.Commands;
using Verification.Domain;

namespace Verification.Api.Controllers
{
    [ApiController]
    [Route("api/admin/configuration")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminConfigurationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;

        public AdminConfigurationController(IMediator mediator, ApplicationDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _mediator.Send(new GetAdminConfigurationQuery());
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("declarations")]
        public async Task<IActionResult> GetDeclarations()
        {
            try
            {
                var items = await _db.DeclarationConfigs
                    .OrderBy(item => item.FlowKey)
                    .ThenBy(item => item.DisplayOrder)
                    .Select(item => new DeclarationConfigDto(item.Id, item.FlowKey, item.Title, item.Body, item.IsRequired, item.IsActive, item.DisplayOrder))
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("declarations")]
        public async Task<IActionResult> CreateDeclaration([FromBody] UpsertDeclarationConfigDto dto)
        {
            try
            {
                var entity = new Shared.Data.Entities.DeclarationConfig
                {
                    Id = Guid.NewGuid(),
                    FlowKey = dto.FlowKey.Trim(),
                    Title = dto.Title.Trim(),
                    Body = dto.Body.Trim(),
                    IsRequired = dto.IsRequired,
                    IsActive = dto.IsActive,
                    DisplayOrder = dto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };
                _db.DeclarationConfigs.Add(entity);
                await _db.SaveChangesAsync();
                return Ok(new DeclarationConfigDto(entity.Id, entity.FlowKey, entity.Title, entity.Body, entity.IsRequired, entity.IsActive, entity.DisplayOrder));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPut("declarations/{id:guid}")]
        public async Task<IActionResult> UpdateDeclaration(Guid id, [FromBody] UpsertDeclarationConfigDto dto)
        {
            try
            {
                var entity = await _db.DeclarationConfigs.FirstOrDefaultAsync(item => item.Id == id);
                if (entity == null) return NotFound(new { errors = new[] { "Declaration not found." } });
                entity.FlowKey = dto.FlowKey.Trim();
                entity.Title = dto.Title.Trim();
                entity.Body = dto.Body.Trim();
                entity.IsRequired = dto.IsRequired;
                entity.IsActive = dto.IsActive;
                entity.DisplayOrder = dto.DisplayOrder;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new DeclarationConfigDto(entity.Id, entity.FlowKey, entity.Title, entity.Body, entity.IsRequired, entity.IsActive, entity.DisplayOrder));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateProfessionalCategoryDto dto)
        {
            var result = await _mediator.Send(new CreateProfessionalCategoryCommand(dto.Name, dto.Slug));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("categories/{id:guid}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateProfessionalCategoryDto dto)
        {
            var entity = await _db.ProfessionalCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Professional category not found." } });
            entity.Name = dto.Name;
            entity.Slug = dto.Slug;
            entity.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new ProfessionalCategoryAdminDto(entity.Id, entity.Name, entity.Slug, entity.IsActive));
        }

        [HttpPost("job-engagement-types")]
        public async Task<IActionResult> CreateJobEngagementType([FromBody] UpsertJobEngagementTypeDto dto)
        {
            try
            {
                var slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Name) : Slugify(dto.Slug);
                if (await _db.JobEngagementTypes.AnyAsync(item => item.Slug == slug))
                {
                    return BadRequest(new { errors = new[] { "Job engagement type already exists." } });
                }

                var entity = new Job.Domain.JobEngagementType
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name.Trim(),
                    Slug = slug,
                    Description = dto.Description?.Trim() ?? string.Empty,
                    AllowsShiftPattern = dto.AllowsShiftPattern,
                    IsActive = dto.IsActive,
                    DisplayOrder = dto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };
                _db.JobEngagementTypes.Add(entity);
                await _db.SaveChangesAsync();
                return Ok(new JobEngagementTypeAdminDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.AllowsShiftPattern, entity.IsActive, entity.DisplayOrder));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPut("job-engagement-types/{id:guid}")]
        public async Task<IActionResult> UpdateJobEngagementType(Guid id, [FromBody] UpsertJobEngagementTypeDto dto)
        {
            try
            {
                var entity = await _db.JobEngagementTypes.FirstOrDefaultAsync(item => item.Id == id);
                if (entity == null) return NotFound(new { errors = new[] { "Job engagement type not found." } });
                var slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Name) : Slugify(dto.Slug);
                var duplicate = await _db.JobEngagementTypes.AnyAsync(item => item.Id != id && item.Slug == slug);
                if (duplicate) return BadRequest(new { errors = new[] { "Another job engagement type uses this slug." } });
                entity.Name = dto.Name.Trim();
                entity.Slug = slug;
                entity.Description = dto.Description?.Trim() ?? string.Empty;
                entity.AllowsShiftPattern = dto.AllowsShiftPattern;
                entity.IsActive = dto.IsActive;
                entity.DisplayOrder = dto.DisplayOrder;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new JobEngagementTypeAdminDto(entity.Id, entity.Name, entity.Slug, entity.Description, entity.AllowsShiftPattern, entity.IsActive, entity.DisplayOrder));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("subscription-plans")]
        public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanDto dto)
        {
            var result = await _mediator.Send(new CreateSubscriptionPlanCommand(dto.Name, dto.Slug, dto.Description, dto.PriceAmount, dto.Currency, dto.BillingInterval, dto.MaxPublishedJobs, dto.MaxTeamMembers, dto.MaxCandidateInvitesPerPeriod, dto.MaxMessagesPerPeriod, dto.CanAccessJobPostingModule, dto.CanAccessApplicantReviewModule, dto.CanAccessTalentSearchModule, dto.CanAccessReportsModule, dto.CanAccessCommunicationsModule, dto.CanViewProfessionalProfiles, dto.CanViewProfessionalContactDetails, dto.CanViewProfessionalDocuments, dto.CanViewProfessionalVerificationStatus, dto.CanInviteCandidates, dto.CanMessageCandidates, dto.CanUseEmailCommunications, dto.CanUseSmsCommunications, dto.CanUseWhatsAppCommunications, dto.RequiresEmployerVerificationToPublishJobs, dto.IsDefault));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("subscription-plans/{id:guid}")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] CreateSubscriptionPlanDto dto)
        {
            var entity = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Subscription plan not found." } });
            if (dto.IsDefault)
            {
                await _db.SubscriptionPlans.Where(p => p.Id != id && p.IsDefault).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsDefault, false));
            }
            entity.Name = dto.Name;
            entity.Slug = dto.Slug;
            entity.Description = dto.Description;
            entity.PriceAmount = dto.PriceAmount;
            entity.Currency = dto.Currency;
            entity.BillingInterval = dto.BillingInterval;
            entity.MaxPublishedJobs = dto.MaxPublishedJobs;
            entity.MaxTeamMembers = dto.MaxTeamMembers;
            entity.MaxCandidateInvitesPerPeriod = dto.MaxCandidateInvitesPerPeriod;
            entity.MaxMessagesPerPeriod = dto.MaxMessagesPerPeriod;
            entity.CanAccessJobPostingModule = dto.CanAccessJobPostingModule;
            entity.CanAccessApplicantReviewModule = dto.CanAccessApplicantReviewModule;
            entity.CanAccessTalentSearchModule = dto.CanAccessTalentSearchModule;
            entity.CanAccessReportsModule = dto.CanAccessReportsModule;
            entity.CanAccessCommunicationsModule = dto.CanAccessCommunicationsModule;
            entity.CanViewProfessionalProfiles = dto.CanViewProfessionalProfiles;
            entity.CanViewProfessionalContactDetails = dto.CanViewProfessionalContactDetails;
            entity.CanViewProfessionalDocuments = dto.CanViewProfessionalDocuments;
            entity.CanViewProfessionalVerificationStatus = dto.CanViewProfessionalVerificationStatus;
            entity.CanInviteCandidates = dto.CanInviteCandidates;
            entity.CanMessageCandidates = dto.CanMessageCandidates;
            entity.CanUseEmailCommunications = dto.CanUseEmailCommunications;
            entity.CanUseSmsCommunications = dto.CanUseSmsCommunications;
            entity.CanUseWhatsAppCommunications = dto.CanUseWhatsAppCommunications;
            entity.RequiresEmployerVerificationToPublishJobs = dto.RequiresEmployerVerificationToPublishJobs;
            entity.IsDefault = dto.IsDefault;
            await _db.SaveChangesAsync();
            return Ok(AdminConfigurationMappings.MapPlan(entity));
        }

        [HttpPost("document-rules")]
        public async Task<IActionResult> CreateDocumentRule([FromBody] CreateDocumentRuleDto dto)
        {
            var result = await _mediator.Send(new CreateRequiredDocumentRuleCommand(dto.TargetType, dto.AppliesToCategoryOrFacilityType, dto.DocumentType, dto.IsMandatory));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("document-rules/{id:guid}")]
        public async Task<IActionResult> UpdateDocumentRule(Guid id, [FromBody] CreateDocumentRuleDto dto)
        {
            var entity = await _db.RequiredDocumentRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Document rule not found." } });
            entity.TargetType = dto.TargetType;
            entity.AppliesToCategoryOrFacilityType = dto.AppliesToCategoryOrFacilityType;
            entity.DocumentType = dto.DocumentType;
            entity.IsMandatory = dto.IsMandatory;
            await _db.SaveChangesAsync();
            return Ok(new RequiredDocumentRuleDto(entity.Id, entity.TargetType, entity.AppliesToCategoryOrFacilityType, entity.DocumentType, entity.IsMandatory));
        }

        [HttpPost("verification-policies")]
        public async Task<IActionResult> CreateVerificationPolicy([FromBody] CreateVerificationPolicyDto dto)
        {
            var result = await _mediator.Send(new CreateVerificationPolicyCommand(dto.Name, dto.SubjectType, dto.Stage, dto.ActionKey, dto.PolicyMode, dto.DocumentType, dto.FieldName, dto.IntegrationConfigId, dto.RequireVerifiedStatusForAction, dto.RequireAllMandatoryDocuments, dto.BlockOnPending, dto.BlockOnFailure, dto.BypassWhenIntegrationMissing, dto.AllowManualOverride, dto.Notes));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("verification-policies/{id:guid}")]
        public async Task<IActionResult> UpdateVerificationPolicy(Guid id, [FromBody] CreateVerificationPolicyDto dto)
        {
            var entity = await _db.VerificationPolicies.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Verification policy not found." } });
            entity.Name = dto.Name;
            entity.SubjectType = dto.SubjectType;
            entity.Stage = dto.Stage;
            entity.ActionKey = dto.ActionKey;
            entity.PolicyMode = dto.PolicyMode;
            entity.DocumentType = dto.DocumentType;
            entity.FieldName = dto.FieldName;
            entity.IntegrationConfigId = dto.IntegrationConfigId;
            entity.RequireVerifiedStatusForAction = dto.RequireVerifiedStatusForAction;
            entity.RequireAllMandatoryDocuments = dto.RequireAllMandatoryDocuments;
            entity.BlockOnPending = dto.BlockOnPending;
            entity.BlockOnFailure = dto.BlockOnFailure;
            entity.BypassWhenIntegrationMissing = dto.BypassWhenIntegrationMissing;
            entity.AllowManualOverride = dto.AllowManualOverride;
            entity.Notes = dto.Notes;
            await _db.SaveChangesAsync();
            return Ok(new VerificationPolicyDto(entity.Id, entity.Name, entity.SubjectType, entity.Stage, entity.ActionKey, entity.PolicyMode, entity.DocumentType, entity.FieldName, entity.IntegrationConfigId, entity.RequireVerifiedStatusForAction, entity.RequireAllMandatoryDocuments, entity.BlockOnPending, entity.BlockOnFailure, entity.BypassWhenIntegrationMissing, entity.AllowManualOverride, entity.Notes));
        }

        [HttpPost("document-types")]
        public async Task<IActionResult> CreateDocumentType([FromBody] CreateDocumentTypeDto dto)
        {
            var result = await _mediator.Send(new CreateDocumentTypeCommand(dto.Name, dto.Slug, dto.TargetType, dto.Description ?? string.Empty, dto.AllowedExtensions ?? string.Empty, dto.MaxFileSizeMb, dto.IsActive));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("document-types/{id:guid}")]
        public async Task<IActionResult> UpdateDocumentType(Guid id, [FromBody] CreateDocumentTypeDto dto)
        {
            var entity = await _db.DocumentTypes.FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Document type not found." } });
            entity.Name = dto.Name;
            entity.Slug = dto.Slug;
            entity.TargetType = dto.TargetType;
            entity.Description = dto.Description ?? string.Empty;
            entity.AllowedExtensions = dto.AllowedExtensions ?? string.Empty;
            entity.MaxFileSizeMb = dto.MaxFileSizeMb;
            entity.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new DocumentTypeDto(entity.Id, entity.Name, entity.Slug, entity.TargetType, entity.Description, entity.AllowedExtensions, entity.MaxFileSizeMb, entity.IsActive));
        }

        [HttpPost("verification-integrations")]
        public async Task<IActionResult> CreateVerificationIntegration([FromBody] CreateVerificationIntegrationDto dto)
        {
            var result = await _mediator.Send(new CreateVerificationIntegrationCommand(dto.Name, dto.Subject, dto.DocumentType, dto.FieldName, dto.TransportMode, dto.EndpointUrl, dto.HttpMethod, dto.ApiKeySecret, dto.AuthenticationType, dto.RequestHeadersJson, dto.QueryParametersJson, dto.RequestBodyTemplate, dto.RequestFieldMapJson, dto.SuccessConditionsJson, dto.FailureConditionsJson, dto.ResponseMapJson, dto.TimeoutSeconds, dto.RetryCount, dto.RetryDelaySeconds, dto.RetryOnTimeout, dto.RetryOn5xx, dto.ParseJsonResponse, dto.StoreRawRequestResponse, dto.IsEnabled, dto.AllowManualOverride));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("verification-integrations/{id:guid}")]
        public async Task<IActionResult> UpdateVerificationIntegration(Guid id, [FromBody] CreateVerificationIntegrationDto dto)
        {
            var entity = await _db.VerificationIntegrationConfigs.FirstOrDefaultAsync(v => v.Id == id);
            if (entity == null) return NotFound(new { errors = new[] { "Verification integration not found." } });
            entity.Name = dto.Name;
            entity.Subject = dto.Subject;
            entity.DocumentType = dto.DocumentType;
            entity.FieldName = dto.FieldName;
            entity.TransportMode = dto.TransportMode;
            entity.EndpointUrl = dto.EndpointUrl;
            entity.HttpMethod = dto.HttpMethod;
            if (!string.IsNullOrWhiteSpace(dto.ApiKeySecret)) entity.ApiKeySecret = dto.ApiKeySecret;
            entity.AuthenticationType = dto.AuthenticationType;
            entity.RequestHeadersJson = dto.RequestHeadersJson;
            entity.QueryParametersJson = dto.QueryParametersJson;
            entity.RequestBodyTemplate = dto.RequestBodyTemplate;
            entity.RequestFieldMapJson = dto.RequestFieldMapJson;
            entity.SuccessConditionsJson = dto.SuccessConditionsJson;
            entity.FailureConditionsJson = dto.FailureConditionsJson;
            entity.ResponseMapJson = dto.ResponseMapJson;
            entity.TimeoutSeconds = dto.TimeoutSeconds;
            entity.RetryCount = dto.RetryCount;
            entity.RetryDelaySeconds = dto.RetryDelaySeconds;
            entity.RetryOnTimeout = dto.RetryOnTimeout;
            entity.RetryOn5xx = dto.RetryOn5xx;
            entity.ParseJsonResponse = dto.ParseJsonResponse;
            entity.StoreRawRequestResponse = dto.StoreRawRequestResponse;
            entity.IsEnabled = dto.IsEnabled;
            entity.AllowManualOverride = dto.AllowManualOverride;
            await _db.SaveChangesAsync();
            return Ok(new VerificationIntegrationDto(entity.Id, entity.Name, entity.Subject, entity.DocumentType, entity.FieldName, entity.TransportMode, entity.EndpointUrl, entity.HttpMethod, entity.AuthenticationType, entity.RequestHeadersJson, entity.QueryParametersJson, entity.RequestBodyTemplate, entity.RequestFieldMapJson, entity.SuccessConditionsJson, entity.FailureConditionsJson, entity.ResponseMapJson, entity.TimeoutSeconds, entity.RetryCount, entity.RetryDelaySeconds, entity.RetryOnTimeout, entity.RetryOn5xx, entity.ParseJsonResponse, entity.StoreRawRequestResponse, entity.IsEnabled, entity.AllowManualOverride));
        }

        private static string Slugify(string value)
        {
            var source = string.IsNullOrWhiteSpace(value) ? "job-type" : value.Trim().ToLowerInvariant();
            var chars = source.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
            return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public record CreateProfessionalCategoryDto(string Name, string Slug);
    public record UpdateProfessionalCategoryDto(string Name, string Slug, bool IsActive);
    public record UpsertJobEngagementTypeDto(string Name, string? Slug, string? Description, bool AllowsShiftPattern, bool IsActive, int DisplayOrder);
    public record CreateSubscriptionPlanDto(string Name, string Slug, string Description, decimal PriceAmount, string Currency, string BillingInterval, int MaxPublishedJobs, int MaxTeamMembers, int MaxCandidateInvitesPerPeriod, int MaxMessagesPerPeriod, bool CanAccessJobPostingModule, bool CanAccessApplicantReviewModule, bool CanAccessTalentSearchModule, bool CanAccessReportsModule, bool CanAccessCommunicationsModule, bool CanViewProfessionalProfiles, bool CanViewProfessionalContactDetails, bool CanViewProfessionalDocuments, bool CanViewProfessionalVerificationStatus, bool CanInviteCandidates, bool CanMessageCandidates, bool CanUseEmailCommunications, bool CanUseSmsCommunications, bool CanUseWhatsAppCommunications, bool RequiresEmployerVerificationToPublishJobs, bool IsDefault);
    public record CreateDocumentRuleDto(DocumentTargetType TargetType, string? AppliesToCategoryOrFacilityType, string DocumentType, bool IsMandatory);
    public record CreateVerificationPolicyDto(string Name, VerificationSubjectType SubjectType, VerificationStage Stage, string ActionKey, VerificationPolicyMode PolicyMode, string? DocumentType, string? FieldName, Guid? IntegrationConfigId, bool RequireVerifiedStatusForAction, bool RequireAllMandatoryDocuments, bool BlockOnPending, bool BlockOnFailure, bool BypassWhenIntegrationMissing, bool AllowManualOverride, string? Notes);
    public record CreateDocumentTypeDto(string Name, string Slug, DocumentTargetType TargetType, string? Description, string? AllowedExtensions, int MaxFileSizeMb, bool IsActive);
    public record CreateVerificationIntegrationDto(string Name, string Subject, string DocumentType, string? FieldName, VerificationTransportMode TransportMode, string? EndpointUrl, string? HttpMethod, string? ApiKeySecret, string? AuthenticationType, string? RequestHeadersJson, string? QueryParametersJson, string? RequestBodyTemplate, string? RequestFieldMapJson, string? SuccessConditionsJson, string? FailureConditionsJson, string? ResponseMapJson, int TimeoutSeconds, int RetryCount, int RetryDelaySeconds, bool RetryOnTimeout, bool RetryOn5xx, bool ParseJsonResponse, bool StoreRawRequestResponse, bool IsEnabled, bool AllowManualOverride);
    public record DeclarationConfigDto(Guid Id, string FlowKey, string Title, string Body, bool IsRequired, bool IsActive, int DisplayOrder);
    public record UpsertDeclarationConfigDto(string FlowKey, string Title, string Body, bool IsRequired, bool IsActive, int DisplayOrder);
}
