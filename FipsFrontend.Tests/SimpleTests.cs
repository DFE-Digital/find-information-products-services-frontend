using Xunit;
using FluentAssertions;

namespace FipsFrontend.Tests;

public class SimpleTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 42;
        var actual = 42;

        // Act & Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void StringTest_ShouldPass()
    {
        // Arrange
        var expected = "Hello, World!";
        var actual = "Hello, World!";

        // Act & Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void TheoryTest_ShouldPass(int expected, int actual)
    {
        // Act & Assert
        actual.Should().Be(expected);
    }
}
