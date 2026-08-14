using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Verification.Api.Controllers;

[ApiController]
[Route("api/client-theme")]
public class ClientThemeController : ControllerBase
{
    private const string DefaultKey = "default";
    private readonly ApplicationDbContext _db;

    public ClientThemeController(ApplicationDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPublic()
    {
        try
        {
            var entity = await _db.ClientThemeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key == DefaultKey && item.IsPublished, HttpContext.RequestAborted);

            return Ok(entity == null ? DefaultTheme() : Map(entity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdmin()
    {
        try
        {
            var entity = await _db.ClientThemeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key == DefaultKey, HttpContext.RequestAborted);

            return Ok(entity == null ? DefaultTheme() : Map(entity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPut("admin")]
    public async Task<IActionResult> Save([FromBody] ClientThemeConfigDto request)
    {
        try
        {
            var entity = await _db.ClientThemeConfigs.FirstOrDefaultAsync(item => item.Key == DefaultKey, HttpContext.RequestAborted);
            if (entity == null)
            {
                entity = new ClientThemeConfig
                {
                    Id = Guid.NewGuid(),
                    Key = DefaultKey,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.ClientThemeConfigs.Add(entity);
            }

            entity.PrimaryColor = NormalizeColor(request.PrimaryColor, "#50b998");
            entity.SecondaryColor = NormalizeColor(request.SecondaryColor, "#111827");
            entity.AccentColor = NormalizeColor(request.AccentColor, "#b66a3c");
            entity.BackgroundColor = NormalizeColor(request.BackgroundColor, "#fbf7ef");
            entity.SurfaceColor = NormalizeColor(request.SurfaceColor, "#ffffff");
            entity.TextColor = NormalizeColor(request.TextColor, "#111827");
            entity.MutedTextColor = NormalizeColor(request.MutedTextColor, "#667085");
            entity.DarkBackgroundColor = NormalizeColor(request.DarkBackgroundColor, "#111820");
            entity.DarkSurfaceColor = NormalizeColor(request.DarkSurfaceColor, "#1d2a31");
            entity.DarkTextColor = NormalizeColor(request.DarkTextColor, "#f7f2ea");
            entity.DarkMutedTextColor = NormalizeColor(request.DarkMutedTextColor, "#c1c8c4");
            entity.IsPublished = request.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(Map(entity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    private static string NormalizeColor(string? value, string fallback)
    {
        var color = value?.Trim();
        if (string.IsNullOrWhiteSpace(color))
        {
            return fallback;
        }

        return color.Length <= 20 ? color : fallback;
    }

    private static ClientThemeConfigDto Map(ClientThemeConfig entity)
    {
        return new ClientThemeConfigDto(
            entity.PrimaryColor,
            entity.SecondaryColor,
            entity.AccentColor,
            entity.BackgroundColor,
            entity.SurfaceColor,
            entity.TextColor,
            entity.MutedTextColor,
            entity.DarkBackgroundColor,
            entity.DarkSurfaceColor,
            entity.DarkTextColor,
            entity.DarkMutedTextColor,
            entity.IsPublished,
            entity.UpdatedAt ?? entity.CreatedAt);
    }

    private static ClientThemeConfigDto DefaultTheme()
    {
        return new ClientThemeConfigDto(
            "#50b998",
            "#111827",
            "#b66a3c",
            "#fbf7ef",
            "#ffffff",
            "#111827",
            "#667085",
            "#111820",
            "#1d2a31",
            "#f7f2ea",
            "#c1c8c4",
            true,
            DateTime.UtcNow);
    }
}

public record ClientThemeConfigDto(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string SurfaceColor,
    string TextColor,
    string MutedTextColor,
    string DarkBackgroundColor,
    string DarkSurfaceColor,
    string DarkTextColor,
    string DarkMutedTextColor,
    bool IsPublished,
    DateTime UpdatedAt);
