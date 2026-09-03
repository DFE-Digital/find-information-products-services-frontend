using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// The footer's accessibility statement link: every page carries one, and where it points is the instance's
/// setting, with the departmental statement when nothing is configured. The address is an environment's concern,
/// so the code's job is to render what it is given.
/// </summary>
[TestFixture]
public class FooterLinksTests
{
    private static string? StatementHref(string html) =>
        Html.Parse(html).QuerySelectorAll("footer a").FirstOrDefault(a => Html.Text(a).StartsWith("Accessibility statement"))?.GetAttribute("href");

    [Test]
    public async Task Footer_WhenNoStatementAddressIsConfigured_LinksToTheDepartmentalStatement()
    {
        using var app = new FipsApplication();

        var html = await app.Client.GetStringAsync("/cookies");

        Assert.That(StatementHref(html), Is.EqualTo("https://accessibility-statements.education.gov.uk/s/43"));
    }

    [Test]
    public async Task Footer_WhenAStatementAddressIsConfigured_LinksToIt()
    {
        using var app = new FipsApplication(settings: new Dictionary<string, string?> { ["AccessibilityStatement:Url"] = "http://statements.example.com/s/7" });

        var html = await app.Client.GetStringAsync("/cookies");

        Assert.That(StatementHref(html), Is.EqualTo("http://statements.example.com/s/7"));
    }

    [Test]
    public async Task Footer_WhenTheStatementAddressIsBlank_StillLinksToTheDepartmentalStatement()
    {
        using var app = new FipsApplication(settings: new Dictionary<string, string?> { ["AccessibilityStatement:Url"] = "" });

        var html = await app.Client.GetStringAsync("/cookies");

        Assert.That(StatementHref(html), Is.EqualTo("https://accessibility-statements.education.gov.uk/s/43"), "a footer without a statement is not an option");
    }
}
