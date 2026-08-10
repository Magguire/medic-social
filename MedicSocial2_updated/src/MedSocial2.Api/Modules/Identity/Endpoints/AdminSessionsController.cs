using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/admin/sessions")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
public class AdminSessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminSessionsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var now = DateTime.UtcNow;
            var activeCutoff = now.AddMinutes(-15);
            var tokens = await _db.RefreshTokens
                .Where(token => !token.RevokedAt.HasValue && token.Expiry > now)
                .Join(_db.Users, token => token.UserId, user => user.Id, (token, user) => new { token, user })
                .ToListAsync();

            var sessions = tokens
                .GroupBy(row => new { row.token.UserId, row.token.DeviceId })
                .Select(group => group.OrderByDescending(row => row.token.LastSeenAt).First())
                .Where(row => row.token.LastSeenAt >= activeCutoff)
                .OrderByDescending(row => row.token.LastSeenAt)
                .Select(row => new
                {
                    sessionId = row.token.Id,
                    userId = row.user.Id,
                    row.user.Email,
                    fullName = $"{row.user.FirstName} {row.user.LastName}".Trim(),
                    role = row.user.UserType.ToString(),
                    row.token.DeviceId,
                    row.token.Ip,
                    row.token.UserAgent,
                    row.token.CreatedAt,
                    row.token.LastSeenAt,
                    row.token.Expiry
                })
                .ToList();
            return Ok(new { items = sessions, activeUsers = sessions.Select(item => item.userId).Distinct().Count(), activeSessions = sessions.Count });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> Detail(Guid sessionId)
    {
        try
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(item => item.Id == sessionId);
            if (token == null) return NotFound(new { errors = new[] { "Session not found." } });
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == token.UserId);
            var activities = await _db.AuditLog
                .Where(item => item.UserId == token.UserId && !item.IsArchived)
                .OrderByDescending(item => item.Timestamp)
                .Take(100)
                .Select(item => new { item.Id, item.Action, item.EntityName, item.EntityId, item.Changes, item.Timestamp, item.IpAddress, item.UserAgent })
                .ToListAsync();
            return Ok(new
            {
                session = new { sessionId = token.Id, token.UserId, user?.Email, fullName = user == null ? "" : $"{user.FirstName} {user.LastName}".Trim(), role = user?.UserType.ToString(), token.DeviceId, token.Ip, token.UserAgent, token.CreatedAt, token.LastSeenAt, user?.LastActivityAt, token.Expiry, token.RevokedAt },
                activities
            });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("{sessionId:guid}/end")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> End(Guid sessionId)
    {
        try
        {
            var selected = await _db.RefreshTokens.FirstOrDefaultAsync(item => item.Id == sessionId);
            if (selected == null) return NotFound(new { errors = new[] { "Session not found." } });
            var now = DateTime.UtcNow;
            var tokens = await _db.RefreshTokens.Where(item => item.UserId == selected.UserId && item.DeviceId == selected.DeviceId && !item.RevokedAt.HasValue).ToListAsync();
            foreach (var token in tokens) { token.RevokedAt = now; token.LastSeenAt = now; }
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == selected.UserId);
            if (user != null) user.SessionsInvalidatedAt = now;
            await _db.SaveChangesAsync();
            return Ok(new { endedTokens = tokens.Count });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }

    [HttpPost("users/{userId:guid}/end")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<IActionResult> EndUser(Guid userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var tokens = await _db.RefreshTokens.Where(item => item.UserId == userId && !item.RevokedAt.HasValue).ToListAsync();
            foreach (var token in tokens) { token.RevokedAt = now; token.LastSeenAt = now; }
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null) return NotFound(new { errors = new[] { "User not found." } });
            user.SessionsInvalidatedAt = now;
            await _db.SaveChangesAsync();
            return Ok(new { endedTokens = tokens.Count });
        }
        catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
    }
}
