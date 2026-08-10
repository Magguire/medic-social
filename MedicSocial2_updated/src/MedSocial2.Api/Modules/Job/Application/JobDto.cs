using System;
using System.Collections.Generic;

namespace Job.Application
{
    public record JobDto(
        Guid Id,
        Guid EmployerId,
        Guid TenantId,
        string Title,
        string Description,
        string Department,
        string EngagementType,
        string? ShiftPattern,
        string Location,
        decimal SalaryMin,
        decimal SalaryMax,
        string? RequiredProfessionalCategory,
        int? MinimumYearsOfExperience,
        bool RequireVerifiedProfessional,
        bool AllowInvites,
        string Status,
        string DisplayStatus,
        string? ModerationReason,
        DateTime? ModeratedAt,
        DateTime PublishedAt,
        DateTime ClosesAt,
        DateTime CreatedAt,
        List<JobRequiredDocumentDto> RequiredDocuments,
        List<JobPosterDto> Posters);

    public record JobRequiredDocumentDto(
        Guid Id,
        string DocumentType,
        bool IsMandatory,
        string VerificationMode,
        bool AllowAdminOverride);

    public record JobPosterDto(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        string PublicUrl,
        int DisplayOrder,
        DateTime CreatedAt);

    public record JobListDto(List<JobDto> Jobs, int TotalCount);
}
