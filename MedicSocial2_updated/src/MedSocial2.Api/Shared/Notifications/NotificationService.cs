using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Shared.Notifications;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string type, string title, string message, string? actionUrl, string? entityType, Guid? entityId, CancellationToken cancellationToken);
    Task NotifyJobWatchersAsync(Guid jobId, string title, string message, CancellationToken cancellationToken);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(Guid userId, string type, string title, string message, string? actionUrl, string? entityType, Guid? entityId, CancellationToken cancellationToken)
    {
        _db.InAppNotifications.Add(new InAppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            EntityType = entityType,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyJobWatchersAsync(Guid jobId, string title, string message, CancellationToken cancellationToken)
    {
        var watchers = await _db.JobWatches.Where(watch => watch.JobId == jobId).ToListAsync(cancellationToken);
        if (watchers.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var watcher in watchers)
        {
            watcher.LastNotifiedAt = now;
            _db.InAppNotifications.Add(new InAppNotification
            {
                Id = Guid.NewGuid(),
                UserId = watcher.UserId,
                Type = "JobWatch",
                Title = title,
                Message = message,
                ActionUrl = $"/jobs/{jobId}",
                EntityType = "Job",
                EntityId = jobId,
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
