using System;

using System;

namespace Job.Domain
{
    public class JobApplication
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid JobId { get; set; }
        public Guid ProfessionalId { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
        public int? Score { get; set; }
        public bool IsShortlisted { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ApplicationStatus
    {
        Submitted = 0,
        Shortlisted = 1,
        Rejected = 2,
        InterviewSelected = 3,
        OfferMade = 4,
        Hired = 5,
        Declined = 6
    }
}
