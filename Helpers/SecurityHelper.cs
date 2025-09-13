using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using FipsFrontend.Services;

namespace FipsFrontend.Helpers;

public static class SecurityHelper
{
    public static string GetNonce(this IHtmlHelper htmlHelper)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.GetNonce(htmlHelper.ViewContext.HttpContext) ?? string.Empty;
    }

    public static bool HasRole(this IHtmlHelper htmlHelper, string role)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.HasRole(htmlHelper.ViewContext.HttpContext, role) ?? false;
    }

    public static bool IsAuthenticated(this IHtmlHelper htmlHelper)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.IsAuthenticated(htmlHelper.ViewContext.HttpContext) ?? false;
    }

    public static string GetUserId(this IHtmlHelper htmlHelper)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.GetUserId(htmlHelper.ViewContext.HttpContext) ?? string.Empty;
    }

    public static List<string> GetUserRoles(this IHtmlHelper htmlHelper)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.GetUserRoles(htmlHelper.ViewContext.HttpContext) ?? new List<string>();
    }

    public static bool CanAccessResource(this IHtmlHelper htmlHelper, string resource)
    {
        var securityService = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<ISecurityService>();
        return securityService?.CanAccessResource(htmlHelper.ViewContext.HttpContext, resource) ?? false;
    }

    public static string GetSecurityScript(this IHtmlHelper htmlHelper)
    {
        var nonce = htmlHelper.GetNonce();
        var isAuthenticated = htmlHelper.IsAuthenticated();
        var userId = htmlHelper.GetUserId();
        var userRoles = htmlHelper.GetUserRoles();

        return $@"
<script nonce=""{nonce}"">
    window.FIPS = window.FIPS || {{}};
    window.FIPS.Security = {{
        isAuthenticated: {isAuthenticated.ToString().ToLowerInvariant()},
        userId: '{userId}',
        userRoles: {System.Text.Json.JsonSerializer.Serialize(userRoles)},
        nonce: '{nonce}',
        hasRole: function(role) {{
            return this.userRoles.includes(role);
        }},
        canAccess: function(resource) {{
            switch(resource.toLowerCase()) {{
                case 'admin':
                    return this.hasRole('Admin') || this.hasRole('Administrator');
                case 'editor':
                    return this.hasRole('Editor') || this.hasRole('Admin') || this.hasRole('Administrator');
                case 'viewer':
                    return this.hasRole('Viewer') || this.hasRole('Editor') || this.hasRole('Admin') || this.hasRole('Administrator');
                default:
                    return false;
            }}
        }},
        checkSessionTimeout: function() {{
            // Check if session is still valid
            fetch('/api/session/check', {{
                method: 'GET',
                credentials: 'include',
                headers: {{
                    'X-Requested-With': 'XMLHttpRequest'
                }}
            }}).then(response => {{
                if (!response.ok) {{
                    window.location.href = '/Account/Login';
                }}
            }}).catch(() => {{
                window.location.href = '/Account/Login';
            }});
        }}
    }};

    // Set up session timeout check every 5 minutes
    setInterval(function() {{
        if (window.FIPS.Security.isAuthenticated) {{
            window.FIPS.Security.checkSessionTimeout();
        }}
    }}, 5 * 60 * 1000);
</script>";
    }
}
