using Shared.Kernel;

namespace Shared.Auth
{
    public interface IJwtService
    {
        string GenerateAccessToken(UserClaims claims);
        string GenerateRefreshToken(UserClaims claims, string deviceId);

        TokenValidationResult ValidateAccessToken(string token);
        TokenValidationResult ValidateRefreshToken(string token);

        (string RefreshToken, UserClaims Claims) RotateRefreshToken(string oldToken, string deviceId);
        void RevokeRefreshToken(string token);
    }
}
