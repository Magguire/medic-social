using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Data.Entities;

namespace Shared.Features;

public interface IPlatformFeatureService
{
    Task<bool> IsEnabledAsync(string featureKey, CancellationToken cancellationToken);
    Task<PlatformFeatureConfig> GetOrCreateAsync(string featureKey, string? defaultMessage, CancellationToken cancellationToken);
    Task<PlatformFeatureConfig> SetAsync(string featureKey, bool enabled, string? disabledMessage, CancellationToken cancellationToken);
}

public class PlatformFeatureService : IPlatformFeatureService
{
    private readonly ApplicationDbContext _db;

    public PlatformFeatureService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsEnabledAsync(string featureKey, CancellationToken cancellationToken)
    {
        var config = await GetOrCreateAsync(featureKey, null, cancellationToken);
        return config.IsEnabled;
    }

    public async Task<PlatformFeatureConfig> GetOrCreateAsync(string featureKey, string? defaultMessage, CancellationToken cancellationToken)
    {
        var config = await _db.PlatformFeatureConfigs.FirstOrDefaultAsync(f => f.FeatureKey == featureKey, cancellationToken);
        if (config != null)
        {
            return config;
        }

        config = new PlatformFeatureConfig
        {
            Id = Guid.NewGuid(),
            FeatureKey = featureKey,
            IsEnabled = true,
            DisabledMessage = defaultMessage,
            CreatedAt = DateTime.UtcNow
        };
        _db.PlatformFeatureConfigs.Add(config);
        await _db.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task<PlatformFeatureConfig> SetAsync(string featureKey, bool enabled, string? disabledMessage, CancellationToken cancellationToken)
    {
        var config = await GetOrCreateAsync(featureKey, disabledMessage, cancellationToken);
        config.IsEnabled = enabled;
        config.DisabledMessage = disabledMessage;
        config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return config;
    }
}
