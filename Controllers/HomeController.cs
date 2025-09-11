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

    public async Task<IActionResult> Index()
    {
        try
        {
            // Get only published products for the count
            var publishedProducts = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>("products?filters[publishedAt][$notNull]=true&pagination[pageSize]=1");
            var publishedCount = publishedProducts?.Meta?.Pagination?.Total ?? 0;
            
            // Get published category types count
            var categoryTypes = await _cmsApiService.GetAsync<ApiCollectionResponse<CategoryType>>("category-types?filters[publishedAt][$notNull]=true&filters[enabled]=true&pagination[pageSize]=1");
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
