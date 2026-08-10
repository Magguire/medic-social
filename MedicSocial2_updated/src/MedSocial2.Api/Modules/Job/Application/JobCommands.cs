using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Employer.Domain;
using Job.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Data;
using Shared.Kernel;
using Verification.Application;
using Verification.Domain;
using Shared.Notifications;

namespace Job.Application.Commands
{
    public record CreateJobRequiredDocumentItem(
        string DocumentType,
        bool IsMandatory,
        string VerificationMode,
        bool AllowAdminOverride);

    public record CreateJobCommand(
        Guid EmployerId,
        Guid TenantId,
        string Title,
        string Description,
        string Department,
        string? EngagementType,
        string? ShiftPattern,
        string Location,
        decimal SalaryMin,
        decimal SalaryMax,
        string? RequiredProfessionalCategory,
        int? MinimumYearsOfExperience,
        bool RequireVerifiedProfessional,
        bool AllowInvites,
        DateTime ClosesAt,
        IReadOnlyCollection<CreateJobRequiredDocumentItem>? RequiredDocuments) : IRequest<Result<JobDto>>;

    public record PublishJobCommand(Guid JobId, Guid TenantId) : IRequest<Result>;
    public record ListJobsCommand(
        Guid? TenantId,
        int PageNumber = 1,
        int PageSize = 20,
        string? Search = null,
        string? Category = null,
        string? Department = null,
        string? EngagementType = null,
        string? Location = null,
        bool? RequireVerifiedProfessional = null,
        decimal? SalaryMin = null,
        decimal? SalaryMax = null) : IRequest<Result<JobListDto>>;
    public record GetJobByIdQuery(Guid JobId) : IRequest<Result<JobDto>>;
    public record ApplyForJobCommand(Guid JobId, Guid ProfessionalId, Guid TenantId) : IRequest<Result>;
    public record ShortlistCandidateCommand(Guid ApplicationId, Guid TenantId) : IRequest<Result>;

    public class CreateJobHandler : IRequestHandler<CreateJobCommand, Result<JobDto>>
    {
        private readonly ApplicationDbContext _db;
        private readonly Employer.Application.ISubscriptionService _subscriptions;
        public CreateJobHandler(ApplicationDbContext db, Employer.Application.ISubscriptionService subscriptions) { _db = db; _subscriptions = subscriptions; }

        public async Task<Result<JobDto>> Handle(CreateJobCommand request, CancellationToken cancellationToken)
        {
            var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == request.EmployerId && e.TenantId == request.TenantId, cancellationToken);
            if (employer == null)
                return Result<JobDto>.Failure("Employer not found");

            var entitlement = await _subscriptions.RequireModuleAsync(employer.Id, "job-posting", cancellationToken);
            if (!entitlement.IsAllowed) return Result<JobDto>.Failure(entitlement.Error ?? "Job posting access is not available.");

            var job = new Job.Domain.Job
            {
                Id = Guid.NewGuid(),
                EmployerId = request.EmployerId,
                TenantId = request.TenantId,
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
                Status = Job.Domain.JobStatus.Draft,
                ClosesAt = request.ClosesAt,
                CreatedAt = DateTime.UtcNow
            };

