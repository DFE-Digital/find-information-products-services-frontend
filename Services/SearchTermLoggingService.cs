using System.Collections.Concurrent;
using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

/// <summary>
/// Represents a search result with document ID and title
/// </summary>
public class SearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Service for logging search terms to CMS with rate limiting and deduplication
/// to prevent DoS attacks and database bloat.
/// </summary>
public interface ISearchTermLoggingService
{
    /// <summary>
    /// Logs a search term asynchronously (non-blocking) with rate limiting and deduplication.
    /// </summary>
    /// <param name="searchTerm">The search term to log</param>
    /// <param name="resultCount">Number of results returned</param>
    /// <param name="results">List of search results with document ID and title</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    void LogSearchTerm(string? searchTerm, int resultCount, List<SearchResult>? results, string? ipAddress, string? userAgent);
}

public class SearchTermLoggingService : ISearchTermLoggingService
{
    private readonly CmsApiService _cmsApiService;
    private readonly ILogger<SearchTermLoggingService> _logger;
    private readonly IConfiguration _configuration;
    
    // In-memory cache for rate limiting: key = "ip:searchTerm", value = last log time
    private static readonly ConcurrentDictionary<string, DateTime> _rateLimitCache = new();
    private static readonly object _cleanupLock = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;
    
    // Configuration defaults
    private readonly int _minSearchTermLength;
    private readonly int _rateLimitSeconds;
    private readonly int _deduplicationWindowSeconds;
    private readonly int _maxSearchTermLength;

    public SearchTermLoggingService(
        CmsApiService cmsApiService,
        ILogger<SearchTermLoggingService> logger,
        IConfiguration configuration)
    {
        _cmsApiService = cmsApiService;
        _logger = logger;
        _configuration = configuration;
        
        // Load configuration with sensible defaults
        _minSearchTermLength = _configuration.GetValue<int>("SearchLogging:MinSearchTermLength", 2);
        _rateLimitSeconds = _configuration.GetValue<int>("SearchLogging:RateLimitSeconds", 5);
        _deduplicationWindowSeconds = _configuration.GetValue<int>("SearchLogging:DeduplicationWindowSeconds", 60);
        _maxSearchTermLength = _configuration.GetValue<int>("SearchLogging:MaxSearchTermLength", 200);
    }

    public void LogSearchTerm(string? searchTerm, int resultCount, List<SearchResult>? results, string? ipAddress, string? userAgent)
    {
        // Fire and forget - don't block the main request
        _ = Task.Run(async () =>
        {
            try
            {
                await LogSearchTermAsync(searchTerm, resultCount, results, ipAddress, userAgent);
            }
            catch (Exception ex)
            {
                // Log but don't throw - this is non-critical
                _logger.LogWarning(ex, "Failed to log search term: {SearchTerm}", searchTerm);
            }
        });
    }

    private async Task LogSearchTermAsync(string? searchTerm, int resultCount, List<SearchResult>? results, string? ipAddress, string? userAgent)
    {
        // Validate search term
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return; // Don't log empty searches
        }

        var trimmedTerm = searchTerm.Trim();
        
        // Filter out very short or very long terms (likely junk)
        if (trimmedTerm.Length < _minSearchTermLength || trimmedTerm.Length > _maxSearchTermLength)
        {
            _logger.LogDebug("Skipping search term logging: length {Length} outside allowed range", trimmedTerm.Length);
            return;
        }

        // Normalize IP address (use "Unknown" if not provided)
        var normalizedIp = string.IsNullOrWhiteSpace(ipAddress) ? "Unknown" : ipAddress.Trim();
        
        // Create rate limit key: IP + normalized search term (case-insensitive)
        var rateLimitKey = $"{normalizedIp}:{trimmedTerm.ToLowerInvariant()}";
        
        // Check rate limiting: prevent same IP+term from being logged too frequently
        var now = DateTime.UtcNow;
        if (_rateLimitCache.TryGetValue(rateLimitKey, out var lastLogTime))
        {
            var timeSinceLastLog = (now - lastLogTime).TotalSeconds;
            if (timeSinceLastLog < _rateLimitSeconds)
            {
                _logger.LogDebug("Rate limit hit for search term: {SearchTerm} from IP: {IpAddress} (last logged {Seconds} seconds ago)", 
                    trimmedTerm, normalizedIp, (int)timeSinceLastLog);
                return; // Too soon, skip logging
            }
        }

        // Check deduplication: prevent same term from being logged multiple times in quick succession
        // (even from different IPs, to prevent coordinated attacks)
        var deduplicationKey = trimmedTerm.ToLowerInvariant();
        if (_rateLimitCache.TryGetValue(deduplicationKey, out var lastGlobalLogTime))
        {
            var timeSinceLastGlobalLog = (now - lastGlobalLogTime).TotalSeconds;
            if (timeSinceLastGlobalLog < _deduplicationWindowSeconds)
            {
                _logger.LogDebug("Deduplication hit for search term: {SearchTerm} (last logged globally {Seconds} seconds ago)", 
                    trimmedTerm, (int)timeSinceLastGlobalLog);
                return; // Same term logged recently globally, skip
            }
        }

        // Cleanup old entries periodically (every 5 minutes)
        CleanupOldEntries(now);

        // Update rate limit cache
        _rateLimitCache.AddOrUpdate(rateLimitKey, now, (key, oldValue) => now);
        _rateLimitCache.AddOrUpdate(deduplicationKey, now, (key, oldValue) => now);

        // Prepare results as JSON array (limit to first 100 to avoid bloat)
        // Format: [{"documentId": "xxx", "title": "Product Name"}, ...]
        object? resultsJson = null;
        if (results != null && results.Count > 0)
        {
            var limitedResults = results
                .Where(r => !string.IsNullOrEmpty(r.DocumentId)) // Only include results with valid document IDs
                .Take(100) // Limit to first 100 results
                .Select(r => new { documentId = r.DocumentId, title = r.Title ?? string.Empty })
                .ToList();
            resultsJson = limitedResults;
        }

        // Log to CMS
        var searchTermData = new
        {
            data = new
            {
                search_term = trimmedTerm,
                result_count = resultCount,
                results = resultsJson,
                ip_address = normalizedIp,
                user_agent = userAgent?.Trim() ?? "Unknown",
                timestamp = now
            }
        };

        try
        {
            await _cmsApiService.PostAsync<object>("search-terms", searchTermData);
            _logger.LogDebug("Logged search term: {SearchTerm} with {ResultCount} results", trimmedTerm, resultCount);
        }
        catch (Exception ex)
        {
            // Remove from cache on failure so it can be retried
            _rateLimitCache.TryRemove(rateLimitKey, out _);
            _rateLimitCache.TryRemove(deduplicationKey, out _);
            throw;
        }
    }

    private void CleanupOldEntries(DateTime now)
    {
        // Only cleanup every 5 minutes to avoid overhead
        if ((now - _lastCleanup).TotalMinutes < 5)
        {
            return;
        }

        lock (_cleanupLock)
        {
            // Double-check after acquiring lock
            if ((now - _lastCleanup).TotalMinutes < 5)
            {
                return;
            }

            var cutoffTime = now.AddSeconds(-Math.Max(_rateLimitSeconds, _deduplicationWindowSeconds) * 2);
            var keysToRemove = _rateLimitCache
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _rateLimitCache.TryRemove(key, out _);
            }

            _lastCleanup = now;
            _logger.LogDebug("Cleaned up {Count} old rate limit entries", keysToRemove.Count);
        }
    }
}
