using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public class CmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsApiService> _logger;
    private readonly string _baseUrl;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, CacheItemInfo> _cacheTracking;
    private readonly object _cacheTrackingLock = new();

    public CmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<CmsApiService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _cacheTracking = new Dictionary<string, CacheItemInfo>();
        _baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
    }

    public async Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            
            // Create cache key based on endpoint
            var cacheKey = $"cms_api_{CreateCacheKey(endpoint)}";
            
            // Check cache first for GET requests (if cache duration is specified)
            if (cacheDuration.HasValue && _cache.TryGetValue(cacheKey, out T? cachedResult))
            {
                _logger.LogInformation("Cache hit for endpoint: {Endpoint} with key: {Key}", endpoint, cacheKey);
                // Update last accessed time in tracking
                lock (_cacheTrackingLock)
                {
                    if (_cacheTracking.ContainsKey(cacheKey))
                    {
                        _cacheTracking[cacheKey].LastAccessed = DateTimeOffset.UtcNow;
                        _cacheTracking[cacheKey].HitCount++;
                        _logger.LogInformation("Updated cache tracking for key: {Key}, hits: {Hits}", cacheKey, _cacheTracking[cacheKey].HitCount);
                    }
                    else
                    {
                        _logger.LogWarning("Cache hit but no tracking found for key: {Key}", cacheKey);
                    }
                }
                return cachedResult;
            }
            
            // Use read API key for GET requests
            var readApiKey = _configuration["CmsApi:ReadApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", readApiKey);
            
            var response = await _httpClient.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && result != null)
            {
                var expirationTime = DateTimeOffset.UtcNow.Add(cacheDuration.Value);
                
                // Track cache entry first with lock
                lock (_cacheTrackingLock)
                {
                    _cacheTracking[cacheKey] = new CacheItemInfo
                    {
                        Endpoint = endpoint,
                        Key = cacheKey,
                        CreatedAt = DateTimeOffset.UtcNow,
                        LastAccessed = DateTimeOffset.UtcNow,
                        ExpiresAt = expirationTime,
                        Duration = cacheDuration.Value,
                        HitCount = 0,
                        Size = EstimateObjectSize(result)
                    };
                }
                
                // Set cache with callback to track expiration
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheDuration.Value
                };
                
                cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var keyStr = key.ToString() ?? string.Empty;
                    _logger.LogInformation("Cache entry evicted: {Key}, reason: {Reason}, removing from tracking", keyStr, reason);
                    lock (_cacheTrackingLock)
                    {
                        _cacheTracking.Remove(keyStr);
                    }
                });
                
                _cache.Set(cacheKey, result, cacheEntryOptions);
                
                _logger.LogInformation("Cached result for endpoint: {Endpoint} with key: {Key} and duration: {Duration}", endpoint, cacheKey, cacheDuration.Value);
                _logger.LogInformation("Cache tracking now has {Count} entries", _cacheTracking.Count);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Use write API key for POST requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.PostAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Use write API key for PUT requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.PutAsync(fullUrl, content);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            
            // Use write API key for DELETE requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.DeleteAsync(fullUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API endpoint: {Endpoint}", endpoint);
            return false;
        }
    }

    private string CreateCacheKey(string endpoint)
    {
        // Create a hash of the endpoint for consistent cache keys
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(endpoint));
        return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }

    public void ClearCache()
    {
        // Clear all CMS API cache entries
        var keysToRemove = _cacheTracking.Keys.ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
        _cacheTracking.Clear();
        _logger.LogInformation("CMS API cache cleared - {Count} entries removed", keysToRemove.Count);
    }

    public void ClearCacheForEndpoint(string endpoint)
    {
        var cacheKey = $"cms_api_{CreateCacheKey(endpoint)}";
        _cache.Remove(cacheKey);
        _cacheTracking.Remove(cacheKey);
        _logger.LogInformation("CMS API cache cleared for endpoint: {Endpoint}", endpoint);
    }

    public Dictionary<string, CacheItemInfo> GetCacheInfo()
    {
        lock (_cacheTrackingLock)
        {
            _logger.LogInformation("GetCacheInfo called - tracking has {Count} entries", _cacheTracking.Count);
            
            // Clean up expired entries from tracking
            CleanupExpiredEntries();
            
            _logger.LogInformation("After cleanup - tracking has {Count} entries", _cacheTracking.Count);
            
            foreach (var entry in _cacheTracking)
            {
                _logger.LogInformation("Cache entry: {Key} -> {Endpoint}, Expires: {ExpiresAt}", 
                    entry.Key, entry.Value.Endpoint, entry.Value.ExpiresAt);
            }
            
            return new Dictionary<string, CacheItemInfo>(_cacheTracking);
        }
    }

    private void CleanupExpiredEntries()
    {
        var expiredKeys = _cacheTracking
            .Where(kvp => kvp.Value.ExpiresAt <= DateTimeOffset.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cacheTracking.Remove(key);
        }
    }

    private string EstimateObjectSize(object obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj);
            return $"{json.Length} bytes";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Specialized methods for category data with individual caching
    public async Task<List<CategoryValue>?> GetCategoryValuesByType(string categoryTypeName, TimeSpan? cacheDuration = null)
    {
        var endpoint = $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&populate[category_type]=true&populate[parent]=true&populate[children]=true&pagination[pageSize]=10000";
        var response = await GetAsync<ApiCollectionResponse<CategoryValue>>(endpoint, cacheDuration);
        return response?.Data;
    }

    public async Task<List<CategoryValue>?> GetAllCategoryValues(TimeSpan? cacheDuration = null)
    {
        var allResults = new List<CategoryValue>();
        var page = 1;
        var pageSize = 100; // Strapi 5 limit
        var hasMorePages = true;
        
        _logger.LogInformation("Getting all category values (with pagination)");
        
        while (hasMorePages)
        {
            var endpoint = $"category-values?populate[category_type]=true&populate[parent]=true&populate[children]=true&pagination[page]={page}&pagination[pageSize]={pageSize}";
            _logger.LogInformation("Fetching page {Page} for all category values", page);
            var response = await GetAsync<ApiCollectionResponse<CategoryValue>>(endpoint, cacheDuration);
            
            if (response?.Data != null && response.Data.Any())
            {
                allResults.AddRange(response.Data);
                _logger.LogInformation("Page {Page}: Got {Count} items (Total so far: {Total})", page, response.Data.Count, allResults.Count);
                
                // Check if there are more pages
                if (response.Meta?.Pagination != null)
                {
                    hasMorePages = page < response.Meta.Pagination.PageCount;
                    _logger.LogInformation("Pagination info - Page: {Page}/{PageCount}, Total: {Total}", 
                        page, response.Meta.Pagination.PageCount, response.Meta.Pagination.Total);
                }
                else
                {
                    // If no pagination info, assume we're done if we got less than pageSize
                    hasMorePages = response.Data.Count == pageSize;
                }
                
                page++;
            }
            else
            {
                hasMorePages = false;
            }
        }
        
        _logger.LogInformation("Found {Count} total category values (across {Pages} pages)", allResults.Count, page - 1);
        
        // Log channel values specifically for debugging
        var channelValues = allResults.Where(cv => cv.CategoryType?.Name?.Equals("Channel", StringComparison.OrdinalIgnoreCase) == true).ToList();
        _logger.LogInformation("Channel values in GetAllCategoryValues: {Count}", channelValues.Count);
        foreach (var cv in channelValues)
        {
            _logger.LogInformation("Channel value: {Name} (Slug: {Slug}, Enabled: {Enabled}, Published: {Published})", 
                cv.Name, cv.Slug, cv.Enabled, cv.PublishedAt.HasValue);
        }
        
        return allResults;
    }

    public async Task<List<CategoryType>?> GetAllCategoryTypes(TimeSpan? cacheDuration = null)
    {
        var endpoint = "category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&populate=values&sort=sort_order:asc&pagination[pageSize]=1000";
        _logger.LogInformation("Getting all category types from endpoint: {Endpoint}", endpoint);
        var response = await GetAsync<ApiCollectionResponse<CategoryType>>(endpoint, cacheDuration);
        _logger.LogInformation("Category types response: {Count} items", response?.Data?.Count ?? 0);
        
        // Debug: Log all category type names
        if (response?.Data != null)
        {
            foreach (var ct in response.Data)
            {
                _logger.LogInformation("Category type: {Name}", ct.Name);
            }
        }
        
        return response?.Data;
    }

    public async Task<List<CategoryValue>?> GetCategoryValuesForFilter(string categoryTypeName, TimeSpan? cacheDuration = null)
    {
        var allResults = new List<CategoryValue>();
        var page = 1;
        var pageSize = 100; // Strapi 5 limit
        var hasMorePages = true;
        
        _logger.LogInformation("Getting category values for filter - type: {Type} (with pagination)", categoryTypeName);
        
        while (hasMorePages)
        {
            // For User group category type, we need to populate parent and children relationships
            var endpoint = (categoryTypeName.Equals("User group", StringComparison.OrdinalIgnoreCase) ||
                           categoryTypeName.Equals("User Group", StringComparison.OrdinalIgnoreCase) ||
                           categoryTypeName.Equals("Audience", StringComparison.OrdinalIgnoreCase))
                ? $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&sort=sort_order:asc&populate[parent]=true&populate[children]=true&pagination[page]={page}&pagination[pageSize]={pageSize}"
                : $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&sort=sort_order:asc&fields[0]=name&fields[1]=slug&pagination[page]={page}&pagination[pageSize]={pageSize}";
            
            _logger.LogInformation("Fetching page {Page} for category type '{Type}'", page, categoryTypeName);
            var response = await GetAsync<ApiCollectionResponse<CategoryValue>>(endpoint, cacheDuration);
            
            if (response?.Data != null && response.Data.Any())
            {
                allResults.AddRange(response.Data);
                _logger.LogInformation("Page {Page}: Got {Count} items (Total so far: {Total})", page, response.Data.Count, allResults.Count);
                
                // Check if there are more pages
                if (response.Meta?.Pagination != null)
                {
                    hasMorePages = page < response.Meta.Pagination.PageCount;
                    _logger.LogInformation("Pagination info - Page: {Page}/{PageCount}, Total: {Total}", 
                        page, response.Meta.Pagination.PageCount, response.Meta.Pagination.Total);
                }
                else
                {
                    // If no pagination info, assume we're done if we got less than pageSize
                    hasMorePages = response.Data.Count == pageSize;
                }
                
                page++;
            }
            else
            {
                hasMorePages = false;
            }
        }
        
        _logger.LogInformation("Category values for {Type}: {Count} total items (across {Pages} pages)", categoryTypeName, allResults.Count, page - 1);
        
        // Debug: Log first few category values to see what we're getting
        if (allResults.Any())
        {
            var firstFew = allResults.Take(3);
            foreach (var cv in firstFew)
            {
                _logger.LogInformation("Sample category value: {Name} (Slug: {Slug}, CategoryType: {CategoryType})", 
                    cv.Name, cv.Slug, cv.CategoryType?.Name ?? "Unknown");
            }
        }
        
        return allResults;
    }

}
