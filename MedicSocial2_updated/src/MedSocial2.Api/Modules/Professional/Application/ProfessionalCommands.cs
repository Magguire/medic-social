using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Professional.Domain;
using Shared.Kernel;
using Shared.Tenant;
using Shared.Data;
using Verification.Application;
using Verification.Domain;

namespace Professional.Application.Commands
{
    public record RegisterProfessionalCommand(
        Guid UserId,
        string Nationality,
        string? PhoneNumber,
        string? EmailAddress,
        string? NationalIdOrPassport,
        string? AddressLine,
        string? City,
        string? County,
        string? PostalAddress,
        string ProfessionalCategory,
        string LicenseNumber,
        string LicenseBoard,
        int YearsOfExperience,
        string Specialty) : IRequest<Result<ProfessionalDto>>;

    public record UpdateProfessionalCommand(
        Guid ProfessionalId,
        string? Nationality,
        string? PhoneNumber,
        string? EmailAddress,
        string? NationalIdOrPassport,
        string? AddressLine,
        string? City,
        string? County,
        string? PostalAddress,
        string? Bio,
        int? YearsOfExperience,
        string? CurrentPosition,
        string? CurrentEmployer,
        string? PreferredLocation,
        int? RelocationWillingness,
        decimal? ExpectedSalary,
        string? AvailabilityType,
        string? ProfessionalCategory,
        DateTime? LicenseExpiryDate,
        string? Skills,
        string? Languages,
        string? WorkPermitStatus,
        string? Specialty) : IRequest<Result<ProfessionalDto>>;

    public record AddEducationRecordCommand(
        Guid ProfessionalId,
        string Institution,
        string Award,
        string FieldOfStudy,
        DateTime StartDate,
        DateTime? EndDate,
        string? Grade) : IRequest<Result<EducationDto>>;

    public record AddQualificationRecordCommand(
        Guid ProfessionalId,
        string Title,
        string IssuingBody,
        string? LicenseNumber,
        DateTime? IssuedOn,
        DateTime? ExpiresOn) : IRequest<Result<QualificationDto>>;

    public record AddExperienceRecordCommand(
        Guid ProfessionalId,
        string EmployerName,
        string JobTitle,
        string? EmploymentType,
        string? Location,
        DateTime StartDate,
        DateTime? EndDate,
        bool IsCurrentRole,
        string? Responsibilities) : IRequest<Result<ExperienceDto>>;

    public record UploadDocumentCommand(
        Guid ProfessionalId,
        string DocumentType,
        byte[] FileData,
        string FileName) : IRequest<Result>;

    public record VerifyProfessionalCommand(
        Guid ProfessionalId,
        bool IsApproved,
        string? RejectionReason) : IRequest<Result>;

    public record GetProfessionalCategoriesQuery() : IRequest<Result<List<ProfessionalCategoryDto>>>;
    public record GetEducationRecordsQuery(Guid ProfessionalId) : IRequest<Result<List<EducationDto>>>;
    public record GetQualificationRecordsQuery(Guid ProfessionalId) : IRequest<Result<List<QualificationDto>>>;
    public record GetExperienceRecordsQuery(Guid ProfessionalId) : IRequest<Result<List<ExperienceDto>>>;

    public class RegisterProfessionalHandler : IRequestHandler<RegisterProfessionalCommand, Result<ProfessionalDto>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        private readonly ApplicationDbContext _applicationDb;

        public RegisterProfessionalHandler(Professional.Infrastructure.ProfessionalDbContext db, ApplicationDbContext applicationDb)
        {
            _db = db;
            _applicationDb = applicationDb;
        }