            _db.Jobs.Add(job);
            var requiredDocuments = (request.RequiredDocuments ?? Array.Empty<CreateJobRequiredDocumentItem>())
                .Where(item => !string.IsNullOrWhiteSpace(item.DocumentType))
                .GroupBy(item => item.DocumentType.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(item => new JobRequiredDocument
                {
                    Id = Guid.NewGuid(),
                    JobId = job.Id,
                    DocumentType = item.DocumentType.Trim(),
                    IsMandatory = item.IsMandatory,
                    VerificationMode = JobMappings.NormalizeVerificationMode(item.VerificationMode),
                    AllowAdminOverride = item.AllowAdminOverride,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (requiredDocuments.Count > 0)
            {
                _db.JobRequiredDocuments.AddRange(requiredDocuments);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _subscriptions.RecordUsageAsync(entitlement.Context!, Employer.Application.SubscriptionMetrics.JobsCreated, 1, cancellationToken);
            return Result<JobDto>.Success(JobMappings.Map(job, requiredDocuments.Select(JobMappings.Map), []));
        }
    }

    public class PublishJobHandler : IRequestHandler<PublishJobCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        private readonly Employer.Application.ISubscriptionService _subscriptions;
        public PublishJobHandler(ApplicationDbContext db, Employer.Application.ISubscriptionService subscriptions) { _db = db; _subscriptions = subscriptions; }

        public async Task<Result> Handle(PublishJobCommand request, CancellationToken cancellationToken)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId && j.TenantId == request.TenantId, cancellationToken);
            if (job == null) return Result.Failure("Job not found");

            var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == job.EmployerId && e.TenantId == request.TenantId, cancellationToken);
            if (employer == null) return Result.Failure("Employer not found");

            var entitlement = await _subscriptions.RequireModuleAsync(employer.Id, "job-posting", cancellationToken);
            if (!entitlement.IsAllowed) return Result.Failure(entitlement.Error ?? "Job posting access is not available.");
            var plan = entitlement.Context!.Plan;

            if (plan.RequiresEmployerVerificationToPublishJobs && !string.Equals(employer.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("Employer must be verified before publishing jobs on this subscription plan");

            var currentPublishedCount = await _db.Jobs.CountAsync(j => j.TenantId == request.TenantId && j.Status == Job.Domain.JobStatus.Published, cancellationToken);
            if (currentPublishedCount >= plan.MaxPublishedJobs)
                return Result.Failure("Subscription plan job publishing limit reached");
            var usage = await _subscriptions.RequireUsageAsync(employer.Id, Employer.Application.SubscriptionMetrics.JobsPublished, plan.MaxPublishedJobs, cancellationToken);
            if (!usage.IsAllowed) return Result.Failure(usage.Error ?? "Subscription publishing limit reached.");

            var verificationGate = await VerificationPolicyEngine.EvaluateAsync(
                _db,
                VerificationSubjectType.Employer,
                VerificationStage.EmployerPublishing,
                "PublishJob",
                employer.Id,
                employer.TenantId,
                facilityType: employer.FacilityType,
                cancellationToken: cancellationToken);

            if (!verificationGate.IsAllowed)
            {
                return Result.Failure(verificationGate.Error ?? "Employer publishing verification policy blocked this action.");
            }

            job.Status = Job.Domain.JobStatus.Published;
            job.PublishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _subscriptions.RecordUsageAsync(entitlement.Context, Employer.Application.SubscriptionMetrics.JobsPublished, 1, cancellationToken);
            return Result.Success();
        }
    }

    public class ListJobsHandler : IRequestHandler<ListJobsCommand, Result<JobListDto>>
    {
        private readonly ApplicationDbContext _db;
        public ListJobsHandler(ApplicationDbContext db) => _db = db;

