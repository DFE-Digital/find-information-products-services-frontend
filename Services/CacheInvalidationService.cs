using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface ICacheInvalidationService
{
    Task InvalidateProductCacheAsync(int? productId = null);
    Task InvalidateCategoryCacheAsync(string? categoryType = null);
    Task InvalidateSearchCacheAsync();
    Task InvalidateHomePageCacheAsync();
    Task InvalidateAllCacheAsync();
    Task InvalidateByPatternAsync(string pattern);
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IEnhancedCacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IEnhancedCacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidateProductCacheAsync(int? productId = null)
    {
        try
        {
            if (productId.HasValue)
            {
                // Invalidate specific product cache
                await _cacheService.RemoveAsync($"product_{productId}");
                _logger.LogInformation("Invalidated cache for product {ProductId}", productId);
            }
            else
            {
                // Invalidate all product-related cache
                await _cacheService.RemoveByPatternAsync("products");
                await _cacheService.RemoveByPatternAsync("product_");
                await _cacheService.RemoveAsync("products_count");
                await _cacheService.RemoveAsync("home_published_products_count");
                _logger.LogInformation("Invalidated all product cache");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating product cache for product {ProductId}", productId);
        }
    }

    public async Task InvalidateCategoryCacheAsync(string? categoryType = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(categoryType))
            {
                // Invalidate specific category type cache
                await _cacheService.RemoveAsync($"category_filter_{categoryType.ToLower()}");
                _logger.LogInformation("Invalidated cache for category type {CategoryType}", categoryType);
            }
            else
            {
                // Invalidate all category-related cache
                await _cacheService.RemoveByPatternAsync("category");
                await _cacheService.RemoveAsync("home_category_types_count");
                _logger.LogInformation("Invalidated all category cache");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating category cache for type {CategoryType}", categoryType);
        }
    }

    public async Task InvalidateSearchCacheAsync()
    {
        try
        {
            await _cacheService.RemoveByPatternAsync("search");
            _logger.LogInformation("Invalidated search cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating search cache");
        }
    }

    public async Task InvalidateHomePageCacheAsync()
    {
        try
        {
            await _cacheService.RemoveAsync("home");
            await _cacheService.RemoveAsync("home_published_products_count");
            await _cacheService.RemoveAsync("home_category_types_count");
            _logger.LogInformation("Invalidated home page cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating home page cache");
        }
    }

    public async Task InvalidateAllCacheAsync()
    {
        try
        {
            // This would need to be implemented based on the specific cache implementation
            // For now, we'll remove common patterns
            var patterns = new[]
            {
                "products",
                "product_",
                "category",
                "search",
                "home"
            };

            foreach (var pattern in patterns)
            {
                await _cacheService.RemoveByPatternAsync(pattern);
            }

            _logger.LogInformation("Invalidated all cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all cache");
        }
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        try
        {
            await _cacheService.RemoveByPatternAsync(pattern);
            _logger.LogInformation("Invalidated cache matching pattern {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by pattern {Pattern}", pattern);
        }
    }
}
