using System.Text.Json;
using FipsFrontend.Models;

namespace FipsFrontend.Services;

public interface IApiLoggingService
{
    Task LogApiRequestAsync(string controller, string action, string endpoint, object? requestData = null);
    Task LogApiResponseAsync(string controller, string action, string endpoint, object? responseData, TimeSpan duration, bool fromCache = false);
    Task LogApiErrorAsync(string controller, string action, string endpoint, Exception exception, TimeSpan duration);
}

public class NullApiLoggingService : IApiLoggingService
{
    public Task LogApiRequestAsync(string controller, string action, string endpoint, object? requestData = null)
    {
        return Task.CompletedTask;
    }

    public Task LogApiResponseAsync(string controller, string action, string endpoint, object responseData, TimeSpan duration, bool fromCache = false)
    {
        return Task.CompletedTask;
    }

    public Task LogApiErrorAsync(string controller, string action, string endpoint, Exception exception, TimeSpan duration)
    {
        return Task.CompletedTask;
    }
}

public class ApiLoggingService : IApiLoggingService
{
    private readonly ILogger<ApiLoggingService> _logger;
    private readonly string _logDirectory;

    public ApiLoggingService(ILogger<ApiLoggingService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _logDirectory = Path.Combine(environment.ContentRootPath, "logs");
        
        // Ensure logs directory exists
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public async Task LogApiRequestAsync(string controller, string action, string endpoint, object? requestData = null)
    {
        var logEntry = new ApiLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "REQUEST",
            Controller = controller,
            Action = action,
            Endpoint = endpoint,
            RequestData = requestData,
            ThreadId = Environment.CurrentManagedThreadId
        };

        await WriteLogEntryAsync(logEntry);
        
        _logger.LogInformation("=== API REQUEST START ===");
        _logger.LogInformation("Controller: {Controller}", controller);
        _logger.LogInformation("Action: {Action}", action);
        _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
        if (requestData != null)
        {
            _logger.LogInformation("Request Data: {RequestData}", JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true }));
        }
        _logger.LogInformation("Thread ID: {ThreadId}", Environment.CurrentManagedThreadId);
        _logger.LogInformation("=== API REQUEST END ===");
    }

    public async Task LogApiResponseAsync(string controller, string action, string endpoint, object? responseData, TimeSpan duration, bool fromCache = false)
    {
        var logEntry = new ApiLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "RESPONSE",
            Controller = controller,
            Action = action,
            Endpoint = endpoint,
            ResponseData = responseData,
            Duration = duration,
            FromCache = fromCache,
            ThreadId = Environment.CurrentManagedThreadId
        };

        await WriteLogEntryAsync(logEntry);
        
        _logger.LogInformation("=== API RESPONSE START ===");
        _logger.LogInformation("Controller: {Controller}", controller);
        _logger.LogInformation("Action: {Action}", action);
        _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
        _logger.LogInformation("Duration: {Duration}ms", duration.TotalMilliseconds);
        _logger.LogInformation("From Cache: {FromCache}", fromCache);
        _logger.LogInformation("Thread ID: {ThreadId}", Environment.CurrentManagedThreadId);
        
        if (responseData != null)
        {
            // Log response summary for large objects
            var responseJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });
            if (responseJson.Length > 10000) // Large response
            {
                _logger.LogInformation("Response Data (Large - {Size} chars): {ResponseSummary}", 
                    responseJson.Length, GetResponseSummary(responseData));
            }
            else
            {
                _logger.LogInformation("Response Data: {ResponseData}", responseJson);
            }
        }
        _logger.LogInformation("=== API RESPONSE END ===");
    }

    public async Task LogApiErrorAsync(string controller, string action, string endpoint, Exception exception, TimeSpan duration)
    {
        var logEntry = new ApiLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "ERROR",
            Controller = controller,
            Action = action,
            Endpoint = endpoint,
            ErrorMessage = exception.Message,
            ErrorStackTrace = exception.StackTrace,
            Duration = duration,
            ThreadId = Environment.CurrentManagedThreadId
        };

        await WriteLogEntryAsync(logEntry);
        
        _logger.LogError(exception, "=== API ERROR START ===");
        _logger.LogError("Controller: {Controller}", controller);
        _logger.LogError("Action: {Action}", action);
        _logger.LogError("Endpoint: {Endpoint}", endpoint);
        _logger.LogError("Duration: {Duration}ms", duration.TotalMilliseconds);
        _logger.LogError("Error: {ErrorMessage}", exception.Message);
        _logger.LogError("Thread ID: {ThreadId}", Environment.CurrentManagedThreadId);
        _logger.LogError("=== API ERROR END ===");
    }

    private async Task WriteLogEntryAsync(ApiLogEntry logEntry)
    {
        try
        {
            var logFileName = $"api-requests-{DateTimeOffset.UtcNow:yyyy-MM-dd}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);
            
            var logLine = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
            
            await File.AppendAllTextAsync(logFilePath, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write API log entry to file");
        }
    }

    private string GetResponseSummary(object responseData)
    {
        try
        {
            if (responseData is IEnumerable<object> enumerable)
            {
                var count = enumerable.Count();
                return $"Collection with {count} items";
            }
            
            if (responseData is ApiCollectionResponse<object> apiCollection)
            {
                var count = apiCollection.Data?.Count ?? 0;
                return $"API Collection Response with {count} items";
            }
            
            if (responseData is ApiResponse<object> apiResponse)
            {
                return $"API Response with data type: {apiResponse.Data?.GetType().Name ?? "null"}";
            }
            
            return $"Object of type: {responseData.GetType().Name}";
        }
        catch
        {
            return "Unknown response type";
        }
    }
}

public class ApiLogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // REQUEST, RESPONSE, ERROR
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public object? RequestData { get; set; }
    public object? ResponseData { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool FromCache { get; set; }
    public int ThreadId { get; set; }
}
