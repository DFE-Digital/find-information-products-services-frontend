using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FipsFrontend.Services;

public interface ISecurityService
{
    string GetNonce(HttpContext context);
    bool HasRole(HttpContext context, string role);
    bool IsAuthenticated(HttpContext context);
    string GetUserId(HttpContext context);
    List<string> GetUserRoles(HttpContext context);
    bool CanAccessResource(HttpContext context, string resource);
}

public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
    }

    public string GetNonce(HttpContext context)
    {
        if (context.Items.TryGetValue("Nonce", out var nonce) && nonce is string nonceString)
        {
            return nonceString;
        }
        
        _logger.LogWarning("No nonce found in context");
        return string.Empty;
    }

    public bool HasRole(HttpContext context, string role)
    {
        if (!IsAuthenticated(context))
        {
            return false;
        }

        var user = context.User;
        return user.IsInRole(role) || user.HasClaim(ClaimTypes.Role, role);
    }

    public bool IsAuthenticated(HttpContext context)
    {
        return context.User?.Identity?.IsAuthenticated ?? false;
    }

    public string GetUserId(HttpContext context)
    {
        if (!IsAuthenticated(context))
        {
            return string.Empty;
        }

        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               context.User.FindFirst("sub")?.Value ?? 
               string.Empty;
    }

    public List<string> GetUserRoles(HttpContext context)
    {
        if (!IsAuthenticated(context))
        {
            return new List<string>();
        }

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.AddRange(context.User.FindAll("role").Select(c => c.Value));
        
        return roles.Distinct().ToList();
    }

    public bool CanAccessResource(HttpContext context, string resource)
    {
        if (!IsAuthenticated(context))
        {
            return false;
        }

        // Define resource-based access control
        var userRoles = GetUserRoles(context);
        
        return resource.ToLowerInvariant() switch
        {
            "admin" => userRoles.Contains("Admin") || userRoles.Contains("Administrator"),
            "editor" => userRoles.Contains("Editor") || userRoles.Contains("Admin") || userRoles.Contains("Administrator"),
            "viewer" => userRoles.Contains("Viewer") || userRoles.Contains("Editor") || userRoles.Contains("Admin") || userRoles.Contains("Administrator"),
            _ => false
        };
    }
}
