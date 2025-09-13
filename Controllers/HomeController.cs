using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly CmsApiService _cmsApiService;

    public HomeController(ILogger<HomeController> logger, CmsApiService cmsApiService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
    }

    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get only published products for the count (cache for 5 minutes)
            var publishedProducts = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>("products?filters[publishedAt][$notNull]=true&pagination[pageSize]=1", TimeSpan.FromMinutes(5));
            var publishedCount = publishedProducts?.Meta?.Pagination?.Total ?? 0;
            
            // Get published category types count (cache for 10 minutes)
            var categoryTypes = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryType>>("category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&pagination[pageSize]=1", TimeSpan.FromMinutes(10));
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
