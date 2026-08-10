using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Professional.Application;
using Professional.Application.Commands;
using Professional.Infrastructure;
using Professional.Infrastructure.Storage;
using Shared.Data;
using Shared.Security;
using Employer.Application;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Professional.Api.Controllers
{
    using Microsoft.AspNetCore.Authorization;

    [ApiController]
    [Route("api/[controller]")]
    public class ProfessionalsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ProfessionalDbContext _db;
        private readonly IDocumentStorageService _storage;
        private readonly IFileUploadSecurityService _fileSecurity;
        private readonly ISubscriptionService _subscriptions;

        public ProfessionalsController(IMediator mediator, ProfessionalDbContext db, IDocumentStorageService storage, IFileUploadSecurityService fileSecurity, ISubscriptionService subscriptions)
        {
            _mediator = mediator;
            _db = db;
            _storage = storage;
            _fileSecurity = fileSecurity;
            _subscriptions = subscriptions;
        }

        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> Categories()
        {
            var result = await _mediator.Send(new GetProfessionalCategoriesQuery());
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterProfessionalRequest request)
        {
            var command = new RegisterProfessionalCommand(
                request.UserId,
                request.Nationality,
                request.PhoneNumber,
                request.EmailAddress,
                request.NationalIdOrPassport,
                request.AddressLine,
                request.City,
                request.County,
                request.PostalAddress,
                request.ProfessionalCategory,
                request.LicenseNumber,
                request.LicenseBoard,
                request.YearsOfExperience,
                request.Specialty);

            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List(
            [FromQuery] Guid? tenantId,
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? location,
            [FromQuery] string? specialty,
            [FromQuery] int? minimumYearsOfExperience,
            [FromQuery] string? verificationStatus)
        {
            try
            {
                var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("TenantAdmin") || User.IsInRole("Auditor");
                if (isAdmin)
                {
                    var users = _db.Users.Where(user => user.UserType == Identity.Domain.UserType.Professional);
                    if (tenantId.HasValue)
                    {
                        users = users.Where(user => user.TenantId == tenantId.Value);
                    }

                    var accountsQuery = users
                        .GroupJoin(
                            _db.ProfessionalProfiles,
                            user => user.Id,
                            profile => profile.UserId,
                            (user, profiles) => new { user, profile = profiles.FirstOrDefault() });

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var term = search.Trim();
                        accountsQuery = accountsQuery.Where(row =>
                            row.user.Email.Contains(term) ||
                            row.user.FirstName.Contains(term) ||
                            row.user.LastName.Contains(term) ||
                            (row.profile != null && (
                                (row.profile.ProfessionalCategory != null && row.profile.ProfessionalCategory.Contains(term)) ||
                                (row.profile.Specialty != null && row.profile.Specialty.Contains(term)))));
                    }
                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        accountsQuery = accountsQuery.Where(row => row.profile != null && row.profile.ProfessionalCategory != null && row.profile.ProfessionalCategory.Contains(category));
                    }
                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        accountsQuery = accountsQuery.Where(row => row.profile != null &&
                            ((row.profile.PreferredLocation != null && row.profile.PreferredLocation.Contains(location)) ||
                             (row.profile.City != null && row.profile.City.Contains(location)) ||
                             (row.profile.County != null && row.profile.County.Contains(location))));
                    }
                    if (!string.IsNullOrWhiteSpace(specialty))
                    {
                        accountsQuery = accountsQuery.Where(row => row.profile != null && row.profile.Specialty != null && row.profile.Specialty.Contains(specialty));
                    }
                    if (minimumYearsOfExperience.HasValue)
                    {
                        accountsQuery = accountsQuery.Where(row => row.profile != null && row.profile.YearsOfExperience >= minimumYearsOfExperience.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(verificationStatus))
                    {
                        accountsQuery = accountsQuery.Where(row => row.profile != null && row.profile.VerificationStatus == verificationStatus);
                    }

                    var accounts = await accountsQuery
                        .OrderByDescending(row => row.user.CreatedAt)
                        .Select(row => new
                        {
                            id = row.profile == null ? (Guid?)null : row.profile.Id,
                            userId = row.user.Id,
                            row.user.TenantId,
                            row.user.Email,
                            fullName = (row.user.FirstName + " " + row.user.LastName).Trim(),
                            row.user.PhoneNumber,
                            accountStatus = row.user.Status.ToString(),
                            row.user.IsActive,
                            hasCompletedProfile = row.profile != null,
                            row.user.CreatedAt,
                            row.user.LastLoginAt,
                            professionalCategory = row.profile == null ? null : row.profile.ProfessionalCategory,
                            specialty = row.profile == null ? null : row.profile.Specialty,
                            preferredLocation = row.profile == null ? null : row.profile.PreferredLocation,
                            city = row.profile == null ? null : row.profile.City,
                            county = row.profile == null ? null : row.profile.County,
                            yearsOfExperience = row.profile == null ? 0 : row.profile.YearsOfExperience,
                            verificationStatus = row.profile == null
                                ? row.user.VerificationStatus
                                : row.profile.VerificationStatus
                        })
                        .ToListAsync();

                    return Ok(accounts);
                }

                Employer.Domain.SubscriptionPlan? employerPlan = null;
                if (User.IsInRole("Employer"))
                {
                    var userIdValue = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    string? email = null;
                    if (Guid.TryParse(userIdValue, out var userId))
                    {
                        email = await _db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync();
                    }

                    email ??= User.FindFirst(ClaimTypes.Email)?.Value;
                    var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.ContactEmail == email);
                    if (employer == null)
                    {
                        return Forbid();
                    }

                    var entitlement = await _subscriptions.RequireModuleAsync(employer.Id, "talent-search", HttpContext.RequestAborted);
                    if (!entitlement.IsAllowed) return StatusCode(403, new { errors = new[] { entitlement.Error } });
                    var visibility = await _subscriptions.RequireModuleAsync(employer.Id, "professional-profiles", HttpContext.RequestAborted);
                    if (!visibility.IsAllowed) return StatusCode(403, new { errors = new[] { visibility.Error } });
                    employerPlan = entitlement.Context!.Plan;
                }

                var query = _db.ProfessionalProfiles.AsQueryable();
                if (tenantId.HasValue)
                {
                    query = query.Where(p => p.TenantId == tenantId.Value);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(profile =>
                        (profile.ProfessionalCategory != null && profile.ProfessionalCategory.Contains(term)) ||
                        (profile.Specialty != null && profile.Specialty.Contains(term)) ||
                        (profile.PreferredLocation != null && profile.PreferredLocation.Contains(term)) ||
                        (profile.City != null && profile.City.Contains(term)));
                }
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(profile => profile.ProfessionalCategory != null && profile.ProfessionalCategory.Contains(category));
                }
                if (!string.IsNullOrWhiteSpace(location))
                {
                    query = query.Where(profile =>
                        (profile.PreferredLocation != null && profile.PreferredLocation.Contains(location)) ||
                        (profile.City != null && profile.City.Contains(location)) ||
                        (profile.County != null && profile.County.Contains(location)));
                }
                if (!string.IsNullOrWhiteSpace(specialty))
                {
                    query = query.Where(profile => profile.Specialty != null && profile.Specialty.Contains(specialty));
                }
                if (minimumYearsOfExperience.HasValue)
                {
                    query = query.Where(profile => profile.YearsOfExperience >= minimumYearsOfExperience.Value);
                }
                if (!string.IsNullOrWhiteSpace(verificationStatus))
                {
                    query = query.Where(profile => profile.VerificationStatus == verificationStatus);
                }

                var list = await query
                    .GroupJoin(_db.Users,
                        profile => profile.UserId,
                        user => user.Id,
                        (profile, users) => new { profile, user = users.FirstOrDefault() })
                    .Select(row => new
                    {
                        row.profile.Id,
                        row.profile.UserId,
                        row.profile.TenantId,
                        row.profile.ProfessionalCategory,
                        row.profile.Specialty,
                        row.profile.YearsOfExperience,
                        row.profile.PreferredLocation,
                        row.profile.City,
                        row.profile.County,
                        verificationStatus = employerPlan == null || employerPlan.CanViewProfessionalVerificationStatus ? row.profile.VerificationStatus : "Restricted",
                        email = employerPlan == null || employerPlan.CanViewProfessionalContactDetails ? row.user != null ? row.user.Email : null : null,
                        fullName = employerPlan == null || employerPlan.CanViewProfessionalContactDetails ? row.user != null ? row.user.FirstName + " " + row.user.LastName : null : null
                    })
                    .ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("by-user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var p = await _db.ProfessionalProfiles.FirstOrDefaultAsync(profile => profile.UserId == userId);
            if (p == null) return NotFound();
            return Ok(ProfessionalMappings.Map(p));
        }

        [HttpGet("{professionalId:guid}")]
        [Authorize]
        public async Task<IActionResult> Get(Guid professionalId)
        {
            var p = await _db.ProfessionalProfiles.FindAsync(professionalId);
            if (p == null) return NotFound();
            return Ok(ProfessionalMappings.Map(p));
        }

        [HttpPut("{professionalId:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid professionalId, [FromBody] UpdateProfessionalRequest request)
        {
            var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("TenantAdmin");
            if (!isAdmin)
            {
                var userIdValue = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    return Unauthorized();
                }

                var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == professionalId);
                if (profile == null || profile.UserId != userId)
                {
                    return Forbid();
                }
            }

            var result = await _mediator.Send(new UpdateProfessionalCommand(
                professionalId,
                request.Nationality,
                request.PhoneNumber,
                request.EmailAddress,
                request.NationalIdOrPassport,
                request.AddressLine,
                request.City,
                request.County,
                request.PostalAddress,
                request.Bio,
                request.YearsOfExperience,
                request.CurrentPosition,
                request.CurrentEmployer,
                request.PreferredLocation,
                request.RelocationWillingness,
                request.ExpectedSalary,
                request.AvailabilityType,
                request.ProfessionalCategory,
                request.LicenseExpiryDate,
                request.Skills,
                request.Languages,
                request.WorkPermitStatus,
                request.Specialty));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{professionalId:guid}/verification")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> SetVerification(Guid professionalId, [FromBody] SetProfessionalVerificationRequest request)
        {
            var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == professionalId);
            if (profile == null) return NotFound();

            profile.VerificationStatus = request.Status;
            profile.RejectionReason = request.Notes;
            profile.VerifiedAt = string.Equals(request.Status, "Verified", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : profile.VerifiedAt;
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(ProfessionalMappings.Map(profile));
        }

        [HttpPost("{professionalId:guid}/education")]
        [Authorize]
        public async Task<IActionResult> AddEducation(Guid professionalId, [FromBody] AddEducationRequest request)
        {
            var result = await _mediator.Send(new AddEducationRecordCommand(professionalId, request.Institution, request.Award, request.FieldOfStudy, request.StartDate, request.EndDate, request.Grade));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("{professionalId:guid}/education")]
        [Authorize]
        public async Task<IActionResult> GetEducation(Guid professionalId)
        {
            var result = await _mediator.Send(new GetEducationRecordsQuery(professionalId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{professionalId:guid}/qualifications")]
        [Authorize]
        public async Task<IActionResult> AddQualification(Guid professionalId, [FromBody] AddQualificationRequest request)
        {
            var result = await _mediator.Send(new AddQualificationRecordCommand(professionalId, request.Title, request.IssuingBody, request.LicenseNumber, request.IssuedOn, request.ExpiresOn));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("{professionalId:guid}/qualifications")]
        [Authorize]
        public async Task<IActionResult> GetQualifications(Guid professionalId)
        {
            var result = await _mediator.Send(new GetQualificationRecordsQuery(professionalId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{professionalId:guid}/experience")]
        [Authorize]
        public async Task<IActionResult> AddExperience(Guid professionalId, [FromBody] AddExperienceRequest request)
        {
            var result = await _mediator.Send(new AddExperienceRecordCommand(professionalId, request.EmployerName, request.JobTitle, request.EmploymentType, request.Location, request.StartDate, request.EndDate, request.IsCurrentRole, request.Responsibilities));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("{professionalId:guid}/experience")]
        [Authorize]
        public async Task<IActionResult> GetExperience(Guid professionalId)
        {
            var result = await _mediator.Send(new GetExperienceRecordsQuery(professionalId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{professionalId:guid}/documents")]
        [Authorize]
        public async Task<IActionResult> UploadDocument(Guid professionalId, [FromForm] UploadDocumentRequest request)
        {
            try
            {
                var security = await _fileSecurity.ValidateAsync(request.File, cancellationToken: HttpContext.RequestAborted);
                if (!security.IsSafe) return BadRequest(new { errors = new[] { security.Error } });
                using var ms = new MemoryStream();
                await request.File.CopyToAsync(ms, HttpContext.RequestAborted);
                var result = await _mediator.Send(new UploadDocumentCommand(professionalId, request.DocumentType, ms.ToArray(), request.File.FileName));
                return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("{professionalId:guid}/documents")]
        [Authorize]
        public async Task<IActionResult> GetDocuments(Guid professionalId)
        {
            if (User.IsInRole("Employer"))
            {
                var subscriptionTier = User.FindFirst("SubscriptionTier")?.Value;
                var plan = string.IsNullOrWhiteSpace(subscriptionTier) ? null : await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Slug == subscriptionTier);
                if (plan == null || !plan.CanViewProfessionalDocuments)
                {
                    return Forbid();
                }
            }

            var docs = await _db.Documents.Where(d => d.ProfessionalId == professionalId)
                .Select(d => new { d.Id, d.FileName, type = d.DocumentTypeName ?? d.Type.ToString(), status = d.Status.ToString(), d.VerificationNotes, d.CreatedAt })
                .ToListAsync();
            return Ok(docs);
        }

        [HttpGet("documents/{documentId:guid}")]
        [Authorize]
        public async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var doc = await _db.Documents.FindAsync(documentId);
            if (doc == null) return NotFound();
            var stream = await _storage.OpenReadAsync(doc.StoragePath);
            return File(stream, doc.ContentType, doc.FileName);
        }
    }

    public record RegisterProfessionalRequest(Guid UserId, string Nationality, string? PhoneNumber, string? EmailAddress, string? NationalIdOrPassport, string? AddressLine, string? City, string? County, string? PostalAddress, string ProfessionalCategory, string LicenseNumber, string LicenseBoard, int YearsOfExperience, string Specialty);
    public record UpdateProfessionalRequest(string? Nationality, string? PhoneNumber, string? EmailAddress, string? NationalIdOrPassport, string? AddressLine, string? City, string? County, string? PostalAddress, string? Bio, int? YearsOfExperience, string? CurrentPosition, string? CurrentEmployer, string? PreferredLocation, int? RelocationWillingness, decimal? ExpectedSalary, string? AvailabilityType, string? ProfessionalCategory, DateTime? LicenseExpiryDate, string? Skills, string? Languages, string? WorkPermitStatus, string? Specialty);
    public record SetProfessionalVerificationRequest(string Status, string? Notes);
    public record AddEducationRequest(string Institution, string Award, string FieldOfStudy, DateTime StartDate, DateTime? EndDate, string? Grade);
    public record AddQualificationRequest(string Title, string IssuingBody, string? LicenseNumber, DateTime? IssuedOn, DateTime? ExpiresOn);
    public record AddExperienceRequest(string EmployerName, string JobTitle, string? EmploymentType, string? Location, DateTime StartDate, DateTime? EndDate, bool IsCurrentRole, string? Responsibilities);
    public record UploadDocumentRequest(IFormFile File, string DocumentType);
}
