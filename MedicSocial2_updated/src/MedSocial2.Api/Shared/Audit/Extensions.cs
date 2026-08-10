using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Audit
{
    public static class Extensions
    {
        public static IServiceCollection AddAudit(this IServiceCollection services)
        {
            services.AddScoped<AuditInterceptor>();
            return services;
        }

        public static DbContextOptionsBuilder UseAudit(this DbContextOptionsBuilder options, IServiceProvider provider)
        {
            var interceptor = provider.GetService<AuditInterceptor>();
            if (interceptor != null)
                options.AddInterceptors(interceptor);
            return options;
        }
    }
}