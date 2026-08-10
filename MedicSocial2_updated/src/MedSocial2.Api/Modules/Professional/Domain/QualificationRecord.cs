using System;

namespace Professional.Domain
{
    public class QualificationRecord
    {
        public Guid Id { get; set; }
        public Guid ProfessionalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string IssuingBody { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public DateTime? IssuedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
