using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using FipsFrontend.Models;
using FipsFrontend.Services;

namespace FipsFrontend.Tests.Services;

public class CmsApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<CmsApiService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly CmsApiService _service;

    public CmsApiServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<CmsApiService>>();
        _mockMemoryCache = new Mock<IMemoryCache>();

        // Setup configuration
        _mockConfiguration.Setup(x => x["CmsApi:BaseUrl"])
            .Returns("http://localhost:1337/api");

        // Create service with real HttpClient but mocked handler
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new CmsApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object, _mockMemoryCache.Object);
    }

    [Fact]
    public async Task GetAsync_WithValidEndpoint_ReturnsData()
    {
        // Arrange
        var expectedProduct = new Product
        {
            Id = 1,
            FipsId = "test-product",
            Name = "Test Product",
            Description = "Test Description"
        };

        var apiResponse = new ApiCollectionResponse<Product>
        {
            Data = new List<Product> { expectedProduct },
            Meta = new ApiMeta { Pagination = new ApiPagination { Total = 1 } }
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.GetAsync<ApiCollectionResponse<Product>>("products");

        // Assert
        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data.First().Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetAsync_WithInvalidEndpoint_ReturnsNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found", Encoding.UTF8, "text/plain")
            });

        // Act
        var result = await _service.GetAsync<ApiCollectionResponse<Product>>("invalid-endpoint");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithServerError_ReturnsNull()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
            });

        // Act
        var result = await _service.GetAsync<ApiCollectionResponse<Product>>("products");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithCacheHit_ReturnsCachedData()
    {
        // Arrange
        var cachedData = new ApiCollectionResponse<Product>
        {
            Data = new List<Product> { new Product { Id = 1, Name = "Cached Product" } },
            Meta = new ApiMeta { Pagination = new ApiPagination { Total = 1 } }
        };

        var cacheKey = "cms_api_products";
        var cacheEntry = new Mock<ICacheEntry>();
        
        _mockMemoryCache.Setup(x => x.TryGetValue(cacheKey, out cachedData))
            .Returns(true);

        // Act
        var result = await _service.GetAsync<ApiCollectionResponse<Product>>("products", TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Data.First().Name.Should().Be("Cached Product");
        
        // Verify no HTTP call was made
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_WithValidData_ReturnsCreatedResponse()
    {
        // Arrange
        var product = new Product
        {
            Name = "New Product",
            Description = "New Description"
        };

        var createdProduct = new Product
        {
            Id = 1,
            Name = "New Product",
            Description = "New Description"
        };

        var jsonResponse = JsonSerializer.Serialize(new { data = createdProduct }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.PostAsync<Product>("products", product);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("New Product");
    }

    [Fact]
    public async Task PutAsync_WithValidData_ReturnsUpdatedResponse()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Updated Product",
            Description = "Updated Description"
        };

        var jsonResponse = JsonSerializer.Serialize(new { data = product }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _service.PutAsync<Product>("products/1", product);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _service.DeleteAsync("products/1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _service.DeleteAsync("products/999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateCacheKey_WithValidEndpoint_ReturnsExpectedKey()
    {
        // Arrange
        var endpoint = "products?filters[publishedAt][$notNull]=true";

        // Act
        var result = _service.GetType()
            .GetMethod("CreateCacheKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_service, new object[] { endpoint }) as string;

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("products");
    }
}
