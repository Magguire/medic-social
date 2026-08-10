using System;
using System.Collections.Generic;
using System.Linq;

namespace Identity.Domain
{
    public class PasswordPolicyConfig
    {
        public static readonly Guid DefaultId = new("00000000-0000-0000-0000-000000000010");

        public Guid Id { get; set; } = DefaultId;
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSymbol { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public static PasswordPolicyConfig Default() => new()
        {
            Id = DefaultId,
            MinLength = 8,
            RequireUppercase = false,
            RequireLowercase = false,
            RequireDigit = false,
            RequireSymbol = false,
            CreatedAt = DateTime.UtcNow
        };

        public IReadOnlyList<string> Validate(string? password)
        {
            var errors = new List<string>();
            var value = password ?? string.Empty;

            if (value.Length < MinLength)
            {
                errors.Add($"New password must be at least {MinLength} characters long.");
            }

            if (RequireUppercase && !value.Any(char.IsUpper))
            {
                errors.Add("New password must include an uppercase letter.");
            }

            if (RequireLowercase && !value.Any(char.IsLower))
            {
                errors.Add("New password must include a lowercase letter.");
            }

            if (RequireDigit && !value.Any(char.IsDigit))
            {
                errors.Add("New password must include a number.");
            }

            if (RequireSymbol && !value.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errors.Add("New password must include a symbol.");
            }

            return errors;
        }
    }
}
