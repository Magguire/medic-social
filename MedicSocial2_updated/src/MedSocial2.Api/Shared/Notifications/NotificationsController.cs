using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Shared.Notifications;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = CurrentUserId();
            var query = _db.InAppNotifications.AsNoTracking().Where(item => item.UserId == userId);
            if (unreadOnly)
            {
                query = query.Where(item => item.ReadAt == null);
            }

            var notifications = await query
                .OrderByDescending(item => item.CreatedAt)
                .Take(Math.Clamp(pageSize, 1, 100))
                .Select(item => new
                {
                    item.Id,
                    item.Type,
                    item.Title,
                    item.Message,
                    item.ActionUrl,
                    item.EntityType,
                    item.EntityId,
                    item.CreatedAt,
                    item.ReadAt
                })
                .ToListAsync(cancellationToken);
            var unreadCount = await _db.InAppNotifications.CountAsync(item => item.UserId == userId && item.ReadAt == null, cancellationToken);
            return Ok(new { items = notifications, unreadCount });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var notification = await _db.InAppNotifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);
            if (notification == null)
            {
                return NotFound(new { errors = new[] { "Notification not found." } });
            }

            notification.ReadAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { notification.Id, notification.ReadAt });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        try
        {
            var userId = CurrentUserId();
            var unread = await _db.InAppNotifications.Where(item => item.UserId == userId && item.ReadAt == null).ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var item in unread)
            {
                item.ReadAt = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { updated = unread.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
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
}
