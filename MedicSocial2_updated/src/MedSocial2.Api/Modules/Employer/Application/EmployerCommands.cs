#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Employer.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Kernel;
using Identity.Domain;
using Shared.Tenant;
using Verification.Application;
using Verification.Domain;

namespace Employer.Application.Commands
{
    public record RegisterEmployerCommand(
        string Name,
        string FacilityType,
        string ContactEmail,
        string? ContactPhone,
        bool IsContactPhonePublic,
        string? Address,
        string? BusinessRegistrationNumber,
        string? KraPin,
        string? LicenseNumber) : IRequest<Result<EmployerDto>>;

    public record UpdateEmployerCommand(
        Guid EmployerId,
        string? Name,
        string? FacilityType,
        string? ContactEmail,
        string? ContactPhone,
        bool? IsContactPhonePublic,
        string? Address,
        string? BusinessRegistrationNumber,
        string? KraPin,
        string? LicenseNumber,
        string? SubscriptionTier,
        string? VerificationStatus) : IRequest<Result<EmployerDto>>;

    public record UploadEmployerDocumentCommand(Guid EmployerId, string DocumentType, byte[] FileData, string FileName) : IRequest<Result>;
    public record ListEmployersQuery(Guid TenantId) : IRequest<Result<EmployerListDto>>;
    public record GetEmployerDocumentsQuery(Guid EmployerId) : IRequest<Result<List<EmployerDocumentDto>>>;

    public record EmployerDto(
        Guid Id,
        Guid TenantId,
        string Name,
        string OrganizationSlug,
        string FacilityType,
        string ContactEmail,
        string? ContactPhone,
        bool IsContactPhonePublic,
        string? Address,
        string SubscriptionTier,
        string VerificationStatus,
        string? BusinessRegistrationNumber,
        string? KraPin,
        string? LicenseNumber);

    public record EmployerDocumentDto(Guid Id, Guid EmployerId, string DocumentType, string FileName, string Status, string? VerificationNotes, DateTime CreatedAt);
    public record EmployerListDto(List<EmployerDto> Items, int TotalCount);

    public class RegisterEmployerHandler : IRequestHandler<RegisterEmployerCommand, Result<EmployerDto>>
    {
        private readonly ApplicationDbContext _db;
        public RegisterEmployerHandler(ApplicationDbContext db) => _db = db;

        public async Task<Result<EmployerDto>> Handle(RegisterEmployerCommand request, CancellationToken cancellationToken)
        {
            var normalizedName = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedName))
                return Result<EmployerDto>.Failure("Employer name is required");

