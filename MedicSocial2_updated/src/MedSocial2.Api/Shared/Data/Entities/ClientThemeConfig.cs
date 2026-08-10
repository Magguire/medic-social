namespace Shared.Data.Entities;

public class ClientThemeConfig
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "default";
    public string PrimaryColor { get; set; } = "#607f75";
    public string SecondaryColor { get; set; } = "#111827";
    public string AccentColor { get; set; } = "#b66a3c";
    public string BackgroundColor { get; set; } = "#fbf7ef";
    public string SurfaceColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#111827";
    public string MutedTextColor { get; set; } = "#667085";
    public string DarkBackgroundColor { get; set; } = "#111820";
    public string DarkSurfaceColor { get; set; } = "#1d2a31";
    public string DarkTextColor { get; set; } = "#f7f2ea";
    public string DarkMutedTextColor { get; set; } = "#c1c8c4";
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
