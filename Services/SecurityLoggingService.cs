using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FipsFrontend.Services
{
    public interface ISecurityLoggingService
    {
        void LogSecurityEvent(string eventType, string description, string? userId = null, string? ipAddress = null, Dictionary<string, object>? additionalData = null);
        void LogAuthenticationEvent(string eventType, string userId, string ipAddress, bool success, string? reason = null);
        void LogAuthorizationEvent(string eventType, string userId, string resource, bool success, string? reason = null);
        void LogDataAccessEvent(string eventType, string userId, string dataType, string operation, bool success);
    }

    public class SecurityLoggingService : ISecurityLoggingService
    {
        private readonly ILogger<SecurityLoggingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityLoggingService(ILogger<SecurityLoggingService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogSecurityEvent(string eventType, string description, string? userId = null, string? ipAddress = null, Dictionary<string, object>? additionalData = null)
        {
            var context = _httpContextAccessor.HttpContext;
            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                EventType = eventType,
                Description = description,
                UserId = userId ?? GetCurrentUserId(),
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = context?.Request.Headers["User-Agent"].ToString(),
                RequestPath = context?.Request.Path,
                RequestMethod = context?.Request.Method,
                SessionId = context?.Session?.Id,
                AdditionalData = additionalData
            };

            _logger.LogInformation("Security Event: {EventType} - {Description} | User: {UserId} | IP: {IpAddress} | Data: {@LogData}", 
                eventType, description, logData.UserId, logData.IpAddress, logData);
        }

        public void LogAuthenticationEvent(string eventType, string userId, string ipAddress, bool success, string? reason = null)
        {
            var additionalData = new Dictionary<string, object>
            {
                ["Success"] = success,
                ["Reason"] = reason ?? (success ? "Authentication successful" : "Authentication failed")
            };

            LogSecurityEvent($"AUTH_{eventType}", $"Authentication {eventType.ToLower()}", userId, ipAddress, additionalData);
        }

        public void LogAuthorizationEvent(string eventType, string userId, string resource, bool success, string? reason = null)
        {
            var additionalData = new Dictionary<string, object>
            {
                ["Resource"] = resource,
                ["Success"] = success,
                ["Reason"] = reason ?? (success ? "Authorization granted" : "Authorization denied")
            };

            LogSecurityEvent($"AUTHZ_{eventType}", $"Authorization {eventType.ToLower()} for resource: {resource}", userId, null, additionalData);
        }

        public void LogDataAccessEvent(string eventType, string userId, string dataType, string operation, bool success)
        {
            var additionalData = new Dictionary<string, object>
            {
                ["DataType"] = dataType,
                ["Operation"] = operation,
                ["Success"] = success
            };

            LogSecurityEvent($"DATA_{eventType}", $"Data {operation.ToLower()} on {dataType}", userId, null, additionalData);
        }

        private string GetCurrentUserId()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            // Check for forwarded IP first (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
