using Job.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Notifications;

namespace Job.Application;

public class JobLifecycleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobLifecycleService> _logger;

    public JobLifecycleService(IServiceScopeFactory scopeFactory, ILogger<JobLifecycleService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to close expired jobs.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task CloseExpiredJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;
        var expiredJobs = await db.Jobs
            .Where(job => job.Status == JobStatus.Published && job.ClosesAt < now)
            .ToListAsync(cancellationToken);
        if (expiredJobs.Count == 0)
        {
            return;
        }

        foreach (var job in expiredJobs)
        {
            job.Status = JobStatus.Closed;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var job in expiredJobs)
        {
            await notifications.NotifyJobWatchersAsync(
                job.Id,
                "A watched job has closed",
                $"{job.Title} has reached its closing date.",
                cancellationToken);
        }
    }
}
