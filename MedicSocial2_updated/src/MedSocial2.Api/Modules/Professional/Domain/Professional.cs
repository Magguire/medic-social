using System;

using System;

namespace Professional.Domain
{
    public class Professional
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}