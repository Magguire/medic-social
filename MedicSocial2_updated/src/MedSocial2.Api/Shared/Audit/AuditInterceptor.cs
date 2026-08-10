using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shared.Audit
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            AddAuditEntries(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            AddAuditEntries(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AddAuditEntries(DbContext? context)
        {
            if (context == null) return;

            var httpContext = _httpContextAccessor.HttpContext;
            var entries = context.ChangeTracker.Entries().ToList();

            foreach (var entry in entries)
            {
                if (entry.Entity is AuditLog) continue;
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

                var log = new AuditLog
                {
                    TenantId = TryParseGuid(httpContext?.User?.FindFirst("TenantId")?.Value),
                    UserId = TryParseNullableGuid(httpContext?.User?.FindFirst("UserId")?.Value),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty,
                    Action = entry.State.ToString(),
                    Changes = JsonSerializer.Serialize(GetChanges(entry)),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
                };

                context.Add(log);
            }
        }

        private static object GetChanges(EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties)
            {
                if (prop.IsTemporary) continue;
                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[prop.Metadata.Name] = prop.CurrentValue;
                        break;
                    case EntityState.Modified:
                        changes[prop.Metadata.Name] = new { Old = prop.OriginalValue, New = prop.CurrentValue };
                        break;
                    case EntityState.Deleted:
                        changes[prop.Metadata.Name] = prop.OriginalValue;
                        break;
                }
            }
            return changes;
        }

        private static Guid TryParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : Guid.Empty;
        private static Guid? TryParseNullableGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
    }
}
