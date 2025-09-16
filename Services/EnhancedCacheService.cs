using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FipsFrontend.Services;

public interface ICacheConfigurationService
{
    TimeSpan GetCacheDuration(string cacheKey);
    bool IsCacheEnabled(string cacheKey);
    string GetCacheKey(string baseKey, params string[] parameters);
    CacheStrategy GetCacheStrategy(string cacheKey);
}

public enum CacheStrategy
{
    MemoryOnly,
    DistributedOnly,
    MemoryWithDistributedFallback,
    NoCache
}

public class CacheConfigurationService : ICacheConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CacheConfigurationService> _logger;
    private readonly Dictionary<string, CacheConfig> _cacheConfigs;

    public CacheConfigurationService(IConfiguration configuration, ILogger<CacheConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _cacheConfigs = InitializeCacheConfigs();
    }

    public TimeSpan GetCacheDuration(string cacheKey)
    {
        var config = GetCacheConfig(cacheKey);
        return config.Duration;
    }

    public bool IsCacheEnabled(string cacheKey)
    {
        var config = GetCacheConfig(cacheKey);
        return config.Enabled;
    }

    public string GetCacheKey(string baseKey, params string[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return baseKey;

        var paramString = string.Join("_", parameters.Select(p => p?.ToString() ?? "null"));
        return $"{baseKey}_{paramString}";
    }

    public CacheStrategy GetCacheStrategy(string cacheKey)
    {
        var config = GetCacheConfig(cacheKey);
        return config.Strategy;
    }

    private CacheConfig GetCacheConfig(string cacheKey)
    {
        // Try exact match first
        if (_cacheConfigs.TryGetValue(cacheKey, out var exactConfig))
            return exactConfig;

        // Try pattern matching
        foreach (var kvp in _cacheConfigs)
        {
            if (cacheKey.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        // Return default configuration
        return new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(5),
            Enabled = true,
            Strategy = CacheStrategy.MemoryOnly
        };
    }

    private Dictionary<string, CacheConfig> InitializeCacheConfigs()
    {
        var configs = new Dictionary<string, CacheConfig>(StringComparer.OrdinalIgnoreCase);

        // Get cache durations from configuration
        var durationsSection = _configuration.GetSection("Caching:Durations");
        var defaultDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:DefaultDurationMinutes", 5));

        // Home page caching
        configs["home"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("Home", 5)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        // Product caching
        configs["products"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("Products", 5)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        configs["product_"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("ProductDetail", 10)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        // Category caching
        configs["category-types"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("CategoryTypes", 15)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        configs["category-values"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("CategoryValues", 15)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        // Search caching
        configs["search"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("Search", 2)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        // Static content caching
        configs["static"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("Static", 60)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        // Admin operations - no caching
        configs["admin"] = new CacheConfig
        {
            Duration = TimeSpan.Zero,
            Enabled = false,
            Strategy = CacheStrategy.NoCache
        };

        // Health checks - short cache
        configs["health"] = new CacheConfig
        {
            Duration = TimeSpan.FromMinutes(durationsSection.GetValue<double>("Health", 0.5)),
            Enabled = true,
            Strategy = CacheStrategy.MemoryWithDistributedFallback
        };

        return configs;
    }

    private class CacheConfig
    {
        public TimeSpan Duration { get; set; }
        public bool Enabled { get; set; }
        public CacheStrategy Strategy { get; set; }
    }
}

public interface IEnhancedCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? duration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task<long> GetMemoryUsageAsync();
    Task<Dictionary<string, object>> GetCacheStatsAsync();
}

public class EnhancedCacheService : IEnhancedCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ICacheConfigurationService _configService;
    private readonly ILogger<EnhancedCacheService> _logger;
    private readonly Dictionary<string, CacheEntryInfo> _cacheTracking;

    public EnhancedCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ICacheConfigurationService configService,
        ILogger<EnhancedCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _configService = configService;
        _logger = logger;
        _cacheTracking = new Dictionary<string, CacheEntryInfo>();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var cacheKey = _configService.GetCacheKey(key);
        
        if (!_configService.IsCacheEnabled(cacheKey))
            return default;

        var strategy = _configService.GetCacheStrategy(cacheKey);

        try
        {
            // Try memory cache first for most strategies
            if (strategy == CacheStrategy.MemoryOnly || strategy == CacheStrategy.MemoryWithDistributedFallback)
            {
                if (_memoryCache.TryGetValue(cacheKey, out T? memoryValue))
                {
                    UpdateCacheStats(cacheKey, true);
                    _logger.LogDebug("Cache hit (memory): {Key}", cacheKey);
                    return memoryValue;
                }
            }

            // Try distributed cache
            if (strategy == CacheStrategy.DistributedOnly || strategy == CacheStrategy.MemoryWithDistributedFallback)
            {
                var distributedValue = await GetFromDistributedCache<T>(cacheKey);
                if (distributedValue != null)
                {
                    // Store in memory cache for faster access
                    if (strategy == CacheStrategy.MemoryWithDistributedFallback)
                    {
                        var duration = _configService.GetCacheDuration(cacheKey);
                        _memoryCache.Set(cacheKey, distributedValue, duration);
                    }
                    
                    UpdateCacheStats(cacheKey, true);
                    _logger.LogDebug("Cache hit (distributed): {Key}", cacheKey);
                    return distributedValue;
                }
            }

            UpdateCacheStats(cacheKey, false);
            _logger.LogDebug("Cache miss: {Key}", cacheKey);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", cacheKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? duration = null)
    {
        var cacheKey = _configService.GetCacheKey(key);
        
        if (!_configService.IsCacheEnabled(cacheKey))
            return;

        var strategy = _configService.GetCacheStrategy(cacheKey);
        var cacheDuration = duration ?? _configService.GetCacheDuration(cacheKey);

        try
        {
            // Store in memory cache
            if (strategy == CacheStrategy.MemoryOnly || strategy == CacheStrategy.MemoryWithDistributedFallback)
            {
                _memoryCache.Set(cacheKey, value, cacheDuration);
                _logger.LogDebug("Cached in memory: {Key}", cacheKey);
            }

            // Store in distributed cache
            if (strategy == CacheStrategy.DistributedOnly || strategy == CacheStrategy.MemoryWithDistributedFallback)
            {
                await SetInDistributedCache(cacheKey, value, cacheDuration);
                _logger.LogDebug("Cached in distributed: {Key}", cacheKey);
            }

            TrackCacheEntry(cacheKey, cacheDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", cacheKey);
        }
    }

    public async Task RemoveAsync(string key)
    {
        var cacheKey = _configService.GetCacheKey(key);
        
        try
        {
            _memoryCache.Remove(cacheKey);
            await _distributedCache.RemoveAsync(cacheKey);
            _cacheTracking.Remove(cacheKey);
            
            _logger.LogDebug("Removed from cache: {Key}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", cacheKey);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var keysToRemove = _cacheTracking.Keys
                .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }

            _logger.LogInformation("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var cacheKey = _configService.GetCacheKey(key);
        
        if (_memoryCache.TryGetValue(cacheKey, out _))
            return true;

        try
        {
            var distributedValue = await _distributedCache.GetStringAsync(cacheKey);
            return distributedValue != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> GetMemoryUsageAsync()
    {
        // This is a simplified implementation
        // In a real scenario, you might want to use more sophisticated memory tracking
        await Task.CompletedTask; // Remove async warning
        return _cacheTracking.Count * 1024; // Rough estimate
    }

    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalEntries"] = _cacheTracking.Count,
            ["MemoryHits"] = _cacheTracking.Values.Sum(v => v.MemoryHits),
            ["DistributedHits"] = _cacheTracking.Values.Sum(v => v.DistributedHits),
            ["Misses"] = _cacheTracking.Values.Sum(v => v.Misses),
            ["TotalHits"] = _cacheTracking.Values.Sum(v => v.MemoryHits + v.DistributedHits),
            ["HitRate"] = CalculateHitRate(),
            ["MemoryUsage"] = await GetMemoryUsageAsync()
        };

        return stats;
    }

    private async Task<T?> GetFromDistributedCache<T>(string key)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key);
            if (value == null)
                return default;

            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing from distributed cache: {Key}", key);
            return default;
        }
    }

    private async Task SetInDistributedCache<T>(string key, T value, TimeSpan duration)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing to distributed cache: {Key}", key);
        }
    }

    private void TrackCacheEntry(string key, TimeSpan duration)
    {
        _cacheTracking[key] = new CacheEntryInfo
        {
            Key = key,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(duration),
            Duration = duration
        };
    }

    private void UpdateCacheStats(string key, bool isHit)
    {
        if (_cacheTracking.TryGetValue(key, out var entry))
        {
            if (isHit)
                entry.MemoryHits++;
            else
                entry.Misses++;
        }
    }

    private double CalculateHitRate()
    {
        var totalRequests = _cacheTracking.Values.Sum(v => v.MemoryHits + v.DistributedHits + v.Misses);
        if (totalRequests == 0)
            return 0;

        var totalHits = _cacheTracking.Values.Sum(v => v.MemoryHits + v.DistributedHits);
        return (double)totalHits / totalRequests * 100;
    }

    private class CacheEntryInfo
    {
        public string Key { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public TimeSpan Duration { get; set; }
        public int MemoryHits { get; set; }
        public int DistributedHits { get; set; }
        public int Misses { get; set; }
    }
}
