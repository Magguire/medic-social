using System;
using System.Linq;
using System.Threading.Tasks;
using Communication.Application;
using Communication.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Shared.Data;
using Employer.Application;

namespace Communication.Api.Controllers
{
    [ApiController]
    [Route("api/communications")]
    [Authorize]
    public class CommunicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommunicationService _communicationService;
        private readonly ISubscriptionService _subscriptions;

        public CommunicationsController(ApplicationDbContext db, ICommunicationService communicationService, ISubscriptionService subscriptions)
        {
            _db = db;
            _communicationService = communicationService;
            _subscriptions = subscriptions;
        }

        [HttpPost("send")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Employer,Recruiter")]
        public async Task<IActionResult> Send([FromBody] SendCommunicationRequest request)
        {
            try
            {
                if (User.IsInRole("Employer") || User.IsInRole("Recruiter"))
                {
                    var userId = Guid.TryParse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : Guid.Empty;
                    var employerId = await _db.EmployerTeamMembers.Where(item => item.UserId == userId && item.IsActive).Select(item => item.EmployerId).FirstOrDefaultAsync();
                    if (employerId == Guid.Empty) return BadRequest(new { errors = new[] { "Employer workspace was not found." } });
                    var module = await _subscriptions.RequireModuleAsync(employerId, "communications", HttpContext.RequestAborted);
                    if (!module.IsAllowed) return StatusCode(403, new { errors = new[] { module.Error } });
                    var channelKey = request.Channel switch { CommunicationChannel.Email => "email", CommunicationChannel.Sms => "sms", CommunicationChannel.WhatsApp => "whatsapp", _ => "" };
                    var channel = await _subscriptions.RequireModuleAsync(employerId, channelKey, HttpContext.RequestAborted);
                    if (!channel.IsAllowed) return StatusCode(403, new { errors = new[] { channel.Error } });
                    var messageUsage = await _subscriptions.RequireUsageAsync(employerId, SubscriptionMetrics.MessagesSent, module.Context!.Plan.MaxMessagesPerPeriod, HttpContext.RequestAborted);
                    if (!messageUsage.IsAllowed) return StatusCode(403, new { errors = new[] { messageUsage.Error } });
                    var result = await _communicationService.SendAsync(request);
                    await _subscriptions.RecordUsageAsync(module.Context, SubscriptionMetrics.MessagesSent, 1, HttpContext.RequestAborted);
                    return Ok(result);
                }
                return Ok(await _communicationService.SendAsync(request));
            }
            catch (Exception ex) { return StatusCode(500, new { errors = new[] { ex.Message } }); }
        }

        [HttpGet("messages")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> Messages([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, [FromQuery] CommunicationChannel? channel = null)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.CommunicationMessages.AsQueryable();
            if (channel.HasValue)
            {
                query = query.Where(m => m.Channel == channel.Value);
            }

            var totalCount = await query.CountAsync();
            var rows = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = rows.Select(CommunicationService.Map).ToList();

            return Ok(new { items, totalCount, pageNumber, pageSize });
        }

        [HttpGet("configs")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Configs()
        {
            var rows = await _db.CommunicationProviderConfigs
                .OrderBy(c => c.Channel)
                .ToListAsync();
            var configs = rows.Select(CommunicationService.Map).ToList();
            return Ok(configs);
        }

        [HttpPost("configs")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> UpsertConfig([FromBody] UpsertCommunicationProviderConfigDto dto)
        {
            var config = await _db.CommunicationProviderConfigs.FirstOrDefaultAsync(c => c.Channel == dto.Channel);
            if (config == null)
            {
                config = new CommunicationProviderConfig
                {
                    Id = Guid.NewGuid(),
                    Channel = dto.Channel,
                    CreatedAt = DateTime.UtcNow
                };
                _db.CommunicationProviderConfigs.Add(config);
            }

            config.ProviderName = dto.ProviderName;
            config.IsEnabled = dto.IsEnabled;
            config.BaseUrl = dto.BaseUrl;
            config.SenderId = dto.SenderId;
            if (!string.IsNullOrWhiteSpace(dto.ApiKeySecret))
            {
                config.ApiKeySecret = dto.ApiKeySecret;
            }
            config.AccountSid = dto.AccountSid;
            config.TemplateNamespace = dto.TemplateNamespace;
            config.SimulateWhenDisabled = dto.SimulateWhenDisabled;
            config.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(CommunicationService.Map(config));
        }
    }
}
