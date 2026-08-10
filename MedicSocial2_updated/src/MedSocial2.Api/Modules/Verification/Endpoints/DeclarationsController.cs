using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace Verification.Api.Controllers;

[ApiController]
[Route("api/declarations")]
public class DeclarationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DeclarationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("{flowKey}")]
    public async Task<IActionResult> GetByFlow(string flowKey)
    {
        try
        {
            var normalized = flowKey.Trim();
            var declarations = await _db.DeclarationConfigs
                .Where(item => item.FlowKey == normalized && item.IsActive)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Title)
                .Select(item => new PublicDeclarationConfigDto(item.Id, item.FlowKey, item.Title, item.Body, item.IsRequired, item.DisplayOrder))
                .ToListAsync(HttpContext.RequestAborted);
            return Ok(declarations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}

public record PublicDeclarationConfigDto(Guid Id, string FlowKey, string Title, string Body, bool IsRequired, int DisplayOrder);
