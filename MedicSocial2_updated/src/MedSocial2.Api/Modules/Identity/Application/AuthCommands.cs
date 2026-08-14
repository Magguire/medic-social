using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Shared.Kernel;
using Shared.Auth;
using Identity.Application.DTOs;
using Identity.Infrastructure;
using Identity.Domain;
using Employer.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Tenant;

namespace Identity.Application.Commands
{
    public record RegisterUserCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string UserType,
        string? OrganizationName,
        string? BusinessPhoneNumber) : IRequest<Result<AuthResponse>>;

    public record RefreshTokenCommand(string RefreshToken, string? DeviceId) : IRequest<Result<AuthResponse>>;
    public record LogoutCommand(string RefreshToken) : IRequest<Result>;

    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
    {
        private readonly IdentityDbContext _db;
        private readonly IJwtService _jwtService;

        public RegisterUserHandler(IdentityDbContext db, IJwtService jwtService)
        {
            _db = db;
            _jwtService = jwtService;
        }

        public async Task<Result<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var requestedUserType = Enum.TryParse<Identity.Domain.UserType>(request.UserType, true, out var parsedUserType)
                ? parsedUserType
                : Identity.Domain.UserType.Professional;
            var organizationName = request.OrganizationName?.Trim() ?? string.Empty;
            var businessPhoneNumber = request.BusinessPhoneNumber?.Trim() ?? string.Empty;
            if (requestedUserType == Identity.Domain.UserType.Employer)
            {
                if (string.IsNullOrWhiteSpace(organizationName))
                    return Result<AuthResponse>.Failure("Organization name is required for employer accounts");
                if (string.IsNullOrWhiteSpace(businessPhoneNumber))
                    return Result<AuthResponse>.Failure("Business phone number is required for employer accounts");
            }

            // Public self-service users belong to the platform tenant until an org-specific model is introduced.
            if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.TenantId == PlatformTenant.Id, cancellationToken))
            {
                return Result<AuthResponse>.Failure("Email already registered");
            }

            var policy = await _db.PasswordPolicies.FirstOrDefaultAsync(p => p.Id == PasswordPolicyConfig.DefaultId, cancellationToken)
                ?? await _db.PasswordPolicies.OrderBy(p => p.CreatedAt).FirstOrDefaultAsync(cancellationToken)
                ?? PasswordPolicyConfig.Default();
            var passwordErrors = policy.Validate(request.Password);
            if (passwordErrors.Count > 0)
            {
                return Result<AuthResponse>.Failure(passwordErrors.ToArray());
            }

            // create user
            var user = new Identity.Domain.User
            {
                Id = Guid.NewGuid(),
                TenantId = PlatformTenant.Id,
                Email = request.Email,
                PasswordHash = PasswordHasher.Hash(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = requestedUserType == Identity.Domain.UserType.Employer ? businessPhoneNumber : request.PhoneNumber,
                UserType = requestedUserType,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Status = Identity.Domain.UserStatus.Active
            };

            _db.Users.Add(user);

            if (user.UserType == Identity.Domain.UserType.Employer &&
                !await _db.EmployerProfiles.AnyAsync(e => e.ContactEmail == request.Email, cancellationToken))
            {
                var defaultPlan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.IsDefault, cancellationToken);
                _db.EmployerProfiles.Add(new EmployerProfile
                {
                    Id = Guid.NewGuid(),
                    TenantId = PlatformTenant.Id,
                    Name = organizationName,
                    OrganizationSlug = BuildSlug(organizationName),
                    FacilityType = "Healthcare Facility",
                    ContactEmail = request.Email,
                    ContactPhone = businessPhoneNumber,
                    IsContactPhonePublic = false,
                    SubscriptionTier = defaultPlan?.Slug ?? "free",
                    VerificationStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            var userClaims = new UserClaims(user.Id, user.TenantId, user.UserType.ToString(), user.SubscriptionTier, user.VerificationStatus);
            var access = _jwtService.GenerateAccessToken(userClaims);
            var refresh = _jwtService.GenerateRefreshToken(userClaims, request.PhoneNumber ?? "unknown");

            var response = new AuthResponse(
                new UserResponse(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.SubscriptionTier, user.VerificationStatus, user.CreatedAt),
                access, refresh);

            return Result<AuthResponse>.Success(response);
        }

        private static string BuildSlug(string value)
        {
            var slugBase = new string((value ?? "employer").ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(slugBase))
            {
                slugBase = "employer";
            }

            slugBase = slugBase.Length > 24 ? slugBase[..24] : slugBase;
            return $"{slugBase}-{Guid.NewGuid():N}"[..Math.Min(slugBase.Length + 9, 32)];
        }
    }

    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
    {
        private readonly IJwtService _jwtService;
        private readonly Shared.Auth.IRefreshTokenStore _store;

        public RefreshTokenHandler(IJwtService jwtService, Shared.Auth.IRefreshTokenStore store)
        {
            _jwtService = jwtService;
            _store = store;
        }

        public Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var validation = _jwtService.ValidateRefreshToken(request.RefreshToken);
            if (!validation.IsValid)
                return Task.FromResult(Result<AuthResponse>.Failure("Invalid refresh token"));

            // rotate
            var rotated = _jwtService.RotateRefreshToken(request.RefreshToken, request.DeviceId ?? "unknown");
            var access = _jwtService.GenerateAccessToken(rotated.Claims);
            var auth = new AuthResponse(
                new UserResponse(rotated.Claims.UserId, rotated.Claims.TenantId, "", "", "", rotated.Claims.Role, rotated.Claims.SubscriptionTier, rotated.Claims.VerificationStatus, DateTime.UtcNow),
                access,
                rotated.RefreshToken);
            return Task.FromResult(Result<AuthResponse>.Success(auth));
        }
    }

    public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IJwtService _jwtService;
        public LogoutHandler(IJwtService jwtService) => _jwtService = jwtService;
        public Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            _jwtService.RevokeRefreshToken(request.RefreshToken);
            return Task.FromResult(Result.Success());
        }
    }
}
