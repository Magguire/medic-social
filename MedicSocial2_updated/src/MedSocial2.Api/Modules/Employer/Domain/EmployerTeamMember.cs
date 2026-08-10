namespace Employer.Domain;

public class EmployerTeamMember
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = "Member";
    public bool CanManageProfile { get; set; }
    public bool CanManageSettings { get; set; }
    public bool CanCreateJobs { get; set; }
    public bool CanPublishJobs { get; set; }
    public bool CanViewApplications { get; set; }
    public bool CanVerifyApplications { get; set; }
    public bool CanInviteProfessionals { get; set; }
    public bool CanMessageProfessionals { get; set; }
    public bool CanManageTeam { get; set; }
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
