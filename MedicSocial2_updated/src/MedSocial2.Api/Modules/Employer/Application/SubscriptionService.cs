using Employer.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Employer.Application;

public static class SubscriptionMetrics
{
    public const string JobsCreated = "jobs-created";
    public const string JobsPublished = "jobs-published";
    public const string CandidateInvites = "candidate-invites";
    public const string MessagesSent = "messages-sent";
}

public record SubscriptionContext(EmployerProfile Employer, SubscriptionPlan Plan, EmployerSubscription? Subscription, bool IsLegacyFallback);
public record EntitlementResult(bool IsAllowed, SubscriptionContext? Context, string? Error, int? Limit = null, int? Used = null);

public interface ISubscriptionService
{
    Task<SubscriptionContext?> GetCurrentAsync(Guid employerId, CancellationToken cancellationToken);
    Task<EntitlementResult> RequireModuleAsync(Guid employerId, string module, CancellationToken cancellationToken);
    Task<EntitlementResult> RequireUsageAsync(Guid employerId, string metricKey, int limit, CancellationToken cancellationToken);
    Task RecordUsageAsync(SubscriptionContext context, string metricKey, int quantity, CancellationToken cancellationToken);
    Task<EmployerSubscription> ActivateAsync(Guid employerId, Guid planId, string source, Guid? paymentTransactionId, Guid? approvedByUserId, int? durationDays, string? notes, CancellationToken cancellationToken);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionService(ApplicationDbContext db) => _db = db;

    public async Task<SubscriptionContext?> GetCurrentAsync(Guid employerId, CancellationToken cancellationToken)
    {
        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(item => item.Id == employerId, cancellationToken);
        if (employer == null) return null;

        var now = DateTime.UtcNow;
        var subscription = await _db.EmployerSubscriptions
            .Where(item => item.EmployerId == employerId && item.Status == EmployerSubscriptionStatus.Active && item.StartsAt <= now && item.EndsAt > now)
            .OrderByDescending(item => item.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

        SubscriptionPlan? plan;
        if (subscription != null)
        {
            plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == subscription.PlanId, cancellationToken);
        }
        else
        {
            plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(item => item.Slug == employer.SubscriptionTier, cancellationToken)
                ?? await _db.SubscriptionPlans.FirstOrDefaultAsync(item => item.IsDefault, cancellationToken);
        }

        return plan == null ? null : new SubscriptionContext(employer, plan, subscription, subscription == null);
    }

    public async Task<EntitlementResult> RequireModuleAsync(Guid employerId, string module, CancellationToken cancellationToken)
    {
        var context = await GetCurrentAsync(employerId, cancellationToken);
        if (context == null) return new(false, null, "No subscription plan is configured for this employer.");

        var allowed = module switch
        {
            "job-posting" => context.Plan.CanAccessJobPostingModule,
            "applicant-review" => context.Plan.CanAccessApplicantReviewModule,
            "talent-search" => context.Plan.CanAccessTalentSearchModule,
            "reports" => context.Plan.CanAccessReportsModule,
            "communications" => context.Plan.CanAccessCommunicationsModule,
            "professional-profiles" => context.Plan.CanViewProfessionalProfiles,
            "professional-contacts" => context.Plan.CanViewProfessionalContactDetails,
            "professional-documents" => context.Plan.CanViewProfessionalDocuments,
            "professional-verification" => context.Plan.CanViewProfessionalVerificationStatus,
            "candidate-invites" => context.Plan.CanInviteCandidates,
            "candidate-messages" => context.Plan.CanMessageCandidates,
            "email" => context.Plan.CanUseEmailCommunications,
            "sms" => context.Plan.CanUseSmsCommunications,
            "whatsapp" => context.Plan.CanUseWhatsAppCommunications,
            _ => false
        };

        return allowed
            ? new(true, context, null)
            : new(false, context, $"{context.Plan.Name} does not include {module.Replace('-', ' ')} access. Upgrade the subscription to continue.");
    }

