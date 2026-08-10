namespace Shared.Data.Entities;

public class PlatformFeatureConfig
{
    public Guid Id { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? DisabledMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
