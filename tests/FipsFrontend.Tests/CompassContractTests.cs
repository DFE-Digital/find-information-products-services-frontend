using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Compass.FipsApi.Contracts;
using Compass.FipsApi.Contracts.Generated;
using Microsoft.Extensions.Logging.Abstractions;

namespace FipsFrontend.Tests;

/// <summary>
/// The generated records against responses recorded from a local COMPASS on synthetic seed data (see Fixtures/compass/README.md).
/// Every recording parses with no member unnamed; the seeded rows carry the values the seed promised;
/// an omitted member is null, an empty one is empty; an unknown member is reported once.
/// </summary>
[TestFixture]
public class CompassContractTests
{
    // The stub's seeded scenario, linked into this project's output; names are endpoint paths under /api/v1/ServiceRegister/.
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "scenarios", "seeded", "api", "v1", "ServiceRegister", name);

    private static T Parse<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, CompassJson.Options) ?? throw new InvalidOperationException("payload deserialised to null");

    private static T ParseFixture<T>(string file) => Parse<T>(File.ReadAllText(Fixture(file)));

    private static ContractObservations FreshObservations() => new(NullLogger<ContractObservations>.Instance);

    // Every endpoint the client reads, each recorded from a COMPASS seeded with its minimal scenario.
    [TestCase("products.json", typeof(ServiceRegisterGetProductsResponse))]
    [TestCase("products/enterprise-active.json", typeof(ServiceRegisterGetProductsResponse))]
    [TestCase("products/_by-id.json", typeof(ServiceRegisterGetProductResponse))]
    [TestCase("categorisation-items.json", typeof(ServiceRegisterGetCategorisationItemsResponse))]
    [TestCase("fips/configuration.json", typeof(ServiceRegisterGetFipsConfigurationBundleResponse))]
    [TestCase("fips/channels.json", typeof(ServiceRegisterGetFipsChannelsV1Response))]
    [TestCase("fips/types.json", typeof(ServiceRegisterGetFipsChannelsV1Response))]
    [TestCase("fips/business-areas.json", typeof(ServiceRegisterGetFipsBusinessAreasV1Response))]
    [TestCase("fips/user-groups.json", typeof(ServiceRegisterGetFipsUserGroupsV1Response))]
    [TestCase("fips/contact-roles.json", typeof(ServiceRegisterGetFipsContactRolesV1Response))]
    [TestCase("fips/categorisation.json", typeof(ServiceRegisterGetFipsCategorisationNestedV1Response))]
    public void RecordedPayload_ParsesThroughTheGeneratedRootType_WithNoMemberUnnamed(string file, Type root)
    {
        var parsed = JsonSerializer.Deserialize(File.ReadAllText(Fixture(file)), root, CompassJson.Options);
        Assert.That(parsed, Is.Not.Null);

        var observations = FreshObservations();
        observations.Observe(file, parsed);

        Assert.That(observations.Seen, Is.Empty,
            $"{file} carries members the records do not name - regenerate the records from COMPASS's source: " +
            string.Join(", ", observations.Seen.Select(s => s.Field)));
    }

    [Test]
    public void RecordedProducts_CarryTheValuesThePagesNeed()
    {
        var page = ParseFixture<ServiceRegisterGetProductsResponse>("products.json");

        Assert.That(page.Pagination?.TotalRecords, Is.EqualTo(page.Data?.Count), "the recorded page holds every product");
        Assert.That(page.Data, Has.All.Matches<ServiceRegisterGetProductsResponseDataItem>(p => p.Id is { } id && id != Guid.Empty && !string.IsNullOrEmpty(p.ProductName)));
        Assert.That(page.Data!.Select(p => p.Status), Has.All.Not.Null);
    }

    // The seed's promises, as the recording captured them (the seed's own verify script asks the same of the live API).
    [Test]
    public void RecordedProducts_SeededProductCarriesItsCategoriesInGroupOrder_AndItsContacts()
    {
        var products = ParseFixture<ServiceRegisterGetProductsResponse>("products.json").Data!;
        var product = products.Single(p => p.ProductName!.StartsWith("Apply for Teacher Training", StringComparison.Ordinal));

        Assert.That(product.Categories!.Select(c => $"{c.GroupName}/{c.Name}"), Is.EqualTo(new[] { "Channel/Web", "Type/Transactional" }));
        Assert.That(products.Where(p => p.Contacts is { Count: > 0 }).SelectMany(p => p.Contacts!),
            Has.All.Matches<ServiceRegisterGetProductsResponseDataItemContact>(c => c.Role is not null && c.RoleId is not null && c.Email is not null && c.CanManage is not null),
            "every recorded contact carries the members the pages read");
    }

    [Test]
    public void RecordedLookups_CarrySeededRows_InactiveIncluded_AndNestedUserGroups()
    {
        var channels = ParseFixture<ServiceRegisterGetFipsChannelsV1Response>("fips/channels.json").Data!;
        var groups = ParseFixture<ServiceRegisterGetFipsUserGroupsV1Response>("fips/user-groups.json").Data!;
        var bundle = ParseFixture<ServiceRegisterGetFipsConfigurationBundleResponse>("fips/configuration.json");

        Assert.That(channels.Select(c => c.Name), Is.SupersetOf(new[] { "Web", "Native app", "Telephone", "Post" }));
        Assert.That(channels.Single(c => c.Name == "Post").Active, Is.False, "the lookups return inactive rows too: Active is a flag, not a filter");
        var teachers = groups.Single(g => g.Name == "Teachers");
        Assert.That(teachers.Children, Is.EqualTo(new[] { "Classroom teachers", "Headteachers" }));
        Assert.That(teachers.Synonyms, Is.EqualTo(new[] { "Teaching staff" }));
        Assert.That(groups.Select(g => g.Name), Does.Not.Contain("Classroom teachers"), "children are nested, not repeated as roots");
        Assert.That(bundle.CategorisationGroups!.SelectMany(g => g.Items!), Has.All.Matches<ServiceRegisterGetFipsConfigurationBundleResponseCategorisationGroupItem>(i => i.CategorisationGroupId is not null));
    }

    [Test]
    public void Product_WhenAMemberIsOmitted_IsNull_AndWhenSentEmpty_IsEmpty()
    {
        // The distinction a consumer depends on: "COMPASS did not say" versus "COMPASS said none".
        var sent = Parse<ServiceRegisterGetProductsResponseDataItem>("""{ "id": "11111111-1111-1111-1111-111111111111", "categories": [] }""");
        var omitted = Parse<ServiceRegisterGetProductsResponseDataItem>("""{ "id": "11111111-1111-1111-1111-111111111111" }""");

        Assert.That(sent.Categories, Is.Not.Null.And.Empty);
        Assert.That(omitted.Categories, Is.Null);
        Assert.That(omitted.IsEnterpriseService, Is.Null, "value members are nullable too: nothing is guaranteed by the far side");
    }

    [Test]
    public void Product_WhenCompassSendsAMemberNobodyHereKnows_ParsesAndIsReportedOnce()
    {
        var node = JsonNode.Parse(File.ReadAllText(Fixture("products/_by-id.json")))!;
        node["data"]!["riskAppetite"] = "tolerant";                       // a member on the product
        node["data"]!["categories"] = new JsonArray(new JsonObject { ["id"] = 1, ["name"] = "Web", ["groupId"] = 2, ["groupName"] = "Channel", ["icon"] = "globe" }); // and one inside a list

        var envelope = Parse<ServiceRegisterGetProductResponse>(node.ToJsonString());
        var observations = FreshObservations();

        observations.Observe("products/{id}", envelope);
        observations.Observe("products/{id}", envelope);

        Assert.That(envelope.Data?.ProductName, Is.Not.Null, "the known members still parse");
        Assert.That(envelope.Data?.Categories?[0].Name, Is.EqualTo("Web"));
        Assert.That(observations.Seen.Select(s => s.Field), Is.EquivalentTo(new[] { "Data.riskAppetite", "Data.Categories[].icon" }),
            "each unexpected member is recorded once, with the path that locates it");
    }

    [Test]
    public void Options_NeverRejectAnUnmappedMember()
    {
        // The tempting "strict" setting would turn every additive COMPASS release into an outage here.
        Assert.That(CompassJson.Options.UnmappedMemberHandling, Is.EqualTo(JsonUnmappedMemberHandling.Skip));
    }
}
