namespace Shared.Data.Entities;

public class LandingPageContent
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "default";
    public string BrandName { get; set; } = "medicSocial";
    public string BrandTagline { get; set; } = "Healthcare hiring";
    public bool IsHeroMediaVisible { get; set; } = true;
    public string BadgeText { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string HighlightText { get; set; } = string.Empty;
    public string Subheading { get; set; } = string.Empty;
    public string PrimaryCallToActionText { get; set; } = string.Empty;
    public string PrimaryCallToActionUrl { get; set; } = string.Empty;
    public string SecondaryCallToActionText { get; set; } = string.Empty;
    public string SecondaryCallToActionUrl { get; set; } = string.Empty;
    public string HeroSlidesJson { get; set; } = "[]";
    public string FeatureCardsJson { get; set; } = "[]";
    public string EmployerCalloutTitle { get; set; } = string.Empty;
    public string EmployerCalloutBody { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
