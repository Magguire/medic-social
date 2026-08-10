using System;

namespace Job.Domain
{
    public class Job
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid EmployerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string EngagementType { get; set; } = "Permanent";
        public string? ShiftPattern { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal SalaryMin { get; set; }
        public decimal SalaryMax { get; set; }
        public string? RequiredProfessionalCategory { get; set; }
        public int? MinimumYearsOfExperience { get; set; }
        public bool RequireVerifiedProfessional { get; set; } = true;
        public bool AllowInvites { get; set; } = true;
        public JobStatus Status { get; set; } = JobStatus.Draft;
        public string? ModerationReason { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public Guid? ModeratedByUserId { get; set; }
        public JobStatus? PreviousStatusBeforeModeration { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime ClosesAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum JobStatus
    {
        Draft = 0,
        Published = 1,
        Closed = 2,
        Cancelled = 3,
        Flagged = 4,
        Removed = 5
    }

    public class JobPoster
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public Guid TenantId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class JobEngagementType
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool AllowsShiftPattern { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