        public async Task<Result<ProfessionalDto>> Handle(RegisterProfessionalCommand request, CancellationToken cancellationToken)
        {
            var existing = await _db.ProfessionalProfiles.FirstOrDefaultAsync(
                profile => profile.UserId == request.UserId && profile.TenantId == PlatformTenant.Id,
                cancellationToken);
            if (existing != null)
            {
                existing.Nationality = request.Nationality;
                existing.PhoneNumber = request.PhoneNumber;
                existing.EmailAddress = request.EmailAddress;
                existing.NationalIdOrPassport = request.NationalIdOrPassport;
                existing.AddressLine = request.AddressLine;
                existing.City = request.City;
                existing.County = request.County;
                existing.PostalAddress = request.PostalAddress;
                existing.LicenseNumber = request.LicenseNumber;
                existing.LicenseBoard = request.LicenseBoard;
                existing.YearsOfExperience = request.YearsOfExperience;
                existing.Specialty = request.Specialty;

                var normalizedExistingCategories = await ProfessionalCategoryResolver.NormalizeCategoriesAsync(_db, request.ProfessionalCategory, cancellationToken);
                if (normalizedExistingCategories == null)
                    return Result<ProfessionalDto>.Failure("Professional category is not configured");

                existing.ProfessionalCategory = normalizedExistingCategories;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<ProfessionalDto>.Success(ProfessionalMappings.Map(existing));
            }

            var normalizedCategories = await ProfessionalCategoryResolver.NormalizeCategoriesAsync(_db, request.ProfessionalCategory, cancellationToken);
            if (normalizedCategories == null)
                return Result<ProfessionalDto>.Failure("Professional category is not configured");

            var profile = new ProfessionalProfile
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                TenantId = PlatformTenant.Id,
                Nationality = request.Nationality,
                PhoneNumber = request.PhoneNumber,
                EmailAddress = request.EmailAddress,
                NationalIdOrPassport = request.NationalIdOrPassport,
                AddressLine = request.AddressLine,
                City = request.City,
                County = request.County,
                PostalAddress = request.PostalAddress,
                ProfessionalCategory = normalizedCategories,
                LicenseNumber = request.LicenseNumber,
                LicenseBoard = request.LicenseBoard,
                YearsOfExperience = request.YearsOfExperience,
                Specialty = request.Specialty,
                VerificationStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.ProfessionalProfiles.Add(profile);
            await _db.SaveChangesAsync(cancellationToken);
            await VerificationPolicyEngine.TriggerStagePoliciesAsync(
                _applicationDb,
                VerificationSubjectType.Professional,
                VerificationStage.Registration,
                "RegisterProfessional",
                profile.Id,
                profile.TenantId,
                cancellationToken);
            return Result<ProfessionalDto>.Success(ProfessionalMappings.Map(profile));
        }
    }

    public class UpdateProfessionalHandler : IRequestHandler<UpdateProfessionalCommand, Result<ProfessionalDto>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        private readonly ApplicationDbContext _applicationDb;

        public UpdateProfessionalHandler(Professional.Infrastructure.ProfessionalDbContext db, ApplicationDbContext applicationDb)
        {
            _db = db;
            _applicationDb = applicationDb;
        }

        public async Task<Result<ProfessionalDto>> Handle(UpdateProfessionalCommand request, CancellationToken cancellationToken)
        {
            var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (profile == null)
                return Result<ProfessionalDto>.Failure("Professional not found");

            if (!string.IsNullOrWhiteSpace(request.Nationality)) profile.Nationality = request.Nationality;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) profile.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(request.EmailAddress)) profile.EmailAddress = request.EmailAddress;
            if (!string.IsNullOrWhiteSpace(request.NationalIdOrPassport)) profile.NationalIdOrPassport = request.NationalIdOrPassport;
            if (!string.IsNullOrWhiteSpace(request.AddressLine)) profile.AddressLine = request.AddressLine;
            if (!string.IsNullOrWhiteSpace(request.City)) profile.City = request.City;
            if (!string.IsNullOrWhiteSpace(request.County)) profile.County = request.County;
            if (!string.IsNullOrWhiteSpace(request.PostalAddress)) profile.PostalAddress = request.PostalAddress;
            if (!string.IsNullOrWhiteSpace(request.Bio)) profile.Bio = request.Bio;
            if (request.YearsOfExperience.HasValue) profile.YearsOfExperience = request.YearsOfExperience.Value;
            if (!string.IsNullOrWhiteSpace(request.CurrentPosition)) profile.CurrentPosition = request.CurrentPosition;
            if (!string.IsNullOrWhiteSpace(request.CurrentEmployer)) profile.CurrentEmployer = request.CurrentEmployer;
            if (!string.IsNullOrWhiteSpace(request.PreferredLocation)) profile.PreferredLocation = request.PreferredLocation;
            if (request.RelocationWillingness.HasValue) profile.RelocationWillingness = request.RelocationWillingness.Value;
            if (request.ExpectedSalary.HasValue) profile.ExpectedSalary = request.ExpectedSalary.Value;
            if (!string.IsNullOrWhiteSpace(request.AvailabilityType)) profile.AvailabilityType = request.AvailabilityType;
            if (request.LicenseExpiryDate.HasValue) profile.LicenseExpiryDate = request.LicenseExpiryDate.Value;
            if (!string.IsNullOrWhiteSpace(request.Skills)) profile.Skills = request.Skills;
            if (!string.IsNullOrWhiteSpace(request.Languages)) profile.Languages = request.Languages;
            if (!string.IsNullOrWhiteSpace(request.WorkPermitStatus)) profile.WorkPermitStatus = request.WorkPermitStatus;
            if (!string.IsNullOrWhiteSpace(request.Specialty)) profile.Specialty = request.Specialty;
            if (!string.IsNullOrWhiteSpace(request.ProfessionalCategory))
            {
                var normalizedCategories = await ProfessionalCategoryResolver.NormalizeCategoriesAsync(_db, request.ProfessionalCategory, cancellationToken);
                if (normalizedCategories == null)
                {
                    return Result<ProfessionalDto>.Failure("Professional category is not configured");
                }

                profile.ProfessionalCategory = normalizedCategories;
            }
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await VerificationPolicyEngine.TriggerStagePoliciesAsync(
                _applicationDb,
                VerificationSubjectType.Professional,
                VerificationStage.ProfileCompletion,
                "UpdateProfessionalProfile",
                profile.Id,
                profile.TenantId,
                cancellationToken);
            return Result<ProfessionalDto>.Success(ProfessionalMappings.Map(profile));
        }
    }

    public class AddExperienceRecordHandler : IRequestHandler<AddExperienceRecordCommand, Result<ExperienceDto>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public AddExperienceRecordHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;

        public async Task<Result<ExperienceDto>> Handle(AddExperienceRecordCommand request, CancellationToken cancellationToken)
        {
            var exists = await _db.ProfessionalProfiles.AnyAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (!exists) return Result<ExperienceDto>.Failure("Professional not found");

            var entity = new ExperienceRecord
            {
                Id = Guid.NewGuid(),
                ProfessionalId = request.ProfessionalId,
                EmployerName = request.EmployerName,
                JobTitle = request.JobTitle,
                EmploymentType = request.EmploymentType,
                Location = request.Location,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCurrentRole = request.IsCurrentRole,
                Responsibilities = request.Responsibilities,
                CreatedAt = DateTime.UtcNow
            };

            _db.ExperienceRecords.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<ExperienceDto>.Success(new ExperienceDto(entity.Id, entity.ProfessionalId, entity.EmployerName, entity.JobTitle, entity.EmploymentType, entity.Location, entity.StartDate, entity.EndDate, entity.IsCurrentRole, entity.Responsibilities));
        }
    }

    public class AddEducationRecordHandler : IRequestHandler<AddEducationRecordCommand, Result<EducationDto>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public AddEducationRecordHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<EducationDto>> Handle(AddEducationRecordCommand request, CancellationToken cancellationToken)
        {
            var exists = await _db.ProfessionalProfiles.AnyAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (!exists) return Result<EducationDto>.Failure("Professional not found");

            var entity = new EducationRecord
            {
                Id = Guid.NewGuid(),
                ProfessionalId = request.ProfessionalId,
                Institution = request.Institution,
                Award = request.Award,
                FieldOfStudy = request.FieldOfStudy,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Grade = request.Grade,
                CreatedAt = DateTime.UtcNow
            };
            _db.EducationRecords.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<EducationDto>.Success(new EducationDto(entity.Id, entity.ProfessionalId, entity.Institution, entity.Award, entity.FieldOfStudy, entity.StartDate, entity.EndDate, entity.Grade));
        }
    }

    public class AddQualificationRecordHandler : IRequestHandler<AddQualificationRecordCommand, Result<QualificationDto>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public AddQualificationRecordHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<QualificationDto>> Handle(AddQualificationRecordCommand request, CancellationToken cancellationToken)
        {
            var exists = await _db.ProfessionalProfiles.AnyAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (!exists) return Result<QualificationDto>.Failure("Professional not found");

            var entity = new QualificationRecord
            {
                Id = Guid.NewGuid(),
                ProfessionalId = request.ProfessionalId,
                Title = request.Title,
                IssuingBody = request.IssuingBody,
                LicenseNumber = request.LicenseNumber,
                IssuedOn = request.IssuedOn,
                ExpiresOn = request.ExpiresOn,
                CreatedAt = DateTime.UtcNow
            };
            _db.QualificationRecords.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<QualificationDto>.Success(new QualificationDto(entity.Id, entity.ProfessionalId, entity.Title, entity.IssuingBody, entity.LicenseNumber, entity.IssuedOn, entity.ExpiresOn));
        }
    }

    public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, Result>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        private readonly Professional.Infrastructure.Storage.IDocumentStorageService _storage;
        private readonly IMediator _mediator;

        public UploadDocumentHandler(Professional.Infrastructure.ProfessionalDbContext db, Professional.Infrastructure.Storage.IDocumentStorageService storage, IMediator mediator)
        {
            _db = db;
            _storage = storage;
            _mediator = mediator;
        }

        public async Task<Result> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (profile == null)
                return Result.Failure("Professional not found");

            var extension = System.IO.Path.GetExtension(request.FileName)?.ToLowerInvariant() ?? string.Empty;
            var config = await _db.DocumentTypes.FirstOrDefaultAsync(d =>
                d.TargetType == DocumentTargetType.Professional &&
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

            var type = Enum.TryParse<DocumentType>(request.DocumentType, true, out var parsed)
                ? parsed
                : DocumentType.Passport;

            var storageKey = profile.UserId == Guid.Empty ? request.ProfessionalId.ToString() : profile.UserId.ToString();
            var path = await _storage.SaveAsync(request.FileData, request.FileName, storageKey);

            var doc = new Document
            {
                Id = Guid.NewGuid(),
                ProfessionalId = request.ProfessionalId,
                TenantId = profile.TenantId,
                Type = type,
                DocumentTypeName = config?.Name ?? request.DocumentType,
                FileName = request.FileName,
                StoragePath = path,
                ContentType = "application/octet-stream",
                FileSizeBytes = request.FileData.LongLength,
                CreatedAt = DateTime.UtcNow,
                Status = DocumentStatus.Pending
            };

            _db.Documents.Add(doc);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new Verification.Application.Commands.CreateVerificationRequestCommand(
                Verification.Domain.VerificationSubjectType.Professional,
                request.ProfessionalId,
                profile.TenantId,
                doc.Id,
                null), cancellationToken);

            return Result.Success();
        }
    }

    public class VerifyProfessionalHandler : IRequestHandler<VerifyProfessionalCommand, Result>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;

        public VerifyProfessionalHandler(Professional.Infrastructure.ProfessionalDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(VerifyProfessionalCommand request, CancellationToken cancellationToken)
        {
            var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == request.ProfessionalId, cancellationToken);
            if (profile == null)
                return Result.Failure("Professional not found");

            profile.VerificationStatus = request.IsApproved ? "Verified" : "Rejected";
            profile.RejectionReason = request.RejectionReason;
            profile.VerifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class GetProfessionalCategoriesHandler : IRequestHandler<GetProfessionalCategoriesQuery, Result<List<ProfessionalCategoryDto>>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public GetProfessionalCategoriesHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<List<ProfessionalCategoryDto>>> Handle(GetProfessionalCategoriesQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.Categories.OrderBy(c => c.Name)
                .Select(c => new ProfessionalCategoryDto(c.Id, c.Name, c.Slug, c.IsActive))
                .ToListAsync(cancellationToken);
            return Result<List<ProfessionalCategoryDto>>.Success(items);
        }

    }

    internal static class ProfessionalCategoryResolver
    {
        internal static async Task<string?> NormalizeCategoriesAsync(
            Professional.Infrastructure.ProfessionalDbContext db,
            string requestedCategory,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(requestedCategory))
            {
                return null;
            }

            var selectedValues = requestedCategory
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (selectedValues.Count == 0)
            {
                return null;
            }

            var normalized = new List<string>();
            foreach (var selectedValue in selectedValues)
            {
                if (selectedValue.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
                {
                    var customValue = selectedValue["Other:".Length..].Trim();
                    if (string.IsNullOrWhiteSpace(customValue))
                    {
                        return null;
                    }

                    normalized.Add($"Other: {customValue}");
                    continue;
                }

                var trimmed = selectedValue.Trim();
                var category = await db.Categories.FirstOrDefaultAsync(item =>
                    item.Name == trimmed ||
                    item.Slug == trimmed ||
                    item.Id.ToString() == trimmed,
                    cancellationToken);

                if (category == null)
                {
                    return null;
                }

                normalized.Add(category.Name);
            }

            return string.Join(", ", normalized.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        internal static string[] SplitCategories(string? categories)
        {
            return (categories ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }
    }

    public class GetEducationRecordsHandler : IRequestHandler<GetEducationRecordsQuery, Result<List<EducationDto>>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public GetEducationRecordsHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<List<EducationDto>>> Handle(GetEducationRecordsQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.EducationRecords.Where(e => e.ProfessionalId == request.ProfessionalId)
                .OrderByDescending(e => e.EndDate ?? e.StartDate)
                .Select(e => new EducationDto(e.Id, e.ProfessionalId, e.Institution, e.Award, e.FieldOfStudy, e.StartDate, e.EndDate, e.Grade))
                .ToListAsync(cancellationToken);
            return Result<List<EducationDto>>.Success(items);
        }
    }

    public class GetQualificationRecordsHandler : IRequestHandler<GetQualificationRecordsQuery, Result<List<QualificationDto>>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public GetQualificationRecordsHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<List<QualificationDto>>> Handle(GetQualificationRecordsQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.QualificationRecords.Where(e => e.ProfessionalId == request.ProfessionalId)
                .OrderByDescending(e => e.IssuedOn)
                .Select(e => new QualificationDto(e.Id, e.ProfessionalId, e.Title, e.IssuingBody, e.LicenseNumber, e.IssuedOn, e.ExpiresOn))
                .ToListAsync(cancellationToken);
            return Result<List<QualificationDto>>.Success(items);
        }
    }

    public class GetExperienceRecordsHandler : IRequestHandler<GetExperienceRecordsQuery, Result<List<ExperienceDto>>>
    {
        private readonly Professional.Infrastructure.ProfessionalDbContext _db;
        public GetExperienceRecordsHandler(Professional.Infrastructure.ProfessionalDbContext db) => _db = db;
        public async Task<Result<List<ExperienceDto>>> Handle(GetExperienceRecordsQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.ExperienceRecords.Where(e => e.ProfessionalId == request.ProfessionalId)
                .OrderByDescending(e => e.IsCurrentRole)
                .ThenByDescending(e => e.EndDate ?? DateTime.MaxValue)
                .Select(e => new ExperienceDto(e.Id, e.ProfessionalId, e.EmployerName, e.JobTitle, e.EmploymentType, e.Location, e.StartDate, e.EndDate, e.IsCurrentRole, e.Responsibilities))
                .ToListAsync(cancellationToken);
            return Result<List<ExperienceDto>>.Success(items);
        }
    }

    internal static class ProfessionalMappings
    {
        internal static ProfessionalDto Map(ProfessionalProfile profile) => new(
            profile.Id,
            profile.UserId,
            profile.TenantId,
            profile.Nationality,
            profile.PhoneNumber,
            profile.EmailAddress,
            profile.NationalIdOrPassport,
            profile.AddressLine,
            profile.City,
            profile.County,
            profile.PostalAddress,
            profile.ProfessionalCategory,
            profile.Specialty,
            profile.Bio,
            profile.LicenseNumber,
            profile.LicenseBoard,
            profile.LicenseExpiryDate,
            profile.YearsOfExperience,
            profile.CurrentPosition,
            profile.CurrentEmployer,
            profile.PreferredLocation,
            profile.RelocationWillingness,
            profile.ExpectedSalary,
            profile.AvailabilityType,
            profile.Skills,
            profile.Languages,
            profile.WorkPermitStatus,
            profile.VerificationStatus);
    }
}

