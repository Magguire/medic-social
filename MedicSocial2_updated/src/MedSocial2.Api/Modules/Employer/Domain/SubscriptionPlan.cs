using System;

namespace Employer.Domain
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string BillingInterval { get; set; } = "Monthly";
        public int MaxPublishedJobs { get; set; }
        public int MaxTeamMembers { get; set; } = 1;
        public int MaxCandidateInvitesPerPeriod { get; set; }
        public int MaxMessagesPerPeriod { get; set; }
        public bool CanAccessJobPostingModule { get; set; } = true;
        public bool CanAccessApplicantReviewModule { get; set; } = true;
        public bool CanAccessTalentSearchModule { get; set; }
        public bool CanAccessReportsModule { get; set; }
        public bool CanAccessCommunicationsModule { get; set; }
        public bool CanViewProfessionalProfiles { get; set; } = true;
        public bool CanViewProfessionalContactDetails { get; set; }
        public bool CanViewProfessionalDocuments { get; set; }
        public bool CanViewProfessionalVerificationStatus { get; set; } = true;
        public bool CanInviteCandidates { get; set; }
        public bool CanMessageCandidates { get; set; }
        public bool CanUseEmailCommunications { get; set; } = true;
        public bool CanUseSmsCommunications { get; set; }
        public bool CanUseWhatsAppCommunications { get; set; }
        public bool RequiresEmployerVerificationToPublishJobs { get; set; } = true;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
