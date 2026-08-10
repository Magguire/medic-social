using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Verification.Application.Commands;
using Verification.Domain;

namespace Verification.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;

        public VerificationController(IMediator mediator, ApplicationDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        [HttpGet("document-types")]
        [AllowAnonymous]
        public async Task<IActionResult> DocumentTypes([FromQuery] DocumentTargetType? targetType)
        {
            var query = _db.DocumentTypes.Where(d => d.IsActive);
            if (targetType.HasValue)
            {
                query = query.Where(d => d.TargetType == targetType.Value);
            }

            var items = await query.OrderBy(d => d.Name)
                .Select(d => new { d.Name, d.Slug, targetType = d.TargetType.ToString(), d.Description, d.AllowedExtensions, d.MaxFileSizeMb })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("required-documents")]
        [AllowAnonymous]
        public async Task<IActionResult> RequiredDocuments([FromQuery] DocumentTargetType targetType, [FromQuery] string? category, [FromQuery] string? facilityType)
        {
            var query = _db.RequiredDocumentRules.Where(rule => rule.TargetType == targetType);
            if (targetType == DocumentTargetType.Professional)
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(rule => rule.AppliesToCategoryOrFacilityType == null || rule.AppliesToCategoryOrFacilityType == category);
                }
                else
                {
                    query = query.Where(rule => rule.AppliesToCategoryOrFacilityType == null);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(facilityType))
                {
                    query = query.Where(rule => rule.AppliesToCategoryOrFacilityType == null || rule.AppliesToCategoryOrFacilityType == facilityType);
                }
                else
                {
                    query = query.Where(rule => rule.AppliesToCategoryOrFacilityType == null);
                }
            }

            var items = await query
                .OrderByDescending(rule => rule.IsMandatory)
                .ThenBy(rule => rule.DocumentType)
                .Select(rule => new
                {
                    rule.Id,
                    rule.DocumentType,
                    rule.IsMandatory,
                    appliesTo = rule.AppliesToCategoryOrFacilityType
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("request")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateVerificationRequestDto dto)
        {
            var result = await _mediator.Send(new CreateVerificationRequestCommand(dto.SubjectType, dto.SubjectId, dto.TenantId, dto.DocumentId, dto.Notes));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> List([FromQuery] Guid? tenantId, [FromQuery] string? status)
        {
            var query = _db.VerificationRequests.AsQueryable();
            if (tenantId.HasValue)
            {
                query = query.Where(v => v.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<VerificationStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(v => v.Status == parsedStatus);
            }

            var items = await query
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    v.Id,
                    subjectType = v.SubjectType.ToString(),
                    v.SubjectId,
                    v.TenantId,
                    v.DocumentId,
                    status = v.Status.ToString(),
                    v.Notes,
                    v.CreatedAt,
                    v.ReviewedAt,
                    v.ReviewedBy
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewDto dto)
        {
            var result = await _mediator.Send(new ApproveVerificationRequestCommand(id, dto.ReviewerId, dto.BypassIntegration));
            return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectDto dto)
        {
            var result = await _mediator.Send(new RejectVerificationRequestCommand(id, dto.ReviewerId, dto.Reason, dto.BypassIntegration));
            return result.IsSuccess ? Ok() : BadRequest(new { errors = result.Errors });
        }

        [HttpGet("tenant/{tenantId:guid}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Auditor")]
        public async Task<IActionResult> ListForTenant(Guid tenantId)
        {
            var result = await _mediator.Send(new GetRequestsForTenantQuery(tenantId));
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
        }
    }

    public record CreateVerificationRequestDto(VerificationSubjectType SubjectType, Guid SubjectId, Guid TenantId, Guid? DocumentId, string? Notes);
    public record ReviewDto(Guid ReviewerId, bool BypassIntegration = false);
    public record RejectDto(Guid ReviewerId, string Reason, bool BypassIntegration = false);
}
