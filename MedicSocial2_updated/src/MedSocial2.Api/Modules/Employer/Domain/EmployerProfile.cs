using System;

namespace Employer.Domain
{
    public class EmployerProfile
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
        public string FacilityType { get; set; } = "Healthcare Facility";
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string SubscriptionTier { get; set; } = "Free";
        public string VerificationStatus { get; set; } = "Pending";
        public string? BusinessRegistrationNumber { get; set; }
        public string? KraPin { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
