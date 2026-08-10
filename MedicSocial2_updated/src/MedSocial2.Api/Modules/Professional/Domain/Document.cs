using System;

namespace Professional.Domain
{
    public class Document
    {
        public Guid Id { get; set; }
        public Guid ProfessionalId { get; set; }
        public Guid TenantId { get; set; }
        public DocumentType Type { get; set; }
        public string? DocumentTypeName { get; set; }
        public string FileName { get; set; } = null!;
        public string StoragePath { get; set; } = null!; // S3/Blob URL
        public string? SignedUrl { get; set; }
        public DateTime SignedUrlExpiry { get; set; }
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = null!;
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
        public string? VerificationNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
    }

    public enum DocumentType
    {
        NationalId = 0,
        Passport = 1,
        License = 2,
        EducationCertificate = 3,
        ExperienceLetter = 4,
        Certification = 5,
        WorkPermit = 6
    }

    public enum DocumentStatus
    {
        Pending = 0,
        Verified = 1,
        Rejected = 2,
        Expired = 3
    }
}
