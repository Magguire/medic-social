using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Data;
using Verification.Domain;

namespace Verification.Application
{
    public record VerificationPolicyCheckResult(bool IsAllowed, string? Error, IReadOnlyList<string> AppliedPolicies);

    internal static class VerificationPolicyEngine
    {
        internal static async Task<VerificationPolicyCheckResult> EvaluateAsync(
            ApplicationDbContext db,
            VerificationSubjectType subjectType,
            VerificationStage stage,
            string actionKey,
            Guid subjectId,
            Guid tenantId,
            IEnumerable<string>? categories = null,
            string? facilityType = null,
            CancellationToken cancellationToken = default)
        {
            var policies = await db.VerificationPolicies
                .Where(policy => policy.SubjectType == subjectType && policy.Stage == stage && policy.ActionKey == actionKey)
                .OrderBy(policy => policy.Name)
                .ToListAsync(cancellationToken);

            if (policies.Count == 0)
            {
                return new VerificationPolicyCheckResult(true, null, Array.Empty<string>());
            }

            var categoryValues = (categories ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string verificationStatus;
            if (subjectType == VerificationSubjectType.Professional)
            {
                verificationStatus = await db.ProfessionalProfiles
                    .Where(profile => profile.Id == subjectId)
                    .Select(profile => profile.VerificationStatus)
                    .FirstOrDefaultAsync(cancellationToken) ?? "Pending";
            }
            else
            {
                verificationStatus = await db.EmployerProfiles
                    .Where(profile => profile.Id == subjectId)
                    .Select(profile => profile.VerificationStatus)
                    .FirstOrDefaultAsync(cancellationToken) ?? "Pending";
            }

            var appliedPolicies = new List<string>();
            foreach (var policy in policies)
            {
                appliedPolicies.Add(policy.Name);

                if (policy.PolicyMode == VerificationPolicyMode.StatusGate && policy.RequireVerifiedStatusForAction)
                {
                    if (!string.Equals(verificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
                    {
                        return new VerificationPolicyCheckResult(false, $"{policy.Name}: verified status is required for this action.", appliedPolicies);
                    }
                }

                if (policy.PolicyMode == VerificationPolicyMode.MandatoryDocumentsGate && policy.RequireAllMandatoryDocuments)
                {
                    var documentCheck = await EvaluateMandatoryDocumentsAsync(
                        db,
                        subjectType,
                        subjectId,
                        categoryValues,
                        facilityType,
                        cancellationToken);

                    if (!documentCheck.IsAllowed)
                    {
                        return new VerificationPolicyCheckResult(false, $"{policy.Name}: {documentCheck.Error}", appliedPolicies);
                    }
                }

                if (policy.PolicyMode == VerificationPolicyMode.DocumentIntegration)
                {
                    var documentIntegrationCheck = await EvaluateDocumentIntegrationAsync(
                        db,
                        policy,
                        subjectType,
                        subjectId,
                        tenantId,
                        cancellationToken);

                    if (!documentIntegrationCheck.IsAllowed)
                    {
                        return new VerificationPolicyCheckResult(false, $"{policy.Name}: {documentIntegrationCheck.Error}", appliedPolicies);
                    }
                }

                if (policy.PolicyMode == VerificationPolicyMode.FieldIntegration)
                {
                    var fieldIntegrationCheck = await EvaluateFieldIntegrationAsync(
                        db,
                        policy,
                        subjectType,
                        subjectId,
                        tenantId,
                        cancellationToken);

                    if (!fieldIntegrationCheck.IsAllowed)
                    {
                        return new VerificationPolicyCheckResult(false, $"{policy.Name}: {fieldIntegrationCheck.Error}", appliedPolicies);
                    }
                }
            }

            return new VerificationPolicyCheckResult(true, null, appliedPolicies);
        }

        internal static async Task TriggerStagePoliciesAsync(
            ApplicationDbContext db,
            VerificationSubjectType subjectType,
            VerificationStage stage,
            string actionKey,
            Guid subjectId,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            var policies = await db.VerificationPolicies
                .Where(policy =>
                    policy.SubjectType == subjectType &&
                    policy.Stage == stage &&
                    policy.ActionKey == actionKey &&
                    policy.PolicyMode == VerificationPolicyMode.FieldIntegration)
                .OrderBy(policy => policy.Name)
                .ToListAsync(cancellationToken);

            if (policies.Count == 0)
            {
                return;
            }

            foreach (var policy in policies)
            {
                var fieldValue = await ResolveFieldValueAsync(db, subjectType, subjectId, policy.FieldName, cancellationToken);
                if (string.IsNullOrWhiteSpace(fieldValue))
                {
                    continue;
                }

                var existingRequest = await db.VerificationRequests.AnyAsync(
                    request => request.SubjectId == subjectId &&
                               request.TenantId == tenantId &&
                               request.DocumentId == null &&
                               request.Notes != null &&
                               request.Notes.Contains(policy.Id.ToString()),
                    cancellationToken);

                if (existingRequest)
                {
                    continue;
                }

                db.VerificationRequests.Add(new VerificationRequest
                {
                    Id = Guid.NewGuid(),
                    SubjectType = subjectType,
                    SubjectId = subjectId,
                    ProfessionalId = subjectType == VerificationSubjectType.Professional ? subjectId : null,
                    EmployerId = subjectType == VerificationSubjectType.Employer ? subjectId : null,
                    TenantId = tenantId,
                    Notes = $"Policy:{policy.Id}|Stage:{policy.Stage}|Action:{policy.ActionKey}|Field:{policy.FieldName}|Value:{fieldValue}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        private static async Task<VerificationPolicyCheckResult> EvaluateMandatoryDocumentsAsync(
            ApplicationDbContext db,
            VerificationSubjectType subjectType,
            Guid subjectId,
            IReadOnlyCollection<string> categories,
            string? facilityType,
            CancellationToken cancellationToken)
        {
            List<string> requiredDocuments;
            List<string> uploadedDocuments;

            if (subjectType == VerificationSubjectType.Professional)
            {
                requiredDocuments = await db.RequiredDocumentRules
                    .Where(rule =>
                        rule.TargetType == DocumentTargetType.Professional &&
                        rule.IsMandatory &&
                        (rule.AppliesToCategoryOrFacilityType == null ||
                         categories.Contains(rule.AppliesToCategoryOrFacilityType)))
                    .Select(rule => rule.DocumentType)
                    .ToListAsync(cancellationToken);

                uploadedDocuments = await db.Documents
                    .Where(document => document.ProfessionalId == subjectId)
                    .Select(document => document.DocumentTypeName ?? document.Type.ToString())
                    .ToListAsync(cancellationToken);
            }
            else
            {
                requiredDocuments = await db.RequiredDocumentRules
                    .Where(rule =>
                        rule.TargetType == DocumentTargetType.Employer &&
                        rule.IsMandatory &&
                        (rule.AppliesToCategoryOrFacilityType == null || rule.AppliesToCategoryOrFacilityType == facilityType))
                    .Select(rule => rule.DocumentType)
                    .ToListAsync(cancellationToken);

                uploadedDocuments = await db.EmployerDocuments
                    .Where(document => document.EmployerId == subjectId)
                    .Select(document => document.DocumentType)
                    .ToListAsync(cancellationToken);
            }

            var missingDocuments = requiredDocuments
                .Except(uploadedDocuments, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return missingDocuments.Count > 0
                ? new VerificationPolicyCheckResult(false, $"mandatory documents are missing: {string.Join(", ", missingDocuments)}", Array.Empty<string>())
                : new VerificationPolicyCheckResult(true, null, Array.Empty<string>());
        }

        private static async Task<VerificationPolicyCheckResult> EvaluateDocumentIntegrationAsync(
            ApplicationDbContext db,
            VerificationPolicy policy,
            VerificationSubjectType subjectType,
            Guid subjectId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var integration = await ResolveIntegrationAsync(db, policy, subjectType, cancellationToken);
            if (integration == null)
            {
                return policy.BypassWhenIntegrationMissing
                    ? new VerificationPolicyCheckResult(true, null, Array.Empty<string>())
                    : new VerificationPolicyCheckResult(false, "no matching verification integration is configured.", Array.Empty<string>());
            }

            if (!integration.IsEnabled && !policy.BypassWhenIntegrationMissing)
            {
                return new VerificationPolicyCheckResult(false, "the bound verification integration is disabled.", Array.Empty<string>());
            }

            var matchingDocuments = subjectType == VerificationSubjectType.Professional
                ? await db.Documents
                    .Where(document => document.ProfessionalId == subjectId &&
                                       (policy.DocumentType == null || (document.DocumentTypeName ?? document.Type.ToString()) == policy.DocumentType))
                    .Select(document => new { document.Id, Status = document.Status.ToString() })
                    .ToListAsync(cancellationToken)
                : await db.EmployerDocuments
                    .Where(document => document.EmployerId == subjectId &&
                                       (policy.DocumentType == null || document.DocumentType == policy.DocumentType))
                    .Select(document => new { document.Id, document.Status })
                    .ToListAsync(cancellationToken);

            if (matchingDocuments.Count == 0)
            {
                return new VerificationPolicyCheckResult(false, $"no uploaded document matches {policy.DocumentType ?? "the configured document rule"}.", Array.Empty<string>());
            }

            var documentIds = matchingDocuments.Select(document => document.Id).ToList();
            var requestStatuses = await db.VerificationRequests
                .Where(request => request.TenantId == tenantId && request.DocumentId.HasValue && documentIds.Contains(request.DocumentId.Value))
                .OrderByDescending(request => request.CreatedAt)
                .Select(request => request.Status)
                .ToListAsync(cancellationToken);

            return EvaluateVerificationStatuses(policy, requestStatuses, "document verification");
        }

        private static async Task<VerificationPolicyCheckResult> EvaluateFieldIntegrationAsync(
            ApplicationDbContext db,
            VerificationPolicy policy,
            VerificationSubjectType subjectType,
            Guid subjectId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            var fieldValue = await ResolveFieldValueAsync(db, subjectType, subjectId, policy.FieldName, cancellationToken);
            if (string.IsNullOrWhiteSpace(fieldValue))
            {
                return new VerificationPolicyCheckResult(false, $"{policy.FieldName ?? "Configured field"} has not been captured yet.", Array.Empty<string>());
            }

            var integration = await ResolveIntegrationAsync(db, policy, subjectType, cancellationToken);
            if (integration == null)
            {
                return policy.BypassWhenIntegrationMissing
                    ? new VerificationPolicyCheckResult(true, null, Array.Empty<string>())
                    : new VerificationPolicyCheckResult(false, "no matching field verification integration is configured.", Array.Empty<string>());
            }

            if (!integration.IsEnabled && !policy.BypassWhenIntegrationMissing)
            {
                return new VerificationPolicyCheckResult(false, "the bound field verification integration is disabled.", Array.Empty<string>());
            }

            var requestStatuses = await db.VerificationRequests
                .Where(request =>
                    request.TenantId == tenantId &&
                    request.SubjectId == subjectId &&
                    request.DocumentId == null &&
                    request.Notes != null &&
                    request.Notes.Contains(policy.FieldName ?? string.Empty))
                .OrderByDescending(request => request.CreatedAt)
                .Select(request => request.Status)
                .ToListAsync(cancellationToken);

            return EvaluateVerificationStatuses(policy, requestStatuses, $"{policy.FieldName ?? "field"} verification");
        }

        private static VerificationPolicyCheckResult EvaluateVerificationStatuses(
            VerificationPolicy policy,
            IReadOnlyCollection<VerificationStatus> statuses,
            string label)
        {
            if (statuses.Count == 0)
            {
                return policy.BlockOnPending
                    ? new VerificationPolicyCheckResult(false, $"{label} has not started.", Array.Empty<string>())
                    : new VerificationPolicyCheckResult(true, null, Array.Empty<string>());
            }

            if (policy.BlockOnFailure && statuses.Any(status => status == VerificationStatus.Rejected))
            {
                return new VerificationPolicyCheckResult(false, $"{label} failed and must be resolved before continuing.", Array.Empty<string>());
            }

            if (policy.BlockOnPending && statuses.Any(status => status == VerificationStatus.Pending))
            {
                return new VerificationPolicyCheckResult(false, $"{label} is still pending.", Array.Empty<string>());
            }

            return new VerificationPolicyCheckResult(true, null, Array.Empty<string>());
        }

        private static async Task<VerificationIntegrationConfig?> ResolveIntegrationAsync(
            ApplicationDbContext db,
            VerificationPolicy policy,
            VerificationSubjectType subjectType,
            CancellationToken cancellationToken)
        {
            if (policy.IntegrationConfigId.HasValue)
            {
                return await db.VerificationIntegrationConfigs.FirstOrDefaultAsync(
                    integration => integration.Id == policy.IntegrationConfigId.Value,
                    cancellationToken);
            }

            if (policy.PolicyMode == VerificationPolicyMode.DocumentIntegration)
            {
                return await db.VerificationIntegrationConfigs
                    .OrderBy(integration => integration.Name)
                    .FirstOrDefaultAsync(
                        integration => integration.Subject == "Document" &&
                                       (policy.DocumentType == null || integration.DocumentType == policy.DocumentType),
                        cancellationToken);
            }

            if (policy.PolicyMode == VerificationPolicyMode.FieldIntegration)
            {
                var subject = subjectType == VerificationSubjectType.Professional ? "ProfessionalField" : "EmployerField";
                return await db.VerificationIntegrationConfigs
                    .OrderBy(integration => integration.Name)
                    .FirstOrDefaultAsync(
                        integration => integration.Subject == subject &&
                                       integration.FieldName == policy.FieldName,
                        cancellationToken);
            }

            return null;
        }

        private static async Task<string?> ResolveFieldValueAsync(
            ApplicationDbContext db,
            VerificationSubjectType subjectType,
            Guid subjectId,
            string? fieldName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            var normalizedField = fieldName.Trim().ToLowerInvariant();
            if (subjectType == VerificationSubjectType.Professional)
            {
                var profile = await db.ProfessionalProfiles.FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
                if (profile == null)
                {
                    return null;
                }

                return normalizedField switch
                {
                    "licensenumber" or "professionallicensenumber" => profile.LicenseNumber,
                    "licenseboard" => profile.LicenseBoard,
                    "nationality" => profile.Nationality,
                    "professionalcategory" => profile.ProfessionalCategory,
                    _ => null
                };
            }

            var employer = await db.EmployerProfiles.FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
            if (employer == null)
            {
                return null;
            }

            return normalizedField switch
            {
                "businessregistrationnumber" => employer.BusinessRegistrationNumber,
                "krapin" => employer.KraPin,
                "licensenumber" => employer.LicenseNumber,
                "facilitytype" => employer.FacilityType,
                _ => null
            };
        }
    }
}
