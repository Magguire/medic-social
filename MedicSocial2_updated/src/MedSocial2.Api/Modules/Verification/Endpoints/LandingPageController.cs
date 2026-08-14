using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Verification.Api.Controllers;

[ApiController]
[Route("api/landing-page")]
public class LandingPageController : ControllerBase
{
    private const string DefaultKey = "default";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _db;

    public LandingPageController(ApplicationDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPublic()
    {
        try
        {
            var entity = await _db.LandingPageContents
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key == DefaultKey && item.IsPublished, HttpContext.RequestAborted);

            return Ok(entity == null ? DefaultLandingPage() : Map(entity));
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
            var entity = await _db.LandingPageContents
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key == DefaultKey, HttpContext.RequestAborted);

            return Ok(entity == null ? DefaultLandingPage() : Map(entity));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { ex.Message } });
        }
    }

    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [HttpPut("admin")]
    public async Task<IActionResult> Save([FromBody] LandingPageContentDto request)
    {
        try
        {
            var entity = await _db.LandingPageContents.FirstOrDefaultAsync(item => item.Key == DefaultKey, HttpContext.RequestAborted);
            if (entity == null)
            {
                entity = new LandingPageContent
                {
                    Id = Guid.NewGuid(),
                    Key = DefaultKey,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.LandingPageContents.Add(entity);
            }

            entity.IsHeroMediaVisible = request.IsHeroMediaVisible;
            entity.BrandName = Clean(request.BrandName, "medicSocial");
            entity.BrandTagline = Clean(request.BrandTagline, "Healthcare hiring");
            entity.BadgeText = request.BadgeText.Trim();
            entity.Headline = request.Headline.Trim();
            entity.HighlightText = request.HighlightText.Trim();
            entity.Subheading = request.Subheading.Trim();
            entity.PrimaryCallToActionText = request.PrimaryCallToActionText.Trim();
            entity.PrimaryCallToActionUrl = request.PrimaryCallToActionUrl.Trim();
            entity.SecondaryCallToActionText = request.SecondaryCallToActionText.Trim();
            entity.SecondaryCallToActionUrl = request.SecondaryCallToActionUrl.Trim();
            entity.HeroSlidesJson = JsonSerializer.Serialize(request.HeroSlides ?? [], JsonOptions);
            entity.FeatureCardsJson = JsonSerializer.Serialize(request.FeatureCards ?? [], JsonOptions);
            entity.EmployerCalloutTitle = request.EmployerCalloutTitle.Trim();
            entity.EmployerCalloutBody = request.EmployerCalloutBody.Trim();
            entity.JourneySectionTitle = Clean(request.JourneySectionTitle, "One platform. Two clear paths.");
            entity.JourneySectionBody = Clean(request.JourneySectionBody, "Create a free account to connect with the people and opportunities that move healthcare forward.");
            entity.ProfessionalJourneyTitle = Clean(request.ProfessionalJourneyTitle, "For healthcare professionals");
            entity.ProfessionalJourneyBody = Clean(request.ProfessionalJourneyBody, "Build one trusted profile, discover suitable roles, and connect directly with potential employers.");
            entity.EmployerJourneyTitle = Clean(request.EmployerJourneyTitle, "For employers");
            entity.EmployerJourneyBody = Clean(request.EmployerJourneyBody, "Grow a searchable talent pool, publish opportunities, and reach healthcare professionals ready for their next role.");
            entity.FreeAccessTitle = Clean(request.FreeAccessTitle, "Start free in three simple steps");
            entity.FreeAccessBody = Clean(request.FreeAccessBody, "Choose your account type, create your free account, then complete your profile and start connecting.");
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

    private static LandingPageContentDto Map(LandingPageContent entity)
    {
        return new LandingPageContentDto(
            entity.IsHeroMediaVisible,
            string.IsNullOrWhiteSpace(entity.BrandName) ? "medicSocial" : entity.BrandName,
            string.IsNullOrWhiteSpace(entity.BrandTagline) ? "Healthcare hiring" : entity.BrandTagline,
            entity.BadgeText,
            entity.Headline,
            entity.HighlightText,
            entity.Subheading,
            entity.PrimaryCallToActionText,
            entity.PrimaryCallToActionUrl,
            entity.SecondaryCallToActionText,
            entity.SecondaryCallToActionUrl,
            DeserializeList<LandingHeroSlideDto>(entity.HeroSlidesJson),
            DeserializeList<LandingFeatureCardDto>(entity.FeatureCardsJson),
            entity.EmployerCalloutTitle,
            entity.EmployerCalloutBody,
            Clean(entity.JourneySectionTitle, "One platform. Two clear paths."),
            Clean(entity.JourneySectionBody, "Create a free account to connect with the people and opportunities that move healthcare forward."),
            Clean(entity.ProfessionalJourneyTitle, "For healthcare professionals"),
            Clean(entity.ProfessionalJourneyBody, "Build one trusted profile, discover suitable roles, and connect directly with potential employers."),
            Clean(entity.EmployerJourneyTitle, "For employers"),
            Clean(entity.EmployerJourneyBody, "Grow a searchable talent pool, publish opportunities, and reach healthcare professionals ready for their next role."),
            Clean(entity.FreeAccessTitle, "Start free in three simple steps"),
            Clean(entity.FreeAccessBody, "Choose your account type, create your free account, then complete your profile and start connecting."),
            entity.IsPublished,
            entity.UpdatedAt ?? entity.CreatedAt);
    }

    private static List<T> DeserializeList<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static LandingPageContentDto DefaultLandingPage()
    {
        return new LandingPageContentDto(
            true,
            "medicSocial",
            "Healthcare hiring",
            "Open roles available now",
            "The specialized home for healthcare careers.",
            "healthcare",
            "Connecting medical professionals, healthcare facilities, and hiring teams through verified profiles, configurable workflows, and a marketplace built for care work.",
            "Find roles",
            "/jobs",
            "Join network",
            "/register",
            [
                new LandingHeroSlideDto(
                    true,
                    "Verified hiring partners",
                    "Verified employer",
                    "We can review applications, documents, and candidate conversations in one calmer workspace.",
                    "medicSocial partner",
                    "https://images.unsplash.com/photo-1550831107-1553da8c8464?auto=format&fit=crop&w=1400&q=80",
                    "Healthcare professional with stethoscope in a clinical setting",
                    0),
                new LandingHeroSlideDto(
                    true,
                    "Application-ready professionals",
                    "Professional network",
                    "I can browse first, watch interesting roles, then apply once my profile and documents are ready.",
                    "Verified professional",
                    "https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=1400&q=80",
                    "Medical professional reviewing patient or career information",
                    1),
                new LandingHeroSlideDto(
                    true,
                    "A marketplace with conversation",
                    "Community feed",
                    "The Feed keeps hiring conversations, role signals, and healthcare career discussions visible.",
                    "Healthcare community",
                    "https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=1400&q=80",
                    "Healthcare team collaborating around a tablet",
                    2)
            ],
            [
                new LandingFeatureCardDto("Verified profiles", "Profiles, education, experience, documents, and verification status stay connected to applications.", true, 0),
                new LandingFeatureCardDto("Employer workspaces", "Facilities manage job posts, team access, applicants, candidate invites, and communication.", true, 1),
                new LandingFeatureCardDto("Feed and messages", "Registered users can post, join channels, request chats, and receive in-app notifications.", true, 2),
                new LandingFeatureCardDto("Admin-configured rules", "Subscriptions, pay-as-you-go, declarations, legal pages, document rules, and policies remain configurable.", true, 3)
            ],
            "Hiring for a medical facility? Find vetted talent with a clearer pipeline.",
            "Post openings, configure requirements, manage applicants, invite matching professionals, and keep communication inside the same workspace.",
            "One platform. Two clear paths.",
            "Create a free account to connect with the people and opportunities that move healthcare forward.",
            "For healthcare professionals",
            "Build one trusted profile, discover suitable roles, and connect directly with potential employers.",
            "For employers",
            "Grow a searchable talent pool, publish opportunities, and reach healthcare professionals ready for their next role.",
            "Start free in three simple steps",
            "Choose your account type, create your free account, then complete your profile and start connecting.",
            true,
            DateTime.UtcNow);
    }

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

public record LandingPageContentDto(
    bool IsHeroMediaVisible,
    string BrandName,
    string BrandTagline,
    string BadgeText,
    string Headline,
    string HighlightText,
    string Subheading,
    string PrimaryCallToActionText,
    string PrimaryCallToActionUrl,
    string SecondaryCallToActionText,
    string SecondaryCallToActionUrl,
    List<LandingHeroSlideDto> HeroSlides,
    List<LandingFeatureCardDto> FeatureCards,
    string EmployerCalloutTitle,
    string EmployerCalloutBody,
    string JourneySectionTitle,
    string JourneySectionBody,
    string ProfessionalJourneyTitle,
    string ProfessionalJourneyBody,
    string EmployerJourneyTitle,
    string EmployerJourneyBody,
    string FreeAccessTitle,
    string FreeAccessBody,
    bool IsPublished,
    DateTime UpdatedAt);

public record LandingHeroSlideDto(
    bool IsVisible,
    string Title,
    string Label,
    string Testimonial,
    string Author,
    string ImageUrl,
    string ImageAlt,
    int DisplayOrder);

public record LandingFeatureCardDto(
    string Title,
    string Body,
    bool IsVisible,
    int DisplayOrder);
