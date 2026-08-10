namespace Shared.Auth
{
    public record TokenValidationResult(bool IsValid, UserClaims? Claims, string? ErrorMessage);
}