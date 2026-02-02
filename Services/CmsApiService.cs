using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public class CmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsApiService> _logger;
    private readonly string _baseUrl;
    private readonly IEnhancedCacheService _cache;
    private readonly Dictionary<string, CacheItemInfo> _cacheTracking;
    private readonly object _cacheTrackingLock = new();

    public CmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<CmsApiService> logger, IEnhancedCacheService cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _cacheTracking = new Dictionary<string, CacheItemInfo>();
        _baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
        
        // Log CMS endpoint on startup
        Console.WriteLine($"[CmsApiService] Connecting to CMS endpoint: {_baseUrl}");
        _logger.LogInformation("CmsApiService initialized - CMS Base URL: {BaseUrl}", _baseUrl);
    }

    public async Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            
            // Create cache key based on endpoint
            var cacheKey = $"cms_api_{CreateCacheKey(endpoint)}";
            
            // Check cache first for GET requests (if cache duration is specified)
            if (cacheDuration.HasValue)
            {
                var cachedResult = await _cache.GetAsync<T>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogInformation("CACHE HIT: {Endpoint}", endpoint);
                    // Update last accessed time in tracking
                    lock (_cacheTrackingLock)
                    {
                        if (_cacheTracking.ContainsKey(cacheKey))
                        {
                            _cacheTracking[cacheKey].LastAccessed = DateTimeOffset.UtcNow;
                            _cacheTracking[cacheKey].HitCount++;
                        }
                        else
                        {
                            // Recreate tracking entry if it's missing
                            _cacheTracking[cacheKey] = new CacheItemInfo
                            {
                                Endpoint = endpoint,
                                Key = cacheKey,
                                CreatedAt = DateTimeOffset.UtcNow,
                                LastAccessed = DateTimeOffset.UtcNow,
                                ExpiresAt = DateTimeOffset.UtcNow.Add(cacheDuration.Value),
                                Duration = cacheDuration.Value,
                                HitCount = 1,
                                Size = EstimateObjectSize(cachedResult)
                            };
                        }
                    }
                    return cachedResult;
                }
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
                    _logger.LogInformation("TRACKING: Added {Key} -> {Endpoint} (Total tracking entries: {Count})", cacheKey, endpoint, _cacheTracking.Count);
                }
                
                // Use EnhancedCacheService to cache the result
                await _cache.SetAsync(cacheKey, result, cacheDuration.Value);
                
                _logger.LogInformation("CACHED: {Endpoint} for {Duration}", endpoint, cacheDuration.Value);
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
            
            _logger.LogInformation("POST Request to: {Url}", fullUrl);
            _logger.LogInformation("POST Request body: {Json}", json);
            
            // Use write API key for POST requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            if (string.IsNullOrEmpty(writeApiKey))
            {
                throw new InvalidOperationException("CmsApi:WriteApiKey is not configured");
            }
            
            // Clear any existing authorization header and set the write API key
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            var response = await _httpClient.PostAsync(fullUrl, content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("POST Response status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("POST Response body: {Content}", responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("POST request failed with status {StatusCode}. Response: {Response}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"POST request failed with status {response.StatusCode}: {responseContent}");
            }
            
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API POST endpoint: {Endpoint}. Message: {Message}", endpoint, ex.Message);
            throw; // Re-throw instead of swallowing the exception
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("PUT Request to: {Url}", fullUrl);
            _logger.LogInformation("PUT Request body: {Json}", json);
            
            // Use write API key for PUT requests
            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            if (string.IsNullOrEmpty(writeApiKey))
            {
                throw new InvalidOperationException("CmsApi:WriteApiKey is not configured");
            }
            
            // Clear any existing authorization header and set the write API key
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            
            _logger.LogInformation("Using WriteApiKey: {Key}", writeApiKey?.Substring(0, Math.Min(10, writeApiKey?.Length ?? 0)) + "...");
            
            var response = await _httpClient.PutAsync(fullUrl, content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("PUT Response status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("PUT Response body: {Content}", responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PUT request failed with status {StatusCode}. Response: {Response}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"PUT request failed with status {response.StatusCode}: {responseContent}");
            }
            
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CMS API PUT endpoint: {Endpoint}. Message: {Message}", endpoint, ex.Message);
            throw; // Re-throw instead of swallowing the exception
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

    public async Task ClearCache()
    {
        // Clear all CMS API cache entries
        var keysToRemove = _cacheTracking.Keys.ToList();
        foreach (var key in keysToRemove)
        {
            await _cache.RemoveAsync(key);
        }
        _cacheTracking.Clear();
        _logger.LogInformation("CMS API cache cleared - {Count} entries removed", keysToRemove.Count);
    }

    public async Task ClearCacheForEndpoint(string endpoint)
    {
        var cacheKey = $"cms_api_{CreateCacheKey(endpoint)}";
        await _cache.RemoveAsync(cacheKey);
        _cacheTracking.Remove(cacheKey);
        _logger.LogInformation("CMS API cache cleared for endpoint: {Endpoint}", endpoint);
    }

    public async Task<Dictionary<string, CacheItemInfo>> GetCacheInfo()
    {
        _logger.LogInformation("GetCacheInfo called - tracking has {Count} entries", _cacheTracking.Count);
        
        // Create a snapshot of current tracking entries
        var snapshot = new Dictionary<string, CacheItemInfo>();
        
        // Only clean up expired entries occasionally, not every time
        // This prevents aggressive cleanup that might remove valid entries
        if (_cacheTracking.Count > 50)
        {
            await CleanupExpiredEntries();
        }
        
        // Build snapshot and verify entries exist in actual cache
        foreach (var kvp in _cacheTracking)
        {
            var key = kvp.Key;
            var info = kvp.Value;
            
            // Check if entry still exists in actual cache
            var exists = await _cache.ExistsAsync(key);
            if (exists)
            {
                snapshot[key] = info;
                _logger.LogInformation("Cache entry verified: {Key} -> {Endpoint}", key, info.Endpoint);
            }
            else
            {
                _logger.LogWarning("Cache entry {Key} is tracked but not in actual cache - removing from tracking", key);
                _cacheTracking.Remove(key);
            }
        }
        
        _logger.LogInformation("Returning {Count} valid cache entries", snapshot.Count);
        return snapshot;
    }

    private async Task CleanupExpiredEntries()
    {
        var expiredKeys = _cacheTracking
            .Where(kvp => kvp.Value.ExpiresAt <= DateTimeOffset.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        _logger.LogInformation("Cleaning up {Count} expired cache entries", expiredKeys.Count);

        foreach (var key in expiredKeys)
        {
            await _cache.RemoveAsync(key); // Remove from actual cache
            _cacheTracking.Remove(key); // Remove from tracking
            _logger.LogInformation("Removed expired cache entry: {Key}", key);
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

    private long EstimateObjectSizeBytes(object obj)
    {
        try
        {
            // Use a fixed size per cache entry instead of calculating actual JSON size
            // This prevents extremely large API responses from consuming all cache space
            return 1000; // 1KB per entry - reasonable for most API responses
        }
        catch
        {
            return 1000; // Default size
        }
    }

    // Specialized methods for category data with individual caching
    public async Task<List<CategoryValue>?> GetCategoryValuesByType(string categoryTypeName, TimeSpan? cacheDuration = null)
    {
        var endpoint = $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&populate[category_type][fields][0]=name&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[children][fields][0]=name&populate[children][fields][1]=slug&pagination[pageSize]=10000";
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
            var endpoint = $"category-values?populate[category_type][fields][0]=name&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[children][fields][0]=name&populate[children][fields][1]=slug&pagination[page]={page}&pagination[pageSize]={pageSize}";
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
        var endpoint = "category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&populate[values][fields][0]=name&populate[values][fields][1]=slug&populate[values][fields][2]=enabled&populate[values][fields][3]=sort_order&sort=sort_order:asc&pagination[pageSize]=1000";
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
                ? $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&sort=sort_order:asc&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[children][fields][0]=name&populate[children][fields][1]=slug&pagination[page]={page}&pagination[pageSize]={pageSize}"
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

    // CMDB Matching Methods
    
    /// <summary>
    /// Get products that don't have a cmdb_sys_id (unmapped products)
    /// </summary>
    public async Task<List<Product>> GetUnmappedProductsAsync()
    {
        try
        {
            var response = await GetAsync<ApiCollectionResponse<Product>>("products?filters[cmdb_sys_id][$null]=true");
            return response?.Data ?? new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unmapped products");
            return new List<Product>();
        }
    }

    public async Task<CategoryValue?> GetCategoryValueBySlugAsync(string categoryTypeSlug, string slug, string? parentDocumentId = null, TimeSpan? cacheDuration = null)
    {
        var endpointBuilder = new StringBuilder();
        endpointBuilder.Append("category-values?");
        endpointBuilder.Append($"filters[slug][$eq]={Uri.EscapeDataString(slug)}");
        endpointBuilder.Append("&filters[publishedAt][$notNull]=true&filters[enabled]=true");

        if (!string.IsNullOrWhiteSpace(parentDocumentId))
        {
            endpointBuilder.Append($"&filters[parent][documentId][$eq]={Uri.EscapeDataString(parentDocumentId)}");
        }

        endpointBuilder.Append("&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[parent][fields][2]=documentId");
        endpointBuilder.Append("&populate[children][fields][0]=name&populate[children][fields][1]=slug&populate[children][fields][2]=documentId");
        endpointBuilder.Append("&sort=sort_order:asc&pagination[pageSize]=200");

        var endpoint = endpointBuilder.ToString();
        var response = await GetAsync<ApiCollectionResponse<CategoryValue>>(endpoint, cacheDuration);
        return response?.Data?.FirstOrDefault();
    }

    /// <summary>
    /// Get CMDB entries that don't have corresponding CMS products
    /// </summary>
    public async Task<List<object>> GetUnmappedCmdbEntriesAsync()
    {
        try
        {
            var response = await GetAsync<ApiCollectionResponse<object>>("admin/unmapped-cmdb-entries");
            return response?.Data ?? new List<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unmapped CMDB entries");
            return new List<object>();
        }
    }

    /// <summary>
    /// Assign a CMDB sys_id to a CMS product
    /// </summary>
    public async Task<bool> AssignCmdbSysIdAsync(int productId, string cmdbSysId)
    {
        try
        {
            var requestData = new { cmdb_sys_id = cmdbSysId };
            var response = await PostAsync<object>($"admin/products/{productId}/assign-cmdb-sys-id", requestData);
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning CMDB sys_id {CmdbSysId} to product {ProductId}", cmdbSysId, productId);
            return false;
        }
    }

    /// <summary>
    /// Remove cmdb_sys_id from a product (unlink from CMDB)
    /// </summary>
    public async Task<bool> UnlinkCmdbSysIdAsync(int productId)
    {
        try
        {
            var response = await PostAsync<object>($"admin/products/{productId}/unlink-cmdb-sys-id", new { });
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking CMDB sys_id from product {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Gets product documentIds that have a specific category value
    /// </summary>
    public async Task<List<string>> GetProductDocumentIdsByCategoryValueAsync(string categoryValueDocumentId, string? categoryValueSlug = null, TimeSpan? cacheDuration = null)
    {
        try
        {
            // Try filtering by documentId first
            var endpoint = $"products?filters[category_values][documentId][$eq]={Uri.EscapeDataString(categoryValueDocumentId)}&fields[0]=documentId&fields[1]=title&pagination[pageSize]=1000";
            _logger.LogInformation("Fetching products for category value {DocumentId} from endpoint: {Endpoint}", categoryValueDocumentId, endpoint);
            var response = await GetAsync<ApiCollectionResponse<Product>>(endpoint, cacheDuration);
            
            // If no results and we have a slug, try filtering by slug instead
            if ((response?.Data == null || !response.Data.Any()) && !string.IsNullOrEmpty(categoryValueSlug))
            {
                _logger.LogInformation("No products found by documentId, trying slug: {Slug}", categoryValueSlug);
                endpoint = $"products?filters[category_values][slug][$eq]={Uri.EscapeDataString(categoryValueSlug)}&fields[0]=documentId&fields[1]=title&pagination[pageSize]=1000";
                response = await GetAsync<ApiCollectionResponse<Product>>(endpoint, cacheDuration);
            }
            
            if (response?.Data == null || !response.Data.Any())
            {
                _logger.LogWarning("No products found for category value {DocumentId} (slug: {Slug})", categoryValueDocumentId, categoryValueSlug ?? "null");
                return new List<string>();
            }

            var documentIds = response.Data
                .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                .Select(p => p.DocumentId!)
                .Distinct()
                .ToList();
            
            _logger.LogInformation("Found {Count} products for category value {DocumentId}: {DocumentIds}", 
                documentIds.Count, categoryValueDocumentId, string.Join(", ", documentIds.Take(10)));
            
            // Also log product titles for debugging
            var productsWithTitles = response.Data
                .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                .Select(p => $"{p.Title} ({p.DocumentId})")
                .Take(5)
                .ToList();
            _logger.LogInformation("Sample products: {Products}", string.Join(", ", productsWithTitles));
            
            return documentIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products for category value {DocumentId}", categoryValueDocumentId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets all search terms (for filtering by category value)
    /// </summary>
    public async Task<List<SearchTerm>> GetAllSearchTermsAsync(TimeSpan? cacheDuration = null)
    {
        try
        {
            // Fetch recent search terms (limit to 100 most recent)
            var endpoint = $"search-terms?sort=timestamp:desc&pagination[pageSize]=100";
            _logger.LogInformation("Fetching all search terms from: {Endpoint}", endpoint);
            var response = await GetAsync<ApiCollectionResponse<SearchTerm>>(endpoint, cacheDuration);
            
            if (response?.Data == null)
            {
                _logger.LogWarning("No search terms data returned from API");
                return new List<SearchTerm>();
            }

            _logger.LogInformation("Fetched {Count} total search terms from API", response.Data.Count);
            return response.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all search terms");
            return new List<SearchTerm>();
        }
    }

    /// <summary>
    /// Gets search terms that returned this product in their results
    /// </summary>
    public async Task<List<SearchTerm>> GetSearchTermsForProductAsync(string productDocumentId, TimeSpan? cacheDuration = null)
    {
        try
        {
            // Fetch recent search terms (limit to 100 most recent)
            var endpoint = $"search-terms?sort=timestamp:desc&pagination[pageSize]=100";
            _logger.LogInformation("Fetching search terms from: {Endpoint}", endpoint);
            var response = await GetAsync<ApiCollectionResponse<SearchTerm>>(endpoint, cacheDuration);
            
            if (response?.Data == null)
            {
                _logger.LogWarning("No search terms data returned from API");
                return new List<SearchTerm>();
            }

            _logger.LogInformation("Fetched {Count} total search terms from API", response.Data.Count);

            // Filter search terms where results contain this product's documentId
            var matchingSearchTerms = response.Data
                .Where(st => st.Results != null && st.Results.Any(r => r.DocumentId == productDocumentId))
                .ToList();

            _logger.LogInformation("Found {Count} search terms matching product {DocumentId}", matchingSearchTerms.Count, productDocumentId);
            
            // Log first few results for debugging
            if (response.Data.Any() && response.Data.First().Results != null)
            {
                var firstResult = response.Data.First();
                _logger.LogInformation("First search term: '{Term}', Results count: {Count}, First result DocumentId: {DocId}", 
                    firstResult.SearchTermText, 
                    firstResult.Results?.Count ?? 0,
                    firstResult.Results?.FirstOrDefault()?.DocumentId ?? "null");
            }

            return matchingSearchTerms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching search terms for product {DocumentId}", productDocumentId);
            return new List<SearchTerm>();
        }
    }

}
