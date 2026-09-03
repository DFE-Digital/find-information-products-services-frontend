using System.Net;
using Compass.FipsApi.Contracts;
using Compass.FipsApi.Stub;
using FipsFrontend.Configuration;
using FipsFrontend.Services.Compass;
using FipsFrontend.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace FipsFrontend.Tests;

/// <summary>
/// The COMPASS client as the application registers it, answered by the stub's scenarios in-process:
/// off when nothing is configured, refused when half configured, and otherwise reading the seeded
/// vocabularies and products, reporting drift once, and naming the endpoint when COMPASS refuses.
/// </summary>
[TestFixture]
public class CompassClientTests
{
    private static readonly Dictionary<string, string?> Configured = new()
    {
        ["Compass:BaseUrl"] = "http://compass.example.com/seeded",
        ["Compass:ApiToken"] = "test-only",
    };

    private static readonly Scenarios Scenarios = new(Path.Combine(AppContext.BaseDirectory, "scenarios"));

    /// <summary>Serves a request from a scenario the way the stub would, ignoring the base address's own prefix.</summary>
    private static (HttpStatusCode, string)? Serve(HttpRequestMessage request, string scenario)
    {
        var path = request.RequestUri!.AbsolutePath.Replace("/seeded/", "/", StringComparison.Ordinal);
        var answer = Scenarios.Answer(scenario, path);
        return ((HttpStatusCode)answer.Status, answer.Body);
    }

    [Test]
    public void Compass_WhenNothingConfigured_IsOff_AndTheApplicationStarts()
    {
        using var app = new FipsApplication();

        var options = app.Factory.Services.GetRequiredService<CompassOptions>();
        Assert.That(options.IsConfigured, Is.False);
    }

