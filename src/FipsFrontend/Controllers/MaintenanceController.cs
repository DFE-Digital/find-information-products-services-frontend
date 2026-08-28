using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;

namespace FipsFrontend.Controllers;

public class MaintenanceController : Controller
{
    private readonly ILogger<MaintenanceController> _logger;
    private readonly ICmsHealthService _cmsHealthService;

    public MaintenanceController(ILogger<MaintenanceController> logger, ICmsHealthService cmsHealthService)
    {
        _logger = logger;
        _cmsHealthService = cmsHealthService;
    }

    public async Task<IActionResult> Index()
    {
        // Get diagnostic information for debugging
        var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
        var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var maintenanceModeEnabled = configuration.GetValue<bool>("MaintenanceMode:Enabled", false);
        
        // Pass diagnostic data to the view
        ViewBag.DebugInfo = new
        {
            MaintenanceModeEnabled = maintenanceModeEnabled,
            CmsHealthServiceMaintenanceMode = isMaintenanceMode,
            CmsAvailable = isCmsAvailable,
            Timestamp = DateTime.UtcNow,
            RequestPath = HttpContext.Request.Path,
            UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
            RemoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        
        return View();
    }

    [HttpGet]
    [Route("api/health")]
    public async Task<IActionResult> HealthCheck()
    {
        var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
        var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var maintenanceModeEnabled = configuration.GetValue<bool>("MaintenanceMode:Enabled", false);
        
        // Perform a fresh CMS health check for detailed diagnostics
        var cmsHealthCheckResult = await _cmsHealthService.CheckCmsHealthAsync();
        
        var healthStatus = new
        {
            status = isCmsAvailable && !isMaintenanceMode ? "healthy" : "unhealthy",
            cmsAvailable = isCmsAvailable,
            maintenanceMode = isMaintenanceMode,
            configurationMaintenanceMode = maintenanceModeEnabled,
            cmsHealthCheckResult = cmsHealthCheckResult,
            timestamp = DateTime.UtcNow,
            debug = new
            {
                whyMaintenanceShown = !isCmsAvailable ? "CMS unavailable" : 
                                     isMaintenanceMode ? "Health service maintenance mode enabled" :
                                     maintenanceModeEnabled ? "Configuration maintenance mode enabled" : "Unknown",
                cmsBaseUrl = configuration["CmsApi:BaseUrl"] ?? "Not configured",
                healthCheckTimeout = configuration["MaintenanceMode:HealthCheckTimeoutSeconds"] ?? "10",
                healthCheckInterval = configuration["MaintenanceMode:HealthCheckIntervalSeconds"] ?? "30"
            }
        };

        return Json(healthStatus);
    }
}
