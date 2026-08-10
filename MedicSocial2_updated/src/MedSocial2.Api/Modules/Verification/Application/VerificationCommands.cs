using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Data;
using Shared.Kernel;
using Verification.Domain;

namespace Verification.Application.Commands
{
    public record CreateVerificationRequestCommand(VerificationSubjectType SubjectType, Guid SubjectId, Guid TenantId, Guid? DocumentId, string? Notes) : IRequest<Result<VerificationRequestDto>>;
    public record ApproveVerificationRequestCommand(Guid RequestId, Guid ReviewedBy, bool BypassIntegration) : IRequest<Result>;
    public record RejectVerificationRequestCommand(Guid RequestId, Guid ReviewedBy, string Reason, bool BypassIntegration) : IRequest<Result>;
    public record GetRequestsForTenantQuery(Guid TenantId) : IRequest<Result<VerificationRequestListDto>>;

    public record VerificationRequestDto(Guid Id, VerificationSubjectType SubjectType, Guid SubjectId, Guid TenantId, Guid? DocumentId, string? Notes, VerificationStatus Status, Guid? ReviewedBy, DateTime CreatedAt, DateTime? ReviewedAt);
    public record VerificationRequestListDto(List<VerificationRequestDto> Items, int TotalCount);

    public class CreateVerificationRequestHandler : IRequestHandler<CreateVerificationRequestCommand, Result<VerificationRequestDto>>
    {
        private readonly ApplicationDbContext _db;
        public CreateVerificationRequestHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<VerificationRequestDto>> Handle(CreateVerificationRequestCommand request, CancellationToken cancellationToken)
        {
            var entity = new VerificationRequest
            {
                Id = Guid.NewGuid(),
                SubjectType = request.SubjectType,
                SubjectId = request.SubjectId,
                ProfessionalId = request.SubjectType == VerificationSubjectType.Professional ? request.SubjectId : null,
                EmployerId = request.SubjectType == VerificationSubjectType.Employer ? request.SubjectId : null,
                TenantId = request.TenantId,
                DocumentId = request.DocumentId,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            _db.VerificationRequests.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<VerificationRequestDto>.Success(VerificationMappings.Map(entity));
        }
    }

