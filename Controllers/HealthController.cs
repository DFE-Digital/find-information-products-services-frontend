using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Services;

namespace FipsFrontend.Controllers;

/// <summary>
/// Health check controller for Azure App Service monitoring
/// </summary>
[ApiController]
public class HealthController : Controller
{
    private readonly ILogger<HealthController> _logger;
    private readonly ICmsHealthService _cmsHealthService;

    public HealthController(ILogger<HealthController> logger, ICmsHealthService cmsHealthService)
    {
        _logger = logger;
        _cmsHealthService = cmsHealthService;
    }

    /// <summary>
    /// Basic health check endpoint for Azure App Service
    /// Returns 200 OK if the application is healthy, 503 if unhealthy
    /// </summary>
    [HttpGet]
    [Route("health")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Health()
    {
        try
        {
            var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
            var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();

            var isHealthy = isCmsAvailable && !isMaintenanceMode;

            if (isHealthy)
            {
                return Ok(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    cms = "Available"
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    cms = isCmsAvailable ? "Available" : "Unavailable",
                    maintenanceMode = isMaintenanceMode
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = "Health check failed"
            });
        }
    }

    /// <summary>
    /// Liveness probe - checks if the application is running
    /// Always returns 200 OK unless the application is completely down
    /// </summary>
    [HttpGet]
    [Route("health/live")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Alive",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness probe - checks if the application is ready to serve traffic
    /// Returns 200 OK if ready, 503 if not ready
    /// </summary>
    [HttpGet]
    [Route("health/ready")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Ready()
    {
        try
        {
            var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
            var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();

            var isReady = isCmsAvailable && !isMaintenanceMode;

            if (isReady)
            {
                return Ok(new
                {
                    status = "Ready",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    status = "NotReady",
                    timestamp = DateTime.UtcNow,
                    reason = isMaintenanceMode ? "Maintenance mode" : "CMS unavailable"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed with exception");
            return StatusCode(503, new
            {
                status = "NotReady",
                timestamp = DateTime.UtcNow,
                error = "Readiness check failed"
            });
        }
    }

    /// <summary>
    /// Detailed health check for monitoring and diagnostics
    /// </summary>
    [HttpGet]
    [Route("health/detailed")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> DetailedHealth()
    {
        try
        {
            var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();
            var isMaintenanceMode = _cmsHealthService.IsMaintenanceModeEnabled();
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            
            var cmsHealthCheckResult = await _cmsHealthService.CheckCmsHealthAsync();

            var healthStatus = new
            {
                status = isCmsAvailable && !isMaintenanceMode ? "Healthy" : "Unhealthy",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    cms = new
                    {
                        available = isCmsAvailable,
                        healthCheck = cmsHealthCheckResult,
                        baseUrl = configuration["CmsApi:BaseUrl"] ?? "Not configured"
                    },
                    maintenanceMode = new
                    {
                        enabled = isMaintenanceMode,
                        configValue = configuration.GetValue<bool>("MaintenanceMode:Enabled", false)
                    }
                },
                application = new
                {
                    name = "FIPS Frontend",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
                }
            };

            if (isCmsAvailable && !isMaintenanceMode)
            {
                return Ok(healthStatus);
            }
            else
            {
                return StatusCode(503, healthStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed with exception");
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                application = new
                {
                    name = "FIPS Frontend",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                }
            });
        }
    }
}

