using Microsoft.AspNetCore.Mvc;
using MediatR;
using Identity.Application.Commands;
using Identity.Application.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using Identity.Domain;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IdentityDbContext _db;

        public AuthController(IMediator mediator, IdentityDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            try
            {
                if (!request.AcceptedTerms || !request.AcceptedPrivacyPolicy)
                {
                    return BadRequest(new { errors = new[] { "You must accept the Terms and Conditions and Privacy Policy before creating an account." } });
                }

                var command = new RegisterUserCommand(request.Email, request.Password, request.FirstName,
                    request.LastName, request.PhoneNumber, request.UserType, request.OrganizationName, request.BusinessPhoneNumber);
                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                    return BadRequest(new { errors = result.Errors });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { errors = new[] { ex.Message } });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var command = new LoginCommand(request.Email, request.Password, request.DeviceId);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return Unauthorized(new { errors = result.Errors });

            return Ok(result.Value);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var command = new RefreshTokenCommand(request.RefreshToken, request.DeviceId);
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
                return Unauthorized(new { errors = result.Errors });
            return Ok(result.Value);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var command = new LogoutCommand(request.RefreshToken);
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
                return BadRequest(new { errors = result.Errors });
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("password-policy")]
        public async Task<IActionResult> GetPasswordPolicy()
        {
            var policy = await GetPolicyAsync();
            return Ok(new PasswordPolicyResponse(policy.MinLength, policy.RequireUppercase, policy.RequireLowercase, policy.RequireDigit, policy.RequireSymbol));
        }

        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        [HttpPut("password-policy")]
        public async Task<IActionResult> UpdatePasswordPolicy([FromBody] PasswordPolicyRequest request)
        {
            var policy = await GetPolicyAsync();
            policy.MinLength = Math.Clamp(request.MinLength, 1, 128);
            policy.RequireUppercase = request.RequireUppercase;
            policy.RequireLowercase = request.RequireLowercase;
            policy.RequireDigit = request.RequireDigit;
            policy.RequireSymbol = request.RequireSymbol;
            policy.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new PasswordPolicyResponse(policy.MinLength, policy.RequireUppercase, policy.RequireLowercase, policy.RequireDigit, policy.RequireSymbol));
        }

        [Authorize]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var userIdValue = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var policy = await GetPolicyAsync();
            var passwordErrors = policy.Validate(request.NewPassword);
            if (passwordErrors.Count > 0)
            {
                return BadRequest(new { errors = passwordErrors });
            }

            if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
            {
                return BadRequest(new { errors = new[] { "Password confirmation does not match." } });
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { errors = new[] { "Current password is incorrect." } });
            }

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.MustChangePassword = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully." });
        }

        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        [HttpPost("admin-reset-password")]
        public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordRequest request)
        {
            if (request.UserId == Guid.Empty && string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { errors = new[] { "User id or email is required." } });

            var policy = await GetPolicyAsync();
            var passwordErrors = policy.Validate(request.NewPassword);
            if (passwordErrors.Count > 0)
                return BadRequest(new { errors = passwordErrors });

            var user = request.UserId != Guid.Empty
                ? await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId)
                : await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return NotFound(new { errors = new[] { "User not found." } });

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.MustChangePassword = true;
            user.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Password reset successfully." });
        }

        private async Task<PasswordPolicyConfig> GetPolicyAsync()
        {
            var policy = await _db.PasswordPolicies.FirstOrDefaultAsync(p => p.Id == PasswordPolicyConfig.DefaultId)
                ?? await _db.PasswordPolicies.OrderBy(p => p.CreatedAt).FirstOrDefaultAsync();

            if (policy != null)
            {
                return policy;
            }

            policy = PasswordPolicyConfig.Default();
            _db.PasswordPolicies.Add(policy);
            await _db.SaveChangesAsync();
            return policy;
        }
    }

    public record ResetPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
    public record AdminResetPasswordRequest(Guid UserId, string? Email, string NewPassword);
    public record PasswordPolicyRequest(int MinLength, bool RequireUppercase, bool RequireLowercase, bool RequireDigit, bool RequireSymbol);
    public record PasswordPolicyResponse(int MinLength, bool RequireUppercase, bool RequireLowercase, bool RequireDigit, bool RequireSymbol);
}
