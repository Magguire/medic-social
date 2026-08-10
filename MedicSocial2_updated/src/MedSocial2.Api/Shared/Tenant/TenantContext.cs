using System;

namespace Shared.Tenant
{
    public class TenantContext
    {
        public Guid TenantId { get; set; }
        public string? Name { get; set; }
        public string SubscriptionTier { get; set; } = string.Empty;
    }
}