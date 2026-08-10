using System;

namespace Professional.Application
{
    public record ProfessionalDto(
        Guid Id,
        Guid UserId,
        Guid TenantId,
        string? Nationality,
        string? PhoneNumber,
        string? EmailAddress,
        string? NationalIdOrPassport,
        string? AddressLine,
        string? City,
        string? County,
        string? PostalAddress,
        string? ProfessionalCategory,
        string? Specialty,
        string? Bio,
        string? LicenseNumber,
        string? LicenseBoard,
        DateTime? LicenseExpiryDate,
        int YearsOfExperience,
        string? CurrentPosition,
        string? CurrentEmployer,
        string? PreferredLocation,
        int? RelocationWillingness,
        decimal? ExpectedSalary,
        string? AvailabilityType,
        string? Skills,
        string? Languages,
        string? WorkPermitStatus,
        string VerificationStatus);

    public record EducationDto(
        Guid Id,
        Guid ProfessionalId,
        string Institution,
        string Award,
        string FieldOfStudy,
        DateTime StartDate,
        DateTime? EndDate,
        string? Grade);

    public record QualificationDto(
        Guid Id,
        Guid ProfessionalId,
        string Title,
        string IssuingBody,
        string? LicenseNumber,
        DateTime? IssuedOn,
        DateTime? ExpiresOn);

    public record ExperienceDto(
        Guid Id,
        Guid ProfessionalId,
        string EmployerName,
        string JobTitle,
        string? EmploymentType,
        string? Location,
        DateTime StartDate,
        DateTime? EndDate,
        bool IsCurrentRole,
        string? Responsibilities);

    public record ProfessionalCategoryDto(Guid Id, string Name, string Slug, bool IsActive);
}
