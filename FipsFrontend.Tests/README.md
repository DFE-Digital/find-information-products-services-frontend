# FIPS Frontend Unit Tests

This directory contains comprehensive unit tests for the FIPS Frontend ASP.NET Core application.

## Test Structure

```
Tests/
├── Controllers/          # Controller unit tests
├── Services/            # Service layer tests
├── Helpers/             # Helper class tests
├── Models/              # Model validation tests
├── Integration/         # Integration tests
├── TestBase.cs          # Base test class with common utilities
└── xunit.runner.json    # xUnit configuration
```

## Test Categories

### Unit Tests
- **Controllers**: Test controller actions, model binding, and view results
- **Services**: Test business logic, API calls, and data processing
- **Helpers**: Test utility functions and helper methods
- **Models**: Test model validation and data transformations

### Integration Tests
- **End-to-end**: Test complete request/response cycles
- **API Integration**: Test external service interactions
- **Database Integration**: Test data persistence and retrieval

## Running Tests

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Command Line
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=HomeControllerTests"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Build the solution
3. Click "Run All Tests"

## Test Configuration

### xUnit Configuration
- **Parallel Execution**: Enabled for test collections
- **Max Threads**: 4 concurrent threads
- **Method Display**: Space-separated method names

### Coverage Reports
- **Tool**: Coverlet for code coverage
- **Format**: Cobertura XML
- **Threshold**: 80% line coverage target

## Test Guidelines

### Naming Conventions
- **Test Classes**: `{ClassName}Tests`
- **Test Methods**: `{MethodName}_{Scenario}_{ExpectedResult}`
- **Test Data**: Use `[Theory]` and `[InlineData]` for parameterized tests

### Best Practices
1. **Arrange-Act-Assert**: Structure tests clearly
2. **Mock External Dependencies**: Use Moq for HTTP clients, databases, etc.
3. **Test Edge Cases**: Include null, empty, and error scenarios
4. **Use FluentAssertions**: For readable test assertions
5. **One Assert Per Test**: Focus on single behaviors

### Example Test Structure
```csharp
[Fact]
public async Task GetAsync_WithValidEndpoint_ReturnsData()
{
    // Arrange
    var expectedData = new Product { Id = 1, Name = "Test" };
    _mockHttpClient.Setup(/* ... */);

    // Act
    var result = await _service.GetAsync<Product>("products");

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("Test");
}
```

## Mocking Strategy

### HTTP Clients
- Use `Mock<HttpMessageHandler>` for HttpClient testing
- Mock responses with realistic JSON data
- Test error scenarios (404, 500, timeouts)

### Logging
- Use `Mock<ILogger<T>>` for logging verification
- Verify log levels and messages when appropriate

### Configuration
- Use `Mock<IConfiguration>` for configuration testing
- Test different configuration scenarios

### Memory Cache
- Use `Mock<IMemoryCache>` for caching tests
- Verify cache hit/miss scenarios

## Continuous Integration

Tests are automatically run in CI/CD pipelines:
- **Build**: Compile and run tests
- **Coverage**: Generate coverage reports
- **Quality Gates**: Fail builds if coverage < 80%

## Debugging Tests

### Visual Studio
1. Set breakpoints in test methods
2. Right-click test → "Debug Test"
3. Use Test Explorer for test debugging

### Command Line
```bash
# Debug specific test
dotnet test --filter "MethodName=GetAsync_WithValidEndpoint_ReturnsData" --logger "console;verbosity=detailed"
```

## Test Data

### Test Fixtures
- Use `IClassFixture<T>` for shared test data
- Create test databases for integration tests
- Use `WebApplicationFactory<T>` for integration testing

### Sample Data
- Create realistic test data that matches production
- Use builders for complex object creation
- Avoid hardcoded test data in multiple places

## Performance Testing

### Load Testing
- Use NBomber for load testing
- Test API endpoints under load
- Verify response times and throughput

### Memory Testing
- Monitor memory usage in long-running tests
- Test for memory leaks in caching scenarios

## Troubleshooting

### Common Issues
1. **Test Discovery**: Ensure test projects reference main project
2. **Mock Setup**: Verify mock configurations match actual calls
3. **Async Tests**: Use `async Task` for async test methods
4. **Test Isolation**: Ensure tests don't depend on each other

### Debug Output
```bash
# Enable detailed test output
dotnet test --logger "console;verbosity=detailed"

# Run single test with output
dotnet test --filter "FullyQualifiedName=TestNamespace.TestClass.TestMethod" --logger "console"
```
