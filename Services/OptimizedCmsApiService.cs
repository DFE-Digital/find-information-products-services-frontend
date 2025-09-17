using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IOptimizedCmsApiService
{
    Task<List<Product>> GetProductsForListingAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByIdAsync(int id, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null);
    Task<Product?> GetProductByFipsIdAsync(string fipsId, TimeSpan? cacheDuration = null);
    Task<List<CategoryType>> GetCategoryTypesAsync(TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>> GetCategoryValuesAsync(string categoryTypeSlug, TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>> GetCategoryValuesByParentAsync(string parentDocumentId, TimeSpan? cacheDuration = null);
    Task<int> GetProductsCountAsync(TimeSpan? cacheDuration = null);
    Task<List<Product>> GetProductsForFilterCountsAsync(TimeSpan? cacheDuration = null);
    Task<List<CategoryType>> GetAllCategoryTypes(TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>?> GetCategoryValuesForFilter(string categoryTypeName, TimeSpan? cacheDuration = null);
    Task<List<CategoryValue>?> GetAllCategoryValues(TimeSpan? cacheDuration = null);
}

public class OptimizedCmsApiService : IOptimizedCmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OptimizedCmsApiService> _logger;
    private readonly IEnhancedCacheService _cacheService;
    private readonly string _apiKey;

    public OptimizedCmsApiService(HttpClient httpClient, IConfiguration configuration, ILogger<OptimizedCmsApiService> logger, IEnhancedCacheService cacheService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _cacheService = cacheService;
        _apiKey = _configuration["CmsApi:ReadApiKey"] ?? throw new InvalidOperationException("CmsApi:ReadApiKey not configured");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<List<Product>> GetProductsForListingAsync(int page = 1, int pageSize = 25, string? searchQuery = null, Dictionary<string, string[]>? filters = null, TimeSpan? cacheDuration = null)
    {
        // Create cache key based on parameters
        var cacheKey = $"products_listing_{page}_{pageSize}_{searchQuery ?? "null"}_{string.Join("_", filters?.SelectMany(f => f.Value) ?? new string[0])}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<Product>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Products listing for page {Page}", page);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            // Only essential fields for listing
            "fields[0]=title",
            "fields[1]=short_description", 
            "fields[2]=fips_id",
            // Minimal populate for categories (just names)
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name"
        };

        // Add search query if provided - optimized for performance
        if (!string.IsNullOrEmpty(searchQuery))
        {
            // Use $startsWith for better performance on title field (most common search)
            queryParams.Add($"filters[title][$startsWith]={Uri.EscapeDataString(searchQuery)}");
            
            // Only search other fields if title search doesn't return enough results
            // This will be handled by the fallback search if needed
        }

        // Add filters if provided
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                for (int i = 0; i < filter.Value.Length; i++)
                {
                    queryParams.Add($"filters[category_values][name][$in][{i}]={Uri.EscapeDataString(filter.Value[i])}");
                }
            }
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

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
            
            // If we have a search query and got fewer than 5 results, try a broader search
            if (!string.IsNullOrEmpty(searchQuery) && products.Count < 5)
            {
                var broaderResults = await GetBroaderSearchResults(searchQuery, page, pageSize, filters);
                if (broaderResults.Any())
                {
                    products = broaderResults;
                }
            }
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && products.Any())
            {
                await _cacheService.SetAsync(cacheKey, products, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products listing for page {Page} for {Duration}", page, cacheDuration.Value);
            }
            
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return new List<Product>();
        }
    }

    private async Task<List<Product>> GetBroaderSearchResults(string searchQuery, int page, int pageSize, Dictionary<string, string[]>? filters)
    {
        var queryParams = new List<string>
        {
            $"pagination[page]={page}",
            $"pagination[pageSize]={pageSize}",
            "fields[0]=title",
            "fields[1]=short_description", 
            "fields[2]=fips_id",
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name"
        };

        // Use broader search with $containsi on multiple fields
        queryParams.Add($"filters[$or][0][title][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][1][short_description][$containsi]={Uri.EscapeDataString(searchQuery)}");
        queryParams.Add($"filters[$or][2][long_description][$containsi]={Uri.EscapeDataString(searchQuery)}");

        // Add filters if provided
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                for (int i = 0; i < filter.Value.Length; i++)
                {
                    queryParams.Add($"filters[category_values][category_type][name][$eq]={Uri.EscapeDataString(filter.Key)}&filters[category_values][slug][$in]={Uri.EscapeDataString(filter.Value[i])}");
                }
            }
        }

        var queryString = string.Join("&", queryParams);
        var url = $"products?{queryString}";

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
        var cacheKey = $"product_detail_{id}";
        
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
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
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
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var result = JsonSerializer.Deserialize<ApiResponse<Product>>(jsonContent, options);
            var product = result?.Data;
            
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
        var cacheKey = $"product_document_{documentId}";
        
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
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
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
        var cacheKey = $"product_fips_{fipsId}";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Product by FIPS ID {FipsId}", fipsId);
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            $"filters[fips_id][$eq]={Uri.EscapeDataString(fipsId)}",
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
            "populate[category_values][populate][category_type][fields][0]=name",
            "populate[product_contacts][fields][0]=role",
            "populate[product_contacts][populate][users_permissions_user][fields][0]=first_name",
            "populate[product_contacts][populate][users_permissions_user][fields][1]=last_name",
            "populate[product_contacts][populate][users_permissions_user][fields][2]=display_name",
            "populate[product_contacts][populate][users_permissions_user][fields][3]=email",
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
                _logger.LogInformation("CACHED: Product by FIPS ID {FipsId} for {Duration}", fipsId, cacheDuration.Value);
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

    public async Task<int> GetProductsCountAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "products_count_total";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<int>(cacheKey);
            if (cachedResult > 0)
            {
                _logger.LogInformation("CACHE HIT: Products count");
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            "pagination[pageSize]=1",
            "fields[0]=id" // Only need ID for count
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
            var count = result?.Meta?.Pagination?.Total ?? 0;
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && count > 0)
            {
                await _cacheService.SetAsync(cacheKey, count, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products count {Count} for {Duration}", count, cacheDuration.Value);
            }
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API Error: {Url}", url);
            return 0;
        }
    }

    public async Task<List<Product>> GetProductsForFilterCountsAsync(TimeSpan? cacheDuration = null)
    {
        var cacheKey = "products_filter_counts";
        
        // Try to get from cache first
        if (cacheDuration.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<List<Product>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("CACHE HIT: Products for filter counts");
                return cachedResult;
            }
        }

        var queryParams = new List<string>
        {
            "pagination[pageSize]=1000", // Reasonable limit for filter counts
            // Only fields needed for filtering
            "fields[0]=id",
            "fields[1]=fips_id",
            "populate[category_values][fields][0]=name",
            "populate[category_values][fields][1]=slug",
            "populate[category_values][populate][category_type][fields][0]=name"
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
            var products = result?.Data ?? new List<Product>();
            
            // Cache the result if cache duration is specified
            if (cacheDuration.HasValue && products.Any())
            {
                await _cacheService.SetAsync(cacheKey, products, cacheDuration.Value);
                _logger.LogInformation("CACHED: Products for filter counts for {Duration}", cacheDuration.Value);
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
}
