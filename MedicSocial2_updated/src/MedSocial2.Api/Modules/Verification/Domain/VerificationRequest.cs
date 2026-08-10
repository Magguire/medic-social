using System;

namespace Verification.Domain
{
    public class VerificationRequest
    {
        public Guid Id { get; set; }
        public VerificationSubjectType SubjectType { get; set; } = VerificationSubjectType.Professional;
        public Guid? ProfessionalId { get; set; }
        public Guid? EmployerId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid TenantId { get; set; }
        public Guid? DocumentId { get; set; }
        public string? Notes { get; set; }
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
        public Guid? ReviewedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }

    public enum VerificationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