        public async Task<Result<JobListDto>> Handle(ListJobsCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var query = _db.Jobs.Where(j => j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= now);
            if (request.TenantId.HasValue)
            {
                query = query.Where(j => j.TenantId == request.TenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(j =>
                    j.Title.Contains(search) ||
                    j.Description.Contains(search) ||
                    j.Department.Contains(search) ||
                    j.Location.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim();
                query = query.Where(j => j.RequiredProfessionalCategory == category || (j.RequiredProfessionalCategory != null && j.RequiredProfessionalCategory.Contains(category)));
            }

            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                var department = request.Department.Trim();
                query = query.Where(j => j.Department.Contains(department));
            }

            if (!string.IsNullOrWhiteSpace(request.EngagementType))
            {
                var engagementType = request.EngagementType.Trim();
                query = query.Where(j => j.EngagementType == engagementType || j.EngagementType.Contains(engagementType));
            }

            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                var location = request.Location.Trim();
                query = query.Where(j => j.Location.Contains(location));
            }

            if (request.RequireVerifiedProfessional.HasValue)
            {
                query = query.Where(j => j.RequireVerifiedProfessional == request.RequireVerifiedProfessional.Value);
            }

            if (request.SalaryMin.HasValue)
            {
                query = query.Where(j => j.SalaryMax >= request.SalaryMin.Value);
            }

            if (request.SalaryMax.HasValue)
            {
                query = query.Where(j => j.SalaryMin <= request.SalaryMax.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var jobEntities = await query.OrderByDescending(j => j.PublishedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
            var requirementLookup = await JobMappings.LoadRequirementLookup(_db, jobEntities.Select(j => j.Id).ToList(), cancellationToken);
            var posterLookup = await JobMappings.LoadPosterLookup(_db, jobEntities.Select(j => j.Id).ToList(), cancellationToken);
            var jobs = jobEntities.Select(job => JobMappings.Map(job, requirementLookup.GetValueOrDefault(job.Id), posterLookup.GetValueOrDefault(job.Id))).ToList();
            return Result<JobListDto>.Success(new JobListDto(jobs, totalCount));
        }
    }

    public class GetJobByIdHandler : IRequestHandler<GetJobByIdQuery, Result<JobDto>>
    {
        private readonly ApplicationDbContext _db;
        public GetJobByIdHandler(ApplicationDbContext db) => _db = db;

        public async Task<Result<JobDto>> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId && j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow, cancellationToken);
            if (job == null)
            {
                return Result<JobDto>.Failure("Job not found");
            }

            var requirements = await _db.JobRequiredDocuments
                .Where(item => item.JobId == job.Id)
                .OrderBy(item => item.DocumentType)
                .Select(item => JobMappings.Map(item))
                .ToListAsync(cancellationToken);

            var posters = await _db.JobPosters
                .Where(item => item.JobId == job.Id)
                .OrderBy(item => item.DisplayOrder)
                .Select(item => JobMappings.Map(item))
                .ToListAsync(cancellationToken);

            return Result<JobDto>.Success(JobMappings.Map(job, requirements, posters));
        }
    }

    public class ApplyForJobHandler : IRequestHandler<ApplyForJobCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        public ApplyForJobHandler(ApplicationDbContext db) => _db = db;

        public async Task<Result> Handle(ApplyForJobCommand request, CancellationToken cancellationToken)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId && j.TenantId == request.TenantId && j.Status == Job.Domain.JobStatus.Published && j.ClosesAt >= DateTime.UtcNow, cancellationToken);
            if (job == null) return Result.Failure("Job not found");

            var professional = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (professional == null) return Result.Failure("Professional profile not found");

