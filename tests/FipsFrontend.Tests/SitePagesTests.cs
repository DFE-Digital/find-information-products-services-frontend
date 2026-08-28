using System.Net;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// The service's own pages as a visitor meets them. Their copy lives in this repository, so a page
/// that reaches for another system to render is a page that has regressed.
/// </summary>
[TestFixture]
public class SitePagesTests
{
    private FipsApplication _app = null!;

    [OneTimeSetUp]
    public void HostTheApplication() => _app = new FipsApplication();

    [OneTimeTearDown]
    public void StopTheApplication() => _app.Dispose();

    [SetUp]
    public void ForgetEarlierRequests() => _app.Outbound.Clear();

    public static IEnumerable<TestCaseData> Pages()
    {
        yield return new TestCaseData("/about", "About this service").SetName("{m}(about)");
        yield return new TestCaseData("/contact", "Contact us").SetName("{m}(contact)");
        yield return new TestCaseData("/data", "Using the data").SetName("{m}(data)");
        yield return new TestCaseData("/updates", "Keep information updated").SetName("{m}(updates)");
        yield return new TestCaseData("/help", "Help using this service").SetName("{m}(help)");
    }

    [TestCaseSource(nameof(Pages))]
    public async Task SitePage_WhenRequested_RendersItsHeadingAndBody(string path, string heading)
    {
        var response = await _app.Client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{path}: {Html.Excerpt(html)}");
        Assert.That(Html.Headings(html), Is.EqualTo(new[] { heading }), $"{path} carries exactly one h1, its own title");
        Assert.That(Html.MainColumnText(html), Has.Length.GreaterThan(heading.Length + 40), $"{path} carries body copy, not just its heading");
    }

    [TestCaseSource(nameof(Pages))]
    public async Task SitePage_WhenRequested_MakesNoOutboundHttpCall(string path, string heading)
    {
        var response = await _app.Client.GetAsync(path);

        // "Made no call" only means something about a page that rendered.
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{path}: {Html.Excerpt(await response.Content.ReadAsStringAsync())}");
        Assert.That(_app.Outbound.Snapshot(), Is.Empty, $"{path} reached for another system");
    }

    [Test]
    public async Task ProductListing_WhenRequested_ReachesTheCmsThroughTheStandIn()
    {
        // The control for the assertion above: a page that does depend on the CMS is seen doing so.
        await _app.Client.GetAsync("/products");

        Assert.That(_app.Outbound.Snapshot(), Has.Some.Contains("cms.example.com"));
    }
}
