using System;
using System.Collections.Generic;

namespace Matching.Application;

public sealed record MatchCandidateDto(Guid ProfessionalId, Guid JobId, decimal Score, bool MeetsRequirements, string[] Reasons, string? ProfessionalCategory, int YearsOfExperience, string VerificationStatus);
public sealed record MatchCandidateListDto(List<MatchCandidateDto> Items, int TotalCount);
public sealed record MatchInvitationDto(Guid Id, Guid TenantId, Guid JobId, Guid ProfessionalId, string Status, string? Message, DateTime CreatedAt);