            var slugBase = new string(normalizedName.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "employer";
            slugBase = slugBase.Length > 24 ? slugBase[..24] : slugBase;
            var slug = $"{slugBase}-{Guid.NewGuid():N}"[..Math.Min(slugBase.Length + 9, 32)];

            var defaultPlan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.IsDefault, cancellationToken)
                ?? new SubscriptionPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Free",
                    Slug = "free",
                    MaxPublishedJobs = 1,
                    CanInviteCandidates = false,
                    CanMessageCandidates = false,
                    RequiresEmployerVerificationToPublishJobs = true,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                };

            if (defaultPlan.Id == Guid.Empty || !await _db.SubscriptionPlans.AnyAsync(p => p.Id == defaultPlan.Id, cancellationToken))
            {
                _db.SubscriptionPlans.Add(defaultPlan);
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == PlatformTenant.Id || t.Slug == PlatformTenant.Slug, cancellationToken);
            if (tenant == null)
            {
                tenant = new Tenant
                {
                    Id = PlatformTenant.Id,
                    Name = PlatformTenant.Name,
                    Slug = PlatformTenant.Slug,
                    SubscriptionTier = defaultPlan.Slug,
                    Status = TenantStatus.Active,
                    RegionCode = "KE",
                    MaxEmployers = 200,
                    MaxProfessionals = 500,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Tenants.Add(tenant);
            }

            var profile = new EmployerProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = normalizedName,
                OrganizationSlug = slug,
                FacilityType = string.IsNullOrWhiteSpace(request.FacilityType) ? "Healthcare Facility" : request.FacilityType,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                IsContactPhonePublic = request.IsContactPhonePublic,
                Address = request.Address,
                BusinessRegistrationNumber = request.BusinessRegistrationNumber,
                KraPin = request.KraPin,
                LicenseNumber = request.LicenseNumber,
                SubscriptionTier = defaultPlan.Slug,
                VerificationStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.EmployerProfiles.Add(profile);
            var ownerUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == profile.ContactEmail, cancellationToken);
            if (ownerUser != null)
            {
                _db.EmployerTeamMembers.Add(new EmployerTeamMember
                {
                    Id = Guid.NewGuid(),
                    EmployerId = profile.Id,
                    TenantId = profile.TenantId,
                    UserId = ownerUser.Id,
                    RoleName = "Owner",
                    CanManageProfile = true,
                    CanManageSettings = true,
                    CanCreateJobs = true,
                    CanPublishJobs = true,
                    CanViewApplications = true,
                    CanVerifyApplications = true,
                    CanInviteProfessionals = true,
                    CanMessageProfessionals = true,
                    CanManageTeam = true,
                    IsOwner = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await VerificationPolicyEngine.TriggerStagePoliciesAsync(
                _db,
                VerificationSubjectType.Employer,
                VerificationStage.Registration,
                "RegisterEmployer",
                profile.Id,
                profile.TenantId,
                cancellationToken);
            return Result<EmployerDto>.Success(EmployerMappings.Map(profile));
        }
    }

    public class UpdateEmployerHandler : IRequestHandler<UpdateEmployerCommand, Result<EmployerDto>>
    {
        private readonly ApplicationDbContext _db;
        public UpdateEmployerHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<EmployerDto>> Handle(UpdateEmployerCommand request, CancellationToken cancellationToken)
        {
            var emp = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == request.EmployerId, cancellationToken);
            if (emp == null) return Result<EmployerDto>.Failure("Employer not found");
            if (request.Name != null) emp.Name = request.Name;
            if (request.FacilityType != null) emp.FacilityType = request.FacilityType;
            if (request.ContactEmail != null) emp.ContactEmail = request.ContactEmail;
            if (request.ContactPhone != null) emp.ContactPhone = request.ContactPhone;
            if (request.IsContactPhonePublic.HasValue) emp.IsContactPhonePublic = request.IsContactPhonePublic.Value;
            if (request.Address != null) emp.Address = request.Address;
            if (request.BusinessRegistrationNumber != null) emp.BusinessRegistrationNumber = request.BusinessRegistrationNumber;
            if (request.KraPin != null) emp.KraPin = request.KraPin;
            if (request.LicenseNumber != null) emp.LicenseNumber = request.LicenseNumber;
            if (request.SubscriptionTier != null) emp.SubscriptionTier = request.SubscriptionTier;
            if (request.VerificationStatus != null) emp.VerificationStatus = request.VerificationStatus;
            emp.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<EmployerDto>.Success(EmployerMappings.Map(emp));
        }
    }

    public class UploadEmployerDocumentHandler : IRequestHandler<UploadEmployerDocumentCommand, Result>
    {
        private readonly ApplicationDbContext _db;
        private readonly Professional.Infrastructure.Storage.IDocumentStorageService _storage;
        private readonly IMediator _mediator;

        public UploadEmployerDocumentHandler(ApplicationDbContext db, Professional.Infrastructure.Storage.IDocumentStorageService storage, IMediator mediator)
        {
            _db = db;
            _storage = storage;
            _mediator = mediator;
        }

        public async Task<Result> Handle(UploadEmployerDocumentCommand request, CancellationToken cancellationToken)
        {
            var employer = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.Id == request.EmployerId, cancellationToken);
            if (employer == null) return Result.Failure("Employer not found");

            var extension = System.IO.Path.GetExtension(request.FileName)?.ToLowerInvariant() ?? string.Empty;
            var config = await _db.DocumentTypes.FirstOrDefaultAsync(d =>
                d.TargetType == DocumentTargetType.Employer &&
                (d.Slug == request.DocumentType || d.Name == request.DocumentType), cancellationToken);
            if (config != null)
            {
                var allowed = (config.AllowedExtensions ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => value.StartsWith('.') ? value.ToLowerInvariant() : $".{value.ToLowerInvariant()}")
                    .ToList();
                if (allowed.Count > 0 && !allowed.Contains(extension))
                    return Result.Failure($"Allowed file types for {config.Name} are: {string.Join(", ", allowed)}");

                var maxBytes = config.MaxFileSizeMb * 1024L * 1024L;
                if (config.MaxFileSizeMb > 0 && request.FileData.LongLength > maxBytes)
                    return Result.Failure($"{config.Name} exceeds the allowed size of {config.MaxFileSizeMb} MB.");
            }

            var path = await _storage.SaveAsync(request.FileData, request.FileName, employer.TenantId.ToString());
            var document = new EmployerDocument
            {
                Id = Guid.NewGuid(),
                EmployerId = request.EmployerId,
                TenantId = employer.TenantId,
                DocumentType = request.DocumentType,
                FileName = request.FileName,
                StoragePath = path,
                FileSizeBytes = request.FileData.LongLength,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.EmployerDocuments.Add(document);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new Verification.Application.Commands.CreateVerificationRequestCommand(
                Verification.Domain.VerificationSubjectType.Employer,
                request.EmployerId,
                employer.TenantId,
                document.Id,
                $"Employer document submitted: {request.DocumentType}"), cancellationToken);

            return Result.Success();
        }
    }

    public class ListEmployersHandler : IRequestHandler<ListEmployersQuery, Result<EmployerListDto>>
    {
        private readonly ApplicationDbContext _db;
        public ListEmployersHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<EmployerListDto>> Handle(ListEmployersQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.EmployerProfiles
                .Where(e => e.TenantId == request.TenantId)
                .Select(e => EmployerMappings.Map(e))
                .ToListAsync(cancellationToken);
            return Result<EmployerListDto>.Success(new EmployerListDto(items, items.Count));
        }
    }

    public class GetEmployerDocumentsHandler : IRequestHandler<GetEmployerDocumentsQuery, Result<List<EmployerDocumentDto>>>
    {
        private readonly ApplicationDbContext _db;
        public GetEmployerDocumentsHandler(ApplicationDbContext db) => _db = db;
        public async Task<Result<List<EmployerDocumentDto>>> Handle(GetEmployerDocumentsQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.EmployerDocuments.Where(d => d.EmployerId == request.EmployerId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new EmployerDocumentDto(d.Id, d.EmployerId, d.DocumentType, d.FileName, d.Status, d.VerificationNotes, d.CreatedAt))
                .ToListAsync(cancellationToken);
            return Result<List<EmployerDocumentDto>>.Success(items);
        }
    }

    internal static class EmployerMappings
    {
        internal static EmployerDto Map(EmployerProfile profile) => new(
            profile.Id,
            profile.TenantId,
            profile.Name,
            profile.OrganizationSlug,
            profile.FacilityType,
            profile.ContactEmail,
            profile.ContactPhone,
            profile.IsContactPhonePublic,
            profile.Address,
            profile.SubscriptionTier,
            profile.VerificationStatus,
            profile.BusinessRegistrationNumber,
            profile.KraPin,
            profile.LicenseNumber);
    }
}

