using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class CacheController : Controller
{
    private readonly ILogger<CacheController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IMemoryCache _cache;

    public CacheController(ILogger<CacheController> logger, CmsApiService cmsApiService, IMemoryCache cache)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _cache = cache;
    }

    [HttpPost]
    public IActionResult ClearAllCache()
    {
        try
        {
            _cmsApiService.ClearCache();
            _logger.LogInformation("All CMS API cache cleared by user: {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "All caches have been cleared successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            TempData["ErrorMessage"] = "An error occurred while clearing the cache.";
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult ClearProductCache()
    {
        try
        {
            _cmsApiService.ClearCacheForEndpoint("products");
            _logger.LogInformation("Product cache cleared by user: {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Product cache has been cleared successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing product cache");
            TempData["ErrorMessage"] = "An error occurred while clearing the product cache.";
        }

        return RedirectToAction("Index", "Products");
    }

    [HttpPost]
    public IActionResult ClearCategoryCache()
    {
        try
        {
            _cmsApiService.ClearCacheForEndpoint("category-types");
            _cmsApiService.ClearCacheForEndpoint("category-values");
            _logger.LogInformation("Category cache cleared by user: {User}", User.Identity?.Name);
            TempData["SuccessMessage"] = "Category cache has been cleared successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing category cache");
            TempData["ErrorMessage"] = "An error occurred while clearing the category cache.";
        }

        return RedirectToAction("Index", "Categories");
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "cache";
        
        var cacheInfo = _cmsApiService.GetCacheInfo();
        
        var viewModel = new CacheInfoViewModel
        {
            CacheEntries = cacheInfo.Values.ToList(),
            TotalEntries = cacheInfo.Count,
            ActiveEntries = cacheInfo.Values.Count(c => !c.IsExpired),
            ExpiredEntries = cacheInfo.Values.Count(c => c.IsExpired),
            TotalHits = cacheInfo.Values.Sum(c => c.HitCount)
        };
        
        return View(viewModel);
    }
}
