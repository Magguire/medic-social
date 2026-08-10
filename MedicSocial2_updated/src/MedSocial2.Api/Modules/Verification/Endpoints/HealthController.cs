using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection;

namespace Verification.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IServiceProvider sp, IConfiguration config, IHostEnvironment env, ILogger<HealthController> logger)
        {
            _sp = sp;
            _config = config;
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = new Dictionary<string, object>();
            result["status"] = "Healthy";
            result["utc"] = DateTime.UtcNow;
            result["environment"] = _env.EnvironmentName;

            // Show database config (mask sensitive parts)
            var dbProvider = _config["Database:Provider"] ?? "(not configured)";
            var connStr = _config.GetConnectionString(dbProvider) ?? "(none)";
            var maskedConn = connStr;
            try
            {
                if (!string.IsNullOrEmpty(connStr))
                {
                    // naive masking of password tokens
                    maskedConn = connStr.Replace("password=", "password=****").Replace("pwd=", "pwd=****");
                }
            }
            catch { }

            result["database"] = new { provider = dbProvider, connection = maskedConn };

            // Discover registered DbContext types and report connectivity and pending migrations
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();
            var ctxTypes = assemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                .Distinct()
                .ToList();

            var contexts = new List<object>();
            foreach (var t in ctxTypes)
            {
                try
                {
                    var ctx = _sp.GetService(t) as DbContext;
                    if (ctx == null)
                    {
                        contexts.Add(new { type = t.FullName, registered = false });
                        continue;
                    }

                    bool canConnect = false;
                    int pending = -1;
                    try
                    {
                        canConnect = await ctx.Database.CanConnectAsync();
                        var pendingMigs = await ctx.Database.GetPendingMigrationsAsync();
                        pending = pendingMigs.Count();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Health check: error probing context {Context}", t.FullName);
                    }

                    contexts.Add(new { type = t.FullName, registered = true, canConnect, pendingMigrations = pending });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check: failed for type {Type}", t.FullName);
                    contexts.Add(new { type = t.FullName, error = ex.Message });
                }
            }

            result["contexts"] = contexts;

            return Ok(result);
        }
    }
}
