using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Employer.Application;
using Job.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;
using Shared.Notifications;
using Shared.Security;

namespace Job.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;
        private readonly IEmployerAccessService _access;
        private readonly ISubscriptionService _subscriptions;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileUploadSecurityService _fileSecurity;
        private readonly INotificationService _notifications;

        public JobsController(IMediator mediator, ApplicationDbContext db, IEmployerAccessService access, ISubscriptionService subscriptions, IWebHostEnvironment environment, IFileUploadSecurityService fileSecurity, INotificationService notifications)
        {
            _mediator = mediator;
            _db = db;
            _access = access;
            _subscriptions = subscriptions;
            _environment = environment;
            _fileSecurity = fileSecurity;
            _notifications = notifications;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), request.EmployerId, EmployerPermissions.CreateJobs, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();

                    var payAsYouGoGate = await EnforceEmployerPostingPayAsYouGoAsync(request.EmployerId, request.TenantId, HttpContext.RequestAborted);
                    if (payAsYouGoGate != null)
                    {
                        return payAsYouGoGate;
                    }
                }

                var result = await _mediator.Send(new CreateJobCommand(request.EmployerId, request.TenantId, request.Title, request.Description, request.Department, request.EngagementType, request.ShiftPattern, request.Location, request.SalaryMin, request.SalaryMax, request.RequiredProfessionalCategory, request.MinimumYearsOfExperience, request.RequireVerifiedProfessional, request.AllowInvites, request.ClosesAt, request.RequiredDocuments?.Select(MapRequiredDocument).ToList()));
                return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{jobId:guid}/publish")]
        [Authorize]
        public async Task<IActionResult> PublishJob(Guid jobId, [FromBody] PublishJobRequest request)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == request.TenantId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.PublishJobs, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new PublishJobCommand(jobId, request.TenantId));
            return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ListJobs(
            [FromQuery] Guid? tenantId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? q = null,
            [FromQuery] string? category = null,
            [FromQuery] string? department = null,
            [FromQuery] string? engagementType = null,
            [FromQuery] string? location = null,
            [FromQuery] bool? requireVerifiedProfessional = null,
            [FromQuery] decimal? salaryMin = null,
            [FromQuery] decimal? salaryMax = null)
        {
            var result = await _mediator.Send(new ListJobsCommand(tenantId, pageNumber, pageSize, q, category, department, engagementType, location, requireVerifiedProfessional, salaryMin, salaryMax));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [AllowAnonymous]
        [HttpGet("search-options")]
        public async Task<IActionResult> SearchOptions()
        {
            var categories = await _db.ProfessionalCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Name, c.Slug })
                .ToListAsync();

            var locations = await _db.Jobs
                .Where(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow && !string.IsNullOrWhiteSpace(j.Location))
                .Select(j => j.Location)
                .Distinct()
                .OrderBy(location => location)
                .ToListAsync();

            var departments = await _db.Jobs
                .Where(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow && !string.IsNullOrWhiteSpace(j.Department))
                .Select(j => j.Department)
                .Distinct()
                .OrderBy(department => department)
                .ToListAsync();

            var engagementTypes = await GetActiveEngagementTypesAsync();

            var metrics = new
            {
                totalPublishedJobs = await _db.Jobs.CountAsync(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow),
                closingSoonJobs = await _db.Jobs.CountAsync(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow && j.ClosesAt <= DateTime.UtcNow.AddDays(3)),
                verifiedRequiredJobs = await _db.Jobs.CountAsync(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow && j.RequireVerifiedProfessional),
                locationCount = locations.Count,
                categoryCount = categories.Count
            };

            return Ok(new { categories, locations, departments, engagementTypes, metrics });
        }

        [AllowAnonymous]
        [HttpGet("marketplace-metrics")]
        public async Task<IActionResult> MarketplaceMetrics()
        {
            try
            {
                var liveJobs = await _db.Jobs.CountAsync(job => job.Status == Job.Domain.JobStatus.Published && job.ClosesAt >= DateTime.UtcNow);
                var employers = await _db.EmployerProfiles.Select(employer => employer.Id).Distinct().CountAsync();
                var professionals = await _db.Users.CountAsync(user => user.UserType == Identity.Domain.UserType.Professional);
                return Ok(new { liveJobs, employers, professionals });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("admin")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> AdminCreateJob([FromBody] AdminCreateJobRequest request)
        {
            try
            {
                var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == request.EmployerId);
                if (employer == null)
                {
                    return BadRequest(new { errors = new[] { "Employer not found." } });
                }

                var job = new Job.Domain.Job
                {
                    Id = Guid.NewGuid(),
                    EmployerId = employer.Id,
                    TenantId = employer.TenantId,
                    Title = request.Title,
                    Description = request.Description,
                    Department = request.Department,
                    EngagementType = string.IsNullOrWhiteSpace(request.EngagementType) ? "Permanent" : request.EngagementType.Trim(),
                    ShiftPattern = string.IsNullOrWhiteSpace(request.ShiftPattern) ? null : request.ShiftPattern.Trim(),
                    Location = request.Location,
                    SalaryMin = request.SalaryMin,
                    SalaryMax = request.SalaryMax,
                    RequiredProfessionalCategory = request.RequiredProfessionalCategory,
                    MinimumYearsOfExperience = request.MinimumYearsOfExperience,
                    RequireVerifiedProfessional = request.RequireVerifiedProfessional,
                    AllowInvites = request.AllowInvites,
                    Status = request.PublishNow ? Job.Domain.JobStatus.Published : Job.Domain.JobStatus.Draft,
                    PublishedAt = request.PublishNow ? DateTime.UtcNow : default,
                    ClosesAt = request.ClosesAt,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Jobs.Add(job);
                var requiredDocuments = (request.RequiredDocuments ?? new List<JobRequiredDocumentInputRequest>())
                    .Where(item => !string.IsNullOrWhiteSpace(item.DocumentType))
                    .GroupBy(item => item.DocumentType.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Select(item => new Job.Domain.JobRequiredDocument
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        DocumentType = item.DocumentType.Trim(),
                        IsMandatory = item.IsMandatory,
                        VerificationMode = NormalizeVerificationMode(item.VerificationMode),
                        AllowAdminOverride = item.AllowAdminOverride,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (requiredDocuments.Count > 0)
                {
                    _db.JobRequiredDocuments.AddRange(requiredDocuments);
                }

                await _db.SaveChangesAsync();
                return Ok(JobMappings.Map(job, requiredDocuments.Select(JobMappings.Map), []));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> AdminListJobs(
            [FromQuery] string? moderationState,
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] string? department,
            [FromQuery] string? engagementType,
            [FromQuery] string? location,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var query = _db.Jobs.AsQueryable();
                var state = (moderationState ?? "active").Trim().ToLowerInvariant();
                query = state switch
                {
                    "flagged" => query.Where(job => job.Status == Job.Domain.JobStatus.Flagged),
                    "removed" or "deleted" => query.Where(job => job.Status == Job.Domain.JobStatus.Removed),
                    "all" => query,
                    _ => query.Where(job => job.Status != Job.Domain.JobStatus.Flagged && job.Status != Job.Domain.JobStatus.Removed)
                };

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var search = q.Trim();
                    query = query.Where(job => job.Title.Contains(search) || job.Description.Contains(search) || job.Department.Contains(search) || job.Location.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(job => job.RequiredProfessionalCategory != null && job.RequiredProfessionalCategory.Contains(category.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(department))
                {
                    query = query.Where(job => job.Department.Contains(department.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(engagementType))
                {
                    query = query.Where(job => job.EngagementType == engagementType.Trim() || job.EngagementType.Contains(engagementType.Trim()));
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    query = query.Where(job => job.Location.Contains(location.Trim()));
                }

                var totalCount = await query.CountAsync(HttpContext.RequestAborted);
                var jobEntities = await query
                    .OrderByDescending(job => job.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(HttpContext.RequestAborted);
                var jobIds = jobEntities.Select(job => job.Id).ToList();
                var requirementLookup = await JobMappings.LoadRequirementLookup(_db, jobIds, HttpContext.RequestAborted);
                var posterLookup = await JobMappings.LoadPosterLookup(_db, jobIds, HttpContext.RequestAborted);
                var applicationCounts = await _db.JobApplications
                    .Where(application => jobIds.Contains(application.JobId))
                    .GroupBy(application => application.JobId)
                    .Select(group => new { JobId = group.Key, Count = group.Count(), Shortlisted = group.Count(item => item.IsShortlisted) })
                    .ToDictionaryAsync(item => item.JobId, HttpContext.RequestAborted);
                var employerIds = jobEntities.Select(job => job.EmployerId).Distinct().ToList();
                var employers = await _db.EmployerProfiles
                    .Where(employer => employerIds.Contains(employer.Id))
                    .ToDictionaryAsync(employer => employer.Id, HttpContext.RequestAborted);

                var jobs = jobEntities.Select(job =>
                {
                    employers.TryGetValue(job.EmployerId, out var employer);
                    applicationCounts.TryGetValue(job.Id, out var counts);
                    return new
                    {
                        job = JobMappings.Map(job, requirementLookup.GetValueOrDefault(job.Id), posterLookup.GetValueOrDefault(job.Id)),
                        employerName = employer?.Name ?? "Unknown employer",
                        employerContactEmail = employer?.ContactEmail,
                        employerContactPhone = employer?.IsContactPhonePublic == true ? employer.ContactPhone : null,
                        applicationsCount = counts?.Count ?? 0,
                        shortlistedCount = counts?.Shortlisted ?? 0
                    };
                });

                return Ok(new { jobs, totalCount, pageNumber, pageSize });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("admin/{jobId:guid}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> AdminGetJob(Guid jobId)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId, HttpContext.RequestAborted);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                var requirements = await _db.JobRequiredDocuments.Where(item => item.JobId == jobId).OrderBy(item => item.DocumentType).Select(item => JobMappings.Map(item)).ToListAsync(HttpContext.RequestAborted);
                var posters = await _db.JobPosters.Where(item => item.JobId == jobId).OrderBy(item => item.DisplayOrder).Select(item => JobMappings.Map(item)).ToListAsync(HttpContext.RequestAborted);
                var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(item => item.Id == job.EmployerId, HttpContext.RequestAborted);
                var applicationCount = await _db.JobApplications.CountAsync(item => item.JobId == jobId, HttpContext.RequestAborted);
                var watchCount = await _db.JobWatches.CountAsync(item => item.JobId == jobId, HttpContext.RequestAborted);

                return Ok(new
                {
                    job = JobMappings.Map(job, requirements, posters),
                    employer,
                    applicationCount,
                    watchCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("admin/{jobId:guid}/status")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> AdminChangeStatus(Guid jobId, [FromBody] AdminChangeJobStatusRequest request)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId, HttpContext.RequestAborted);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                if (!Enum.TryParse<Job.Domain.JobStatus>(request.Status, true, out var status))
                {
                    return BadRequest(new { errors = new[] { "Unsupported job status." } });
                }

                var oldStatus = job.Status;
                if (status is Job.Domain.JobStatus.Flagged or Job.Domain.JobStatus.Removed)
                {
                    job.PreviousStatusBeforeModeration = oldStatus is Job.Domain.JobStatus.Flagged or Job.Domain.JobStatus.Removed ? job.PreviousStatusBeforeModeration : oldStatus;
                    job.ModerationReason = request.Reason;
                    job.ModeratedAt = DateTime.UtcNow;
                    job.ModeratedByUserId = CurrentUserId();
                }
                else
                {
                    job.ModerationReason = null;
                    job.ModeratedAt = null;
                    job.ModeratedByUserId = null;
                    job.PreviousStatusBeforeModeration = null;
                }

                job.Status = status;
                if (status == Job.Domain.JobStatus.Published && job.PublishedAt == default)
                {
                    job.PublishedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(HttpContext.RequestAborted);

                if (status is Job.Domain.JobStatus.Flagged or Job.Domain.JobStatus.Removed)
                {
                    await NotifyEmployerOnlyAsync(job, status == Job.Domain.JobStatus.Flagged ? "flagged" : "removed", request.Reason, HttpContext.RequestAborted);
                }

                return Ok(JobMappings.Map(job));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("admin/{jobId:guid}/restore")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> AdminRestoreJob(Guid jobId)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId, HttpContext.RequestAborted);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                var restoredStatus = job.PreviousStatusBeforeModeration ?? (job.ClosesAt >= DateTime.UtcNow ? Job.Domain.JobStatus.Published : Job.Domain.JobStatus.Closed);
                job.Status = restoredStatus;
                job.ModerationReason = null;
                job.ModeratedAt = null;
                job.ModeratedByUserId = null;
                job.PreviousStatusBeforeModeration = null;
                if (job.Status == Job.Domain.JobStatus.Published && job.PublishedAt == default)
                {
                    job.PublishedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await NotifyEmployerOnlyAsync(job, "restored", null, HttpContext.RequestAborted);
                return Ok(JobMappings.Map(job));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [Authorize]
        [HttpGet("employer/{employerId:guid}")]
        public async Task<IActionResult> ListEmployerJobs(Guid employerId, [FromQuery] Guid tenantId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var access = await _access.RequireAsync(CurrentUserId(), employerId, EmployerPermissions.CreateJobs, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
                var subscription = await _subscriptions.RequireModuleAsync(employerId, "job-posting", HttpContext.RequestAborted);
                if (!subscription.IsAllowed) return StatusCode(403, new { errors = new[] { subscription.Error } });
            }

            var jobEntities = await _db.Jobs
                .Where(j => j.EmployerId == employerId && j.TenantId == tenantId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            var jobIds = jobEntities.Select(j => j.Id).ToList();
            var requirementLookup = await JobMappings.LoadRequirementLookup(_db, jobIds, HttpContext.RequestAborted);
            var posterLookup = await JobMappings.LoadPosterLookup(_db, jobIds, HttpContext.RequestAborted);
            var jobs = jobEntities.Select(job => JobMappings.Map(job, requirementLookup.GetValueOrDefault(job.Id), posterLookup.GetValueOrDefault(job.Id))).ToList();

            return Ok(new { jobs, totalCount = jobs.Count });
        }

        [AllowAnonymous]
        [HttpGet("{jobId:guid}")]
        public async Task<IActionResult> GetJob(Guid jobId)
        {
            var result = await _mediator.Send(new GetJobByIdQuery(jobId));
            return result.IsSuccess ? Ok(result.Value) : NotFound(new { errors = result.Errors });
        }

        [HttpPut("{jobId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateJob(Guid jobId, [FromBody] UpdateJobRequest request)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId && item.TenantId == request.TenantId);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.CreateJobs, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                var oldTitle = job.Title;
                var oldClosesAt = job.ClosesAt;
                var oldStatus = job.Status;
                job.Title = request.Title.Trim();
                job.Description = request.Description.Trim();
                job.Department = request.Department.Trim();
                job.EngagementType = string.IsNullOrWhiteSpace(request.EngagementType) ? "Permanent" : request.EngagementType.Trim();
                job.ShiftPattern = string.IsNullOrWhiteSpace(request.ShiftPattern) ? null : request.ShiftPattern.Trim();
                job.Location = request.Location.Trim();
                job.SalaryMin = request.SalaryMin;
                job.SalaryMax = request.SalaryMax;
                job.RequiredProfessionalCategory = request.RequiredProfessionalCategory;
                job.MinimumYearsOfExperience = request.MinimumYearsOfExperience;
                job.RequireVerifiedProfessional = request.RequireVerifiedProfessional;
                job.AllowInvites = request.AllowInvites;
                job.ClosesAt = request.ClosesAt;
                if (job.Status == Job.Domain.JobStatus.Closed && job.ClosesAt >= DateTime.UtcNow)
                {
                    job.Status = Job.Domain.JobStatus.Published;
                    if (job.PublishedAt == default)
                    {
                        job.PublishedAt = DateTime.UtcNow;
                    }
                }

                var existingRequirements = await _db.JobRequiredDocuments.Where(item => item.JobId == job.Id).ToListAsync(HttpContext.RequestAborted);
                _db.JobRequiredDocuments.RemoveRange(existingRequirements);
                var newRequirements = (request.RequiredDocuments ?? new List<JobRequiredDocumentInputRequest>())
                    .Where(item => !string.IsNullOrWhiteSpace(item.DocumentType))
                    .GroupBy(item => item.DocumentType.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Select(item => new Job.Domain.JobRequiredDocument
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        DocumentType = item.DocumentType.Trim(),
                        IsMandatory = item.IsMandatory,
                        VerificationMode = NormalizeVerificationMode(item.VerificationMode),
                        AllowAdminOverride = item.AllowAdminOverride,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();
                _db.JobRequiredDocuments.AddRange(newRequirements);

                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                if (oldTitle != job.Title || oldClosesAt != job.ClosesAt || oldStatus != job.Status)
                {
                    await _notifications.NotifyJobWatchersAsync(
                        job.Id,
                        "A watched job was updated",
                        $"{job.Title} was updated. Current status: {JobMappings.GetDisplayStatus(job)}. Closing date: {job.ClosesAt:d}.",
                        HttpContext.RequestAborted);
                }
                var posters = await _db.JobPosters.Where(item => item.JobId == job.Id).OrderBy(item => item.DisplayOrder).Select(item => JobMappings.Map(item)).ToListAsync(HttpContext.RequestAborted);
                return Ok(JobMappings.Map(job, newRequirements.Select(JobMappings.Map), posters));
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{jobId:guid}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeStatus(Guid jobId, [FromBody] ChangeJobStatusRequest request)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId && item.TenantId == request.TenantId);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.PublishJobs, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                if (!Enum.TryParse<Job.Domain.JobStatus>(request.Status, true, out var status) || status is Job.Domain.JobStatus.Published)
                {
                    return BadRequest(new { errors = new[] { "Choose Draft, Closed, or Cancelled as the next status." } });
                }

                job.Status = status;
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await _notifications.NotifyJobWatchersAsync(
                    job.Id,
                    "A watched job status changed",
                    $"{job.Title} is now {JobMappings.GetDisplayStatus(job)}.",
                    HttpContext.RequestAborted);
                return Ok(new { job.Id, status = job.Status.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpDelete("{jobId:guid}/posters/{posterId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeletePoster(Guid jobId, Guid posterId)
        {
            try
            {
                var poster = await _db.JobPosters.FirstOrDefaultAsync(item => item.Id == posterId && item.JobId == jobId);
                if (poster == null)
                {
                    return NotFound(new { errors = new[] { "Poster not found." } });
                }

                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.CreateJobs, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                _db.JobPosters.Remove(poster);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                if (System.IO.File.Exists(poster.StoragePath))
                {
                    System.IO.File.Delete(poster.StoragePath);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{jobId:guid}/watch")]
        [Authorize]
        public async Task<IActionResult> WatchJob(Guid jobId)
        {
            try
            {
                var userId = CurrentUserId();
                var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == jobId && item.Status == Job.Domain.JobStatus.Published && item.ClosesAt >= DateTime.UtcNow);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                var existing = await _db.JobWatches.FirstOrDefaultAsync(item => item.JobId == jobId && item.UserId == userId);
                if (existing == null)
                {
                    var professionalId = await _db.ProfessionalProfiles
                        .Where(profile => profile.UserId == userId)
                        .Select(profile => (Guid?)profile.Id)
                        .FirstOrDefaultAsync();
                    _db.JobWatches.Add(new JobWatch
                    {
                        Id = Guid.NewGuid(),
                        JobId = jobId,
                        UserId = userId,
                        ProfessionalId = professionalId,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(HttpContext.RequestAborted);
                }

                return Ok(new { jobId, isWatching = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpDelete("{jobId:guid}/watch")]
        [Authorize]
        public async Task<IActionResult> UnwatchJob(Guid jobId)
        {
            try
            {
                var userId = CurrentUserId();
                var existing = await _db.JobWatches.FirstOrDefaultAsync(item => item.JobId == jobId && item.UserId == userId);
                if (existing != null)
                {
                    _db.JobWatches.Remove(existing);
                    await _db.SaveChangesAsync(HttpContext.RequestAborted);
                }

                return Ok(new { jobId, isWatching = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpGet("{jobId:guid}/watch")]
        [Authorize]
        public async Task<IActionResult> WatchStatus(Guid jobId)
        {
            try
            {
                var userId = CurrentUserId();
                var isWatching = await _db.JobWatches.AnyAsync(item => item.JobId == jobId && item.UserId == userId);
                return Ok(new { jobId, isWatching });
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{jobId:guid}/posters")]
        [Authorize]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> UploadPosters(Guid jobId, [FromForm] JobPosterUploadRequest request)
        {
            try
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(item => item.Id == jobId);
                if (job == null)
                {
                    return NotFound(new { errors = new[] { "Job not found." } });
                }

                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
                {
                    var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.CreateJobs, HttpContext.RequestAborted);
                    if (!access.IsAllowed) return Forbid();
                }

                var files = request.Files?.Where(file => file.Length > 0).ToList() ?? new List<IFormFile>();
                if (files.Count == 0)
                {
                    return BadRequest(new { errors = new[] { "Choose at least one job poster to upload." } });
                }

                var root = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(root))
                {
                    root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                }

                var folderSegment = DateTime.UtcNow.ToString("yyyyMMdd");
                var folder = Path.Combine(root, "job-posters", folderSegment);
                Directory.CreateDirectory(folder);
                var existingCount = await _db.JobPosters.CountAsync(item => item.JobId == job.Id, HttpContext.RequestAborted);
                var posters = new List<Job.Domain.JobPoster>();

                foreach (var file in files)
                {
                    if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && !file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { errors = new[] { "Job posters must be images or PDF files." } });
                    }

                    var security = await _fileSecurity.ValidateAsync(file, 10_000_000, HttpContext.RequestAborted);
                    if (!security.IsSafe)
                    {
                        return BadRequest(new { errors = new[] { security.Error ?? "One of the uploaded posters failed security validation." } });
                    }

                    var extension = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var path = Path.Combine(folder, fileName);
                    await using (var stream = System.IO.File.Create(path))
                    {
                        await file.CopyToAsync(stream, HttpContext.RequestAborted);
                    }

                    posters.Add(new Job.Domain.JobPoster
                    {
                        Id = Guid.NewGuid(),
                        JobId = job.Id,
                        TenantId = job.TenantId,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        SizeBytes = file.Length,
                        StoragePath = path,
                        PublicUrl = $"/job-posters/{folderSegment}/{fileName}",
                        DisplayOrder = existingCount + posters.Count,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.JobPosters.AddRange(posters);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                return Ok(posters.Select(JobMappings.Map).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("{jobId:guid}/apply")]
        [Authorize]
        public async Task<IActionResult> ApplyForJob(Guid jobId, [FromBody] ApplyJobRequest request)
        {
            var result = await _mediator.Send(new ApplyForJobCommand(jobId, request.ProfessionalId, request.TenantId));
            return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
        }

        [Authorize]
        [HttpGet("applications/professional/{professionalId:guid}")]
        public async Task<IActionResult> ProfessionalApplications(Guid professionalId)
        {
            var applications = await _db.JobApplications
                .Where(a => a.ProfessionalId == professionalId)
                .Join(_db.Jobs,
                    application => application.JobId,
                    job => job.Id,
                    (application, job) => new
                    {
                        application.Id,
                        application.JobId,
                        application.ProfessionalId,
                        application.TenantId,
                        status = application.Status.ToString(),
                        application.IsShortlisted,
                        application.Score,
                        application.AppliedAt,
                        jobTitle = job.Title,
                        jobDepartment = job.Department,
                        jobLocation = job.Location,
                        jobStatus = job.Status.ToString(),
                        jobClosesAt = job.ClosesAt
                    })
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(applications);
        }

        [Authorize]
        [HttpGet("{jobId:guid}/applications")]
        public async Task<IActionResult> JobApplications(Guid jobId, [FromQuery] Guid tenantId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.ViewApplications, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
                var subscription = await _subscriptions.RequireModuleAsync(job.EmployerId, "applicant-review", HttpContext.RequestAborted);
                if (!subscription.IsAllowed) return StatusCode(403, new { errors = new[] { subscription.Error } });
            }

            var requiredDocuments = await _db.JobRequiredDocuments
                .Where(item => item.JobId == jobId)
                .OrderBy(item => item.DocumentType)
                .Select(item => JobMappings.Map(item))
                .ToListAsync();

            var applications = await _db.JobApplications
                .Where(a => a.JobId == jobId && a.TenantId == tenantId)
                .Join(_db.ProfessionalProfiles,
                    application => application.ProfessionalId,
                    profile => profile.Id,
                    (application, profile) => new
                    {
                        application.Id,
                        application.JobId,
                        application.ProfessionalId,
                        application.TenantId,
                        status = application.Status.ToString(),
                        application.IsShortlisted,
                        application.Score,
                        application.AppliedAt,
                        professionalCategory = profile.ProfessionalCategory,
                        profile.Specialty,
                        profile.YearsOfExperience,
                        verificationStatus = profile.VerificationStatus
                    })
                .OrderByDescending(a => a.Score)
                .ThenByDescending(a => a.AppliedAt)
                .ToListAsync();

            var professionalIds = applications.Select(application => (Guid)application.ProfessionalId).Distinct().ToList();
            var documents = await _db.Documents
                .Where(document => professionalIds.Contains(document.ProfessionalId))
                .Select(document => new
                {
                    document.Id,
                    document.ProfessionalId,
                    document.FileName,
                    documentType = document.DocumentTypeName ?? document.Type.ToString(),
                    status = document.Status.ToString(),
                    document.VerificationNotes,
                    document.CreatedAt
                })
                .ToListAsync();

            var response = applications.Select(application =>
            {
                var applicantDocuments = documents
                    .Where(document => document.ProfessionalId == application.ProfessionalId)
                    .Select(document => new
                    {
                        document.Id,
                        document.FileName,
                        document.documentType,
                        document.status,
                        document.VerificationNotes,
                        document.CreatedAt,
                        isRequired = requiredDocuments.Any(requirement => string.Equals(requirement.DocumentType, document.documentType, StringComparison.OrdinalIgnoreCase)),
                        verificationMode = requiredDocuments.FirstOrDefault(requirement => string.Equals(requirement.DocumentType, document.documentType, StringComparison.OrdinalIgnoreCase))?.VerificationMode ?? Job.Domain.JobDocumentVerificationMode.PlatformVerification
                    })
                    .ToList();

                var missingRequiredDocuments = requiredDocuments
                    .Where(requirement => requirement.IsMandatory && !applicantDocuments.Any(document => string.Equals(document.documentType, requirement.DocumentType, StringComparison.OrdinalIgnoreCase)))
                    .Select(requirement => requirement.DocumentType)
                    .ToList();

                return new
                {
                    application.Id,
                    application.JobId,
                    application.ProfessionalId,
                    application.TenantId,
                    application.status,
                    application.IsShortlisted,
                    application.Score,
                    application.AppliedAt,
                    application.professionalCategory,
                    application.Specialty,
                    application.YearsOfExperience,
                    application.verificationStatus,
                    requiredDocuments,
                    documents = applicantDocuments,
                    missingRequiredDocuments
                };
            }).ToList();

            return Ok(response);
        }

        [HttpPost("applications/{applicationId:guid}/shortlist")]
        [Authorize]
        public async Task<IActionResult> ShortlistCandidate(Guid applicationId, [FromBody] ShortlistRequest request)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var application = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == applicationId && a.TenantId == request.TenantId);
                if (application == null) return NotFound(new { errors = new[] { "Application not found." } });
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == application.JobId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.ViewApplications, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new ShortlistCandidateCommand(applicationId, request.TenantId));
            return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("applications/{applicationId:guid}/documents/{documentId:guid}/review")]
        [Authorize]
        public async Task<IActionResult> ReviewApplicantDocument(Guid applicationId, Guid documentId, [FromBody] ReviewApplicationDocumentRequest request)
        {
            var application = await _db.JobApplications.FirstOrDefaultAsync(item => item.Id == applicationId && item.TenantId == request.TenantId);
            if (application == null)
            {
                return NotFound(new { errors = new[] { "Application not found." } });
            }

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var jobForAccess = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == application.JobId);
                if (jobForAccess == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), jobForAccess.EmployerId, EmployerPermissions.VerifyApplications, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var requiredDocuments = await _db.JobRequiredDocuments
                .Where(item => item.JobId == application.JobId)
                .ToListAsync();
            if (requiredDocuments.Count == 0)
            {
                return BadRequest(new { errors = new[] { "This job does not have employer-managed applicant document requirements." } });
            }

            var document = await _db.Documents.FirstOrDefaultAsync(item => item.Id == documentId && item.ProfessionalId == application.ProfessionalId);
            if (document == null)
            {
                return NotFound(new { errors = new[] { "Document not found for this applicant." } });
            }

            var documentType = document.DocumentTypeName ?? document.Type.ToString();
            var matchingRequirement = requiredDocuments.FirstOrDefault(item =>
                string.Equals(item.DocumentType, documentType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.VerificationMode, Job.Domain.JobDocumentVerificationMode.EmployerReview, StringComparison.OrdinalIgnoreCase));
            if (matchingRequirement == null)
            {
                return BadRequest(new { errors = new[] { "This document is not configured for employer-side review on the selected job." } });
            }

            document.Status = request.IsApproved ? Professional.Domain.DocumentStatus.Verified : Professional.Domain.DocumentStatus.Rejected;
            document.VerificationNotes = request.Notes;
            document.VerifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                document.Id,
                document.FileName,
                documentType,
                status = document.Status.ToString(),
                document.VerificationNotes,
                document.VerifiedAt
            });
        }

        private static CreateJobRequiredDocumentItem MapRequiredDocument(JobRequiredDocumentInputRequest request) => new(
            request.DocumentType,
            request.IsMandatory,
            NormalizeVerificationMode(request.VerificationMode),
            request.AllowAdminOverride);

        private static string NormalizeVerificationMode(string? verificationMode)
        {
            if (string.Equals(verificationMode, Job.Domain.JobDocumentVerificationMode.PlatformVerification, StringComparison.OrdinalIgnoreCase))
            {
                return Job.Domain.JobDocumentVerificationMode.PlatformVerification;
            }

            return Job.Domain.JobDocumentVerificationMode.EmployerReview;
        }

        private async Task<IActionResult?> EnforceEmployerPostingPayAsYouGoAsync(Guid employerId, Guid tenantId, CancellationToken cancellationToken)
        {
            var rule = await _db.PayAsYouGoRules.FirstOrDefaultAsync(item => item.Action == PayAsYouGoAction.EmployerJobPosting && item.IsEnabled, cancellationToken);
            if (rule == null)
            {
                return null;
            }

            var period = PayAsYouGoPeriodKey(rule.PeriodKey);
            var used = await _db.PayAsYouGoCharges.CountAsync(item =>
                item.Action == PayAsYouGoAction.EmployerJobPosting &&
                item.EmployerId == employerId &&
                item.PeriodKey == period &&
                item.Status != PayAsYouGoChargeStatus.Failed,
                cancellationToken);
            var isChargeRequired = used >= rule.FreeUnitsPerPeriod;
            var provider = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(item => item.IsEnabled, cancellationToken);
            var charge = new PayAsYouGoCharge
            {
                Id = Guid.NewGuid(),
                Action = PayAsYouGoAction.EmployerJobPosting,
                UserId = CurrentUserId(),
                EmployerId = employerId,
                TenantId = tenantId,
                Units = 1,
                UnitPrice = isChargeRequired ? rule.UnitPrice : 0,
                Amount = isChargeRequired ? rule.UnitPrice : 0,
                Currency = rule.Currency,
                Status = !isChargeRequired || rule.UnitPrice <= 0
                    ? PayAsYouGoChargeStatus.Free
                    : provider == null
                        ? PayAsYouGoChargeStatus.PendingAdminReview
                        : PayAsYouGoChargeStatus.PendingPayment,
                PeriodKey = period,
                PayerDetailsJson = "{}",
                FailureReason = isChargeRequired && provider == null ? "No automated payment provider is enabled. Awaiting administrator review." : null,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = !isChargeRequired || rule.UnitPrice <= 0 ? DateTime.UtcNow : null
            };
            _db.PayAsYouGoCharges.Add(charge);
            await _db.SaveChangesAsync(cancellationToken);

            if (isChargeRequired && rule.RequirePaymentBeforeAction && charge.Status is PayAsYouGoChargeStatus.PendingPayment or PayAsYouGoChargeStatus.PendingAdminReview)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    errors = new[] { provider == null ? "Posting request received for administrator payment review." : "Payment is required before creating another job opening." },
                    isChargeRequired = true,
                    chargeId = charge.Id,
                    charge.Status,
                    rule.UnitPrice,
                    rule.Currency,
                    provider = provider == null ? null : new { provider.Provider, provider.DisplayName, provider.PromptFieldsJson, provider.ReceiverAccount }
                });
            }

            return null;
        }

        private static string PayAsYouGoPeriodKey(string? period)
        {
            return string.Equals(period, "Daily", StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : DateTime.UtcNow.ToString("yyyy-MM");
        }

        private async Task NotifyEmployerOnlyAsync(Job.Domain.Job job, string action, string? reason, CancellationToken cancellationToken)
        {
            var employer = await _db.EmployerProfiles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == job.EmployerId, cancellationToken);
            var recipients = await _db.EmployerTeamMembers
                .Where(member => member.EmployerId == job.EmployerId && member.IsActive)
                .Select(member => member.UserId)
                .ToListAsync(cancellationToken);

            if (employer != null && !string.IsNullOrWhiteSpace(employer.ContactEmail))
            {
                var ownerUserId = await _db.Users
                    .Where(user => user.Email == employer.ContactEmail)
                    .Select(user => (Guid?)user.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (ownerUserId.HasValue)
                {
                    recipients.Add(ownerUserId.Value);
                }
            }

            foreach (var userId in recipients.Distinct())
            {
                await _notifications.NotifyAsync(
                    userId,
                    "JobModeration",
                    $"Job {action}",
                    string.IsNullOrWhiteSpace(reason)
                        ? $"{job.Title} was {action} by platform administration."
                        : $"{job.Title} was {action} by platform administration. Reason: {reason}",
                    $"/employer/jobs",
                    "Job",
                    job.Id,
                    cancellationToken);
            }
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

        private async Task<List<JobEngagementTypeDto>> GetActiveEngagementTypesAsync()
        {
            var configured = await _db.JobEngagementTypes
                .Where(item => item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Name)
                .Select(item => new JobEngagementTypeDto(item.Id, item.Name, item.Slug, item.Description, item.AllowsShiftPattern, item.IsActive, item.DisplayOrder))
                .ToListAsync(HttpContext.RequestAborted);

            return configured.Count > 0 ? configured : DefaultEngagementTypes();
        }

        internal static List<JobEngagementTypeDto> DefaultEngagementTypes() =>
        [
            new(Guid.Empty, "Permanent", "permanent", "Ongoing role with no fixed end date.", false, true, 0),
            new(Guid.Empty, "Temporary", "temporary", "Time-bound cover, relief, or interim role.", false, true, 1),
            new(Guid.Empty, "Contract", "contract", "Fixed-term contract engagement.", false, true, 2),
            new(Guid.Empty, "Shift driven", "shift-driven", "Roster, shift, locum, or session-based work.", true, true, 3),
            new(Guid.Empty, "Part time", "part-time", "Reduced-hours or recurring part-time role.", true, true, 4)
        ];
    }

    public record CreateJobRequest(Guid EmployerId, Guid TenantId, string Title, string Description, string Department, string? EngagementType, string? ShiftPattern, string Location, decimal SalaryMin, decimal SalaryMax, string? RequiredProfessionalCategory, int? MinimumYearsOfExperience, bool RequireVerifiedProfessional, bool AllowInvites, DateTime ClosesAt, List<JobRequiredDocumentInputRequest>? RequiredDocuments);
    public record AdminCreateJobRequest(Guid EmployerId, string Title, string Description, string Department, string? EngagementType, string? ShiftPattern, string Location, decimal SalaryMin, decimal SalaryMax, string? RequiredProfessionalCategory, int? MinimumYearsOfExperience, bool RequireVerifiedProfessional, bool AllowInvites, bool PublishNow, DateTime ClosesAt, List<JobRequiredDocumentInputRequest>? RequiredDocuments);
    public record UpdateJobRequest(Guid TenantId, string Title, string Description, string Department, string? EngagementType, string? ShiftPattern, string Location, decimal SalaryMin, decimal SalaryMax, string? RequiredProfessionalCategory, int? MinimumYearsOfExperience, bool RequireVerifiedProfessional, bool AllowInvites, DateTime ClosesAt, List<JobRequiredDocumentInputRequest>? RequiredDocuments);
    public record ChangeJobStatusRequest(Guid TenantId, string Status);
    public record AdminChangeJobStatusRequest(string Status, string? Reason);
    public record PublishJobRequest(Guid TenantId);
    public record ApplyJobRequest(Guid ProfessionalId, Guid TenantId);
    public record ShortlistRequest(Guid TenantId);
    public record JobRequiredDocumentInputRequest(string DocumentType, bool IsMandatory, string VerificationMode, bool AllowAdminOverride);
    public record JobEngagementTypeDto(Guid Id, string Name, string Slug, string Description, bool AllowsShiftPattern, bool IsActive, int DisplayOrder);
    public record ReviewApplicationDocumentRequest(Guid TenantId, bool IsApproved, string? Notes);
    public class JobPosterUploadRequest
    {
        public List<IFormFile>? Files { get; set; }
    }
}