    public async Task<EntitlementResult> RequireUsageAsync(Guid employerId, string metricKey, int limit, CancellationToken cancellationToken)
    {
        var context = await GetCurrentAsync(employerId, cancellationToken);
        if (context == null) return new(false, null, "No subscription plan is configured for this employer.");
        if (limit < 0) return new(true, context, null, limit, 0);

        var (startsAt, endsAt) = Period(context);
        var used = await _db.SubscriptionUsages
            .Where(item => item.EmployerId == context.Employer.Id &&
                           item.EmployerSubscriptionId == (context.Subscription == null ? Guid.Empty : context.Subscription.Id) &&
                           item.MetricKey == metricKey && item.PeriodStartsAt == startsAt)
            .Select(item => item.Quantity)
            .FirstOrDefaultAsync(cancellationToken);

        return used < limit
            ? new(true, context, null, limit, used)
            : new(false, context, $"{context.Plan.Name} has reached its {metricKey.Replace('-', ' ')} limit of {limit} for this billing period.", limit, used);
    }

    public async Task RecordUsageAsync(SubscriptionContext context, string metricKey, int quantity, CancellationToken cancellationToken)
    {
        var (startsAt, endsAt) = Period(context);
        var subscriptionId = context.Subscription?.Id ?? Guid.Empty;
        var usage = await _db.SubscriptionUsages.FirstOrDefaultAsync(item =>
            item.EmployerId == context.Employer.Id && item.EmployerSubscriptionId == subscriptionId && item.MetricKey == metricKey && item.PeriodStartsAt == startsAt, cancellationToken);
        if (usage == null)
        {
            usage = new SubscriptionUsage
            {
                Id = Guid.NewGuid(),
                EmployerSubscriptionId = subscriptionId,
                EmployerId = context.Employer.Id,
                TenantId = context.Employer.TenantId,
                MetricKey = metricKey,
                PeriodStartsAt = startsAt,
                PeriodEndsAt = endsAt
            };
            _db.SubscriptionUsages.Add(usage);
        }
        usage.Quantity += quantity;
        usage.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployerSubscription> ActivateAsync(Guid employerId, Guid planId, string source, Guid? paymentTransactionId, Guid? approvedByUserId, int? durationDays, string? notes, CancellationToken cancellationToken)
    {
        var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(item => item.Id == employerId, cancellationToken)
            ?? throw new InvalidOperationException("Employer not found.");
        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == planId, cancellationToken)
            ?? throw new InvalidOperationException("Subscription plan not found.");

        var now = DateTime.UtcNow;
        var current = await _db.EmployerSubscriptions.Where(item => item.EmployerId == employerId && item.Status == EmployerSubscriptionStatus.Active).ToListAsync(cancellationToken);
        foreach (var item in current)
        {
            item.Status = EmployerSubscriptionStatus.Cancelled;
            item.UpdatedAt = now;
        }

        var subscription = new EmployerSubscription
        {
            Id = Guid.NewGuid(),
            EmployerId = employerId,
            TenantId = employer.TenantId,
            PlanId = planId,
            Status = EmployerSubscriptionStatus.Active,
            StartsAt = now,
            EndsAt = now.AddDays(durationDays ?? DurationDays(plan.BillingInterval)),
            ProvisioningSource = source,
            PaymentTransactionId = paymentTransactionId,
            ApprovedByUserId = approvedByUserId,
            Notes = notes,
            CreatedAt = now
        };
        employer.SubscriptionTier = plan.Slug;
        var employerUsers = await _db.Users.Where(user => user.TenantId == employer.TenantId && user.UserType == Identity.Domain.UserType.Employer).ToListAsync(cancellationToken);
        foreach (var user in employerUsers) user.SubscriptionTier = plan.Slug;
        _db.EmployerSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    private static (DateTime StartsAt, DateTime EndsAt) Period(SubscriptionContext context) =>
        context.Subscription == null
            ? (new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1))
            : (context.Subscription.StartsAt, context.Subscription.EndsAt);

    private static int DurationDays(string interval) => interval.ToLowerInvariant() switch
    {
        "quarterly" => 90,
        "biannual" => 182,
        "annual" => 365,
        "onetime" => 36500,
        _ => 30
    };
}
