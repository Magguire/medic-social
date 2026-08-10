using System;

namespace Employer.Domain
{
    public class EmployerDocument
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public Guid TenantId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = "Pending";
        public string? VerificationNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }
}
