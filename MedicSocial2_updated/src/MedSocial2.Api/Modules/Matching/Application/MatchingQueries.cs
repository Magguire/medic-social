using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Kernel;

namespace Matching.Application;

public sealed record FindMatchingCandidatesQuery(Guid TenantId, Guid JobId) : IRequest<Result<MatchCandidateListDto>>;
public sealed record InviteProfessionalCommand(Guid TenantId, Guid JobId, Guid ProfessionalId, string? Message) : IRequest<Result<MatchInvitationDto>>;
public sealed record GetInvitationsForJobQuery(Guid TenantId, Guid JobId) : IRequest<Result<List<MatchInvitationDto>>>;

public sealed class FindMatchingCandidatesHandler : IRequestHandler<FindMatchingCandidatesQuery, Result<MatchCandidateListDto>>
{
    private readonly ApplicationDbContext _db;
    private readonly Employer.Application.ISubscriptionService _subscriptions;
    public FindMatchingCandidatesHandler(ApplicationDbContext db, Employer.Application.ISubscriptionService subscriptions) { _db = db; _subscriptions = subscriptions; }

    public async Task<Result<MatchCandidateListDto>> Handle(FindMatchingCandidatesQuery request, CancellationToken cancellationToken)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId && j.TenantId == request.TenantId, cancellationToken);
        if (job == null)
            return Result<MatchCandidateListDto>.Failure("Job not found");

        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == job.EmployerId && e.TenantId == request.TenantId, cancellationToken);
        if (employer == null)
            return Result<MatchCandidateListDto>.Failure("Employer not found");

        var entitlement = await _subscriptions.RequireModuleAsync(employer.Id, "talent-search", cancellationToken);
        if (!entitlement.IsAllowed) return Result<MatchCandidateListDto>.Failure(entitlement.Error ?? "Talent search is not available.");
        var visibility = await _subscriptions.RequireModuleAsync(employer.Id, "professional-profiles", cancellationToken);
        if (!visibility.IsAllowed) return Result<MatchCandidateListDto>.Failure(visibility.Error ?? "Professional profiles are not available.");
        var plan = entitlement.Context!.Plan;

        var candidates = await _db.ProfessionalProfiles
            .OrderByDescending(p => p.VerificationStatus == "Verified")
            .ThenByDescending(p => p.YearsOfExperience)
            .Take(50)
            .ToListAsync(cancellationToken);

        var mapped = candidates.Select(p =>
        {
            var reasons = new List<string>();
            decimal score = p.YearsOfExperience * 10;
            var meets = true;
            var professionalCategories = Professional.Application.Commands.ProfessionalCategoryResolver.SplitCategories(p.ProfessionalCategory);
            var requiredCategories = (job.RequiredProfessionalCategory ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (string.Equals(p.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
                reasons.Add("Verified professional profile");
            }
            else if (job.RequireVerifiedProfessional)
            {
                meets = false;
                reasons.Add("Verification required for this job");
            }

            if (requiredCategories.Length > 0 && professionalCategories.Any(category => requiredCategories.Contains(category, StringComparer.OrdinalIgnoreCase)))
            {
                score += 15;
                reasons.Add("Matches required professional category");
            }
            else if (requiredCategories.Length > 0)
            {
                meets = false;
                reasons.Add("Category does not match job requirement");
            }

            if (job.MinimumYearsOfExperience.HasValue)
            {
                if (p.YearsOfExperience >= job.MinimumYearsOfExperience.Value)
                {
                    score += 10;
                    reasons.Add("Meets experience threshold");
                }
                else
                {
                    meets = false;
                    reasons.Add("Below minimum years of experience");
                }
            }

            if (reasons.Count == 0)
                reasons.Add("General profile match");

            return new MatchCandidateDto(p.Id, request.JobId, score, meets, reasons.ToArray(), p.ProfessionalCategory, p.YearsOfExperience, plan.CanViewProfessionalVerificationStatus ? p.VerificationStatus : "Restricted");
        })
        .OrderByDescending(c => c.MeetsRequirements)
        .ThenByDescending(c => c.Score)
        .ToList();

        return Result<MatchCandidateListDto>.Success(new MatchCandidateListDto(mapped, mapped.Count));
    }
}

public sealed class InviteProfessionalHandler : IRequestHandler<InviteProfessionalCommand, Result<MatchInvitationDto>>
{
    private readonly ApplicationDbContext _db;
    private readonly Employer.Application.ISubscriptionService _subscriptions;
    public InviteProfessionalHandler(ApplicationDbContext db, Employer.Application.ISubscriptionService subscriptions) { _db = db; _subscriptions = subscriptions; }

    public async Task<Result<MatchInvitationDto>> Handle(InviteProfessionalCommand request, CancellationToken cancellationToken)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId && j.TenantId == request.TenantId && j.AllowInvites, cancellationToken);
        if (job == null)
            return Result<MatchInvitationDto>.Failure("Job not found or does not allow invites");

        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == job.EmployerId && e.TenantId == request.TenantId, cancellationToken);
        if (employer == null)
            return Result<MatchInvitationDto>.Failure("Employer not found");

        var entitlement = await _subscriptions.RequireModuleAsync(employer.Id, "candidate-invites", cancellationToken);
        if (!entitlement.IsAllowed) return Result<MatchInvitationDto>.Failure(entitlement.Error ?? "Candidate invitations are not available.");
        var usage = await _subscriptions.RequireUsageAsync(employer.Id, Employer.Application.SubscriptionMetrics.CandidateInvites, entitlement.Context!.Plan.MaxCandidateInvitesPerPeriod, cancellationToken);
        if (!usage.IsAllowed) return Result<MatchInvitationDto>.Failure(usage.Error ?? "Candidate invitation limit reached.");

        var professional = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken);
        if (professional == null)
            return Result<MatchInvitationDto>.Failure("Professional not found");

        var existing = await _db.MatchInvitations.FirstOrDefaultAsync(i => i.JobId == request.JobId && i.ProfessionalId == request.ProfessionalId, cancellationToken);
        if (existing != null)
            return Result<MatchInvitationDto>.Failure("Professional has already been invited for this job");

        var invitation = new Matching.Domain.MatchInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            JobId = request.JobId,
            ProfessionalId = request.ProfessionalId,
            Status = "Sent",
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _db.MatchInvitations.Add(invitation);
        await _db.SaveChangesAsync(cancellationToken);
        await _subscriptions.RecordUsageAsync(entitlement.Context, Employer.Application.SubscriptionMetrics.CandidateInvites, 1, cancellationToken);
        return Result<MatchInvitationDto>.Success(MatchingMappings.Map(invitation));
    }
}

public sealed class GetInvitationsForJobHandler : IRequestHandler<GetInvitationsForJobQuery, Result<List<MatchInvitationDto>>>
{
    private readonly ApplicationDbContext _db;
    public GetInvitationsForJobHandler(ApplicationDbContext db) => _db = db;
    public async Task<Result<List<MatchInvitationDto>>> Handle(GetInvitationsForJobQuery request, CancellationToken cancellationToken)
    {
        var entities = await _db.MatchInvitations.Where(i => i.TenantId == request.TenantId && i.JobId == request.JobId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        var items = entities.Select(MatchingMappings.Map).ToList();
        return Result<List<MatchInvitationDto>>.Success(items);
    }
}

internal static class MatchingMappings
{
    internal static MatchInvitationDto Map(Matching.Domain.MatchInvitation invitation) => new(invitation.Id, invitation.TenantId, invitation.JobId, invitation.ProfessionalId, invitation.Status, invitation.Message, invitation.CreatedAt);
}
