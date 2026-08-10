using System;

namespace Job.Domain
{
    public class JobRequiredDocument
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public bool IsMandatory { get; set; } = true;
        public string VerificationMode { get; set; } = JobDocumentVerificationMode.EmployerReview;
        public bool AllowAdminOverride { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class JobDocumentVerificationMode
    {
        public const string EmployerReview = "EmployerReview";
        public const string PlatformVerification = "PlatformVerification";
    }
}
