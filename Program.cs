using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using FipsFrontend.Services;
using FipsFrontend.Middlewares;
using FipsFrontend.Models;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/app-{Date}.log");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Temporarily disable authentication for development
// TODO: Re-enable authentication when Azure AD is properly configured

// Add basic authentication services without Azure AD - DISABLED FOR TESTING
// builder.Services.AddAuthentication("Cookies")
//     .AddCookie("Cookies", options =>
//     {
//         options.LoginPath = "/Home/Index";
//         options.LogoutPath = "/Home/Index";
//         options.AccessDeniedPath = "/Home/Index";
//     });
// builder.Services.AddAuthorization();

// Configure HTTP client for CMS API with optimizations
builder.Services.AddHttpClient<CmsApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    UseProxy = false // Disable proxy for better performance in local development
})
.AddPolicyHandler(GetRetryPolicy());

// Register CMS API service
builder.Services.AddScoped<CmsApiService>();

// Register CMS health service
builder.Services.AddScoped<ICmsHealthService, CmsHealthService>();

// Register security service
builder.Services.AddScoped<ISecurityService, SecurityService>();

// Register security logging service
builder.Services.AddScoped<ISecurityLoggingService, SecurityLoggingService>();
builder.Services.AddHttpContextAccessor();

// Configure feature flags
builder.Services.Configure<EnabledFeatures>(builder.Configuration.GetSection("EnabledFeatures"));

// Add memory caching
builder.Services.AddMemoryCache();

// Add response caching
builder.Services.AddResponseCaching();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Add session support with enhanced security
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20); // Reduced from 30 minutes
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "FIPS.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add maintenance middleware (check CMS availability)
app.UseMiddleware<MaintenanceMiddleware>();

// Add security middleware
app.UseMiddleware<SecurityMiddleware>();

// Add security headers
app.Use(async (context, next) =>
{
    // HTTP Strict Transport Security (HSTS) - Enhanced configuration
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }
    else
    {
        // In development, use a shorter max-age for testing
        context.Response.Headers["Strict-Transport-Security"] = "max-age=300; includeSubDomains";
    }
    
    // Content Security Policy - Enhanced for better security
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    context.Items["Nonce"] = nonce;
    
    context.Response.Headers["Content-Security-Policy"] = 
        $"default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' https://www.googletagmanager.com https://www.clarity.ms https://scripts.clarity.ms; " +
        $"style-src 'self' 'nonce-{nonce}' https://rsms.me; " +
        $"img-src 'self' data: https:; " +
        $"font-src 'self' data: https://rsms.me; " +
        $"connect-src 'self' https://www.clarity.ms https://a.clarity.ms https://c.clarity.ms https://www.google-analytics.com https://region1.google-analytics.com https://analytics.google.com; " +
        $"frame-ancestors 'none'; " +
        $"base-uri 'self'; " +
        $"form-action 'self'; " +
        $"object-src 'none'; " +
        $"upgrade-insecure-requests";
    
    // Additional security headers
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()";
    
    // Additional security headers
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    
    await next();
});

app.UseRouting();

// Use authentication middleware - DISABLED FOR TESTING
// app.UseAuthentication();
// app.UseAuthorization();

app.UseSession();

// Add rate limiting middleware
app.UseRateLimiter();

// Add response caching middleware
app.UseResponseCaching();

app.MapControllerRoute(
    name: "product-categories",
    pattern: "product/{fipsid}/categories",
    defaults: new { controller = "Products", action = "ProductCategories" });

app.MapControllerRoute(
    name: "product-assurance",
    pattern: "product/{fipsid}/assurance",
    defaults: new { controller = "Products", action = "ProductAssurance" });

app.MapControllerRoute(
    name: "product-view",
    pattern: "product/{fipsid}",
    defaults: new { controller = "Products", action = "ViewProduct" });

app.MapControllerRoute(
    name: "categories",
    pattern: "categories/{*slug}",
    defaults: new { controller = "Categories", action = "Detail" });

app.MapControllerRoute(
    name: "cookies",
    pattern: "cookies",
    defaults: new { controller = "Cookies", action = "Preferences" });

app.MapControllerRoute(
    name: "maintenance",
    pattern: "maintenance",
    defaults: new { controller = "Maintenance", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// Retry policy for HTTP client
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}
