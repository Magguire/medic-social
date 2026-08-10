using Employer.Domain;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Employer.Application;

public static class EmployerPermissions
{
    public const string ManageProfile = "ManageProfile";
    public const string ManageSettings = "ManageSettings";
    public const string CreateJobs = "CreateJobs";
    public const string PublishJobs = "PublishJobs";
    public const string ViewApplications = "ViewApplications";
    public const string VerifyApplications = "VerifyApplications";
    public const string InviteProfessionals = "InviteProfessionals";
    public const string MessageProfessionals = "MessageProfessionals";
    public const string ManageTeam = "ManageTeam";
}

public record EmployerAccessResult(bool IsAllowed, EmployerProfile? Employer, EmployerTeamMember? Member, string? Error);

public interface IEmployerAccessService
{
    Task<EmployerAccessResult> RequireAsync(Guid userId, Guid employerId, string permission, CancellationToken cancellationToken);
    Task<EmployerAccessResult> RequireByTenantAsync(Guid userId, Guid tenantId, string permission, CancellationToken cancellationToken);
    Task EnsureOwnerMembershipAsync(EmployerProfile employer, CancellationToken cancellationToken);
}

public class EmployerAccessService : IEmployerAccessService
{
    private readonly ApplicationDbContext _db;

    public EmployerAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<EmployerAccessResult> RequireAsync(Guid userId, Guid employerId, string permission, CancellationToken cancellationToken)
    {
        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == employerId, cancellationToken);
        if (employer == null)
        {
            return new EmployerAccessResult(false, null, null, "Employer not found.");
        }

        await EnsureOwnerMembershipAsync(employer, cancellationToken);
        var member = await _db.EmployerTeamMembers.FirstOrDefaultAsync(m => m.EmployerId == employerId && m.UserId == userId && m.IsActive, cancellationToken);
        return HasPermission(member, permission)
            ? new EmployerAccessResult(true, employer, member, null)
            : new EmployerAccessResult(false, employer, member, "Your employer role does not allow this action.");
    }

    public async Task<EmployerAccessResult> RequireByTenantAsync(Guid userId, Guid tenantId, string permission, CancellationToken cancellationToken)
    {
        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.TenantId == tenantId, cancellationToken);
        return employer == null
            ? new EmployerAccessResult(false, null, null, "Employer not found.")
            : await RequireAsync(userId, employer.Id, permission, cancellationToken);
    }

    public async Task EnsureOwnerMembershipAsync(EmployerProfile employer, CancellationToken cancellationToken)
    {
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Email == employer.ContactEmail, cancellationToken);
        if (owner == null)
        {
            return;
        }

        var existing = await _db.EmployerTeamMembers.FirstOrDefaultAsync(m => m.EmployerId == employer.Id && m.UserId == owner.Id, cancellationToken);
        if (existing != null)
        {
            return;
        }

        _db.EmployerTeamMembers.Add(new EmployerTeamMember
        {
            Id = Guid.NewGuid(),
            EmployerId = employer.Id,
            TenantId = employer.TenantId,
            UserId = owner.Id,
            RoleName = "Owner",
            CanManageProfile = true,
            CanManageSettings = true,
            CanCreateJobs = true,
            CanPublishJobs = true,
            CanViewApplications = true,
            CanVerifyApplications = true,
            CanInviteProfessionals = true,
            CanMessageProfessionals = true,
            CanManageTeam = true,
            IsOwner = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool HasPermission(EmployerTeamMember? member, string permission)
    {
        if (member == null || !member.IsActive)
        {
            return false;
        }

        if (member.IsOwner)
        {
            return true;
        }

        return permission switch
        {
            EmployerPermissions.ManageProfile => member.CanManageProfile,
            EmployerPermissions.ManageSettings => member.CanManageSettings,
            EmployerPermissions.CreateJobs => member.CanCreateJobs,
            EmployerPermissions.PublishJobs => member.CanPublishJobs,
            EmployerPermissions.ViewApplications => member.CanViewApplications,
            EmployerPermissions.VerifyApplications => member.CanVerifyApplications,
            EmployerPermissions.InviteProfessionals => member.CanInviteProfessionals,
            EmployerPermissions.MessageProfessionals => member.CanMessageProfessionals,
            EmployerPermissions.ManageTeam => member.CanManageTeam,
            _ => false
        };
    }
}
