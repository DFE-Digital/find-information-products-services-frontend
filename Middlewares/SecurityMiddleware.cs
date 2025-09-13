using FipsFrontend.Services;
using System.Security.Claims;

namespace FipsFrontend.Middlewares
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;
        private readonly ISecurityLoggingService _securityLogger;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, ISecurityLoggingService securityLogger)
        {
            _next = next;
            _logger = logger;
            _securityLogger = securityLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var userId = GetUserId(context);
            var ipAddress = GetClientIpAddress(context);

            // Log request start
            _securityLogger.LogSecurityEvent("REQUEST_START", 
                $"Request started: {context.Request.Method} {context.Request.Path}", 
                userId, ipAddress);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log security-relevant exceptions
                _securityLogger.LogSecurityEvent("SECURITY_EXCEPTION", 
                    $"Security exception occurred: {ex.Message}", 
                    userId, ipAddress, 
                    new Dictionary<string, object> { ["Exception"] = ex.ToString() });
                
                throw;
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                var statusCode = context.Response.StatusCode;

                // Log request completion
                _securityLogger.LogSecurityEvent("REQUEST_COMPLETE", 
                    $"Request completed: {context.Request.Method} {context.Request.Path} - Status: {statusCode} - Duration: {duration.TotalMilliseconds}ms", 
                    userId, ipAddress,
                    new Dictionary<string, object> 
                    { 
                        ["StatusCode"] = statusCode,
                        ["Duration"] = duration.TotalMilliseconds,
                        ["UserAgent"] = context.Request.Headers["User-Agent"].ToString()
                    });

                // Log potential security issues
                if (statusCode == 401)
                {
                    _securityLogger.LogAuthenticationEvent("UNAUTHORIZED_ACCESS", userId, ipAddress, false, "Unauthorized access attempt");
                }
                else if (statusCode == 403)
                {
                    _securityLogger.LogAuthorizationEvent("FORBIDDEN_ACCESS", userId, context.Request.Path, false, "Forbidden access attempt");
                }
                else if (statusCode >= 400 && statusCode < 500)
                {
                    _securityLogger.LogSecurityEvent("CLIENT_ERROR", 
                        $"Client error {statusCode} for {context.Request.Path}", 
                        userId, ipAddress);
                }
                else if (statusCode >= 500)
                {
                    _securityLogger.LogSecurityEvent("SERVER_ERROR", 
                        $"Server error {statusCode} for {context.Request.Path}", 
                        userId, ipAddress);
                }
            }
        }

        private string GetUserId(HttpContext context)
        {
            return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                   context.User?.FindFirst(ClaimTypes.Name)?.Value ?? 
                   "Anonymous";
        }

        private string GetClientIpAddress(HttpContext context)
        {
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
