using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Identity.Infrastructure;
using System.Linq;
using System.Security.Claims;

namespace Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IdentityDbContext _db;

        public UsersController(IdentityDbContext db) => _db = db;

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var sub = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(sub)) return Unauthorized();
            if (!System.Guid.TryParse(sub, out var userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new {
                id = user.Id,
                tenantId = user.TenantId,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                userType = user.UserType.ToString(),
                subscriptionTier = user.SubscriptionTier,
                verificationStatus = user.VerificationStatus,
                mustChangePassword = user.MustChangePassword,
                createdAt = user.CreatedAt
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(System.Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(new {
                id = user.Id,
                tenantId = user.TenantId,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                userType = user.UserType.ToString(),
                subscriptionTier = user.SubscriptionTier,
                verificationStatus = user.VerificationStatus,
                mustChangePassword = user.MustChangePassword,
                createdAt = user.CreatedAt
            });
        }
    }
}
