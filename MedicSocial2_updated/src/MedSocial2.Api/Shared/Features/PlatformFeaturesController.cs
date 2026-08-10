using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Features;

[ApiController]
[Route("api/admin/features")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public class PlatformFeaturesController : ControllerBase
{
    private readonly IPlatformFeatureService _features;

    public PlatformFeaturesController(IPlatformFeatureService features)
    {
        _features = features;
    }

    [HttpGet("{featureKey}")]
    public async Task<IActionResult> Get(string featureKey, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _features.GetOrCreateAsync(featureKey, null, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{featureKey}")]
    public async Task<IActionResult> Set(string featureKey, [FromBody] PlatformFeatureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _features.SetAsync(featureKey, request.IsEnabled, request.DisabledMessage, cancellationToken));
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}

public record PlatformFeatureRequest(bool IsEnabled, string? DisabledMessage);
