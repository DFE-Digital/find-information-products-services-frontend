using System.Globalization;
using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IOptimizedCmsApiService
{
    Task<(List<Product> Products, int TotalCount)> GetProductsForListingAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null);
    Task<(List<Product> Products, int TotalCount)> GetProductsForListingAsync2(int page = 1, int pageSize = 25, IEnumerable<string>? searchTerms = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByIdAsync(int id, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByFipsIdAsync(string fipsId, TimeSpan? cacheDuration = null);
    Task<List<CategoryType>> GetCategoryTypesAsync(TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>> GetCategoryValuesAsync(string categoryTypeSlug, TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>> GetCategoryValuesByParentAsync(string parentDocumentId, TimeSpan? cacheDuration = null);
    Task<int> GetProductsCountAsync(TimeSpan? cacheDuration = null, string? state = "Active");
    Task<List<Product>> GetProductsForFilterCountsAsync(TimeSpan? cacheDuration = null, string? state = "Active");
    Task<List<CategoryType>> GetAllCategoryTypes(TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>?> GetCategoryValuesForFilter(string categoryTypeName, TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>?> GetAllCategoryValues(TimeSpan? cacheDuration = null);
    Task<string[]> GetAvailableStates(TimeSpan? cacheDuration = null);
    Task<(List<Product> Products, int TotalCount)> GetProductsForAdminAsync(int page = 1, int pageSize = 25, string? searchQuery = null, string? stateFilter = null, TimeSpan? cacheDuration = null);
}

public class OptimizedCmsApiService : IOptimizedCmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OptimizedCmsApiService> _logger;
    private readonly IEnhancedCacheService _cacheService;
    private readonly string _apiKey;
    private static readonly string[] DefaultStates = { "New", "Active", "Rejected", "Deleted" };

    public OptimizedCmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<OptimizedCmsApiService> logger, IEnhancedCacheService cacheService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cacheService = cacheService;
        _apiKey = _configuration["CmsApi:ReadApiKey"] ?? throw new InvalidOperationException("CmsApi:ReadApiKey not configured");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        
        // Log CMS endpoint on startup
        var baseUrl = _httpClient.BaseAddress?.ToString() ?? "Not set";
        Console.WriteLine($"[OptimizedCmsApiService] Connecting to CMS endpoint: {baseUrl}");
        _logger.LogInformation("OptimizedCmsApiService initialized - CMS Base URL: {BaseUrl}", baseUrl);
    }

    public async Task<(List<Product> Products, int TotalCount)> GetProductsForListingAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"products_listing_{page}_{pageSize}_{searchQuery ?? "null"}_{string.Join("_", filters?.SelectMany(f => f.Value) ?? new string[0])}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<(List<Product> Products, int TotalCount)>(cacheKey);
            if (cachedResult.Products != null)
            {
                _logger.LogInformation("CACHE HIT: Products listing for page {Page}", page);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            // Sort products alphabetically by title
            "sort=title:asc",
            // Only essential fields for listing
            "fields[0]=title",
            "fields[1]=long_description", 
            "fields[2]=fips_id",
            "fields[3]=documentId",
            "fields[4]=state",
            // Minimal populate for categories (just names)
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name",
            // Populate user fields for search functionality
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress"
        };

        // Handle state filter and search query together
        // When both exist, we need to use $and to combine state filter with $or search
        bool hasStateFilter = filters != null && filters.ContainsKey("state");
        bool hasSearchQuery = !string.IsNullOrEmpty(searchQuery);
        
        if (hasStateFilter && hasSearchQuery)
        {
            // Combine state filter with search using $and
            var stateValue = filters["state"].Length == 1 ? filters["state"][0] : null;
            if (stateValue != null)
            {
                queryParams.Add($"filters[$and][0][state][$eq]={Uri.EscapeDataString(stateValue)}");
            }
            else
            {
                // Multiple state values
                for (int i = 0; i < filters["state"].Length; i++)
                {
                    queryParams.Add($"filters[$and][0][state][$in][{i}]={Uri.EscapeDataString(filters["state"][i])}");
                }
            }
            
            // Add search query as second $and condition
            // Search in product search_text (includes product synonyms) and category_value search_text (includes category synonyms)
            queryParams.Add($"filters[$and][1][$or][0][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][1][title][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][2][short_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][3][long_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][4][fips_id][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][5][documentId][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][6][category_values][name][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][7][category_values][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in service_owner fields
            queryParams.Add($"filters[$and][1][$or][8][service_owner][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][9][service_owner][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][10][service_owner][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][11][service_owner][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in product_manager fields
            queryParams.Add($"filters[$and][1][$or][12][product_manager][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][13][product_manager][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][14][product_manager][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][15][product_manager][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in senior_responsible_officer fields
            queryParams.Add($"filters[$and][1][$or][16][senior_responsible_officer][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][17][senior_responsible_officer][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][18][senior_responsible_officer][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][19][senior_responsible_officer][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
        }
        else if (hasStateFilter)
        {
            // Only state filter, no search
            if (filters["state"].Length == 1)
            {
                queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(filters["state"][0])}");
            }
            else
            {
                for (int i = 0; i < filters["state"].Length; i++)
                {
                    queryParams.Add($"filters[state][$in][{i}]={Uri.EscapeDataString(filters["state"][i])}");
                }
            }
        }
        else if (hasSearchQuery)
        {
            // Only search query, no explicit state filter (default to Active)
            queryParams.Add($"filters[$and][0][state][$eq]=Active");
            queryParams.Add($"filters[$and][1][$or][0][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][1][title][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][2][short_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][3][long_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][4][fips_id][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][5][documentId][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][6][category_values][name][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][7][category_values][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in service_owner fields
            queryParams.Add($"filters[$and][1][$or][8][service_owner][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][9][service_owner][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][10][service_owner][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][11][service_owner][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in product_manager fields
            queryParams.Add($"filters[$and][1][$or][12][product_manager][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][13][product_manager][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][14][product_manager][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][15][product_manager][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
            // Search in senior_responsible_officer fields
            queryParams.Add($"filters[$and][1][$or][16][senior_responsible_officer][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][17][senior_responsible_officer][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][18][senior_responsible_officer][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$and][1][$or][19][senior_responsible_officer][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
        }
        else if (filters == null)
        {
            // No filters and no search - default to Active
            queryParams.Add("filters[state][$eq]=Active");
        }

        // Add filters if provided
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (filter.Key == "state")
                {
                    // State filter already handled above, skip to avoid duplication
                    continue;
                }
                else if (filter.Key == "category_values.slug")
                {
                    // Handle category value filters
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[category_values][slug][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
                else
                {
                    // Handle other filters generically
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[{filter.Key}][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
            }
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        // Log the query for debugging
        if (!string.IsNullOrEmpty(searchQuery))
        {
            _logger.LogInformation("Search query for '{SearchQuery}': {Url}", searchQuery, url);
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var products = result?.Data ?? new List<Product>();
            var totalCount = result?.Meta?.Pagination?.Total ?? 0;
            
            // If we have a search query and got fewer than 5 results, try a broader search
            if (!string.IsNullOrEmpty(searchQuery) && products.Count < 5)
            {
                var broaderResults = await GetBroaderSearchResults(searchQuery, page, pageSize, filters);
                if (broaderResults.Any())
                {
                    products = broaderResults;
                    // For broader search, we don't have the total count, so we'll use the current page count
                    totalCount = products.Count;
                }
            }
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && products.Any())
            {
                var cacheData = (Products: products, TotalCount: totalCount);
                await _cacheService.SetAsync(cacheKey, cacheData, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products listing for page {Page} for {Duration}", page, cacheDuration.Value);
            }
            
            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return (new List<Product>(), 0);
        }
    }

    public async Task<(List<Product> Products, int TotalCount)> GetProductsForListingAsync2(int page = 1, int pageSize = 25, IEnumerable<string>? searchTerms = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null)
    {
        var searchTermList = searchTerms?.Where(t => !string.IsNullOrWhiteSpace(t))
                                        .Select(t => t.Trim())
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList() 
                            ?? new List<string>();
        var hasSearchQuery = searchTermList.Any();
        // Combined string representation for logging and broader search fallback
        var searchQuery = hasSearchQuery ? string.Join(", ", searchTermList) : null;
        var searchKeyForCache = hasSearchQuery ? string.Join("|", searchTermList) : "null";

        // Comma-separated terms = OR semantics. One giant nested Strapi $or (20+ fields × N terms) is so
        // expensive it often exceeds HttpClient timeout. Run one fast single-term query per term and merge.
        if (searchTermList.Count > 1)
        {
            return await GetProductsForListingMultiTermOrMergeAsync(page, pageSize, searchTermList, filters, cacheDuration);
        }

        // Create cache key based on parameters
        var cacheKey = $"products_listing3_{page}_{pageSize}_{searchKeyForCache}_{string.Join("_", filters?.SelectMany(f => f.Value) ?? Array.Empty<string>())}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<(List<Product> Products, int TotalCount)>(cacheKey);
            if (cachedResult.Products != null)
            {
                _logger.LogInformation("CACHE HIT: Products listing2 for page {Page}", page);
                return cachedResult;
            }
        }

        if (hasSearchQuery)
        {
            // Load all matches for this single term, rank by relevance (title/description before categories), then paginate in memory.
            const int batchSize = 500;
            const int maxPages = 50;
            var all = new List<Product>();
            for (var pn = 1; pn <= maxPages; pn++)
            {
                var (batch, strapiTotal) = await GetListingPageFromStrapiAsync(pn, batchSize, searchTermList, filters);
                if (batch.Count == 0)
                {
                    break;
                }

                all.AddRange(batch);
                if (batch.Count < batchSize || all.Count >= strapiTotal)
                {
                    break;
                }
            }

            if (all.Count < 5)
            {
                var broaderResults = await GetBroaderSearchResults(searchQuery!, 1, Math.Max(pageSize, batchSize), filters);
                if (broaderResults.Any())
                {
                    all = broaderResults;
                }
            }

            all = all
                .Where(p => ProductMatchesAnySearchTerm(p, searchTermList))
                .ToList();

            SortProductsBySearchRelevance(all, searchTermList);
            var totalCount = all.Count;
            var pageItems = all
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            if (cacheDuration.HasValue && pageItems.Any())
            {
                var cacheData = (Products: pageItems, TotalCount: totalCount);
                await _cacheService.SetAsync(cacheKey, cacheData, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products listing2 (relevance) for page {Page} for {Duration}", page, cacheDuration.Value);
            }

            return (pageItems, totalCount);
        }

        try
        {
            var (products, totalCount) = await GetListingPageFromStrapiAsync(page, pageSize, searchTermList, filters);

            if (cacheDuration.HasValue && products.Any())
            {
                var cacheData = (Products: products, TotalCount: totalCount);
                await _cacheService.SetAsync(cacheKey, cacheData, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products listing2 for page {Page} for {Duration}", page, cacheDuration.Value);
            }

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Products listing2 (no-search path)");
            return (new List<Product>(), 0);
        }
    }

    /// <summary>
    /// One page from Strapi for the products listing (0 or 1 search term). Used for aggregation, multi-term fetch, and non-search listing.
    /// </summary>
    private async Task<(List<Product> Products, int TotalCount)> GetListingPageFromStrapiAsync(
        int page,
        int pageSize,
        IReadOnlyList<string> searchTermList,
        Dictionary<string, string[]>? filters)
    {
        var hasSearchQuery = searchTermList.Count == 1;
        var searchQuery = hasSearchQuery ? string.Join(", ", searchTermList) : null;

        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            "sort=title:asc",
            "fields[0]=title",
            "fields[1]=short_description",
            "fields[2]=long_description",
            "fields[3]=search_text",
            "fields[4]=fips_id",
            "fields[5]=documentId",
            "fields[6]=state",
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][fields][2]=search_text",
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress"
        };

        bool hasStateFilter = filters != null && filters.ContainsKey("state");

        if (hasStateFilter && hasSearchQuery)
        {
            var stateValue = filters!["state"].Length == 1 ? filters["state"][0] : null;
            if (stateValue != null)
            {
                queryParams.Add($"filters[$and][0][state][$eq]={Uri.EscapeDataString(stateValue)}");
            }
            else
            {
                for (int i = 0; i < filters["state"].Length; i++)
                {
                    queryParams.Add($"filters[$and][0][state][$in][{i}]={Uri.EscapeDataString(filters["state"][i])}");
                }
            }

            var term = Uri.EscapeDataString(searchTermList[0]);
            queryParams.Add($"filters[$and][1][$or][0][search_text][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][1][title][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][2][short_description][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][3][long_description][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][4][fips_id][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][5][documentId][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][6][category_values][name][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][7][category_values][search_text][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][8][service_owner][displayName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][9][service_owner][firstName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][10][service_owner][lastName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][11][service_owner][emailAddress][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][12][product_manager][displayName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][13][product_manager][firstName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][14][product_manager][lastName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][15][product_manager][emailAddress][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][16][senior_responsible_officer][displayName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][17][senior_responsible_officer][firstName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][18][senior_responsible_officer][lastName][$containsi]={term}");
            queryParams.Add($"filters[$and][1][$or][19][senior_responsible_officer][emailAddress][$containsi]={term}");
        }
        else if (hasStateFilter)
        {
            if (filters!["state"].Length == 1)
            {
                queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(filters["state"][0])}");
            }
            else
            {
                for (int i = 0; i < filters["state"].Length; i++)
                {
                    queryParams.Add($"filters[state][$in][{i}]={Uri.EscapeDataString(filters["state"][i])}");
                }
            }
        }
        else if (hasSearchQuery)
        {
            queryParams.Add($"filters[$and][0][state][$eq]=Active");
            var termOnly = Uri.EscapeDataString(searchTermList[0]);
            queryParams.Add($"filters[$and][1][$or][0][search_text][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][1][title][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][2][short_description][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][3][long_description][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][4][fips_id][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][5][documentId][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][6][category_values][name][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][7][category_values][search_text][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][8][service_owner][displayName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][9][service_owner][firstName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][10][service_owner][lastName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][11][service_owner][emailAddress][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][12][product_manager][displayName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][13][product_manager][firstName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][14][product_manager][lastName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][15][product_manager][emailAddress][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][16][senior_responsible_officer][displayName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][17][senior_responsible_officer][firstName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][18][senior_responsible_officer][lastName][$containsi]={termOnly}");
            queryParams.Add($"filters[$and][1][$or][19][senior_responsible_officer][emailAddress][$containsi]={termOnly}");
        }
        else if (filters == null)
        {
            queryParams.Add("filters[state][$eq]=Active");
        }

        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (filter.Key == "state")
                {
                    continue;
                }
                else if (filter.Key == "category_values.slug")
                {
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[category_values][slug][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
                else
                {
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[{filter.Key}][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
            }
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        if (!string.IsNullOrEmpty(searchQuery))
        {
            _logger.LogInformation("Search query for '{SearchQuery}': {Url}", searchQuery, url);
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var products = result?.Data ?? new List<Product>();
            var totalCount = result?.Meta?.Pagination?.Total ?? 0;
            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return (new List<Product>(), 0);
        }
    }

    /// <summary>
    /// Higher score = stronger match. Title and long description rank above category-only matches; people fields sit between.
    /// </summary>
    private static void SortProductsBySearchRelevance(List<Product> products, IReadOnlyList<string> searchTerms)
    {
        if (products.Count == 0 || searchTerms.Count == 0)
        {
            return;
        }

        var terms = searchTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (terms.Count == 0)
        {
            return;
        }

        products.Sort((a, b) =>
        {
            var sa = ComputeProductSearchRelevanceScore(a, terms);
            var sb = ComputeProductSearchRelevanceScore(b, terms);
            var cmp = sb.CompareTo(sa);
            if (cmp != 0)
            {
                return cmp;
            }

            return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static int ComputeProductSearchRelevanceScore(Product p, List<string> terms)
    {
        var best = 0;
        foreach (var term in terms)
        {
            var s = ScoreProductForSingleSearchTerm(p, term);
            if (s > best)
            {
                best = s;
            }
        }

        return best;
    }

    private static int ScoreProductForSingleSearchTerm(Product p, string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return 0;
        }

        var c = StringComparison.OrdinalIgnoreCase;
        var score = 0;
        var title = p.Title ?? "";
        var shortD = p.ShortDescription ?? "";
        var longD = p.LongDescription ?? "";
        var searchText = p.SearchText ?? "";

        // Strongest title boosts first: exact title and exact word beats partial contains.
        if (title.Equals(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 2_000_000;
        }
        else if (ContainsWholeWord(title, term))
        {
            score += 1_500_000;
        }
        else if (title.StartsWith(term, c))
        {
            score += 1_200_000;
        }

        if (title.Contains(term, c))
        {
            score += 1_000_000;
        }

        if (searchText.Contains(term, c))
        {
            // Product search_text is curated for discoverability (synonyms/keywords),
            // so keep this high but below direct title matches.
            score += 700_000;
        }

        if (longD.Contains(term, c))
        {
            score += 500_000;
        }

        if (shortD.Contains(term, c))
        {
            score += 400_000;
        }

        if ((p.FipsId ?? "").Contains(term, c) || (p.DocumentId ?? "").Contains(term, c))
        {
            score += 100_000;
        }

        if (EntraUsersContainTerm(p.ServiceOwner, term) ||
            EntraUsersContainTerm(p.ProductManager, term) ||
            EntraUsersContainTerm(p.SeniorResponsibleOfficer, term))
        {
            score += 50_000;
        }

        if (CategoryValuesContainTerm(p.CategoryValues, term))
        {
            score += 10_000;
        }

        return score;
    }

    private static bool ProductMatchesAnySearchTerm(Product product, IReadOnlyList<string> searchTerms)
    {
        if (searchTerms.Count == 0)
        {
            return true;
        }

        foreach (var term in searchTerms)
        {
            if (ProductMatchesSearchTerm(product, term))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ProductMatchesSearchTerm(Product product, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        if (RequiresIsolatedWordMatch(term))
        {
            return ContainsWholeWord(product.Title ?? string.Empty, term)
                || ContainsWholeWord(product.SearchText ?? string.Empty, term)
                || ContainsWholeWord(product.ShortDescription ?? string.Empty, term)
                || ContainsWholeWord(product.LongDescription ?? string.Empty, term)
                || ContainsWholeWord(product.FipsId ?? string.Empty, term)
                || ContainsWholeWord(product.DocumentId ?? string.Empty, term)
                || CategoryValuesContainWholeWord(product.CategoryValues, term)
                || EntraUsersContainWholeWord(product.ServiceOwner, term)
                || EntraUsersContainWholeWord(product.ProductManager, term)
                || EntraUsersContainWholeWord(product.SeniorResponsibleOfficer, term);
        }

        var c = StringComparison.OrdinalIgnoreCase;
        return (product.Title ?? string.Empty).Contains(term, c)
            || (product.SearchText ?? string.Empty).Contains(term, c)
            || (product.ShortDescription ?? string.Empty).Contains(term, c)
            || (product.LongDescription ?? string.Empty).Contains(term, c)
            || (product.FipsId ?? string.Empty).Contains(term, c)
            || (product.DocumentId ?? string.Empty).Contains(term, c)
            || CategoryValuesContainTerm(product.CategoryValues, term)
            || EntraUsersContainTerm(product.ServiceOwner, term)
            || EntraUsersContainTerm(product.ProductManager, term)
            || EntraUsersContainTerm(product.SeniorResponsibleOfficer, term);
    }

    private static bool RequiresIsolatedWordMatch(string term)
    {
        // Short acronym-style terms like "AI" should not match inside larger words such as "claim".
        return term.Length <= 3;
    }

    private static bool ContainsWholeWord(string source, string term)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var words = source
            .Split(new[] { ' ', '-', '_', '/', '\\', '(', ')', ',', '.', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

        return words.Any(w => w.Equals(term, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EntraUsersContainTerm(List<EntraUser>? users, string term)
    {
        if (users == null || users.Count == 0)
        {
            return false;
        }

        var c = StringComparison.OrdinalIgnoreCase;
        foreach (var u in users)
        {
            if ((u.DisplayName ?? "").Contains(term, c) ||
                (u.FirstName ?? "").Contains(term, c) ||
                (u.LastName ?? "").Contains(term, c) ||
                (u.EmailAddress ?? "").Contains(term, c))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EntraUsersContainWholeWord(List<EntraUser>? users, string term)
    {
        if (users == null || users.Count == 0)
        {
            return false;
        }

        foreach (var u in users)
        {
            if (ContainsWholeWord(u.DisplayName ?? string.Empty, term) ||
                ContainsWholeWord(u.FirstName ?? string.Empty, term) ||
                ContainsWholeWord(u.LastName ?? string.Empty, term) ||
                ContainsWholeWord(u.EmailAddress ?? string.Empty, term))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CategoryValuesContainTerm(List<CategoryValue>? values, string term)
    {
        if (values == null || values.Count == 0)
        {
            return false;
        }

        var c = StringComparison.OrdinalIgnoreCase;
        foreach (var v in values)
        {
            if ((v.Name ?? "").Contains(term, c) || (v.SearchText ?? "").Contains(term, c))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CategoryValuesContainWholeWord(List<CategoryValue>? values, string term)
    {
        if (values == null || values.Count == 0)
        {
            return false;
        }

        foreach (var v in values)
        {
            if (ContainsWholeWord(v.Name ?? string.Empty, term) || ContainsWholeWord(v.SearchText ?? string.Empty, term))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Comma-separated keywords are OR'd: run one fast single-term CMS query per term (same as searching one word),
    /// then merge and dedupe. Building one giant Strapi $or (20 fields × N terms) often exceeds the HTTP timeout.
    /// </summary>
    private async Task<(List<Product> Products, int TotalCount)> GetProductsForListingMultiTermOrMergeAsync(
        int page,
        int pageSize,
        List<string> searchTermList,
        Dictionary<string, string[]>? filters,
        TimeSpan? cacheDuration)
    {
        const int maxTerms = 15;
        var terms = searchTermList
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxTerms)
            .ToList();

        if (terms.Count == 0)
        {
            return (new List<Product>(), 0);
        }

        var searchKeyForCache = string.Join("|", terms);
        var cacheKey = $"products_listing3_{page}_{pageSize}_{searchKeyForCache}_{string.Join("_", filters?.SelectMany(f => f.Value) ?? Array.Empty<string>())}";

        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<(List<Product> Products, int TotalCount)>(cacheKey);
            if (cachedResult.Products != null)
            {
                _logger.LogInformation("CACHE HIT: Products listing multi-term OR for page {Page}", page);
                return cachedResult;
            }
        }

        _logger.LogInformation("Multi-term OR search: {TermCount} single-term queries in parallel for terms: {Terms}",
            terms.Count, string.Join(", ", terms));

        var lists = await Task.WhenAll(terms.Select(t => FetchAllProductsForSingleSearchTermAsync(t, filters)));

        var merged = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in lists)
        {
            foreach (var p in list)
            {
                var key = !string.IsNullOrEmpty(p.DocumentId)
                    ? p.DocumentId
                    : p.Id.ToString(CultureInfo.InvariantCulture);
                if (!merged.ContainsKey(key))
                {
                    merged[key] = p;
                }
            }
        }

        var sorted = merged.Values
            .Where(p => ProductMatchesAnySearchTerm(p, terms))
            .ToList();
        SortProductsBySearchRelevance(sorted, terms);
        var totalCount = sorted.Count;
        var pageItems = sorted
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .ToList();

        if (cacheDuration.HasValue && pageItems.Any())
        {
            await _cacheService.SetAsync(cacheKey, (Products: pageItems, TotalCount: totalCount), cacheDuration.Value);
            _logger.LogInformation("CACHED: Products listing multi-term OR page {Page} ({Count} results, total distinct {Total})",
                page, pageItems.Count, totalCount);
        }

        return (pageItems, totalCount);
    }

    /// <summary>
    /// Paginates through all results for one search term using the standard single-term query (fast).
    /// </summary>
    private async Task<List<Product>> FetchAllProductsForSingleSearchTermAsync(string term, Dictionary<string, string[]>? filters)
    {
        var all = new List<Product>();
        var pageNum = 1;
        const int batchSize = 500;
        const int maxPages = 50;

        while (pageNum <= maxPages)
        {
            var (prods, total) = await GetListingPageFromStrapiAsync(pageNum, batchSize, new List<string> { term }, filters);
            if (prods.Count == 0)
            {
                break;
            }

            all.AddRange(prods);
            if (prods.Count < batchSize || all.Count >= total)
            {
                break;
            }

            pageNum++;
        }

        return all;
    }

    private async Task<List<Product>> GetBroaderSearchResults(string searchQuery, int page, int pageSize, Dictionary<string, string[]>? filters)
    {
        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            // Sort products alphabetically by title
            "sort=title:asc",
            "fields[0]=title",
            "fields[1]=short_description",
            "fields[2]=long_description",
            "fields[3]=fips_id",
            "fields[4]=documentId",
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][fields][2]=search_text",
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress"
        };

        // Add state filter - default to Active only if no filters are provided at all
        if (filters == null)
        {
            queryParams.Add("filters[state][$eq]=Active");
        }
        else if (filters.ContainsKey("state"))
        {
            // State filter is explicitly provided, use it
            for (int i = 0; i < filters["state"].Length; i++)
            {
                queryParams.Add($"filters[state][$in][{i}]={Uri.EscapeDataString(filters["state"][i])}");
            }
        }

        // Use broader search with $containsi on multiple fields
        // Note: Component fields (synonyms) can't be filtered via REST API, use search_text instead
        queryParams.Add($"filters[$or][0][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][1][title][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][2][short_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][3][long_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
        // Search in category value names and search_text (includes synonyms)
        queryParams.Add($"filters[$or][4][category_values][name][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][5][category_values][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
        // Search in service_owner fields
        queryParams.Add($"filters[$or][6][service_owner][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][7][service_owner][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][8][service_owner][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][9][service_owner][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
        // Search in product_manager fields
        queryParams.Add($"filters[$or][10][product_manager][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][11][product_manager][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][12][product_manager][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][13][product_manager][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");
        // Search in senior_responsible_officer fields
        queryParams.Add($"filters[$or][14][senior_responsible_officer][displayName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][15][senior_responsible_officer][firstName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][16][senior_responsible_officer][lastName][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][17][senior_responsible_officer][emailAddress][$containsi]={Uri.EscapeDataString(searchQuery)}");

        // Add filters if provided
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (filter.Key == "state")
                {
                    // State filter already handled above, skip to avoid duplication
                    continue;
                }
                else if (filter.Key == "category_values.slug")
                {
                    // Handle category value filters
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[category_values][slug][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
                else
                {
                    // Handle other filters generically
                    for (int i = 0; i < filter.Value.Length; i++)
                    {
                        queryParams.Add($"filters[{filter.Key}][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                    }
                }
            }
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        // Log the query for debugging
        if (!string.IsNullOrEmpty(searchQuery))
        {
            _logger.LogInformation("Search query for '{SearchQuery}': {Url}", searchQuery, url);
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            return result?.Data ?? new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Broader search API Error: {Url}", url);
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductByIdAsync(int id, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"product_detail_v2_{id}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Product detail for ID {Id}", id);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            // Essential fields for detailed view
            "fields[0]=id",
            "fields[1]=documentId",
            "fields[2]=title",
            "fields[3]=short_description",
            "fields[4]=long_description",
            "fields[5]=product_url",
            "fields[6]=state",
            "fields[7]=fips_id",
            "fields[8]=cmdb_sys_id",
            "fields[9]=cmdb_last_sync",
            "fields[10]=createdAt",
            "fields[11]=updatedAt",
            "fields[12]=publishedAt",
            // Populate relations with minimal fields
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][fields][2]=short_description",
            "populate[category_values][fields][3]=documentId",
            "populate[category_values][fields][4]=search_text",
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[category_values][populate][category_type][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][2]=multi_level",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[delivery_manager][fields][0]=displayName",
            "populate[delivery_manager][fields][1]=firstName",
            "populate[delivery_manager][fields][2]=lastName",
            "populate[delivery_manager][fields][3]=emailAddress",
            "populate[Information_asset_owner][fields][0]=displayName",
            "populate[Information_asset_owner][fields][1]=firstName",
            "populate[Information_asset_owner][fields][2]=lastName",
            "populate[Information_asset_owner][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress",
            "populate[product_assurances][fields][0]=assurance_type",
            "populate[product_assurances][fields][1]=external_id",
            "populate[product_assurances][fields][2]=external_url",
            "populate[product_assurances][fields][3]=date_of_assurance",
            "populate[product_assurances][fields][4]=outcome",
            "populate[product_assurances][fields][5]=phase"
        };

        var queryString = string.Join("&", queryParams);
        var url = $"products/{id}?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Log the raw JSON response for debugging
            _logger.LogInformation("Raw API Response for product {Id}: {JsonContent}", id, jsonContent);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiResponse<Product>>(jsonContent, options);
            var product = result?.Data;
            
            // Log the deserialized product contact fields for debugging
            if (product != null)
            {
                _logger.LogInformation("Product {Id} - ServiceOwner count: {Count}, ProductManager count: {PmCount}, DeliveryManager count: {DmCount}, InformationAssetOwner count: {IaoCount}, SeniorResponsibleOfficer count: {SroCount}",
                    id,
                    product.ServiceOwner?.Count ?? 0,
                    product.ProductManager?.Count ?? 0,
                    product.DeliveryManager?.Count ?? 0,
                    product.InformationAssetOwner?.Count ?? 0,
                    product.SeniorResponsibleOfficer?.Count ?? 0);
            }
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && product != null)
            {
                await _cacheService.SetAsync(cacheKey, product, cacheDuration.Value);
                _logger.LogInformation("CACHED: Product detail for ID {Id} for {Duration}", id, cacheDuration.Value);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return null;
        }
    }

    public async Task<Product?> GetProductByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"product_document_v2_{documentId}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Product by document ID {DocumentId}", documentId);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"filters[documentId][$eq]={Uri.EscapeDataString(documentId)}",
            // Essential fields for detailed view
            "fields[0]=id",
            "fields[1]=documentId",
            "fields[2]=title",
            "fields[3]=short_description",
            "fields[4]=long_description",
            "fields[5]=product_url",
            "fields[6]=state",
            "fields[7]=fips_id",
            "fields[8]=cmdb_sys_id",
            "fields[9]=cmdb_last_sync",
            "fields[10]=createdAt",
            "fields[11]=updatedAt",
            "fields[12]=publishedAt",
            // Populate relations with minimal fields
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][fields][2]=short_description",
            "populate[category_values][fields][3]=documentId",
            "populate[category_values][fields][4]=search_text",
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[category_values][populate][category_type][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][2]=multi_level",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[delivery_manager][fields][0]=displayName",
            "populate[delivery_manager][fields][1]=firstName",
            "populate[delivery_manager][fields][2]=lastName",
            "populate[delivery_manager][fields][3]=emailAddress",
            "populate[Information_asset_owner][fields][0]=displayName",
            "populate[Information_asset_owner][fields][1]=firstName",
            "populate[Information_asset_owner][fields][2]=lastName",
            "populate[Information_asset_owner][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress",
            "populate[product_assurances][fields][0]=assurance_type",
            "populate[product_assurances][fields][1]=external_id",
            "populate[product_assurances][fields][2]=external_url",
            "populate[product_assurances][fields][3]=date_of_assurance",
            "populate[product_assurances][fields][4]=outcome",
            "populate[product_assurances][fields][5]=phase"
        };

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var product = result?.Data?.FirstOrDefault();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && product != null)
            {
                await _cacheService.SetAsync(cacheKey, product, cacheDuration.Value);
                _logger.LogInformation("CACHED: Product by document ID {DocumentId} for {Duration}", documentId, cacheDuration.Value);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return null;
        }
    }

    public async Task<Product?> GetProductByFipsIdAsync(string fipsId, TimeSpan? cacheDuration = null)
    {
        // Support both FipsId and DocumentId lookups
        // DocumentId format is typically alphanumeric (e.g., "h7pjd1dx4hwvjm9zg6bv2gci")
        // FipsId format is typically "XXX-###" (e.g., "ABC-123")
        var isDocumentId = !string.IsNullOrEmpty(fipsId) && !System.Text.RegularExpressions.Regex.IsMatch(fipsId, @"^[A-Z]{3}-\d{3}$");
        
        // Changed cache key to force refresh after adding new contact fields (product_manager, delivery_manager, Information_asset_owner, senior_responsible_officer)
        var cacheKey = isDocumentId ? $"product_doc_v4_{fipsId}" : $"product_fips_v4_{fipsId}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Product by {Type} {Id}", isDocumentId ? "DocumentId" : "FIPS ID", fipsId);
                return cachedResult;
            }
        }

        var queryParams = new List<string>();
        
        // Use documentId filter if it looks like a DocumentId, otherwise use fips_id filter
        if (isDocumentId)
        {
            queryParams.Add($"filters[documentId][$eq]={Uri.EscapeDataString(fipsId)}");
        }
        else
        {
            queryParams.Add($"filters[fips_id][$eq]={Uri.EscapeDataString(fipsId)}");
        }
        
        queryParams.AddRange(new List<string>
        {
            // Essential fields for detailed view
            "fields[0]=id",
            "fields[1]=documentId",
            "fields[2]=title",
            "fields[3]=short_description",
            "fields[4]=long_description",
            "fields[5]=product_url",
            "fields[6]=state",
            "fields[7]=fips_id",
            "fields[8]=cmdb_sys_id",
            "fields[9]=cmdb_last_sync",
            "fields[10]=createdAt",
            "fields[11]=updatedAt",
            "fields[12]=publishedAt",
            // Populate relations with minimal fields
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][fields][2]=short_description",
            "populate[category_values][fields][3]=documentId",
            "populate[category_values][fields][4]=search_text",
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[category_values][populate][category_type][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][2]=multi_level",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
            "populate[service_owner][fields][0]=displayName",
            "populate[service_owner][fields][1]=firstName",
            "populate[service_owner][fields][2]=lastName",
            "populate[service_owner][fields][3]=emailAddress",
            "populate[product_manager][fields][0]=displayName",
            "populate[product_manager][fields][1]=firstName",
            "populate[product_manager][fields][2]=lastName",
            "populate[product_manager][fields][3]=emailAddress",
            "populate[delivery_manager][fields][0]=displayName",
            "populate[delivery_manager][fields][1]=firstName",
            "populate[delivery_manager][fields][2]=lastName",
            "populate[delivery_manager][fields][3]=emailAddress",
            "populate[Information_asset_owner][fields][0]=displayName",
            "populate[Information_asset_owner][fields][1]=firstName",
            "populate[Information_asset_owner][fields][2]=lastName",
            "populate[Information_asset_owner][fields][3]=emailAddress",
            "populate[senior_responsible_officer][fields][0]=displayName",
            "populate[senior_responsible_officer][fields][1]=firstName",
            "populate[senior_responsible_officer][fields][2]=lastName",
            "populate[senior_responsible_officer][fields][3]=emailAddress",
            "populate[product_assurances][fields][0]=assurance_type",
            "populate[product_assurances][fields][1]=external_id",
            "populate[product_assurances][fields][2]=external_url",
            "populate[product_assurances][fields][3]=date_of_assurance",
            "populate[product_assurances][fields][4]=outcome",
            "populate[product_assurances][fields][5]=phase"
        });

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Log a snippet of the JSON response for debugging (first 500 chars to see structure)
            var jsonSnippet = jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent;
            _logger.LogInformation("API Response snippet for product {FipsId}: {JsonSnippet}", fipsId, jsonSnippet);
            
            // Check if contact fields are present in the raw JSON
            if (jsonContent.Contains("service_owner") || jsonContent.Contains("serviceOwner"))
            {
                _logger.LogInformation("Found service_owner field in JSON response");
            }
            if (jsonContent.Contains("product_manager") || jsonContent.Contains("productManager"))
            {
                _logger.LogInformation("Found product_manager field in JSON response");
            }
            if (jsonContent.Contains("delivery_manager") || jsonContent.Contains("deliveryManager"))
            {
                _logger.LogInformation("Found delivery_manager field in JSON response");
            }
            if (jsonContent.Contains("Information_asset_owner") || jsonContent.Contains("informationAssetOwner"))
            {
                _logger.LogInformation("Found Information_asset_owner field in JSON response");
            }
            if (jsonContent.Contains("senior_responsible_officer") || jsonContent.Contains("seniorResponsibleOfficer"))
            {
                _logger.LogInformation("Found senior_responsible_officer field in JSON response");
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var product = result?.Data?.FirstOrDefault();
            
            // Log the deserialized product contact fields for debugging
            if (product != null)
            {
                _logger.LogInformation("Product {FipsId} - ServiceOwner count: {Count}, ProductManager count: {PmCount}, DeliveryManager count: {DmCount}, InformationAssetOwner count: {IaoCount}, SeniorResponsibleOfficer count: {SroCount}",
                    fipsId,
                    product.ServiceOwner?.Count ?? 0,
                    product.ProductManager?.Count ?? 0,
                    product.DeliveryManager?.Count ?? 0,
                    product.InformationAssetOwner?.Count ?? 0,
                    product.SeniorResponsibleOfficer?.Count ?? 0);
            }

            // Debug: Log category values with search_text
            if (product?.CategoryValues != null)
            {
                foreach (var cv in product.CategoryValues)
                {
                    _logger.LogInformation("Category value '{Name}' (DocumentId: {DocId}): SearchText = '{SearchText}'", 
                        cv.Name, cv.DocumentId ?? "null", cv.SearchText ?? "(null)");
                }
            }
            
            // Log raw JSON for category values to see what the API is returning
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var productData = data[0];
                    if (productData.TryGetProperty("attributes", out var attrs) && 
                        attrs.TryGetProperty("category_values", out var catValues))
                    {
                        if (catValues.TryGetProperty("data", out var catDataArray))
                        {
                            foreach (var catValue in catDataArray.EnumerateArray())
                            {
                                if (catValue.TryGetProperty("attributes", out var catAttrs))
                                {
                                    var name = catAttrs.TryGetProperty("name", out var n) ? n.GetString() : "unknown";
                                    var searchText = catAttrs.TryGetProperty("search_text", out var st) ? st.GetString() : "(not in response)";
                                    _logger.LogInformation("API returned category value '{Name}': search_text = '{SearchText}'", name, searchText ?? "(null)");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON for debugging");
            }
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && product != null)
            {
                await _cacheService.SetAsync(cacheKey, product, cacheDuration.Value);
                _logger.LogInformation("CACHED: Product by {Type} {Id} for {Duration}", isDocumentId ? "DocumentId" : "FIPS ID", fipsId, cacheDuration.Value);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return null;
        }
    }

    public async Task<List<CategoryType>> GetCategoryTypesAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "category_types_all";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<CategoryType>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Category types");
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            "filters[publishedAt][$notNull]=true",
            "filters[enabled]=true",
            "sort=sort_order:asc"
            // Removed field selection - let Strapi return all fields
        };

        var queryString = string.Join("&", queryParams);
        var url = $"category-types?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryType>>(jsonContent, options);
            var categoryTypes = result?.Data ?? new List<CategoryType>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && categoryTypes.Any())
            {
                await _cacheService.SetAsync(cacheKey, categoryTypes, cacheDuration.Value);
                _logger.LogInformation("CACHED: Category types for {Duration}", cacheDuration.Value);
            }
            
            return categoryTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<CategoryType>();
        }
    }

    public async Task<List<CategoryType>> GetAllCategoryTypes(TimeSpan? cacheDuration = null)
    {
        var queryParams = new List<string>
        {
            "filters[enabled]=true",
            "sort=sort_order:asc",
            "pagination[pageSize]=1000"
        };

        var queryString = string.Join("&", queryParams);
        var url = $"category-types?{queryString}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryType>>(jsonContent, options);
            
            return result?.Data ?? new List<CategoryType>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<CategoryType>();
        }
    }

    public async Task<List<CategoryValue>> GetCategoryValuesAsync(string categoryTypeSlug, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"category_values_{categoryTypeSlug}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<CategoryValue>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Category values for {Slug}", categoryTypeSlug);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"filters[category_type][slug][$eq]={Uri.EscapeDataString(categoryTypeSlug)}",
            "filters[publishedAt][$notNull]=true",
            "filters[enabled]=true",
            "sort=sort_order:asc",
            // Populate parent and children with minimal fields
            "populate[parent][fields][0]=name",
            "populate[parent][fields][1]=slug",
            "populate[children][fields][0]=name",
            "populate[children][fields][1]=slug"
        };

        var queryString = string.Join("&", queryParams);
        var url = $"category-values?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValue>>(jsonContent, options);
            var categoryValues = result?.Data ?? new List<CategoryValue>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && categoryValues.Any())
            {
                await _cacheService.SetAsync(cacheKey, categoryValues, cacheDuration.Value);
                _logger.LogInformation("CACHED: Category values for {Slug} for {Duration}", categoryTypeSlug, cacheDuration.Value);
            }
            
            return categoryValues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<CategoryValue>();
        }
    }

    public async Task<List<CategoryValue>> GetCategoryValuesByParentAsync(string parentDocumentId, TimeSpan? cacheDuration = null)
    {
        var cacheKey = $"category_values_parent_{parentDocumentId}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<CategoryValue>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Category values by parent {ParentId}", parentDocumentId);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"filters[parent][documentId][$eq]={Uri.EscapeDataString(parentDocumentId)}",
            "filters[publishedAt][$notNull]=true",
            "filters[enabled]=true",
            "sort=sort_order:asc",
            // Populate parent and children with minimal fields
            "populate[parent][fields][0]=name",
            "populate[parent][fields][1]=slug",
            "populate[children][fields][0]=name",
            "populate[children][fields][1]=slug"
        };

        var queryString = string.Join("&", queryParams);
        var url = $"category-values?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValue>>(jsonContent, options);
            var categoryValues = result?.Data ?? new List<CategoryValue>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && categoryValues.Any())
            {
                await _cacheService.SetAsync(cacheKey, categoryValues, cacheDuration.Value);
                _logger.LogInformation("CACHED: Category values by parent {ParentId} for {Duration}", parentDocumentId, cacheDuration.Value);
            }
            
            return categoryValues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<CategoryValue>();
        }
    }

    public async Task<List<CategoryValue>?> GetCategoryValuesForFilter(string categoryTypeName, TimeSpan? cacheDuration = null)
    {
        // Optimized: Use larger page size and single request for most cases
        var pageSize = 1000; // Increased page size to reduce API calls
        
        // For User group category type, we need to populate parent and children relationships
        var endpoint = (categoryTypeName.Equals("User group", StringComparison.OrdinalIgnoreCase) ||
                       categoryTypeName.Equals("User Group", StringComparison.OrdinalIgnoreCase) ||
                       categoryTypeName.Equals("Audience", StringComparison.OrdinalIgnoreCase))
            ? $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&sort=sort_order:asc&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[children][fields][0]=name&populate[children][fields][1]=slug&pagination[pageSize]={pageSize}"
            : $"category-values?filters[category_type][name]={Uri.EscapeDataString(categoryTypeName)}&sort=sort_order:asc&fields[0]=name&fields[1]=slug&pagination[pageSize]={pageSize}";
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValue>>(jsonContent, options);
            
            return result?.Data ?? new List<CategoryValue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error fetching category values for '{Type}': {Endpoint}", categoryTypeName, endpoint);
            return new List<CategoryValue>();
        }
    }

    public async Task<int> GetProductsCountAsync(TimeSpan? cacheDuration = null, string? state = "Active")
    {
        var cacheKey = $"products_count_{state ?? "all"}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<int>(cacheKey);
            if (cachedResult > 0)
            {
                _logger.LogInformation("CACHE HIT: Products count for state {State}", state);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            "pagination[pageSize]=1",
            "fields[0]=id" // Only need ID for count
        };

        // Add state filter if specified
        if (!string.IsNullOrEmpty(state))
        {
            queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(state)}");
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var count = result?.Meta?.Pagination?.Total ?? 0;
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && count > 0)
            {
                await _cacheService.SetAsync(cacheKey, count, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products count {Count} for state {State} for {Duration}", count, state, cacheDuration.Value);
            }
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return 0;
        }
    }

    public async Task<List<Product>> GetProductsForFilterCountsAsync(TimeSpan? cacheDuration = null, string? state = "Active")
    {
        var cacheKey = $"products_filter_counts_{state ?? "all"}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<Product>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Products for filter counts for state {State}", state);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            "pagination[pageSize]=1000", // Reasonable limit for filter counts
            // Sort products alphabetically by title
            "sort=title:asc",
            // Only fields needed for filtering
            "fields[0]=id",
            "fields[1]=fips_id",
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name"
        };

        // Add state filter if specified
        if (!string.IsNullOrEmpty(state))
        {
            queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(state)}");
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        _logger.LogInformation("API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var products = result?.Data ?? new List<Product>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && products.Any())
            {
                await _cacheService.SetAsync(cacheKey, products, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products for filter counts for state {State} for {Duration}", state, cacheDuration.Value);
            }
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<Product>();
        }
    }

    public async Task<List<CategoryValue>?> GetAllCategoryValues(TimeSpan? cacheDuration = null)
    {
        // Optimized: Use larger page size and single request
        var pageSize = 1000;
        var endpoint = $"category-values?sort=sort_order:asc&populate[category_type][fields][0]=name&populate[category_type][fields][1]=slug&populate[parent][fields][0]=name&populate[parent][fields][1]=slug&populate[children][fields][0]=name&populate[children][fields][1]=slug&pagination[pageSize]={pageSize}";
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiCollectionResponse<CategoryValue>>(jsonContent, options);
            
            return result?.Data ?? new List<CategoryValue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error fetching all category values: {Endpoint}", endpoint);
            return new List<CategoryValue>();
        }
    }

    public async Task<string[]> GetAvailableStates(TimeSpan? cacheDuration = null)
    {
        // For now, return the default states from the CMS schema
        // In the future, this could be made dynamic by querying the CMS schema API
        return await Task.FromResult(DefaultStates);
    }

    public async Task<(List<Product> Products, int TotalCount)> GetProductsForAdminAsync(int page = 1, int pageSize = 25, string? searchQuery = null, string? stateFilter = null, TimeSpan? cacheDuration = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"products_admin_{page}_{pageSize}_{searchQuery ?? "null"}_{stateFilter ?? "all"}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<(List<Product> Products, int TotalCount)>(cacheKey);
            if (cachedResult.Products != null)
            {
                _logger.LogInformation("CACHE HIT: Admin products listing for page {Page}", page);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            // Sort products alphabetically by title
            "sort=title:asc",
            // Only essential fields for admin listing
            "fields[0]=title",
            "fields[1]=short_description", 
            "fields[2]=fips_id",
            "fields[3]=documentId",
            "fields[4]=state",
            "fields[5]=id",
            // Minimal populate for categories (just names)
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name"
        };

        // Add state filter if provided (admin can see all states)
        if (!string.IsNullOrEmpty(stateFilter))
        {
            queryParams.Add($"filters[state][$eq]={Uri.EscapeDataString(stateFilter)}");
        }

        // Add search query if provided - search title and fips_id for admin
        if (!string.IsNullOrEmpty(searchQuery))
        {
            // Use $containsi for case-insensitive search across title and fips_id fields
            queryParams.Add($"filters[$or][0][search_text][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$or][1][title][$containsi]={Uri.EscapeDataString(searchQuery)}");
            queryParams.Add($"filters[$or][2][fips_id][$containsi]={Uri.EscapeDataString(searchQuery)}");
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

        // Log the query for debugging
        if (!string.IsNullOrEmpty(searchQuery))
        {
            _logger.LogInformation("Search query for '{SearchQuery}': {Url}", searchQuery, url);
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Deserialize<ApiCollectionResponse<Product>>(jsonContent, options);
            var products = result?.Data ?? new List<Product>();
            var totalCount = result?.Meta?.Pagination?.Total ?? 0;
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && products.Any())
            {
                await _cacheService.SetAsync(cacheKey, (products, totalCount), cacheDuration.Value);
                _logger.LogInformation("CACHED: Admin products listing page {Page} for {Duration}", page, cacheDuration.Value);
            }
            
            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return (new List<Product>(), 0);
        }
    }
}
