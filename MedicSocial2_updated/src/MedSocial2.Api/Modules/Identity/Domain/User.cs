using System;

namespace Identity.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Pending;
        public string VerificationStatus { get; set; } = "Unverified";
        public string SubscriptionTier { get; set; } = "None";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public DateTime? SessionsInvalidatedAt { get; set; }
        public bool MustChangePassword { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum UserType
    {
        SuperAdmin = 0,
        TenantAdmin = 1,
        Employer = 2,
        Recruiter = 3,
        Professional = 4,
        Auditor = 5
    }

    public enum UserStatus
    {
        Pending = 0,
        Active = 1,
        Suspended = 2,
        Deleted = 3
    }
}
