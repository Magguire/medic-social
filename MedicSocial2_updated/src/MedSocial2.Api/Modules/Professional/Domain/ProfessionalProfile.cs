using System;

namespace Professional.Domain
{
    public class ProfessionalProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string? Nationality { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? NationalIdOrPassport { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? PostalAddress { get; set; }
        public string? ProfessionalCategory { get; set; }
        public string? Bio { get; set; }
        public string? LicenseNumber { get; set; }
        public string? LicenseBoard { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public int YearsOfExperience { get; set; }
        public string? Specialty { get; set; }
        public string? CurrentPosition { get; set; }
        public string? CurrentEmployer { get; set; }
        public string? PreferredLocation { get; set; }
        public int? RelocationWillingness { get; set; }
        public decimal? ExpectedSalary { get; set; }
        public string? AvailabilityType { get; set; }
        public string? Skills { get; set; }
        public string? Languages { get; set; }
        public string? WorkPermitStatus { get; set; }
        public double VerificationScore { get; set; } = 0;
        public string VerificationStatus { get; set; } = "Pending";
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class ExperienceRecord
    {
        public Guid Id { get; set; }
        public Guid ProfessionalId { get; set; }
        public string EmployerName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string? EmploymentType { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentRole { get; set; }
        public string? Responsibilities { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