    public class ApproveVerificationRequestHandler : IRequestHandler<ApproveVerificationRequestCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        private readonly VerificationIntegrationOptions _integrationOptions;
        public ApproveVerificationRequestHandler(ApplicationDbContext db, IOptions<VerificationIntegrationOptions> integrationOptions)
        {
            _db = db;
            _integrationOptions = integrationOptions.Value;
        }
        public async Task<Result> Handle(ApproveVerificationRequestCommand request, CancellationToken cancellationToken)
        {
            if (!_integrationOptions.Enabled && !request.BypassIntegration)
                return Result.Failure("Verification integration is not configured. Retry using admin bypass.");

            var entity = await _db.VerificationRequests.FirstOrDefaultAsync(v => v.Id == request.RequestId, cancellationToken);
            if (entity == null) return Result.Failure("Request not found");

            entity.Status = VerificationStatus.Approved;
            entity.ReviewedBy = request.ReviewedBy;
            entity.ReviewedAt = DateTime.UtcNow;
            if (!_integrationOptions.Enabled && request.BypassIntegration)
                entity.Notes = string.IsNullOrWhiteSpace(entity.Notes) ? "Approved via admin bypass" : entity.Notes + " | Approved via admin bypass";

            if (entity.SubjectType == VerificationSubjectType.Professional)
            {
                var prof = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == entity.SubjectId, cancellationToken);
                if (prof != null)
                {
                    prof.VerificationStatus = "Verified";
                    prof.VerifiedAt = DateTime.UtcNow;
                }
                if (entity.DocumentId.HasValue)
                {
                    var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == entity.DocumentId.Value, cancellationToken);
                    if (doc != null)
                    {
                        doc.Status = Professional.Domain.DocumentStatus.Verified;
                        doc.VerifiedAt = DateTime.UtcNow;
                        doc.VerificationNotes = !_integrationOptions.Enabled && request.BypassIntegration ? "Approved via admin bypass" : "Approved";
                    }
                }
            }
            else
            {
                var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == entity.SubjectId, cancellationToken);
                if (employer != null)
                {
                    employer.VerificationStatus = "Verified";
                    employer.UpdatedAt = DateTime.UtcNow;
                }
                if (entity.DocumentId.HasValue)
                {
                    var doc = await _db.EmployerDocuments.FirstOrDefaultAsync(d => d.Id == entity.DocumentId.Value, cancellationToken);
                    if (doc != null)
                    {
                        doc.Status = "Verified";
                        doc.VerifiedAt = DateTime.UtcNow;
                        doc.VerificationNotes = !_integrationOptions.Enabled && request.BypassIntegration ? "Approved via admin bypass" : "Approved";
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class RejectVerificationRequestHandler : IRequestHandler<RejectVerificationRequestCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        private readonly VerificationIntegrationOptions _integrationOptions;
        public RejectVerificationRequestHandler(ApplicationDbContext db, IOptions<VerificationIntegrationOptions> integrationOptions)
        {
            _db = db;
            _integrationOptions = integrationOptions.Value;
        }
        public async Task<Result> Handle(RejectVerificationRequestCommand request, CancellationToken cancellationToken)
        {
            if (!_integrationOptions.Enabled && !request.BypassIntegration)
                return Result.Failure("Verification integration is not configured. Retry using admin bypass.");

            var entity = await _db.VerificationRequests.FirstOrDefaultAsync(v => v.Id == request.RequestId, cancellationToken);
            if (entity == null) return Result.Failure("Request not found");

            entity.Status = VerificationStatus.Rejected;
            entity.ReviewedBy = request.ReviewedBy;
            entity.Notes = !_integrationOptions.Enabled && request.BypassIntegration ? request.Reason + " | Rejected via admin bypass" : request.Reason;
            entity.ReviewedAt = DateTime.UtcNow;

            if (entity.SubjectType == VerificationSubjectType.Professional)
            {
                var prof = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == entity.SubjectId, cancellationToken);
                if (prof != null)
                {
                    prof.VerificationStatus = "Rejected";
                    prof.RejectionReason = request.Reason;
                    prof.VerifiedAt = DateTime.UtcNow;
                }
                if (entity.DocumentId.HasValue)
                {
                    var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == entity.DocumentId.Value, cancellationToken);
                    if (doc != null)
                    {
                        doc.Status = Professional.Domain.DocumentStatus.Rejected;
                        doc.VerifiedAt = DateTime.UtcNow;
                        doc.VerificationNotes = entity.Notes;
                    }
                }
            }
            else
            {
                var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == entity.SubjectId, cancellationToken);
                if (employer != null)
                {
                    employer.VerificationStatus = "Rejected";
                    employer.UpdatedAt = DateTime.UtcNow;
                }
                if (entity.DocumentId.HasValue)
                {
                    var doc = await _db.EmployerDocuments.FirstOrDefaultAsync(d => d.Id == entity.DocumentId.Value, cancellationToken);
                    if (doc != null)
                    {
                        doc.Status = "Rejected";
                        doc.VerifiedAt = DateTime.UtcNow;
                        doc.VerificationNotes = entity.Notes;
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class GetRequestsForTenantHandler : IRequestHandler<GetRequestsForTenantQuery, Result<VerificationRequestListDto>>
    {
        private readonly ApplicationDbContext _db;
        public GetRequestsForTenantHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<VerificationRequestListDto>> Handle(GetRequestsForTenantQuery request, CancellationToken cancellationToken)
        {
            var entities = await _db.VerificationRequests.Where(v => v.TenantId == request.TenantId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);
            var items = entities.Select(VerificationMappings.Map).ToList();
            return Result<VerificationRequestListDto>.Success(new VerificationRequestListDto(items, items.Count));
        }
    }

    internal static class VerificationMappings
    {
        internal static VerificationRequestDto Map(VerificationRequest entity) => new(entity.Id, entity.SubjectType, entity.SubjectId, entity.TenantId, entity.DocumentId, entity.Notes, entity.Status, entity.ReviewedBy, entity.CreatedAt, entity.ReviewedAt);
    }
}
