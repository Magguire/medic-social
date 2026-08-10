using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Shared.Audit
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AuditController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> Get(
            [FromQuery] int limit = 100,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null,
            [FromQuery] bool archived = false)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize <= 0 ? limit : pageSize, 1, 200);

            var query = _db.AuditLog.Where(item => item.IsArchived == archived);
            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action.Contains(action));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName != null && a.EntityName.Contains(entityName));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.TenantId,
                    a.UserId,
                    a.Action,
                    a.EntityName,
                    a.EntityId,
                    a.Changes,
                    a.Timestamp,
                    a.IpAddress,
                    a.UserAgent
                    ,a.IsArchived
                    ,a.ArchivedAt
                    ,a.ArchivedByUserId
                })
                .ToListAsync();
            return Ok(new { items, totalCount, pageNumber, pageSize });
        }

        [HttpPost("page-view")]
        [Authorize]
        public async Task<IActionResult> PageView([FromBody] PageViewRequest request)
        {
            try
            {
                var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;
                var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var parsedTenantId) ? parsedTenantId : Guid.Empty;
                _db.AuditLog.Add(new AuditLog
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Action = "PAGE_VIEW",
                    EntityName = "ClientPage",
                    EntityId = request.Path,
                    Changes = JsonSerializer.Serialize(new { request.Title, request.Referrer }),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                });
                if (userId.HasValue)
                {
                    await TouchSessionAsync(userId.Value, request.DeviceId);
                }
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        [HttpPost("heartbeat")]
        [Authorize]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
        {
            try
            {
                var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                await TouchSessionAsync(userId.Value, request.DeviceId);
                await _db.SaveChangesAsync();
                return Ok(new { serverTime = DateTime.UtcNow });
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        [HttpPost("{id:long}/archive")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Archive(long id)
        {
            try
            {
                var item = await _db.AuditLog.FirstOrDefaultAsync(log => log.Id == id);
                if (item == null) return NotFound(new { errors = new[] { "Audit record not found." } });
                item.IsArchived = true;
                item.ArchivedAt = DateTime.UtcNow;
                item.ArchivedByUserId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : null;
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        [HttpPost("archive")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> ArchiveMany([FromBody] ArchiveAuditRequest request)
        {
            try
            {
                var userId = Guid.TryParse(User.FindFirst("UserId")?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;
                var items = await _db.AuditLog.Where(item => request.Ids.Contains(item.Id) && !item.IsArchived).ToListAsync();
                foreach (var item in items) { item.IsArchived = true; item.ArchivedAt = DateTime.UtcNow; item.ArchivedByUserId = userId; }
                await _db.SaveChangesAsync();
                return Ok(new { archivedCount = items.Count });
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> Mine([FromQuery] int limit = 30)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var items = await _db.AuditLog
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.TenantId,
                        a.UserId,
                        a.Action,
                        a.EntityName,
                        a.EntityId,
                        a.Changes,
                        a.Timestamp,
                        a.IpAddress,
                        a.UserAgent
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        private async Task TouchSessionAsync(Guid userId, string? deviceId)
        {
            var now = DateTime.UtcNow;
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == userId);
            if (user != null)
            {
                user.LastActivityAt = now;
            }

            var tokenQuery = _db.RefreshTokens
                .Where(item => item.UserId == userId && !item.RevokedAt.HasValue && item.Expiry > now);
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                tokenQuery = tokenQuery.Where(item => item.DeviceId == deviceId);
            }

            var token = await tokenQuery.OrderByDescending(item => item.LastSeenAt).FirstOrDefaultAsync();
            if (token != null)
            {
                token.LastSeenAt = now;
            }
        }
    }

    public record PageViewRequest(string Path, string? Title, string? Referrer, string? DeviceId = null);
    public record HeartbeatRequest(string? DeviceId = null);
    public record ArchiveAuditRequest(List<long> Ids);
}
