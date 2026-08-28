using System.Diagnostics;
using System.Text.Json;

namespace FipsFrontend.Services;

public interface IPerformanceAnalysisService
{
    Task<PerformanceReport> GeneratePerformanceReportAsync();
    Task<List<SlowEndpoint>> GetSlowEndpointsAsync(int thresholdMs = 1000);
    Task<List<CachePerformanceIssue>> GetCachePerformanceIssuesAsync();
    Task<Dictionary<string, EndpointMetrics>> GetEndpointMetricsAsync();
}

public class PerformanceAnalysisService : IPerformanceAnalysisService
{
    private readonly ILogger<PerformanceAnalysisService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ICachePerformanceService _cachePerformanceService;

    public PerformanceAnalysisService(
        ILogger<PerformanceAnalysisService> logger,
        IWebHostEnvironment environment,
        ICachePerformanceService cachePerformanceService)
    {
        _logger = logger;
        _environment = environment;
        _cachePerformanceService = cachePerformanceService;
    }

    public async Task<PerformanceReport> GeneratePerformanceReportAsync()
    {
        var report = new PerformanceReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            CacheMetrics = await _cachePerformanceService.GetPerformanceMetricsAsync(),
            SlowEndpoints = await GetSlowEndpointsAsync(),
            CacheIssues = await GetCachePerformanceIssuesAsync(),
            EndpointMetrics = await GetEndpointMetricsAsync()
        };

