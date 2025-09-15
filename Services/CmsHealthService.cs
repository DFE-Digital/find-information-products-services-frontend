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
        try
        {
            var healthCheckUrl = $"{_baseUrl.TrimEnd('/')}/health";
            var timeout = TimeSpan.FromSeconds(10); // Short timeout for health checks

            using var cts = new CancellationTokenSource(timeout);
            
            // Try to reach the CMS health endpoint
            var response = await _httpClient.GetAsync(healthCheckUrl, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("CMS health check successful");
                return true;
            }

            // If health endpoint doesn't exist, try a simple API call
            var testUrl = $"{_baseUrl.TrimEnd('/')}/products?pagination[pageSize]=1";
            var testResponse = await _httpClient.GetAsync(testUrl, cts.Token);
            
            if (testResponse.IsSuccessStatusCode)
            {
                _logger.LogDebug("CMS API test call successful");
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
