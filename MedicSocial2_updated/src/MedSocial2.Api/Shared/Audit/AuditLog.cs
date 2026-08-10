using System;

namespace Shared.Audit
{
    public class AuditLog
    {
        public long Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string Changes { get; set; } = null!; // JSON
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public Guid? ArchivedByUserId { get; set; }
    }
}
