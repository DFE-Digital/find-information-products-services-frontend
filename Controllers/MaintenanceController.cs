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

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("api/health")]
    public async Task<IActionResult> HealthCheck()
    {
        var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
        var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();
        
        var healthStatus = new
        {
            status = isCmsAvailable && !isMaintenanceMode ? "healthy" : "unhealthy",
            cmsAvailable = isCmsAvailable,
            maintenanceMode = isMaintenanceMode,
            timestamp = DateTime.UtcNow
        };

        return Json(healthStatus);
    }
}
