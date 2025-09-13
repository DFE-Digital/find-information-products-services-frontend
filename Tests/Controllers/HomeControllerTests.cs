using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FipsFrontend.Controllers;
using FipsFrontend.Models;
using FipsFrontend.Services;

namespace FipsFrontend.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly Mock<CmsApiService> _mockCmsApiService;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _mockCmsApiService = new Mock<CmsApiService>(
            Mock.Of<HttpClient>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<CmsApiService>>(),
            Mock.Of<IMemoryCache>());

        _controller = new HomeController(_mockLogger.Object, _mockCmsApiService.Object);
    }

    [Fact]
    public async Task Index_WithValidData_ReturnsViewWithViewModel()
    {
        // Arrange
        var productsResponse = new ApiCollectionResponse<Product>
        {
            Meta = new ApiMeta
            {
                Pagination = new ApiPagination { Total = 25 }
            }
        };

        var categoryTypesResponse = new ApiCollectionResponse<CategoryType>
        {
            Meta = new ApiMeta
            {
                Pagination = new ApiPagination { Total = 8 }
            }
        };

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<Product>>(
            It.Is<string>(s => s.Contains("products") && s.Contains("publishedAt")),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(productsResponse);

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<CategoryType>>(
            It.Is<string>(s => s.Contains("category-types") && s.Contains("enabled")),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(categoryTypesResponse);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.Model.Should().BeOfType<HomeViewModel>();
        
        var viewModel = viewResult.Model as HomeViewModel;
        viewModel!.PublishedProductsCount.Should().Be(25);
        viewModel.CategoryTypesCount.Should().Be(8);
    }

    [Fact]
    public async Task Index_WithApiException_ReturnsViewWithZeroCounts()
    {
        // Arrange
        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<Product>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new HttpRequestException("API Error"));

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<CategoryType>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new HttpRequestException("API Error"));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.Model.Should().BeOfType<HomeViewModel>();
        
        var viewModel = viewResult.Model as HomeViewModel;
        viewModel!.PublishedProductsCount.Should().Be(0);
        viewModel.CategoryTypesCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_WithNullApiResponse_ReturnsViewWithZeroCounts()
    {
        // Arrange
        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<Product>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync((ApiCollectionResponse<Product>?)null);

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<CategoryType>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync((ApiCollectionResponse<CategoryType>?)null);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.Model.Should().BeOfType<HomeViewModel>();
        
        var viewModel = viewResult.Model as HomeViewModel;
        viewModel!.PublishedProductsCount.Should().Be(0);
        viewModel.CategoryTypesCount.Should().Be(0);
    }

    [Fact]
    public async Task Index_WithPartialApiFailure_ReturnsViewWithPartialData()
    {
        // Arrange
        var productsResponse = new ApiCollectionResponse<Product>
        {
            Meta = new ApiMeta
            {
                Pagination = new ApiPagination { Total = 15 }
            }
        };

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<Product>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(productsResponse);

        _mockCmsApiService.Setup(x => x.GetAsync<ApiCollectionResponse<CategoryType>>(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new HttpRequestException("API Error"));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.Model.Should().BeOfType<HomeViewModel>();
        
        var viewModel = viewResult.Model as HomeViewModel;
        viewModel!.PublishedProductsCount.Should().Be(15);
        viewModel.CategoryTypesCount.Should().Be(0);
    }

    [Fact]
    public async Task Privacy_ReturnsView()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Error_ReturnsViewWithErrorViewModel()
    {
        // Act
        var result = _controller.Error();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.Model.Should().BeOfType<ErrorViewModel>();
    }
}