            if (job.RequireVerifiedProfessional && !string.Equals(professional.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("Professional profile must be verified before applying to this job");

            var allowedCategories = (job.RequiredProfessionalCategory ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var professionalCategories = Professional.Application.Commands.ProfessionalCategoryResolver.SplitCategories(professional.ProfessionalCategory);
            var categoryMatches = allowedCategories.Length == 0 || professionalCategories.Any(category => allowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase));
            if (!categoryMatches)
                return Result.Failure("Professional category does not meet this job's requirements");

            if (job.MinimumYearsOfExperience.HasValue && professional.YearsOfExperience < job.MinimumYearsOfExperience.Value)
                return Result.Failure("Professional does not meet the minimum years of experience requirement");

            var jobSpecificRequirements = await _db.JobRequiredDocuments
                .Where(item => item.JobId == job.Id && item.IsMandatory)
                .ToListAsync(cancellationToken);

            var professionalDocuments = await _db.Documents
                .Where(d => d.ProfessionalId == request.ProfessionalId)
                .Select(d => new
                {
                    DocumentType = d.DocumentTypeName ?? d.Type.ToString(),
                    d.Status
                })
                .ToListAsync(cancellationToken);

            var verificationGate = await VerificationPolicyEngine.EvaluateAsync(
                _db,
                VerificationSubjectType.Professional,
                VerificationStage.JobApplication,
                "ApplyForJob",
                professional.Id,
                professional.TenantId,
                categories: professionalCategories,
                cancellationToken: cancellationToken);

            if (!verificationGate.IsAllowed)
            {
                return Result.Failure(verificationGate.Error ?? "Professional verification policy blocked this application.");
            }

            if (jobSpecificRequirements.Count > 0)
            {
                var missingUploads = jobSpecificRequirements
                    .Where(requirement => !professionalDocuments.Any(document =>
                        string.Equals(document.DocumentType, requirement.DocumentType, StringComparison.OrdinalIgnoreCase)))
                    .Select(requirement => requirement.DocumentType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (missingUploads.Count > 0)
                {
                    return Result.Failure($"Professional is missing required documents for this job: {string.Join(", ", missingUploads)}");
                }

                var platformVerifiedDocuments = jobSpecificRequirements
                    .Where(requirement => string.Equals(requirement.VerificationMode, JobDocumentVerificationMode.PlatformVerification, StringComparison.OrdinalIgnoreCase))
                    .Select(requirement => requirement.DocumentType)
                    .ToList();

                if (platformVerifiedDocuments.Count > 0)
                {
                    var verifiedDocs = professionalDocuments
                        .Where(document => document.Status == Professional.Domain.DocumentStatus.Verified)
                        .Select(document => document.DocumentType)
                        .ToList();
                    var missingVerified = platformVerifiedDocuments
                        .Except(verifiedDocs, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (missingVerified.Count > 0)
                    {
                        return Result.Failure($"Professional must have platform-verified documents before applying: {string.Join(", ", missingVerified)}");
                    }
                }
            }

            var existingApp = await _db.JobApplications.FirstOrDefaultAsync(a => a.JobId == request.JobId && a.ProfessionalId == request.ProfessionalId, cancellationToken);
            if (existingApp != null) return Result.Failure("Already applied for this job");

            var score = professional.YearsOfExperience * 10;
            if (string.Equals(professional.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase)) score += 20;
            if (professionalCategories.Any(category => allowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))) score += 10;

            var application = new Job.Domain.JobApplication
            {
                Id = Guid.NewGuid(),
                JobId = request.JobId,
                ProfessionalId = request.ProfessionalId,
                TenantId = request.TenantId,
                Status = Job.Domain.ApplicationStatus.Submitted,
                AppliedAt = DateTime.UtcNow,
                Score = score
            };

            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class ShortlistCandidateHandler : IRequestHandler<ShortlistCandidateCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;
        public ShortlistCandidateHandler(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }
        public async Task<Result> Handle(ShortlistCandidateCommand request, CancellationToken cancellationToken)
        {
            var app = await _db.JobApplications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.TenantId == request.TenantId, cancellationToken);
            if (app == null) return Result.Failure("Application not found");
            app.Status = Job.Domain.ApplicationStatus.Shortlisted;
            app.IsShortlisted = true;
            await _db.SaveChangesAsync(cancellationToken);
            var professional = await _db.ProfessionalProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.Id == app.ProfessionalId, cancellationToken);
            var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == app.JobId, cancellationToken);
            if (professional != null && job != null)
            {
                await _notifications.NotifyAsync(
                    professional.UserId,
                    "ApplicationShortlisted",
                    "You were shortlisted",
                    $"Your application for {job.Title} has been shortlisted.",
                    $"/applications",
                    "JobApplication",
                    app.Id,
                    cancellationToken);
            }
            return Result.Success();
        }
    }

