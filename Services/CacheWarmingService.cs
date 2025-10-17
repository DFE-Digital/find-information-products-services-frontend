using FipsFrontend.Models;
using System.Linq;

namespace FipsFrontend.Services;

public interface ICacheWarmingService
{
    Task WarmCriticalDataAsync();
    Task WarmProductDataAsync();
    Task WarmCategoryDataAsync();
    Task WarmHomePageDataAsync();
    Task<bool> IsDataWarmedAsync(string dataType);
    Task WarmAllDataAsync();
}

public class CacheWarmingService : ICacheWarmingService
{
    private readonly CmsApiService _cmsApiService;
    private readonly IOptimizedCmsApiService _optimizedCmsApiService;
    private readonly IEnhancedCacheService _cacheService;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _warmedDataTypes;

    public CacheWarmingService(
        CmsApiService cmsApiService,
        IOptimizedCmsApiService optimizedCmsApiService,
        IEnhancedCacheService cacheService,
        ILogger<CacheWarmingService> logger,
        IConfiguration configuration)
    {
        _cmsApiService = cmsApiService;
        _optimizedCmsApiService = optimizedCmsApiService;
        _cacheService = cacheService;
        _logger = logger;
        _configuration = configuration;
        _warmedDataTypes = new HashSet<string>();
    }

    public async Task WarmCriticalDataAsync()
    {
        _logger.LogInformation("Starting cache warming for critical data");

        try
        {
            var tasks = new List<Task>
            {
                WarmHomePageDataAsync(),
                WarmCategoryDataAsync(),
                WarmProductDataAsync()
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cache warming completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
        }
    }

    public async Task WarmProductDataAsync()
    {
        try
        {
            _logger.LogInformation("Warming product data cache");

            // Warm product list (first page)
            var productsResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>(
                "products?filters[publishedAt][$notNull]=true&pagination[page]=1&pagination[pageSize]=20",
                TimeSpan.FromMinutes(5));

            if (productsResponse?.Data != null)
            {
                await _cacheService.SetAsync("products_page_1", productsResponse.Data);
                _logger.LogInformation("Warmed product list cache with {Count} products", productsResponse.Data.Count);
            }

            // Warm product count
            var countResponse = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>(
                "products?filters[publishedAt][$notNull]=true&pagination[pageSize]=1",
                TimeSpan.FromMinutes(10));

            if (countResponse?.Meta?.Pagination?.Total != null)
            {
                await _cacheService.SetAsync("products_count", countResponse.Meta.Pagination.Total);
                _logger.LogInformation("Warmed product count cache: {Count}", countResponse.Meta.Pagination.Total);
            }

            _warmedDataTypes.Add("products");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming product data cache");
        }
    }

    public async Task WarmCategoryDataAsync()
    {
        try
        {
            _logger.LogInformation("Warming category data cache");

            // Warm category types
            var categoryTypes = await _cmsApiService.GetAllCategoryTypes(TimeSpan.FromMinutes(15));
            if (categoryTypes != null)
            {
                await _cacheService.SetAsync("category_types", categoryTypes);
                _logger.LogInformation("Warmed category types cache with {Count} types", categoryTypes.Count);
            }

            // Warm category values
            var categoryValues = await _cmsApiService.GetAllCategoryValues(TimeSpan.FromMinutes(15));
            if (categoryValues != null)
            {
                await _cacheService.SetAsync("category_values", categoryValues);
                _logger.LogInformation("Warmed category values cache with {Count} values", categoryValues.Count);
            }

            // Warm specific category filters
            var filterTypes = new[] { "Phase", "Channel", "Business area", "User group", "Type" };
            foreach (var filterType in filterTypes)
            {
                var values = await _cmsApiService.GetCategoryValuesForFilter(filterType, TimeSpan.FromMinutes(15));
                if (values != null)
                {
                    await _cacheService.SetAsync($"category_filter_{filterType.ToLower()}", values);
                    _logger.LogInformation("Warmed {FilterType} filter cache with {Count} values", filterType, values.Count);
                }
            }

            _warmedDataTypes.Add("categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming category data cache");
        }
    }

    public async Task WarmHomePageDataAsync()
    {
        try
        {
            _logger.LogInformation("Warming home page data cache");

            // Warm published products count
            var publishedProducts = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>(
                "products?filters[publishedAt][$notNull]=true&pagination[pageSize]=1",
                TimeSpan.FromMinutes(5));

            var publishedCount = publishedProducts?.Meta?.Pagination?.Total ?? 0;
            await _cacheService.SetAsync("home_published_products_count", publishedCount);

            // Warm category types count
            var categoryTypes = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryType>>(
                "category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&pagination[pageSize]=1",
                TimeSpan.FromMinutes(10));

            var categoryTypesCount = categoryTypes?.Meta?.Pagination?.Total ?? 0;
            await _cacheService.SetAsync("home_category_types_count", categoryTypesCount);

            _logger.LogInformation("Warmed home page data cache - Products: {ProductCount}, Categories: {CategoryCount}", 
                publishedCount, categoryTypesCount);

            _warmedDataTypes.Add("home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming home page data cache");
        }
    }

    public async Task<bool> IsDataWarmedAsync(string dataType)
    {
        await Task.CompletedTask; // Remove async warning
        return _warmedDataTypes.Contains(dataType.ToLower());
    }

    public async Task WarmAllDataAsync()
    {
        _logger.LogInformation("Starting comprehensive cache warming");

        try
        {
            // Get cache durations from configuration
            var durations = _configuration.GetSection("Caching:Durations");
            var homeDuration = TimeSpan.FromMinutes(durations.GetValue<double>("Home", 15));
            var productsDuration = TimeSpan.FromMinutes(durations.GetValue<double>("Products", 15));
            var productDetailDuration = TimeSpan.FromMinutes(durations.GetValue<double>("ProductDetail", 10));
            var categoryTypesDuration = TimeSpan.FromMinutes(durations.GetValue<double>("CategoryTypes", 15));
            var categoryValuesDuration = TimeSpan.FromMinutes(durations.GetValue<double>("CategoryValues", 15));

            var tasks = new List<Task>
            {
                WarmHomePageDataAsync(),
                WarmProductDataAsync(),
                WarmCategoryDataAsync(),
                WarmProductDetailsAsync(productDetailDuration),
                WarmSearchDataAsync()
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("Comprehensive cache warming completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during comprehensive cache warming");
        }
    }

    private async Task WarmProductDetailsAsync(TimeSpan cacheDuration)
    {
        try
        {
            _logger.LogInformation("Warming product details cache");

            // Get first 10 products and cache their details
            var productsResult = await _optimizedCmsApiService.GetProductsForListingAsync(1, 10, cacheDuration: cacheDuration);
            
            foreach (var product in productsResult.Products.Take(10))
            {
                if (product.Id > 0)
                {
                    await _optimizedCmsApiService.GetProductByIdAsync(product.Id, cacheDuration);
                }
            }

            _logger.LogInformation("Warmed product details cache for {Count} products", productsResult.Products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming product details cache");
        }
    }

    private async Task WarmSearchDataAsync()
    {
        try
        {
            _logger.LogInformation("Warming search data cache");

            var searchDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Search", 2));
            
            // Warm common search terms
            var commonSearches = new[] { "social", "health", "education", "digital", "service" };
            
            foreach (var searchTerm in commonSearches)
            {
                await _optimizedCmsApiService.GetProductsForListingAsync(1, 20, searchTerm, cacheDuration: searchDuration);
            }

            _logger.LogInformation("Warmed search data cache for {Count} common terms", commonSearches.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming search data cache");
        }
    }
}
