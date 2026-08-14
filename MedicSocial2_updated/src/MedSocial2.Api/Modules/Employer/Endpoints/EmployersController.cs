using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Employer.Application;
using Employer.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Shared.Data;
using Shared.Security;

namespace Employer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;
        private readonly IEmployerAccessService _access;
        private readonly IFileUploadSecurityService _fileSecurity;
        private readonly ISubscriptionService _subscriptions;

        public EmployersController(IMediator mediator, ApplicationDbContext db, IEmployerAccessService access, IFileUploadSecurityService fileSecurity, ISubscriptionService subscriptions)
        {
            _mediator = mediator;
            _db = db;
            _access = access;
            _fileSecurity = fileSecurity;
            _subscriptions = subscriptions;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var cmd = new RegisterEmployerCommand(dto.Name, dto.FacilityType, dto.ContactEmail, dto.ContactPhone, dto.IsContactPhonePublic, dto.Address, dto.BusinessRegistrationNumber, dto.KraPin, dto.LicenseNumber);
            var result = await _mediator.Send(cmd);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPut("{employerId:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid employerId, [FromBody] UpdateEmployerDto dto)
        {
            var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("TenantAdmin");
            if (!isAdmin)
            {
                var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == employerId);
                if (employer == null)
                {
                    return NotFound(new { errors = new[] { "Employer not found." } });
                }

                var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageProfile, HttpContext.RequestAborted);
                if (!access.IsAllowed)
                {
                    return Forbid();
                }

                dto = dto with { SubscriptionTier = employer.SubscriptionTier, VerificationStatus = employer.VerificationStatus };
            }

            var cmd = new UpdateEmployerCommand(employerId, dto.Name, dto.FacilityType, dto.ContactEmail, dto.ContactPhone, dto.IsContactPhonePublic, dto.Address, dto.BusinessRegistrationNumber, dto.KraPin, dto.LicenseNumber, dto.SubscriptionTier, dto.VerificationStatus);
            var result = await _mediator.Send(cmd);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("{employerId:guid}/team")]
        [Authorize]
        public async Task<IActionResult> Team(Guid employerId)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageTeam, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                var members = await _db.EmployerTeamMembers
                    .Where(m => m.EmployerId == employerId)
                    .Join(_db.Users, member => member.UserId, user => user.Id, (member, user) => new
                    {
                        member.Id,
                        member.EmployerId,
                        member.UserId,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        member.RoleName,
                        member.CanManageProfile,
                        member.CanManageSettings,
                        member.CanCreateJobs,
                        member.CanPublishJobs,
                        member.CanViewApplications,
                        member.CanVerifyApplications,
                        member.CanInviteProfessionals,
                        member.CanMessageProfessionals,
                        member.CanManageTeam,
                        member.IsOwner,
                        member.IsActive,
                        member.CreatedAt,
                        member.UpdatedAt
                    })
                    .OrderByDescending(m => m.IsOwner)
                    .ThenBy(m => m.Email)
                    .ToListAsync();
                return Ok(members);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{employerId:guid}/team")]
        [Authorize]
        public async Task<IActionResult> AddTeamMember(Guid employerId, [FromBody] EmployerTeamMemberRequest request)
        {
            try
            {
                var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageTeam, HttpContext.RequestAborted);
                if (!access.IsAllowed && !User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin")) return Forbid();

                var employer = access.Employer ?? await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == employerId);
                if (employer == null) return NotFound(new { errors = new[] { "Employer not found." } });
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                var accountCreated = false;
                if (user == null)
                {
                    if (!request.CreateAccountIfMissing)
                        return NotFound(new { errors = new[] { "A platform user with this email was not found. Enable account creation to onboard them." } });
                    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.TemporaryPassword))
                        return BadRequest(new { errors = new[] { "First name, last name, and a temporary password are required when creating a team account." } });
                    var passwordPolicy = await _db.PasswordPolicies.FirstOrDefaultAsync(item => item.Id == Identity.Domain.PasswordPolicyConfig.DefaultId)
                        ?? Identity.Domain.PasswordPolicyConfig.Default();
                    var passwordErrors = passwordPolicy.Validate(request.TemporaryPassword);
                    if (passwordErrors.Count > 0) return BadRequest(new { errors = passwordErrors });
                    user = new Identity.Domain.User
                    {
                        Id = Guid.NewGuid(),
                        TenantId = employer.TenantId,
                        Email = request.Email.Trim().ToLowerInvariant(),
                        PasswordHash = Identity.Infrastructure.PasswordHasher.Hash(request.TemporaryPassword),
                        FirstName = request.FirstName.Trim(),
                        LastName = request.LastName.Trim(),
                        PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
                        UserType = Identity.Domain.UserType.Recruiter,
                        Status = Identity.Domain.UserStatus.Active,
                        VerificationStatus = employer.VerificationStatus,
                        SubscriptionTier = employer.SubscriptionTier,
                        MustChangePassword = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(user);
                    accountCreated = true;
                }

                var member = await _db.EmployerTeamMembers.FirstOrDefaultAsync(m => m.EmployerId == employerId && m.UserId == user.Id);
                if (member == null)
                {
                    var subscription = await _subscriptions.GetCurrentAsync(employerId, HttpContext.RequestAborted);
                    if (subscription == null) return BadRequest(new { errors = new[] { "Employer subscription is not configured." } });
                    var activeMembers = await _db.EmployerTeamMembers.CountAsync(m => m.EmployerId == employerId && m.IsActive);
                    if (subscription.Plan.MaxTeamMembers >= 0 && activeMembers >= subscription.Plan.MaxTeamMembers)
                        return BadRequest(new { errors = new[] { $"{subscription.Plan.Name} allows {subscription.Plan.MaxTeamMembers} team member(s). Upgrade the subscription to add more users." } });
                    member = new Employer.Domain.EmployerTeamMember { Id = Guid.NewGuid(), EmployerId = employerId, TenantId = employer.TenantId, UserId = user.Id, CreatedAt = DateTime.UtcNow };
                    _db.EmployerTeamMembers.Add(member);
                }

                ApplyTeamMemberRequest(member, request, isOwner: false);
                await _db.SaveChangesAsync();
                return Ok(new { member.Id, member.EmployerId, member.UserId, member.RoleName, member.IsActive, accountCreated, user.Email, user.FirstName, user.LastName, user.MustChangePassword });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPut("{employerId:guid}/team/{memberId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateTeamMember(Guid employerId, Guid memberId, [FromBody] EmployerTeamMemberUpdateRequest request)
        {
            try
            {
                var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageTeam, HttpContext.RequestAborted);
                if (!access.IsAllowed && !User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin")) return Forbid();
                var member = await _db.EmployerTeamMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.EmployerId == employerId);
                if (member == null) return NotFound(new { errors = new[] { "Team member not found." } });
                if (member.IsOwner) return BadRequest(new { errors = new[] { "Owner permissions cannot be reduced." } });
                ApplyTeamMemberRequest(member, request, isOwner: false);
                await _db.SaveChangesAsync();
                return Ok(member);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> ListAll([FromQuery] Guid? tenantId)
        {
            try
            {
                var users = _db.Users.Where(user => user.UserType == Identity.Domain.UserType.Employer);
                if (tenantId.HasValue)
                {
                    users = users.Where(user => user.TenantId == tenantId.Value);
                }

                var items = await users
                    .GroupJoin(
                        _db.EmployerProfiles,
                        user => user.Email,
                        employer => employer.ContactEmail,
                        (user, employers) => new { user, employer = employers.FirstOrDefault() })
                    .OrderByDescending(row => row.user.CreatedAt)
                    .Select(row => new
                    {
                        id = row.employer == null ? (Guid?)null : row.employer.Id,
                        userId = row.user.Id,
                        row.user.TenantId,
                        name = row.employer == null
                            ? (row.user.FirstName + " " + row.user.LastName).Trim()
                            : row.employer.Name,
                        organizationSlug = row.employer == null ? null : row.employer.OrganizationSlug,
                        facilityType = row.employer == null ? null : row.employer.FacilityType,
                        contactEmail = row.user.Email,
                        contactPhone = row.employer == null ? row.user.PhoneNumber : row.employer.ContactPhone,
                        isContactPhonePublic = row.employer != null && row.employer.IsContactPhonePublic,
                        address = row.employer == null ? null : row.employer.Address,
                        subscriptionTier = row.employer == null ? row.user.SubscriptionTier : row.employer.SubscriptionTier,
                        verificationStatus = row.employer == null ? row.user.VerificationStatus : row.employer.VerificationStatus,
                        businessRegistrationNumber = row.employer == null ? null : row.employer.BusinessRegistrationNumber,
                        kraPin = row.employer == null ? null : row.employer.KraPin,
                        licenseNumber = row.employer == null ? null : row.employer.LicenseNumber,
                        hasCompletedProfile = row.employer != null,
                        accountStatus = row.user.Status.ToString(),
                        row.user.IsActive,
                        createdAt = row.user.CreatedAt,
                        row.user.LastLoginAt,
                        profileUpdatedAt = row.employer == null ? null : row.employer.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { items, totalCount = items.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("by-email")]
        [Authorize]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var employer = await _db.EmployerProfiles
                .Where(e => e.ContactEmail == email)
                .Select(e => new
                {
                    e.Id,
                    e.TenantId,
                    e.Name,
                    e.OrganizationSlug,
                    e.FacilityType,
                    e.ContactEmail,
                    e.ContactPhone,
                    e.IsContactPhonePublic,
                    e.Address,
                    e.SubscriptionTier,
                    e.VerificationStatus,
                    e.BusinessRegistrationNumber,
                    e.KraPin,
                    e.LicenseNumber,
                    e.CreatedAt,
                    e.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return employer == null ? NotFound() : Ok(employer);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentEmployer()
        {
            var userIdValue = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = null;

            if (Guid.TryParse(userIdValue, out var userId))
            {
                email = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
            }

            if (!Guid.TryParse(userIdValue, out var currentUserId))
            {
                return Unauthorized();
            }

            var employer = await _db.EmployerProfiles
                .Where(e => e.ContactEmail == email || _db.EmployerTeamMembers.Any(m => m.EmployerId == e.Id && m.UserId == currentUserId && m.IsActive))
                .Select(e => new
                {
                    e.Id,
                    e.TenantId,
                    e.Name,
                    e.OrganizationSlug,
                    e.FacilityType,
                    e.ContactEmail,
                    e.ContactPhone,
                    e.IsContactPhonePublic,
                    e.Address,
                    e.SubscriptionTier,
                    e.VerificationStatus,
                    e.BusinessRegistrationNumber,
                    e.KraPin,
                    e.LicenseNumber,
                    e.CreatedAt,
                    e.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return employer == null ? NotFound() : Ok(employer);
        }

        [HttpGet("tenant/{tenantId:guid}")]
        [Authorize]
        public async Task<IActionResult> List(Guid tenantId)
        {
            var result = await _mediator.Send(new ListEmployersQuery(tenantId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{employerId:guid}/documents")]
        [Authorize]
        public async Task<IActionResult> UploadDocument(Guid employerId, [FromForm] UploadEmployerDocumentDto dto)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageProfile, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                var security = await _fileSecurity.ValidateAsync(dto.File, cancellationToken: HttpContext.RequestAborted);
                if (!security.IsSafe) return BadRequest(new { errors = new[] { security.Error } });
                using var ms = new MemoryStream();
                await dto.File.CopyToAsync(ms, HttpContext.RequestAborted);
                var result = await _mediator.Send(new UploadEmployerDocumentCommand(employerId, dto.DocumentType, ms.ToArray(), dto.File.FileName));
                return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("{employerId:guid}/documents")]
        [Authorize]
        public async Task<IActionResult> Documents(Guid employerId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.ManageProfile, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new GetEmployerDocumentsQuery(employerId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        private Guid CurrentUserId()
        {
            var userIdValue = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new InvalidOperationException("User id claim is missing.");
            }

            return userId;
        }

        private static void ApplyTeamMemberRequest(Employer.Domain.EmployerTeamMember member, EmployerTeamMemberUpdateRequest request, bool isOwner)
        {
            member.RoleName = string.IsNullOrWhiteSpace(request.RoleName) ? "Member" : request.RoleName.Trim();
            member.CanManageProfile = request.CanManageProfile;
            member.CanManageSettings = request.CanManageSettings;
            member.CanCreateJobs = request.CanCreateJobs;
            member.CanPublishJobs = request.CanPublishJobs;
            member.CanViewApplications = request.CanViewApplications;
            member.CanVerifyApplications = request.CanVerifyApplications;
            member.CanInviteProfessionals = request.CanInviteProfessionals;
            member.CanMessageProfessionals = request.CanMessageProfessionals;
            member.CanManageTeam = request.CanManageTeam;
            member.IsOwner = isOwner;
            member.IsActive = request.IsActive;
            member.UpdatedAt = DateTime.UtcNow;
        }
    }

    public record RegisterDto(string Name, string FacilityType, string ContactEmail, string? ContactPhone, bool IsContactPhonePublic, string? Address, string? BusinessRegistrationNumber, string? KraPin, string? LicenseNumber);
    public record UpdateEmployerDto(string? Name, string? FacilityType, string? ContactEmail, string? ContactPhone, bool? IsContactPhonePublic, string? Address, string? BusinessRegistrationNumber, string? KraPin, string? LicenseNumber, string? SubscriptionTier, string? VerificationStatus);
    public record UploadEmployerDocumentDto(IFormFile File, string DocumentType);
    public record EmployerTeamMemberUpdateRequest(string RoleName, bool CanManageProfile, bool CanManageSettings, bool CanCreateJobs, bool CanPublishJobs, bool CanViewApplications, bool CanVerifyApplications, bool CanInviteProfessionals, bool CanMessageProfessionals, bool CanManageTeam, bool IsActive);
    public record EmployerTeamMemberRequest(string Email, string RoleName, bool CanManageProfile, bool CanManageSettings, bool CanCreateJobs, bool CanPublishJobs, bool CanViewApplications, bool CanVerifyApplications, bool CanInviteProfessionals, bool CanMessageProfessionals, bool CanManageTeam, bool IsActive, bool CreateAccountIfMissing, string? FirstName, string? LastName, string? PhoneNumber, string? TemporaryPassword) : EmployerTeamMemberUpdateRequest(RoleName, CanManageProfile, CanManageSettings, CanCreateJobs, CanPublishJobs, CanViewApplications, CanVerifyApplications, CanInviteProfessionals, CanMessageProfessionals, CanManageTeam, IsActive);
}
