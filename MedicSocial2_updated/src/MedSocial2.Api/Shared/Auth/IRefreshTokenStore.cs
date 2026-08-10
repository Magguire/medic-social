using System;

namespace Shared.Auth
{
    public record RefreshTokenRecord(string HashedToken, string DeviceId, DateTime Expiry, UserClaims Claims);

    public interface IRefreshTokenStore
    {
        void StoreToken(string hashedToken, string deviceId, DateTime expiry, UserClaims claims);
        RefreshTokenRecord? GetToken(string hashedToken);
        void RevokeToken(string hashedToken);
    }
}
