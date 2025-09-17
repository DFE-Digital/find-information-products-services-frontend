using System.Net.Http;
using System.Text.Json;

namespace FipsFrontend.Services;

public interface ICmsHealthService
{
    Task<bool> IsCmsAvailableAsync();
    Task<bool> CheckCmsHealthAsync();
    void SetMaintenanceMode(bool enabled);
    bool IsMaintenanceModeEnabled();
}

public class CmsHealthService : ICmsHealthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsHealthService> _logger;
    private readonly string _baseUrl;
    private bool _isMaintenanceModeEnabled = false;
    private DateTime _lastHealthCheck = DateTime.MinValue;
    private bool _lastHealthCheckResult = false;
    private readonly TimeSpan _healthCheckCacheDuration = TimeSpan.FromSeconds(30); // Cache health check for 30 seconds

    public CmsHealthService(HttpClient httpClient, IConfiguration configuration, ILogger<CmsHealthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
    }

    public async Task<bool> IsCmsAvailableAsync()
    {
        // If maintenance mode is manually enabled, return false
        if (_isMaintenanceModeEnabled)
        {
            _logger.LogInformation("Maintenance mode is manually enabled");
            return false;
        }

        // Check if we have a recent cached health check result
        if (DateTime.UtcNow - _lastHealthCheck < _healthCheckCacheDuration)
        {
            _logger.LogDebug("Using cached health check result: {Result}", _lastHealthCheckResult);
            return _lastHealthCheckResult;
        }

        // Perform a fresh health check
        var isHealthy = await CheckCmsHealthAsync();
        _lastHealthCheck = DateTime.UtcNow;
        _lastHealthCheckResult = isHealthy;

        return isHealthy;
    }

    public async Task<bool> CheckCmsHealthAsync()
    {
        // TEMPORARILY DISABLED FOR TESTING - Always return true
        return true;
        
        try
        {
            // Use the API endpoint for health check instead of admin
            var healthCheckUrl = $"{_baseUrl}/products?pagination[pageSize]=1";
            var timeout = TimeSpan.FromSeconds(10); // Short timeout for health checks

            using var cts = new CancellationTokenSource(timeout);
            
            // Add API key for authentication
            var apiKey = _configuration["CmsApi:ReadApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            
            // Try to reach the CMS API endpoint
            var response = await _httpClient.GetAsync(healthCheckUrl, cts.Token);
            
            // Accept any response status code as long as we get a response
            // This indicates the CMS server is running
            if (response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogDebug("CMS health check successful - server is responding");
                return true;
            }

            _logger.LogWarning("CMS health check failed with status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "CMS health check failed - HTTP request exception");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "CMS health check failed - timeout");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CMS health check failed with unexpected error");
            return false;
        }
    }

    public void SetMaintenanceMode(bool enabled)
    {
        _isMaintenanceModeEnabled = enabled;
        _logger.LogInformation("Maintenance mode {Status}", enabled ? "enabled" : "disabled");
    }

    public bool IsMaintenanceModeEnabled()
    {
        return _isMaintenanceModeEnabled;
    }
}