    internal static class JobMappings
    {
        internal static JobDto Map(Job.Domain.Job job, IEnumerable<JobRequiredDocumentDto>? requiredDocuments = null, IEnumerable<JobPosterDto>? posters = null) => new(
            job.Id,
            job.EmployerId,
            job.TenantId,
            job.Title,
            job.Description,
            job.Department,
            string.IsNullOrWhiteSpace(job.EngagementType) ? "Permanent" : job.EngagementType,
            job.ShiftPattern,
            job.Location,
            job.SalaryMin,
            job.SalaryMax,
            job.RequiredProfessionalCategory,
            job.MinimumYearsOfExperience,
            job.RequireVerifiedProfessional,
            job.AllowInvites,
            job.Status.ToString(),
            GetDisplayStatus(job),
            job.ModerationReason,
            job.ModeratedAt,
            job.PublishedAt,
            job.ClosesAt,
            job.CreatedAt,
            requiredDocuments?.ToList() ?? new List<JobRequiredDocumentDto>(),
            posters?.ToList() ?? new List<JobPosterDto>());

        internal static string GetDisplayStatus(Job.Domain.Job job)
        {
            if (job.Status != Job.Domain.JobStatus.Published)
            {
                return job.Status.ToString();
            }

            var now = DateTime.UtcNow;
            if (job.ClosesAt < now)
            {
                return "Closed";
            }

            return job.ClosesAt <= now.AddDays(3) ? "ClosingSoon" : "Open";
        }

        internal static JobRequiredDocumentDto Map(JobRequiredDocument requirement) => new(
            requirement.Id,
            requirement.DocumentType,
            requirement.IsMandatory,
            requirement.VerificationMode,
            requirement.AllowAdminOverride);

        internal static JobPosterDto Map(JobPoster poster) => new(
            poster.Id,
            poster.FileName,
            poster.ContentType,
            poster.SizeBytes,
            NormalizePosterUrl(poster.PublicUrl),
            poster.DisplayOrder,
            poster.CreatedAt);

        private static string NormalizePosterUrl(string publicUrl)
        {
            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                return publicUrl;
            }

            if (Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) &&
                uri.AbsolutePath.StartsWith("/job-posters/", StringComparison.OrdinalIgnoreCase))
            {
                return uri.AbsolutePath;
            }

            return publicUrl.StartsWith("job-posters/", StringComparison.OrdinalIgnoreCase)
                ? $"/{publicUrl}"
                : publicUrl;
        }

        internal static async Task<Dictionary<Guid, List<JobRequiredDocumentDto>>> LoadRequirementLookup(ApplicationDbContext db, List<Guid> jobIds, CancellationToken cancellationToken)
        {
            if (jobIds.Count == 0)
            {
                return new Dictionary<Guid, List<JobRequiredDocumentDto>>();
            }

            var requirements = await db.JobRequiredDocuments
                .Where(item => jobIds.Contains(item.JobId))
                .OrderBy(item => item.DocumentType)
                .Select(item => new { item.JobId, Requirement = Map(item) })
                .ToListAsync(cancellationToken);

            return requirements
                .GroupBy(item => item.JobId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Requirement).ToList());
        }

        internal static async Task<Dictionary<Guid, List<JobPosterDto>>> LoadPosterLookup(ApplicationDbContext db, List<Guid> jobIds, CancellationToken cancellationToken)
        {
            if (jobIds.Count == 0)
            {
                return new Dictionary<Guid, List<JobPosterDto>>();
            }

            var posters = await db.JobPosters
                .Where(item => jobIds.Contains(item.JobId))
                .OrderBy(item => item.DisplayOrder)
                .Select(item => new { item.JobId, Poster = Map(item) })
                .ToListAsync(cancellationToken);

            return posters
                .GroupBy(item => item.JobId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Poster).ToList());
        }

        internal static string NormalizeVerificationMode(string? verificationMode)
        {
            if (string.Equals(verificationMode, JobDocumentVerificationMode.PlatformVerification, StringComparison.OrdinalIgnoreCase))
            {
                return JobDocumentVerificationMode.PlatformVerification;
            }

            return JobDocumentVerificationMode.EmployerReview;
        }
    }
}

