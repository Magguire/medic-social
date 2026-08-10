using System;
using Shared.Auth;

namespace Identity.Domain
{
    public class RefreshTokenEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string HashedToken { get; set; } = null!;
        public string DeviceId { get; set; } = null!;
        public DateTime Expiry { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsRevoked => RevokedAt.HasValue;

        public RefreshTokenEntity(string hashedToken, string deviceId, DateTime expiry, Guid userId, string? ip, string? userAgent)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            HashedToken = hashedToken;
            DeviceId = deviceId;
            Expiry = expiry;
            Ip = ip;
            UserAgent = userAgent;
            CreatedAt = DateTime.UtcNow;
            LastSeenAt = CreatedAt;
        }
    }
}
