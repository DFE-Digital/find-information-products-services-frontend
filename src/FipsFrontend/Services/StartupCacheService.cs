using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IStartupCacheService
{
    Task WarmCacheOnStartupAsync();
    bool IsStartupWarmingEnabled();
}

public class StartupCacheService : IStartupCacheService
{
    private readonly ICacheWarmingService _cacheWarmingService;
    private readonly ILogger<StartupCacheService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public StartupCacheService(
        ICacheWarmingService cacheWarmingService,
        ILogger<StartupCacheService> logger,
        IConfiguration configuration,
        IHostApplicationLifetime applicationLifetime)
    {
        _cacheWarmingService = cacheWarmingService;
        _logger = logger;
        _configuration = configuration;
        _applicationLifetime = applicationLifetime;
    }

    public async Task WarmCacheOnStartupAsync()
    {
        if (!IsStartupWarmingEnabled())
        {
            _logger.LogInformation("Startup cache warming is disabled");
            return;
        }

        try
        {
            _logger.LogInformation("Starting cache warming on application startup");
            
            // Run cache warming in background to avoid blocking startup
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait a bit for the application to fully start
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    
                    _logger.LogInformation("Beginning startup cache warming");
                    await _cacheWarmingService.WarmCriticalDataAsync();
                    _logger.LogInformation("Startup cache warming completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during startup cache warming");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating startup cache warming");
        }
    }

    public bool IsStartupWarmingEnabled()
    {
        return _configuration.GetValue<bool>("Caching:Performance:EnableWarming", true);
    }
}

public class StartupCacheHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<StartupCacheHostedService> _logger;

    public StartupCacheHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<StartupCacheHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Startup cache service starting");
            
            using var scope = _serviceScopeFactory.CreateScope();
            var startupCacheService = scope.ServiceProvider.GetRequiredService<IStartupCacheService>();
            
            await startupCacheService.WarmCacheOnStartupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in startup cache service");
        }
    }
}
