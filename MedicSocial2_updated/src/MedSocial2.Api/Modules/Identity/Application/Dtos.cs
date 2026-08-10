using System;

namespace Identity.Application.DTOs
{
    public record RegisterUserRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string UserType,
        bool AcceptedTerms,
        bool AcceptedPrivacyPolicy);

    public record LoginRequest(string Email, string Password, string? DeviceId = null);

    public record RefreshTokenRequest(string RefreshToken, string? DeviceId = null);

    public record UserResponse(
        Guid Id,
        Guid TenantId,
        string Email,
        string FirstName,
        string LastName,
        string UserType,
        string SubscriptionTier,
        string VerificationStatus,
        DateTime CreatedAt);

    public record AuthResponse(
        UserResponse User,
        string AccessToken,
        string RefreshToken);
}
