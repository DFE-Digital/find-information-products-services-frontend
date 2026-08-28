using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;

namespace FipsFrontend.Controllers;

// [Authorize] // Temporarily disabled for testing
public class PerformanceController : Controller
{
    private readonly ILogger<PerformanceController> _logger;
    private readonly IPerformanceAnalysisService _performanceAnalysisService;
    private readonly ICachePerformanceService _cachePerformanceService;

    public PerformanceController(
        ILogger<PerformanceController> logger,
        IPerformanceAnalysisService performanceAnalysisService,
        ICachePerformanceService cachePerformanceService)
    {
        _logger = logger;
        _performanceAnalysisService = performanceAnalysisService;
        _cachePerformanceService = cachePerformanceService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Report()
    {
        try
        {
            var report = await _performanceAnalysisService.GeneratePerformanceReportAsync();
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
            TempData["ErrorMessage"] = "Failed to generate performance report.";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public async Task<IActionResult> SlowEndpoints(int thresholdMs = 1000)
    {
        try
        {
            var slowEndpoints = await _performanceAnalysisService.GetSlowEndpointsAsync(thresholdMs);
            ViewBag.ThresholdMs = thresholdMs;
            return View(slowEndpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slow endpoints");
            return Json(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CacheIssues()
    {
        try
        {
            var issues = await _performanceAnalysisService.GetCachePerformanceIssuesAsync();
            return View(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache issues");
            return Json(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EndpointMetrics()
    {
        try
        {
            var metrics = await _performanceAnalysisService.GetEndpointMetricsAsync();
            return View(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting endpoint metrics");
            return Json(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ApiData()
    {
        try
        {
            var report = await _performanceAnalysisService.GeneratePerformanceReportAsync();
            return Json(new
            {
                slowEndpoints = report.SlowEndpoints,
                cacheIssues = report.CacheIssues,
                endpointMetrics = report.EndpointMetrics,
                cacheMetrics = report.CacheMetrics,
                generatedAt = report.GeneratedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance data");
            return Json(new { error = ex.Message });
        }
    }
}
