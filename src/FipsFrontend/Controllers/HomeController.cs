using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Configuration;
using FipsFrontend.Services;
using FipsFrontend.Models;
using System.Diagnostics;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly CmsApiService _cmsApiService;
    private readonly IOptimizedCmsApiService _optimizedCmsApiService;
    private readonly IConfiguration _configuration;

    private readonly IHostEnvironment _environment;

    public HomeController(ILogger<HomeController> logger, CmsApiService cmsApiService, IOptimizedCmsApiService optimizedCmsApiService, IConfiguration configuration, IHostEnvironment environment)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _optimizedCmsApiService = optimizedCmsApiService;
        _configuration = configuration;
        _environment = environment;
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
            var publishedCount = await _optimizedCmsApiService.GetProductsCountAsync(homeDuration);
            
            // Get published category types count - optimized to return only count
            var categoryTypes = await _optimizedCmsApiService.GetCategoryTypesAsync(categoryTypesDuration);
            var categoryTypesCount = categoryTypes?.Count ?? 0;
            
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
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        // Log the error
        _logger.LogError(exception, "Unhandled exception occurred");

        // The exception and its stack are for developers; everyone else gets the request id to quote.
        var showDetails = _environment.IsDevelopmentLike();
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Exception = showDetails ? exception : null,
            ExceptionDetails = showDetails ? exception?.ToString() : null
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult NotFound()
    {
        Response.StatusCode = 404;
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
