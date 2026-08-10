using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Shared.Tenant
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenant)
        {
            // Example: read tenant id from header "X-Tenant-Id"
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var value) && Guid.TryParse(value, out var id))
            {
                tenant.TenantId = id;
            }

            await _next(context);
        }
    }
}