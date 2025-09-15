using FipsFrontend.Services;

namespace FipsFrontend.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;
    private readonly ICmsHealthService _cmsHealthService;
    private readonly IConfiguration _configuration;

    // Paths that should be excluded from maintenance mode
    private readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/health",
        "/maintenance",
        "/css",
        "/js",
        "/images",
        "/favicon.ico",
        "/robots.txt"
    };

    public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger, 
        ICmsHealthService cmsHealthService, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _cmsHealthService = cmsHealthService;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip maintenance check for excluded paths
        if (_excludedPaths.Any(excludedPath => path.StartsWith(excludedPath)))
        {
            await _next(context);
            return;
        }

        // Check if maintenance mode is enabled in configuration
        var maintenanceModeEnabled = _configuration.GetValue<bool>("MaintenanceMode:Enabled", false);
        
        // Check if CMS is available (unless maintenance mode is manually enabled)
        var isCmsAvailable = await _cmsHealthService.IsCmsAvailableAsync();

        // If maintenance mode is enabled OR CMS is not available, show maintenance page
        if (maintenanceModeEnabled || !isCmsAvailable)
        {
            _logger.LogWarning("Showing maintenance page. Maintenance mode: {MaintenanceMode}, CMS available: {CmsAvailable}", 
                maintenanceModeEnabled, isCmsAvailable);

            // Set appropriate status code
            context.Response.StatusCode = 503; // Service Unavailable
            
            // Redirect to maintenance page
            context.Response.Redirect("/maintenance");
            return;
        }

        await _next(context);
    }
}
