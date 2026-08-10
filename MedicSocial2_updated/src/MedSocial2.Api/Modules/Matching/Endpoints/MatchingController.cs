using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Employer.Application;
using Matching.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Matching.Endpoints
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;
        private readonly IEmployerAccessService _access;

        public MatchingController(IMediator mediator, ApplicationDbContext db, IEmployerAccessService access)
        {
            _mediator = mediator;
            _db = db;
            _access = access;
        }

        [HttpGet("jobs/{jobId:guid}/candidates")]
        public async Task<IActionResult> FindCandidates(Guid jobId, [FromQuery] Guid tenantId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.InviteProfessionals, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new FindMatchingCandidatesQuery(tenantId, jobId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("jobs/{jobId:guid}/invite")]
        public async Task<IActionResult> Invite(Guid jobId, [FromBody] InviteCandidateDto dto)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == dto.TenantId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.InviteProfessionals, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new InviteProfessionalCommand(dto.TenantId, jobId, dto.ProfessionalId, dto.Message));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("jobs/{jobId:guid}/invites")]
        public async Task<IActionResult> Invites(Guid jobId, [FromQuery] Guid tenantId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("TenantAdmin"))
            {
                var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);
                if (job == null) return NotFound(new { errors = new[] { "Job not found." } });
                var access = await _access.RequireAsync(CurrentUserId(), job.EmployerId, EmployerPermissions.InviteProfessionals, HttpContext.RequestAborted);
                if (!access.IsAllowed) return Forbid();
            }

            var result = await _mediator.Send(new GetInvitationsForJobQuery(tenantId, jobId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
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

    public record InviteCandidateDto(Guid TenantId, Guid ProfessionalId, string? Message);
}
