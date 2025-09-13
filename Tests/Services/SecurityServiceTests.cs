using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using FipsFrontend.Services;

namespace FipsFrontend.Tests.Services;

public class SecurityServiceTests
{
    private readonly Mock<ILogger<SecurityService>> _mockLogger;
    private readonly SecurityService _service;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;

    public SecurityServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecurityService>>();
        _service = new SecurityService(_mockLogger.Object);
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
    }

    [Fact]
    public void GetNonce_WithValidNonceInContext_ReturnsNonce()
    {
        // Arrange
        var expectedNonce = "test-nonce-123";
        var items = new Dictionary<object, object?> { ["Nonce"] = expectedNonce };
        _mockHttpContext.Setup(x => x.Items).Returns(items);

        // Act
        var result = _service.GetNonce(_mockHttpContext.Object);

        // Assert
        result.Should().Be(expectedNonce);
    }

    [Fact]
    public void GetNonce_WithNoNonceInContext_ReturnsEmptyString()
    {
        // Arrange
        var items = new Dictionary<object, object?>();
        _mockHttpContext.Setup(x => x.Items).Returns(items);

        // Act
        var result = _service.GetNonce(_mockHttpContext.Object);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.IsAuthenticated(_mockHttpContext.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.IsAuthenticated(_mockHttpContext.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithNullUser_ReturnsFalse()
    {
        // Arrange
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal)null!);

        // Act
        var result = _service.IsAuthenticated(_mockHttpContext.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasRole_WithUserHavingRole_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.IsInRole("Admin")).Returns(true);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.HasRole(_mockHttpContext.Object, "Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasRole_WithUserNotHavingRole_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.IsInRole("Admin")).Returns(false);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.HasRole(_mockHttpContext.Object, "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasRole_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.HasRole(_mockHttpContext.Object, "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetUserId_WithAuthenticatedUser_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = "user-123";
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, expectedUserId));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.GetUserId(_mockHttpContext.Object);

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetUserId_WithNoUserIdClaim_ReturnsEmptyString()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.GetUserId(_mockHttpContext.Object);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void GetUserRoles_WithUserHavingRoles_ReturnsRoles()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.FindAll(ClaimTypes.Role))
            .Returns(new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            });
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.GetUserRoles(_mockHttpContext.Object);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Admin");
        result.Should().Contain("User");
    }

    [Fact]
    public void GetUserRoles_WithUserHavingNoRoles_ReturnsEmptyList()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.FindAll(ClaimTypes.Role)).Returns(new List<Claim>());
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.GetUserRoles(_mockHttpContext.Object);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CanAccessResource_WithAdminUser_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.IsInRole("Admin")).Returns(true);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.CanAccessResource(_mockHttpContext.Object, "admin-panel");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanAccessResource_WithRegularUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        _mockUser.Setup(x => x.Identity).Returns(identity);
        _mockUser.Setup(x => x.IsInRole("Admin")).Returns(false);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);

        // Act
        var result = _service.CanAccessResource(_mockHttpContext.Object, "admin-panel");

        // Assert
        result.Should().BeFalse();
    }
}
