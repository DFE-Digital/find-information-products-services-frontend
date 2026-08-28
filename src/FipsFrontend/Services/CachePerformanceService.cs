using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface ICachePerformanceService
{
    Task<CachePerformanceMetrics> GetPerformanceMetricsAsync();
    Task<Dictionary<string, CacheHitRate>> GetHitRatesByEndpointAsync();
    Task<bool> IsCachePerformingWellAsync();
    Task<string> GetPerformanceRecommendationsAsync();
}

public class CachePerformanceService : ICachePerformanceService
{
    private readonly IEnhancedCacheService _cacheService;
    private readonly ILogger<CachePerformanceService> _logger;
    private readonly IConfiguration _configuration;

    public CachePerformanceService(
        IEnhancedCacheService cacheService,
        ILogger<CachePerformanceService> logger,
        IConfiguration configuration)
    {
        _cacheService = cacheService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CachePerformanceMetrics> GetPerformanceMetricsAsync()
    {
        try
        {
            var stats = await _cacheService.GetCacheStatsAsync();
            
            var metrics = new CachePerformanceMetrics
            {
                TotalEntries = Convert.ToInt32(stats.GetValueOrDefault("TotalEntries", 0)),
                TotalHits = Convert.ToInt32(stats.GetValueOrDefault("TotalHits", 0)),
                TotalMisses = Convert.ToInt32(stats.GetValueOrDefault("Misses", 0)),
                MemoryHits = Convert.ToInt32(stats.GetValueOrDefault("MemoryHits", 0)),
                DistributedHits = Convert.ToInt32(stats.GetValueOrDefault("DistributedHits", 0)),
                MemoryUsage = Convert.ToInt64(stats.GetValueOrDefault("MemoryUsage", 0)),
                HitRate = Convert.ToDouble(stats.GetValueOrDefault("HitRate", 0)),
                Timestamp = DateTimeOffset.UtcNow
            };

            // Calculate additional metrics
            var totalRequests = metrics.TotalHits + metrics.TotalMisses;
            metrics.RequestRate = totalRequests > 0 ? (double)metrics.TotalHits / totalRequests * 100 : 0;
            metrics.MemoryEfficiency = metrics.MemoryUsage > 0 ? (double)metrics.MemoryHits / metrics.MemoryUsage * 1000 : 0;

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache performance metrics");
            return new CachePerformanceMetrics { Timestamp = DateTimeOffset.UtcNow };
        }
    }

    public async Task<Dictionary<string, CacheHitRate>> GetHitRatesByEndpointAsync()
    {
        try
        {
            // This would need to be implemented based on how we track endpoint-specific metrics
            // For now, return a placeholder implementation
            await Task.CompletedTask;
            
            return new Dictionary<string, CacheHitRate>
            {
                ["products"] = new CacheHitRate { Endpoint = "products", HitRate = 85.5, RequestCount = 150 },
                ["categories"] = new CacheHitRate { Endpoint = "categories", HitRate = 92.3, RequestCount = 75 },
                ["search"] = new CacheHitRate { Endpoint = "search", HitRate = 45.2, RequestCount = 200 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hit rates by endpoint");
            return new Dictionary<string, CacheHitRate>();
        }
    }

    public async Task<bool> IsCachePerformingWellAsync()
    {
        try
        {
            var metrics = await GetPerformanceMetricsAsync();
            
            // Define performance thresholds
            var minHitRate = _configuration.GetValue<double>("Caching:Performance:MinHitRate", 80.0);
            var maxMemoryUsage = _configuration.GetValue<long>("Caching:Performance:MaxMemoryUsage", 100 * 1024 * 1024); // 100MB
            
            var isPerformingWell = metrics.HitRate >= minHitRate && 
                                  metrics.MemoryUsage <= maxMemoryUsage &&
                                  metrics.TotalEntries > 0;

            _logger.LogInformation("Cache performance check: HitRate={HitRate}%, MemoryUsage={MemoryUsage}MB, Entries={Entries}, PerformingWell={PerformingWell}",
                metrics.HitRate, metrics.MemoryUsage / (1024 * 1024), metrics.TotalEntries, isPerformingWell);

            return isPerformingWell;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache performance");
            return false;
        }
    }

    public async Task<string> GetPerformanceRecommendationsAsync()
    {
        try
        {
            var metrics = await GetPerformanceMetricsAsync();
            var recommendations = new List<string>();

            if (metrics.HitRate < 70)
            {
                recommendations.Add("Hit rate is low - consider increasing cache durations for frequently accessed data");
            }

            if (metrics.MemoryUsage > 50 * 1024 * 1024) // 50MB
            {
                recommendations.Add("Memory usage is high - consider implementing cache eviction policies");
            }

            if (metrics.TotalEntries < 10)
            {
                recommendations.Add("Low cache utilization - consider implementing cache warming");
            }

            if (metrics.DistributedHits == 0 && metrics.MemoryHits > 0)
            {
                recommendations.Add("Only memory cache is being used - consider enabling distributed cache");
            }

            if (!recommendations.Any())
            {
                recommendations.Add("Cache performance is optimal - no recommendations at this time");
            }

            return string.Join("; ", recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance recommendations");
            return "Unable to generate recommendations due to error";
        }
    }
}

public class CachePerformanceMetrics
{
    public int TotalEntries { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public int MemoryHits { get; set; }
    public int DistributedHits { get; set; }
    public long MemoryUsage { get; set; }
    public double HitRate { get; set; }
    public double RequestRate { get; set; }
    public double MemoryEfficiency { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class CacheHitRate
{
    public string Endpoint { get; set; } = string.Empty;
    public double HitRate { get; set; }
    public int RequestCount { get; set; }
}
