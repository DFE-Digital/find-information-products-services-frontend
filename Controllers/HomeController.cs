using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, CmsApiService cmsApiService, IConfiguration configuration)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _configuration = configuration;
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get cache durations from configuration
            var homeDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:Home", 15));
            var categoryTypesDuration = TimeSpan.FromMinutes(_configuration.GetValue<double>("Caching:Durations:CategoryTypes", 15));
            
            // Get only published products count - optimized to return only count
            var publishedProducts = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>("products?filters[publishedAt][$notNull]=true&pagination[pageSize]=1&fields[0]=id", homeDuration);
            var publishedCount = publishedProducts?.Meta?.Pagination?.Total ?? 0;
            
            // Get published category types count - optimized to return only count
            var categoryTypes = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryType>>("category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&pagination[pageSize]=1&fields[0]=id", categoryTypesDuration);
            var categoryTypesCount = categoryTypes?.Meta?.Pagination?.Total ?? 0;
            
            var viewModel = new HomeViewModel
            {
                PublishedProductsCount = publishedCount,
                CategoryTypesCount = categoryTypesCount
            };
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            var viewModel = new HomeViewModel
            {
                PublishedProductsCount = 0,
                CategoryTypesCount = 0
            };
            return View(viewModel);
        }
    }

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache for 1 hour
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }

    [HttpGet]
    [Route("api/session/check")]
    public IActionResult CheckSession()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Json(new { valid = true, userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value });
        }
        
        return Json(new { valid = false });
    }
}
