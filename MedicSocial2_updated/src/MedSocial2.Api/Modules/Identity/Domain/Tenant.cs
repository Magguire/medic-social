using System;

namespace Identity.Domain
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Logo { get; set; }
        public string SubscriptionTier { get; set; } = "Basic";
        public TenantStatus Status { get; set; } = TenantStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DisabledAt { get; set; }
        public string RegionCode { get; set; } = "KE";
        public string? MaxDatabaseProvider { get; set; } // Constraint: which DB they use
        public int MaxProfessionals { get; set; } = 100;
        public int MaxEmployers { get; set; } = 50;
    }

    public enum TenantStatus
    {
        Active = 0,
        Suspended = 1,
        Trial = 2,
        Disabled = 3
    }
}