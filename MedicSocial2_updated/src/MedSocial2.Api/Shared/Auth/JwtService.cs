using System;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth
{
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _options;
        private readonly IRefreshTokenStore _store;

        public JwtService(JwtOptions options, IRefreshTokenStore store)
        {
            _options = options;
            _store = store;
        }

        public string GenerateAccessToken(UserClaims claims)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtClaims = new List<Claim>
            {
                new Claim("UserId", claims.UserId.ToString()),
                new Claim("TenantId", claims.TenantId.ToString()),
                new Claim(ClaimTypes.Role, claims.Role),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("SubscriptionTier", claims.SubscriptionTier),
                new Claim("VerificationStatus", claims.VerificationStatus)
            };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: jwtClaims,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken(UserClaims claims, string deviceId)
        {
            var random = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            var token = Convert.ToBase64String(random);
            _store.StoreToken(Hash(token), deviceId, DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays), claims);
            return token;
        }

        public TokenValidationResult ValidateAccessToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _options.Issuer,
                    ValidAudience = _options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.SigningKey))
                }, out var validatedToken);

                var claims = new UserClaims(
                    Guid.Parse(principal.FindFirst("UserId")!.Value),
                    Guid.Parse(principal.FindFirst("TenantId")!.Value),
                    principal.FindFirst(ClaimTypes.Role)!.Value,
                    principal.FindFirst("SubscriptionTier")!.Value,
                    principal.FindFirst("VerificationStatus")!.Value);

                return new TokenValidationResult(true, claims, null);
            }
            catch (Exception ex)
            {
                return new TokenValidationResult(false, null, ex.Message);
            }
        }

        public TokenValidationResult ValidateRefreshToken(string token)
        {
            var hashed = Hash(token);
            var record = _store.GetToken(hashed);
            if (record == null || record.Expiry < DateTime.UtcNow)
                return new TokenValidationResult(false, null, "Invalid or expired refresh token");

            // optionally check device id etc
            return new TokenValidationResult(true, record.Claims, null);
        }

        public (string RefreshToken, UserClaims Claims) RotateRefreshToken(string oldToken, string deviceId)
        {
            var validation = ValidateRefreshToken(oldToken);
            if (!validation.IsValid) throw new SecurityException("Refresh token invalid");

            _store.RevokeToken(Hash(oldToken));
            var claims = validation.Claims ?? throw new SecurityException("Refresh token claims missing");
            return (GenerateRefreshToken(claims, deviceId), claims);
        }

        public void RevokeRefreshToken(string token)
        {
            _store.RevokeToken(Hash(token));
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
