using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using FipsFrontend.Services;

namespace FipsFrontend.Tests.Integration;

public class HomePageIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomePageIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.ToString().Should().Contain("text/html");
    }

    [Fact]
    public async Task HomePage_ContainsExpectedContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("FIPS");
    }

    [Fact]
    public async Task PrivacyPage_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Home/Privacy");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ProductsPage_RedirectsToLogin_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        // Should redirect to login page since [Authorize] is applied
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task AdminPage_RedirectsToLogin_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Admin");

        // Assert
        // Should redirect to login page since [Authorize] is applied
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task StaticFiles_AreServedCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/css/main.css");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.ToString().Should().Contain("text/css");
    }

    [Fact]
    public async Task Favicon_IsServedCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/favicon.ico");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonExistentPage_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/non-existent-page");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Add test configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CmsApi:BaseUrl"] = "http://localhost:1337/api",
                ["CmsApi:ReadApiKey"] = "test-read-key",
                ["CmsApi:WriteApiKey"] = "test-write-key"
            });
        });

        // Configure services for testing
        builder.ConfigureServices(services =>
        {
            // Replace real services with test implementations if needed
            services.AddSingleton<ILogger<CmsApiService>>(provider =>
                provider.GetRequiredService<ILoggerFactory>().CreateLogger<CmsApiService>());
            
            services.AddSingleton<IMemoryCache, MemoryCache>();
        });

        return base.CreateHost(builder);
    }
}
