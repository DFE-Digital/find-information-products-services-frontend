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

// Enable Azure AD authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

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

// Note: CmsApiService is already registered above via AddHttpClient<CmsApiService>
// No need for additional registration

// Register optimized CMS API service
builder.Services.AddHttpClient<IOptimizedCmsApiService, OptimizedCmsApiService>(client =>
{
    var baseUrl = builder.Configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
    // Ensure BaseAddress ends with '/' for proper relative URL resolution
    if (!baseUrl.EndsWith("/"))
    {
        baseUrl += "/";
    }
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend-Optimized/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    UseProxy = false // Disable proxy for better performance in local development
})
.AddPolicyHandler(GetRetryPolicy());

// Register CMS health service
builder.Services.AddScoped<ICmsHealthService, CmsHealthService>();

// Register enhanced caching services
builder.Services.AddScoped<ICacheConfigurationService, CacheConfigurationService>();
builder.Services.AddScoped<IEnhancedCacheService, EnhancedCacheService>();
builder.Services.AddScoped<ICacheWarmingService, CacheWarmingService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<ICachePerformanceService, CachePerformanceService>();
builder.Services.AddScoped<IStartupCacheService, StartupCacheService>();
builder.Services.AddScoped<IPerformanceAnalysisService, PerformanceAnalysisService>();

// Register startup cache warming as a hosted service
builder.Services.AddHostedService<StartupCacheHostedService>();

// Register security service
builder.Services.AddScoped<ISecurityService, SecurityService>();

// Register security logging service
builder.Services.AddScoped<ISecurityLoggingService, SecurityLoggingService>();

// Register API logging service - ENABLED FOR PERFORMANCE MONITORING
builder.Services.AddScoped<IApiLoggingService, ApiLoggingService>();
// builder.Services.AddScoped<IApiLoggingService, NullApiLoggingService>();

// Register GOV.UK Notify service
builder.Services.AddScoped<INotifyService, NotifyService>();

// Register search term logging service
builder.Services.AddScoped<ISearchTermLoggingService, SearchTermLoggingService>();

builder.Services.AddHttpContextAccessor();

// Register Airtable service
builder.Services.AddHttpClient<IAirtableService, AirtableService>();
builder.Services.Configure<AirtableConfiguration>(builder.Configuration.GetSection("Airtable"));

// Register Service Assessments service
builder.Services.AddHttpClient<IServiceAssessmentsService, ServiceAssessmentsService>(client =>
{
    var baseUrl = builder.Configuration["SAS:TenantId"] ?? "https://service-assessments.education.gov.uk/";
    // Ensure BaseAddress ends with '/' for proper relative URL resolution
    if (!baseUrl.EndsWith("/"))
    {
        baseUrl += "/";
    }
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend-Assessments/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    UseProxy = false // Disable proxy for better performance in local development
})
.AddPolicyHandler(GetRetryPolicy());

// Configure feature flags
builder.Services.Configure<EnabledFeatures>(builder.Configuration.GetSection("EnabledFeatures"));

// Add memory caching
builder.Services.AddMemoryCache(options =>
{
    // Don't set SizeLimit to avoid capacity eviction issues
    // This allows unlimited cache entries based on memory availability
    options.CompactionPercentage = builder.Configuration.GetValue<double>("Caching:MemoryCache:CompactionPercentage", 0.25);
});

// Add distributed caching (Redis support)
var redisEnabled = builder.Configuration.GetValue<bool>("Caching:Redis:Enabled", false);
Console.WriteLine($"Redis enabled: {redisEnabled}");

if (redisEnabled)
{
    var redisConnectionString = builder.Configuration.GetValue<string>("Caching:Redis:ConnectionString", "localhost:6379");
    Console.WriteLine($"Redis connection string: {redisConnectionString}");
    
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration.GetValue<string>("Caching:Redis:KeyPrefix", "fips:");
    });
    Console.WriteLine("Redis cache registered successfully");
}
else
{
    // Fallback to in-memory distributed cache
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("Using in-memory distributed cache");
}

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
// Show detailed errors in both development and production for debugging
app.UseDeveloperExceptionPage();
app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Configure 404 handling
app.UseStatusCodePagesWithReExecute("/Home/NotFound");

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add maintenance middleware (check CMS availability) - TEMPORARILY DISABLED FOR TESTING
// app.UseMiddleware<MaintenanceMiddleware>();

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
    // Note: 'unsafe-inline' is required for Google Tag Manager which dynamically injects scripts
    // Important: We cannot use both 'nonce' and 'unsafe-inline' in the same directive - when a nonce is present,
    // 'unsafe-inline' is ignored by the browser. So we use 'unsafe-inline' for GTM compatibility.
    // The nonce is still generated and stored in context.Items for potential future use, but not included in CSP.
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    context.Items["Nonce"] = nonce;
    
    context.Response.Headers["Content-Security-Policy"] = 
        $"default-src 'self'; " +
        $"script-src 'self' 'unsafe-inline' https://*.googletagmanager.com https://*.google-analytics.com https://*.google.com https://*.clarity.ms https://*.applicationinsights.azure.com https://*.vo.msecnd.net; " +
        $"style-src 'self' 'unsafe-inline' https://rsms.me https://*.googleapis.com; " +
        $"img-src 'self' data: https:; " +
        $"font-src 'self' data: https://rsms.me https://*.googleapis.com https://*.gstatic.com; " +
        $"connect-src 'self' https://*.googletagmanager.com https://*.google-analytics.com https://*.google.com https://*.clarity.ms https://*.applicationinsights.azure.com https://*.vo.msecnd.net https://*.services.visualstudio.com https://login.microsoftonline.com https://graph.microsoft.com; " +
        $"frame-src 'self' https://*.googletagmanager.com https://login.microsoftonline.com; " +
        $"frame-ancestors 'none'; " +
        $"base-uri 'self'; " +
        $"form-action 'self' https://login.microsoftonline.com; " +
        $"object-src 'none'; " +
        $"upgrade-insecure-requests";
    
    // Additional security headers
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()";
    
    // Additional security headers
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "credentialless";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    
    await next();
});

app.UseRouting();

// Use authentication middleware
app.UseAuthentication();
app.UseAuthorization();

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
    name: "product-edit",
    pattern: "product/{fipsid}/edit",
    defaults: new { controller = "Products", action = "ProductEdit" });

app.MapControllerRoute(
    name: "product-propose-change",
    pattern: "product/{fipsid}/propose-change",
    defaults: new { controller = "Products", action = "ProposeChange" });

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