    [Test]
    public void Compass_WhenOnlyTheAddressIsSupplied_RefusesToStartNamingTheMissingKey()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var app = new FipsApplication(settings: new Dictionary<string, string?> { ["Compass:BaseUrl"] = "http://compass.example.com/" });
            _ = app.Factory.Services;
        });

        Assert.That(ex!.Message, Does.Contain("Compass:ApiToken"));
    }

    [Test]
    public async Task Compass_WhenSeeded_TheConfigurationBundleCarriesEveryVocabulary()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var bundle = await client.GetFipsConfigurationAsync();

        Assert.That(bundle.Channels!.Select(c => c.Name), Is.EquivalentTo(new[] { "Web", "Native app", "Telephone", "Post" }));
        Assert.That(bundle.UserGroups!.Single(g => g.Name == "Teachers").Children, Has.Count.EqualTo(2));
        Assert.That(bundle.CategorisationGroups!.Select(g => g.Name), Does.Contain("Phase"));
        Assert.That(app.Outbound.Snapshot(), Has.One.EndsWith("/seeded/api/v1/ServiceRegister/fips/configuration"), "the request carries the scenario prefix from the base address");
    }

    [Test]
    public async Task Compass_WhenSeeded_ProductsAreFilteredByCompass_NotHere()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var page = await client.GetProductsAsync(new ProductQuery(Keywords: "teacher", Status: ["Active", "New"], ChannelIds: [1, 2], Page: 2, PageSize: 10));

        Assert.That(page.Data, Has.Count.EqualTo(28), "the seeded recording is served whole; filtering is the API's job");
        Assert.That(app.Outbound.Snapshot().Single(), Does.EndWith("/products?page=2&pageSize=10&status=Active&status=New&q=teacher&channelIds=1&channelIds=2"));
    }

    [Test]
    public async Task Compass_WhenAProductIsUnknown_TheClientAnswersNull()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => (HttpStatusCode.NotFound, """{"error":"not found"}""");
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        Assert.That(await client.GetProductAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task Compass_WhenAResponseDrifts_TheUnknownMemberIsObservedOnce_AndTheKnownOnesStillRead()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();
        var observations = app.Factory.Services.GetRequiredService<IContractObservations>();

        var bundle = await client.GetFipsConfigurationAsync();
        await client.GetFipsConfigurationAsync();

        Assert.That(bundle.Channels!.Select(c => c.Name), Is.EqualTo(new[] { "Web", "Native app" }));
        Assert.That(bundle.Types!.Single().Description, Is.Null, "a member COMPASS stopped sending reads as null");
        Assert.That(observations.Seen.Select(s => s.Field), Is.EquivalentTo(new[] { "generatedAt", "Channels[].colour", "UserGroups[].parentId" }));
    }

    [Test]
    public void Compass_WhenUnavailable_TheFailureNamesTheEndpointAndStatus()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Unavailable);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var ex = Assert.ThrowsAsync<CompassUnavailableException>(() => client.GetProductsAsync(new ProductQuery()));

        Assert.That(ex!.Endpoint, Is.EqualTo("products"));
        Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public void Compass_WhenTheAnswerIsNotJson_TheFailureSaysSo()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => (HttpStatusCode.OK, "<html>sign in</html>");
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var ex = Assert.ThrowsAsync<CompassUnavailableException>(() => client.GetFipsConfigurationAsync());

        Assert.That(ex!.Message, Does.Contain("not the expected JSON"));
    }

    [Test]
    public async Task Compass_WhenEmpty_EveryCollectionIsPresentAndEmpty()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Empty);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var bundle = await client.GetFipsConfigurationAsync();
        var page = await client.GetProductsAsync(new ProductQuery());

        Assert.That(bundle.Channels, Is.Not.Null.And.Empty);
        Assert.That(page.Data, Is.Not.Null.And.Empty);
        Assert.That(page.Pagination?.TotalRecords, Is.Zero);
    }

    [Test]
    public async Task Compass_WhenEmpty_AProductAskedForById_IsUnknown()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Empty);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();

        var product = await client.GetProductAsync(Guid.NewGuid());

        Assert.That(product, Is.Null, "a COMPASS holding no products knows no id; a list-shaped answer here would read as a product with nothing in it");
    }

    [Test]
    public async Task Compass_WhenNothingConfigured_TheClientAnswersAsACompassHoldingNothing_InCompassShapes()
    {
        // The application's own stand-in handler must answer, not the test host's recording one.
        using var app = new FipsApplication(replaceOutboundClients: false);
        var client = app.Factory.Services.GetRequiredService<ICompassClient>();
        var observations = app.Factory.Services.GetRequiredService<IContractObservations>();

        var bundle = await client.GetFipsConfigurationAsync();
        var page = await client.GetProductsAsync(new ProductQuery());
        var product = await client.GetProductAsync(Guid.NewGuid());

        Assert.That(bundle.Channels, Is.Not.Null.And.Empty);
        Assert.That(page.Pagination, Is.Not.Null, "COMPASS's envelope, not the CMS's meta.pagination");
        Assert.That(page.Pagination?.TotalRecords, Is.Zero);
        Assert.That(product, Is.Null);
        Assert.That(observations.Seen, Is.Empty, "a stand-in in COMPASS's own shapes carries no member the records do not name");
    }

    [Test]
    public void Stub_WhenNoFileAnswersAPath_Answers404NamingTheFile()
    {
        var answer = Scenarios.Answer(Scenarios.Seeded, "api/v1/ServiceRegister/no-such-endpoint");

        Assert.That(answer.Status, Is.EqualTo(404));
        Assert.That(answer.Body, Does.Contain("seeded/api/v1/ServiceRegister/no-such-endpoint.json"));
    }

    [TestCase("seeded", "../../canary", TestName = "Stub_WhenAPathClimbsOutOfTheScenarioFolder_AnswersAsNoFile")]
    [TestCase("..", "canary", TestName = "Stub_WhenTheScenarioNameClimbsOutOfTheScenariosFolder_AnswersAsNoFile")]
    [TestCase("seeded", "api/v1/ServiceRegister/../../../../canary", TestName = "Stub_WhenAPathClimbsOutMidway_AnswersAsNoFile")]
    public void Stub_WhenAPathReachesOutsideItsScenario_AnswersAsNoFile(string scenario, string path)
    {
        // The scenario and path come straight from the URL. A file one level above the scenarios folder stands for
        // anything on the machine the stub must never serve.
        var canary = Path.Combine(AppContext.BaseDirectory, "canary.json");
        File.WriteAllText(canary, """{"secret":"must never be served"}""");
        try
        {
            var answer = Scenarios.Answer(scenario, path);

            Assert.That(answer.Status, Is.EqualTo(404));
            Assert.That(answer.Body, Does.Not.Contain("must never be served"));
        }
        finally
        {
            File.Delete(canary);
        }
    }
}
