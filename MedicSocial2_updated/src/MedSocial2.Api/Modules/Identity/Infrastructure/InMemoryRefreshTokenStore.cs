using System;
using Shared.Auth;
using System.Collections.Concurrent;

namespace Identity.Infrastructure
{
    public class InMemoryRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokens = new();

        public void StoreToken(string hashedToken, string deviceId, DateTime expiry, UserClaims claims)
        {
            _tokens[hashedToken] = new RefreshTokenRecord(hashedToken, deviceId, expiry, claims);
        }

        public RefreshTokenRecord? GetToken(string hashedToken)
        {
            _tokens.TryGetValue(hashedToken, out var rec);
            return rec;
        }

        public void RevokeToken(string hashedToken)
        {
            _tokens.TryRemove(hashedToken, out _);
        }
    }
}
