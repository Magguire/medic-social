using System;

namespace Shared.Auth
{
    public record UserClaims(
        Guid UserId,
        Guid TenantId,
        string Role,
        string SubscriptionTier,
        string VerificationStatus);
}