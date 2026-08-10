using System.Security.Cryptography;
using System.Text;
using Identity.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Data;

namespace Identity.Infrastructure;

public sealed class DatabaseRefreshTokenStore : IRefreshTokenStore
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatabaseRefreshTokenStore(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public void StoreToken(string hashedToken, string deviceId, DateTime expiry, UserClaims claims)
    {
        var context = _httpContextAccessor.HttpContext;
        _db.RefreshTokens.Add(new RefreshTokenEntity(
            hashedToken,
            NormalizeDeviceId(deviceId),
            expiry,
            claims.UserId,
            context?.Connection.RemoteIpAddress?.ToString(),
            context?.Request.Headers.UserAgent.ToString()));

        var user = _db.Users.FirstOrDefault(item => item.Id == claims.UserId);
        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
        }

        _db.SaveChanges();
    }

    public RefreshTokenRecord? GetToken(string hashedToken)
    {
        var token = _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefault(item => item.HashedToken == hashedToken && !item.RevokedAt.HasValue);
        if (token is null)
        {
            return null;
        }

        var user = _db.Users.AsNoTracking().FirstOrDefault(item => item.Id == token.UserId && item.IsActive);
        if (user is null)
        {
            return null;
        }

        return new RefreshTokenRecord(
            token.HashedToken,
            token.DeviceId,
            token.Expiry,
            new UserClaims(user.Id, user.TenantId, user.UserType.ToString(), user.SubscriptionTier, user.VerificationStatus));
    }

    public void RevokeToken(string hashedToken)
    {
        var token = _db.RefreshTokens.FirstOrDefault(item => item.HashedToken == hashedToken);
        if (token is null)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.LastSeenAt = DateTime.UtcNow;
        _db.SaveChanges();
    }

    private static string NormalizeDeviceId(string? deviceId)
    {
        var value = string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim();
        if (value.Length <= 160)
        {
            return value;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
