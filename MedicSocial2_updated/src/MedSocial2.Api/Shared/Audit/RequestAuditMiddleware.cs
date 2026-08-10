using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Shared.Audit
{
    public class RequestAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/audit", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string? requestBody = null;
            if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            await _next(context);

            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var authenticatedUserId = Guid.TryParse(context.User.FindFirst("UserId")?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;
            if (authenticatedUserId.HasValue)
            {
                var now = DateTime.UtcNow;
                var user = await db.Users.FirstOrDefaultAsync(item => item.Id == authenticatedUserId.Value);
                if (user != null) user.LastActivityAt = now;

                var deviceId = context.Request.Headers["X-MedSocial-Device-Id"].FirstOrDefault();
                var tokenQuery = db.RefreshTokens
                    .Where(item => item.UserId == authenticatedUserId.Value && !item.RevokedAt.HasValue && item.Expiry > now);
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
            db.AuditLog.Add(new AuditLog
            {
                TenantId = Guid.TryParse(context.User.FindFirst("TenantId")?.Value, out var tenantId) ? tenantId : Guid.Empty,
                UserId = authenticatedUserId,
                Action = $"{context.Request.Method} {path}",
                EntityName = "ApiRequest",
                EntityId = path,
                Changes = JsonSerializer.Serialize(new
                {
                    statusCode = context.Response.StatusCode,
                    query = context.Request.QueryString.Value,
                    requestBody = IsSensitivePath(path) ? "[REDACTED]" : requestBody
                }),
                Timestamp = DateTime.UtcNow,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString()
            });
            await db.SaveChangesAsync();
        }

        private static bool IsSensitivePath(string path) =>
            path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("payment-config", StringComparison.OrdinalIgnoreCase);
    }
}
