using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Identity.Infrastructure
{
    // simple password hasher using HMACSHA256 and PBKDF2
    public static class PasswordHasher
    {
        // these settings could be moved to config later
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int StoredSize = SaltSize + KeySize;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: KeySize);

            // store salt + hash
            var result = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);
            return Convert.ToBase64String(result);
        }

        public static bool Verify(string password, string hashed)
        {
            try
            {
                if (!TryDecodeHash(hashed, out var bytes))
                {
                    return false;
                }

                var salt = new byte[SaltSize];
                Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);
                var storedHash = new byte[KeySize];
                Buffer.BlockCopy(bytes, SaltSize, storedHash, 0, KeySize);

                byte[] hash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: Iterations,
                    numBytesRequested: KeySize);

                return CryptographicOperations.FixedTimeEquals(hash, storedHash);
            }
            catch
            {
                return false;
            }
        }

        public static bool NeedsRehash(string? hashed)
            => !TryDecodeHash(hashed, out _);

        private static bool TryDecodeHash(string? hashed, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(hashed))
            {
                return false;
            }

            try
            {
                bytes = Convert.FromBase64String(hashed);
                return bytes.Length == StoredSize;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }
    }
}
