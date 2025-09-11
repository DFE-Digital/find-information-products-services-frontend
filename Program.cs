using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using FipsFrontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/app-{Date}.log");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Entra ID authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add Microsoft Identity UI
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Configure HTTP client for CMS API
builder.Services.AddHttpClient<CmsApiService>();

// Register CMS API service
builder.Services.AddScoped<CmsApiService>();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "product-categories",
    pattern: "product/{fipsid}/categories",
    defaults: new { controller = "Products", action = "ProductCategories" });

app.MapControllerRoute(
    name: "product-view",
    pattern: "product/{fipsid}",
    defaults: new { controller = "Products", action = "ViewProduct" });

app.MapControllerRoute(
    name: "categories",
    pattern: "categories/{*slug}",
    defaults: new { controller = "Categories", action = "Detail" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
