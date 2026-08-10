using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Identity.Application.DTOs;
using Shared.Kernel;
using Identity.Domain;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;

namespace Identity.Application.Commands
{
    public record LoginCommand(string Email, string Password, string? DeviceId) : IRequest<Result<AuthResponse>>;

    public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
    {
        private readonly IdentityDbContext _db;
        private readonly IJwtService _jwtService;

        public LoginHandler(IdentityDbContext db, IJwtService jwtService)
        {
            _db = db;
            _jwtService = jwtService;
        }

        public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);
            if (user == null)
                return Result<AuthResponse>.Failure("Invalid credentials");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                return Result<AuthResponse>.Failure("Invalid credentials");

            // create tokens
            var userClaims = new UserClaims(user.Id, user.TenantId, user.UserType.ToString(), user.SubscriptionTier, user.VerificationStatus);
            var access = _jwtService.GenerateAccessToken(userClaims);
            var refresh = _jwtService.GenerateRefreshToken(userClaims, request.DeviceId ?? "unknown");

            var response = new AuthResponse(
                new UserResponse(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName, user.UserType.ToString(), user.SubscriptionTier, user.VerificationStatus, user.CreatedAt),
                access, refresh);

            return Result<AuthResponse>.Success(response);
        }
    }
}
