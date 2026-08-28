using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using FipsFrontend.Configuration;
using FipsFrontend.Services;
using FipsFrontend.Middlewares;
using FipsFrontend.Models;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// The framework maps wwwroot from the source tree only under "Development"; a developer's machine
// runs as "local-dev" (see Configuration/Environments.cs) and needs the same, or every stylesheet
// and script is a 404 from a build folder.
if (builder.Environment.IsLocalDev())
{
    builder.WebHost.UseStaticWebAssets();
}

// Add file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/app-{Date}.log");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Optional sections are read and validated here, before anything is registered: a section left empty
// switches its feature off, a partly supplied one stops the application now, naming the keys
// (see Configuration/ConfigurationSections.cs).
var signIn = AzureAdOptions.Read(builder.Configuration);
var contentSource = CmsApiOptions.Read(builder.Configuration);
builder.Services.AddSingleton(contentSource);

// Sign-in through the identity provider, when configured. Without it the pages are served anonymously.
if (signIn is not null)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(AzureAdOptions.Section));
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// The content source's clients. With no content source configured they talk to an in-process handler
// that answers every request with an empty collection, so a first run shows empty pages, not timeouts.
HttpMessageHandler ContentSourceHandler() => contentSource.IsConfigured
    ? new HttpClientHandler { MaxConnectionsPerServer = 10, UseProxy = false }
    : new NoContentSourceHandler();

builder.Services.AddHttpClient<CmsApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend/1.0");
})
.ConfigurePrimaryHttpMessageHandler(ContentSourceHandler)
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<IOptimizedCmsApiService, OptimizedCmsApiService>(client =>
{
    client.BaseAddress = contentSource.BaseAddress;
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend-Optimized/1.0");
})
.ConfigurePrimaryHttpMessageHandler(ContentSourceHandler)
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

// Register search term logging service
builder.Services.AddScoped<ISearchTermLoggingService, SearchTermLoggingService>();

builder.Services.AddHttpContextAccessor();

// Register Airtable service
builder.Services.AddHttpClient<IAirtableService, AirtableService>();
builder.Services.Configure<AirtableConfiguration>(builder.Configuration.GetSection("Airtable"));

builder.Services.AddOptions<FeedbackOptions>()
    .Bind(builder.Configuration.GetSection(FeedbackOptions.SectionName))
    .Validate(options => options.IsValid(), "Feedback:SurveyUrl must be an absolute http or https URL, or left empty.")
    .ValidateOnStart();

builder.Services.AddOptions<ContactOptions>()
    .Bind(builder.Configuration.GetSection(ContactOptions.SectionName))
    .Validate(options => options.IsValid(), "Contact:Email must be an e-mail address, or left empty.")
    .ValidateOnStart();

// Generated links keep the paths the site has always used ("/about", not "/About").
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

// The service assessments integration, when configured. Off, every lookup answers empty and the
// Assurance feature cannot be on: a feature that depends on a service nobody named is refused here.
var assessments = SasOptions.Read(builder.Configuration);
if (builder.Configuration.GetValue<bool>("EnabledFeatures:Assurance") && !assessments.IsConfigured)
{
    throw new InvalidOperationException(
        $"EnabledFeatures:Assurance is true but the assessments service is not configured: set {SasOptions.SectionName}:BaseUrl and {SasOptions.SectionName}:SecretId, or set EnabledFeatures:Assurance to false.");
}
builder.Services.AddOptions<SasOptions>()
    .Bind(builder.Configuration.GetSection(SasOptions.SectionName));

builder.Services.AddHttpClient<IServiceAssessmentsService, ServiceAssessmentsService>(client =>
{
    client.BaseAddress = ConfigurationSections.TryNormaliseBaseUrl(assessments.EffectiveBaseUrl) ?? new Uri("http://assessments.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Frontend-Assessments/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => assessments.IsConfigured
    ? new HttpClientHandler { MaxConnectionsPerServer = 10, UseProxy = false }
    : new NoContentSourceHandler())
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

// The distributed cache: Redis when switched on and given an address (refused at start-up if switched on without one), in-memory otherwise.
var redis = RedisOptions.Read(builder.Configuration);
if (redis.IsOn)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redis.ConnectionString;
        options.InstanceName = redis.KeyPrefix;
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
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

if (!app.Services.GetRequiredService<IOptions<FeedbackOptions>>().Value.HasSurvey)
{
    app.Logger.LogWarning("Feedback:SurveyUrl is blank, so the feedback survey link is not shown.");
}

if (!app.Services.GetRequiredService<IOptions<ContactOptions>>().Value.HasEmail)
{
    app.Logger.LogWarning("Contact:Email is blank, so the contact page does not offer an e-mail address.");
}

if (app.Services.GetRequiredService<IOptions<SasOptions>>().Value.UsesDeprecatedBaseUrlKey)
{
    app.Logger.LogWarning("SAS:TenantId is a deprecated name and is being read as the service assessments base URL. Set SAS:BaseUrl (SAS__BaseUrl) instead; the old name will stop being read.");
}

// Configure the HTTP request pipeline.
// The page with the stack trace is for developers; everyone else gets the error page. Not the other
// way round by accident: an unnamed environment is Production (see Configuration/Environments.cs).
if (app.Environment.IsDevelopmentLike())
{
    app.UseDeveloperExceptionPage();
}
app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopmentLike())
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
    if (!app.Environment.IsDevelopmentLike())
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

// Lets the test project host the application through WebApplicationFactory<Program>.
public partial class Program { }
