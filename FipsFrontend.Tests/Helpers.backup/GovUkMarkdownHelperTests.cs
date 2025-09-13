using FipsFrontend.Helpers;

namespace FipsFrontend.Tests.Helpers;

public class GovUkMarkdownHelperTests
{
    [Fact]
    public void ToGovUkHtml_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(string.Empty);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToGovUkHtml_WithNullString_ReturnsEmptyString()
    {
        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(null!);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToGovUkHtml_WithSimpleText_AppliesGovUkClasses()
    {
        // Arrange
        var markdown = "This is a simple paragraph.";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-body\"");
    }

    [Fact]
    public void ToGovUkHtml_WithHeadings_AppliesCorrectClasses()
    {
        // Arrange
        var markdown = "# H1 Heading\n## H2 Heading\n### H3 Heading";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-heading-xl\"");
        result.Should().Contain("class=\"govuk-heading-l\"");
        result.Should().Contain("class=\"govuk-heading-m\"");
    }

    [Fact]
    public void ToGovUkHtml_WithLinks_AppliesGovUkLinkClass()
    {
        // Arrange
        var markdown = "[Test Link](https://example.com)";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-link\"");
        result.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void ToGovUkHtml_WithBulletList_AppliesCorrectClasses()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-list govuk-list--bullet\"");
    }

    [Fact]
    public void ToGovUkHtml_WithNumberedList_AppliesCorrectClasses()
    {
        // Arrange
        var markdown = "1. Item 1\n2. Item 2\n3. Item 3";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-list govuk-list--number\"");
    }

    [Fact]
    public void ToGovUkHtml_WithTable_AppliesGovUkTableClasses()
    {
        // Arrange
        var markdown = "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-table\"");
        result.Should().Contain("class=\"govuk-table__header\"");
        result.Should().Contain("class=\"govuk-table__cell\"");
    }

    [Fact]
    public void ToGovUkHtml_WithBlockquote_AppliesGovUkInsetTextClass()
    {
        // Arrange
        var markdown = "> This is a blockquote";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-inset-text\"");
    }

    [Fact]
    public void ToGovUkHtml_WithCode_AppliesGovUkCodeClass()
    {
        // Arrange
        var markdown = "`inline code`";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-code\"");
    }

    [Fact]
    public void ToGovUkHtml_WithStrongText_AppliesGovUkBoldClass()
    {
        // Arrange
        var markdown = "**Bold text**";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-!-font-weight-bold\"");
    }

    [Fact]
    public void ToGovUkHtml_WithEmphasizedText_AppliesGovUkItalicClass()
    {
        // Arrange
        var markdown = "*Italic text*";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-!-font-style-italic\"");
    }

    [Fact]
    public void ToGovUkHtml_WithHorizontalRule_AppliesGovUkSectionBreakClass()
    {
        // Arrange
        var markdown = "---";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-section-break govuk-section-break--visible\"");
    }

    [Fact]
    public void ToGovUkHtml_WithExistingClasses_DoesNotOverride()
    {
        // Arrange
        var markdown = "## Custom Heading";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-heading-l\"");
        result.Should().NotContain("class=\"govuk-heading-l class=\"");
    }

    [Fact]
    public void ToGovUkPlainList_WithBulletList_RemovesBullets()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkPlainList(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-list\"");
        result.Should().NotContain("govuk-list--bullet");
    }

    [Fact]
    public void ToGovUkSummaryList_WithDefinitionList_AppliesSummaryListClasses()
    {
        // Arrange
        var markdown = "Term 1\n: Definition 1\n\nTerm 2\n: Definition 2";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkSummaryList(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-summary-list\"");
        result.Should().Contain("class=\"govuk-summary-list__key\"");
        result.Should().Contain("class=\"govuk-summary-list__value\"");
    }

    [Fact]
    public void ToGovUkHtml_WithComplexMarkdown_AppliesAllClasses()
    {
        // Arrange
        var markdown = @"
# Main Heading
## Sub Heading
This is a paragraph with **bold** and *italic* text.

- List item 1
- List item 2

[External link](https://example.com)

| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |

> Important note

`code snippet`
";

        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        result.Should().Contain("class=\"govuk-heading-xl\"");
        result.Should().Contain("class=\"govuk-heading-l\"");
        result.Should().Contain("class=\"govuk-body\"");
        result.Should().Contain("class=\"govuk-!-font-weight-bold\"");
        result.Should().Contain("class=\"govuk-!-font-style-italic\"");
        result.Should().Contain("class=\"govuk-list govuk-list--bullet\"");
        result.Should().Contain("class=\"govuk-link\"");
        result.Should().Contain("class=\"govuk-table\"");
        result.Should().Contain("class=\"govuk-inset-text\"");
        result.Should().Contain("class=\"govuk-code\"");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("# Heading", "govuk-heading-xl")]
    [InlineData("## Heading", "govuk-heading-l")]
    [InlineData("### Heading", "govuk-heading-m")]
    [InlineData("#### Heading", "govuk-heading-s")]
    [InlineData("##### Heading", "govuk-heading-s")]
    [InlineData("###### Heading", "govuk-heading-s")]
    public void ToGovUkHtml_WithDifferentHeadings_AppliesCorrectClasses(string markdown, string expectedClass)
    {
        // Act
        var result = GovUkMarkdownHelper.ToGovUkHtml(markdown);

        // Assert
        if (string.IsNullOrEmpty(markdown))
        {
            result.Should().Be(string.Empty);
        }
        else
        {
            result.Should().Contain($"class=\"{expectedClass}\"");
        }
    }
}