        return report;
    }

    public async Task<List<SlowEndpoint>> GetSlowEndpointsAsync(int thresholdMs = 1000)
    {
        var slowEndpoints = new List<SlowEndpoint>();
        var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");

        if (!Directory.Exists(logDirectory))
            return slowEndpoints;

        try
        {
            var logFiles = Directory.GetFiles(logDirectory, "api-requests-*.log")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Take(7); // Last 7 days

            var endpointDurations = new Dictionary<string, List<double>>();

            foreach (var logFile in logFiles)
            {
                var lines = await File.ReadAllLinesAsync(logFile);
                foreach (var line in lines)
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<ApiLogEntry>(line);
                        if (logEntry?.Type == "RESPONSE" && logEntry.Duration.HasValue)
                        {
                            var endpoint = $"{logEntry.Controller}.{logEntry.Action}";
                            if (!endpointDurations.ContainsKey(endpoint))
                                endpointDurations[endpoint] = new List<double>();
                            
                            endpointDurations[endpoint].Add(logEntry.Duration.Value.TotalMilliseconds);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed log entries
                        continue;
                    }
                }
            }

            foreach (var kvp in endpointDurations)
            {
                var durations = kvp.Value;
                var avgDuration = durations.Average();
                var maxDuration = durations.Max();
                var minDuration = durations.Min();
                var p95Duration = durations.OrderBy(x => x).Skip((int)(durations.Count * 0.05)).FirstOrDefault();

                if (avgDuration > thresholdMs || maxDuration > thresholdMs * 2)
                {
                    slowEndpoints.Add(new SlowEndpoint
                    {
                        Endpoint = kvp.Key,
                        AverageDurationMs = avgDuration,
                        MaxDurationMs = maxDuration,
                        MinDurationMs = minDuration,
                        P95DurationMs = p95Duration,
                        RequestCount = durations.Count,
                        CacheHitRate = await GetCacheHitRateForEndpoint(kvp.Key)
                    });
                }
            }

            return slowEndpoints.OrderByDescending(e => e.AverageDurationMs).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing slow endpoints");
            return slowEndpoints;
        }
    }

    public async Task<List<CachePerformanceIssue>> GetCachePerformanceIssuesAsync()
    {
        var issues = new List<CachePerformanceIssue>();
        var metrics = await _cachePerformanceService.GetPerformanceMetricsAsync();

        if (metrics.HitRate < 70)
        {
            issues.Add(new CachePerformanceIssue
            {
                Type = "Low Hit Rate",
                Severity = metrics.HitRate < 50 ? "High" : "Medium",
                Description = $"Cache hit rate is {metrics.HitRate:F1}%, below recommended 70%",
                Recommendation = "Consider increasing cache durations or warming more data"
            });
        }

        if (metrics.MemoryUsage > 100 * 1024 * 1024) // 100MB
        {
            issues.Add(new CachePerformanceIssue
            {
                Type = "High Memory Usage",
                Severity = "Medium",
                Description = $"Cache memory usage is {(double)metrics.MemoryUsage / (1024 * 1024):F1}MB",
                Recommendation = "Consider reducing cache durations or implementing more aggressive eviction"
            });
        }

        if (metrics.TotalMisses > metrics.TotalHits * 2)
        {
            issues.Add(new CachePerformanceIssue
            {
                Type = "High Miss Rate",
                Severity = "Medium",
                Description = $"Cache misses ({metrics.TotalMisses}) significantly exceed hits ({metrics.TotalHits})",
                Recommendation = "Review cache key strategies and warming patterns"
            });
        }

        return issues;
    }

    public async Task<Dictionary<string, EndpointMetrics>> GetEndpointMetricsAsync()
    {
        var metrics = new Dictionary<string, EndpointMetrics>();
        var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");

        if (!Directory.Exists(logDirectory))
            return metrics;

        try
        {
            var logFiles = Directory.GetFiles(logDirectory, "api-requests-*.log")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Take(7);

            var endpointData = new Dictionary<string, List<(double duration, bool fromCache)>>();

            foreach (var logFile in logFiles)
            {
                var lines = await File.ReadAllLinesAsync(logFile);
                foreach (var line in lines)
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<ApiLogEntry>(line);
                        if (logEntry?.Type == "RESPONSE" && logEntry.Duration.HasValue)
                        {
                            var endpoint = $"{logEntry.Controller}.{logEntry.Action}";
                            if (!endpointData.ContainsKey(endpoint))
                                endpointData[endpoint] = new List<(double, bool)>();
                            
                            endpointData[endpoint].Add((logEntry.Duration.Value.TotalMilliseconds, logEntry.FromCache));
                        }
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                }
            }

            foreach (var kvp in endpointData)
            {
                var data = kvp.Value;
                var durations = data.Select(d => d.duration).ToList();
                var cacheHits = data.Count(d => d.fromCache);
                var totalRequests = data.Count;

                metrics[kvp.Key] = new EndpointMetrics
                {
                    Endpoint = kvp.Key,
                    RequestCount = totalRequests,
                    AverageDurationMs = durations.Average(),
                    MaxDurationMs = durations.Max(),
                    MinDurationMs = durations.Min(),
                    P95DurationMs = durations.OrderBy(x => x).Skip((int)(durations.Count * 0.05)).FirstOrDefault(),
                    CacheHitRate = totalRequests > 0 ? (double)cacheHits / totalRequests * 100 : 0,
                    CacheHitCount = cacheHits,
                    CacheMissCount = totalRequests - cacheHits
                };
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating endpoint metrics");
            return metrics;
        }
    }

    private Task<double> GetCacheHitRateForEndpoint(string endpoint)
    {
        // This would need to be implemented based on your specific logging structure
        // For now, return a placeholder
        return Task.FromResult(0.0);
    }
}

public class PerformanceReport
{
    public DateTimeOffset GeneratedAt { get; set; }
    public CachePerformanceMetrics CacheMetrics { get; set; } = new();
    public List<SlowEndpoint> SlowEndpoints { get; set; } = new();
    public List<CachePerformanceIssue> CacheIssues { get; set; } = new();
    public Dictionary<string, EndpointMetrics> EndpointMetrics { get; set; } = new();
}

public class SlowEndpoint
{
    public string Endpoint { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public int RequestCount { get; set; }
    public double CacheHitRate { get; set; }
}

public class CachePerformanceIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class EndpointMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public double AverageDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double CacheHitRate { get; set; }
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
}
