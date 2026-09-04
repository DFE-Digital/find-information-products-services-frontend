using System.Net;
using Compass.FipsApi.Stub;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// What a visitor meets at /compass/products and /compass/product/{id}: the products listing and product page as they
/// look from the CMS, read from COMPASS - its vocabularies as the filters, its products as the results - or a plain
/// statement when COMPASS is not configured or cannot be reached.
/// </summary>
[TestFixture]
public class CompassProductsPageTests
{
    private static readonly Dictionary<string, string?> Configured = new()
    {
        ["Compass:BaseUrl"] = "http://compass.example.com/seeded",
        ["Compass:ApiToken"] = "test-only",
    };

    private static readonly Scenarios Scenarios = new(Path.Combine(AppContext.BaseDirectory, "scenarios"));

    private static (HttpStatusCode, string)? Serve(HttpRequestMessage request, string scenario)
    {
        var answer = Scenarios.Answer(scenario, request.RequestUri!.AbsolutePath.Replace("/seeded/", "/", StringComparison.Ordinal));
        return ((HttpStatusCode)answer.Status, answer.Body);
    }

    // The two links on each product card: its title, and the overlay that makes the whole card clickable.
    private const string TitleLinks = "a.product-link.govuk-link";
    private const string OverlayLinks = "a.product-link.dfe-chevron-card__link";

    [Test]
    public async Task CompassProducts_WhenCompassIsNotConfigured_PageSaysSoAndOffersNoFilters()
    {
        using var app = new FipsApplication();

        var response = await app.Client.GetAsync("/compass/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("data-compass-state=\"not-configured\""));
        Assert.That(html, Does.Not.Contain("name=\"channel\""));
        Assert.That(app.Outbound.Snapshot(), Has.None.Contains("ServiceRegister"), "nothing is asked of COMPASS");
    }

    [Test]
    public async Task CompassProducts_WhenSeeded_LooksLikeTheProductsListing_WithCompassVocabulariesAndProducts()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products"));

        // The same listing: the filter form and product cards of /products, pointed under /compass.
        Assert.That(page.QuerySelector("form#products-filter-form")?.GetAttribute("action"), Is.EqualTo("/compass/products"));
        var titleLinks = Html.Hrefs(page, TitleLinks);
        Assert.That(titleLinks, Has.Count.EqualTo(28));
        Assert.That(titleLinks, Has.All.StartWith("/compass/product/"));
        Assert.That(titleLinks.Select(h => Guid.TryParse(h["/compass/product/".Length..], out _)), Has.All.True, "each link carries the product's COMPASS id");
        // Channels: the seed's three active ones offered as filter values, the inactive "Post" not.
        var channels = page.QuerySelectorAll("input[name='channel']").Select(i => i.GetAttribute("value")).ToList();
        Assert.That(channels, Is.SupersetOf(new[] { "Web", "Native app", "Telephone" }));
        Assert.That(channels, Does.Not.Contain("Post"));
        // Phase comes from COMPASS's categorisation bundle; a seeded product carries its phase and business area tags.
        Assert.That(page.QuerySelectorAll("input[name='phase']").Select(i => i.GetAttribute("value")), Does.Contain("Public beta"));
        Assert.That(page.QuerySelectorAll(TitleLinks).Select(Html.Text), Does.Contain("Apply for Teacher Training"));
        Assert.That(page.QuerySelectorAll(".govuk-tag").Select(Html.Text), Does.Contain("Strategy"));
    }

    [Test]
    public async Task CompassProducts_WhenFiltersAreChosenByName_CompassFiltersByItsIds_AndTheChoiceStaysTicked()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products?keywords=teacher&channel=Web&phase=Live&group=Operations&page=2"));

        // The ids are the seeded recording's: Live = 5 in the Phase categorisation group, Web = channel 1, Operations = business area 6.
        Assert.That(app.Outbound.Snapshot(), Has.One.EndsWith("/products?page=2&pageSize=25&status=Active&q=teacher&categoryIds=5&channelIds=1&businessAreaIds=6"));
        Assert.That(page.QuerySelector("input[name='channel'][value='Web']")?.HasAttribute("checked"), Is.True, "the chosen channel stays ticked");
        Assert.That(page.QuerySelectorAll(".filter-badge"), Is.Not.Empty, "the chosen filters are shown as removable badges");
    }

    // The product page is the CMS-backed product's own view fed from COMPASS, so it is recognised by that page's
    // furniture: the masthead strip, the Overview and Categories navigation, and the three sections of the overview.
    private const string ProductId = "00000000-0000-0000-0002-000000000014";
    private const string ProductWithChannelsAndTypes = "00000000-0000-0000-0001-000000000001";

    private static IReadOnlyList<string> TableHeaders(AngleSharp.Html.Dom.IHtmlDocument page) =>
        page.QuerySelectorAll("table.govuk-table th").Select(Html.Text).ToList();

    /// <summary>The categories tab's rows as (name, type), across every table on the page.</summary>
    private static IReadOnlyList<(string Name, string Type)> CategoryRows(AngleSharp.Html.Dom.IHtmlDocument page) =>
        page.QuerySelectorAll("table.govuk-table tbody tr")
            .Select(row => row.QuerySelectorAll("td").Select(Html.Text).ToList())
            .Where(cells => cells.Count >= 2)
            .Select(cells => (cells[0], cells[1]))
            .ToList();

    [Test]
    public async Task CompassProduct_WhenSeeded_IsTheCmsBackedProductPage_FedFromCompass()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        var page = Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductId}"));

        Assert.That(TableHeaders(page), Is.EqualTo(new[] { "Phase", "Business area", "Contacts", "View product" }), "the masthead strip of the CMS-backed product page");
        Assert.That(Html.Hrefs(page, "nav.dfe-vertical-nav a"), Is.EqualTo(new[] { $"/compass/product/{ProductId}", $"/compass/product/{ProductId}/categories" }), "Overview and Categories, pointing at this product's own pages");
        Assert.That(page.QuerySelectorAll("h2").Select(Html.Text), Is.SupersetOf(new[] { "Description", "Responsibilities and contacts" }));
        Assert.That(page.QuerySelector("details .govuk-details__summary-text") is { } identifiers && Html.Text(identifiers) == "Product identifiers");
        Assert.That(Html.Hrefs(page, "a.govuk-back-link"), Is.EqualTo(new[] { "/compass/products" }), "back goes to this listing, not the CMS-backed one");
        Assert.That(app.Outbound.Snapshot(), Has.One.EndsWith($"/products/{ProductId}"));
    }

    [Test]
    public async Task CompassProduct_WhatCompassCannotSupply_ShowsAsNotAvailable_AndItsOwnIdShowsAsCompassId()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        var rows = Html.SummaryRows(Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductId}")));

        Assert.That(rows, Is.SupersetOf(new[] { ("FIPS ID", "Not available: COMPASS identifies products by its own id"), ("COMPASS ID", ProductId), ("CMDB System ID", "No CMDB entry found") }));
        Assert.That(rows.Select(r => r.Key), Does.Not.Contain("Document ID"), "the CMS's document id is not reused for COMPASS's identifier");
    }

    [Test]
    public async Task CompassProduct_WhenCompassLinksItToChannelsAndTypes_TheCategoriesTabShowsThem_LinkingIntoThisListing()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // The assignment rule links this product to a channel, a type, and a business area; the recording is its own file in the scenario.
        var page = Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductWithChannelsAndTypes}/categories"));

        Assert.That(Html.H1Headings(page), Is.EqualTo(new[] { "Apply for Teacher Training" }));
        Assert.That(CategoryRows(page), Is.SupersetOf(new[] { ("Phone", "Channel"), ("Transactional", "Type"), ("Strategy", "Business area") }));
        Assert.That(Html.Hrefs(page, ".govuk-grid-column-three-quarters table.govuk-table a"), Has.All.StartWith("/compass/products?"), "a category's link filters this listing by that value");
    }

    /// <summary>The categories tab's rows as (name, description), from the table that has a description column.</summary>
    private static IReadOnlyList<(string Name, string Description)> CategoryDescriptions(AngleSharp.Html.Dom.IHtmlDocument page) =>
        page.QuerySelectorAll("table.govuk-table tbody tr")
            .Select(row => row.QuerySelectorAll("td").Select(Html.Text).ToList())
            .Where(cells => cells.Count == 3)
            .Select(cells => (cells[0], cells[2]))
            .ToList();

    [Test]
    public async Task CompassProduct_CategoriesTab_ShowsTheDescriptionCompassHoldsForAValue()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // The seed describes the Transactional type; the description lives in COMPASS's configuration bundle, not on the product.
        var rows = CategoryDescriptions(Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductWithChannelsAndTypes}/categories")));

        Assert.That(rows.Single(r => r.Name == "Transactional").Description, Does.StartWith("The user completes a task"));
    }

    [Test]
    public async Task CompassProduct_CategoriesTab_SaysWhenCompassHoldsNoDescriptionForAValue()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // The seed describes four values only; the Private Beta phase (the row's spelling, which its tag folds into) is not one of
        // them, and the cell says so rather than staying blank.
        var rows = CategoryDescriptions(Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductWithChannelsAndTypes}/categories")));

        Assert.That(rows.Single(r => r.Name == "Private Beta").Description, Is.EqualTo("Not available: COMPASS holds no description for this value"));
    }

    [Test]
    public async Task CompassProduct_CategoriesTab_ListsTheUsersOfTheProduct_LinkingIntoThisListing()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // The assignment rule gives every seeded product a user group, carried as a categorisation tag.
        var page = Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductWithChannelsAndTypes}/categories"));

        Assert.That(page.QuerySelectorAll("h2").Select(Html.Text), Does.Contain("Users of this product"));
        var userGroupLinks = page.QuerySelectorAll("table.govuk-table a").Where(a => Html.Text(a) == "Chief Social Worker for Children and Families").ToList();
        Assert.That(userGroupLinks, Has.Count.EqualTo(1));
        Assert.That(userGroupLinks[0].GetAttribute("href"), Does.StartWith("/compass/products?keywords="));
    }

    [Test]
    public async Task CompassProduct_WhenPhaseAndBusinessAreaAreBothTaggedAndNamedOnTheRow_EachShowsOnce()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);

        // The drift scenario's product carries Live and Digital Services both as categorisation tags and as the row's own phase and business area.
        var page = Html.Parse(await app.Client.GetStringAsync("/compass/product/00000000-0000-0000-0002-000000000001/categories"));

        var rows = CategoryRows(page);
        Assert.That(rows.Where(r => r.Type == "Phase"), Is.EqualTo(new[] { ("Live", "Phase") }));
        Assert.That(rows.Where(r => r.Type == "Business area"), Is.EqualTo(new[] { ("Digital Services", "Business area") }));
    }

    [Test]
    public async Task CompassProduct_WhenCompassLinksAndTagsTheSameChannelOrType_EachShowsOnce()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // The seed both links this product to the Phone channel and Transactional type and tags it "Channel: Phone" and "Type: Transactional".
        var rows = CategoryRows(Html.Parse(await app.Client.GetStringAsync($"/compass/product/{ProductWithChannelsAndTypes}/categories")));

        Assert.That(rows.Where(r => r.Type == "Channel"), Is.EqualTo(new[] { ("Phone", "Channel") }));
        Assert.That(rows.Where(r => r.Type == "Type"), Is.EqualTo(new[] { ("Transactional", "Type") }));
    }

    [Test]
    public async Task CompassProduct_WhenTheRowAndItsTagDisagreeOnThePhase_BothShow()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);

        // The drift scenario's second product is tagged "Phase: Live" while its own phase field says "Public beta".
        var rows = CategoryRows(Html.Parse(await app.Client.GetStringAsync("/compass/product/00000000-0000-0000-0002-000000000002/categories")));

        Assert.That(rows.Where(r => r.Type == "Phase"), Is.EqualTo(new[] { ("Public beta", "Phase"), ("Live (from COMPASS's categorisation group, not its phase field)", "Phase") }),
            "a conflict in COMPASS's data is shown with its sources, not resolved here");
    }

    [Test]
    public async Task CompassProduct_WhenTheRowAndItsTagDisagreeOnThePhase_TheMastheadShowsTheRowsPhase()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);

        var page = Html.Parse(await app.Client.GetStringAsync("/compass/product/00000000-0000-0000-0002-000000000002"));

        Assert.That(Html.Text(page.QuerySelector(".dfe-masthead .govuk-tag")), Is.EqualTo("Public beta"), "the row's own phase field, the direct source, is what the strip shows");
    }

    [Test]
    public async Task CompassProduct_WhenOnePersonHoldsTwoRoles_EachRoleShowsThem()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);

        // The drift scenario's third product names the same person as product manager and as service owner. COMPASS
        // sends one contact row per role, and the page shows each role with its person, in the order the view lists roles.
        var rows = Html.SummaryRows(Html.Parse(await app.Client.GetStringAsync("/compass/product/00000000-0000-0000-0002-000000000003")));

        Assert.That(rows.Where(r => r.Value.Contains("One Person")), Is.EqualTo(new[] { ("Service Owner", "One Person"), ("Product Manager", "One Person") }));
    }

    [Test]
    public async Task CompassProduct_WhenTheProductIsNotActive_StillRenders_AsTheCmsBackedProductPageDoes()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Drift);

        // The drift scenario's second product is Inactive: absent from the listing, reachable by its link.
        var response = await app.Client.GetAsync("/compass/product/00000000-0000-0000-0002-000000000002");
        var page = Html.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Html.H1Headings(page), Is.EqualTo(new[] { "Drifted product (row and tag disagree)" }));
    }

    [Test]
    public async Task CompassProducts_WhenAFilterNameIsUnknownToCompass_NothingIsListed_AndTheChoiceStaysRemovable()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // A stale link or a typed url can name a phase COMPASS does not hold. No product can match it.
        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products?phase=ThisPhaseDoesNotExist"));

        Assert.That(page.QuerySelectorAll("a.product-link"), Is.Empty);
        Assert.That(page.QuerySelectorAll(".filter-badge").Select(Html.Text), Has.Some.Contains("ThisPhaseDoesNotExist"), "the choice is shown so it can be removed");
        Assert.That(app.Outbound.Snapshot(), Has.None.Contains("/ServiceRegister/products?"), "COMPASS is not asked for an unfiltered listing in its place");
    }

    [Test]
    public async Task CompassProduct_WhenCompassDoesNotKnowTheId_Answers404()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => (HttpStatusCode.NotFound, """{"error":"not found"}""");

        var response = await app.Client.GetAsync($"/compass/product/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CompassProducts_WhenCompassIsUnavailable_PageSaysSoWithoutTechnicalDetail()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Unavailable);

        var response = await app.Client.GetAsync("/compass/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("data-compass-state=\"unavailable\""));
        // The page body, not the layout: the telemetry script in <head> legitimately mentions exceptions.
        var main = Html.Text(Html.Parse(html).QuerySelector("main"));
        Assert.That(main, Is.Not.Empty.And.Not.Contain("Exception").And.Not.Contain("503"));
    }

    [Test]
    public async Task CompassProducts_WhenCompassHasNoProducts_ListingShowsNoneAndStillOffersFilters()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Empty);

        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products"));

        Assert.That(page.QuerySelectorAll("a.product-link"), Is.Empty);
        Assert.That(page.QuerySelector("input[name='keywords']"), Is.Not.Null);
    }

    [Test]
    public async Task CompassProducts_WhenSeeded_OnlyActiveProductsAreAskedOf_Compass_AsTheCmsListingShowsActiveOnly()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        await app.Client.GetStringAsync("/compass/products");

        // COMPASS holds New, Inactive and Rejected products too; the public listing shows what it lists from the CMS: Active only.
        var products = app.Outbound.Snapshot().Single(r => r.Contains("/ServiceRegister/products?"));
        Assert.That(products, Does.Contain("status=Active"));
        Assert.That(products, Does.Not.Contain("status=New").And.Not.Contain("status=Inactive").And.Not.Contain("status=Rejected"));
    }

    [Test]
    public async Task CompassProducts_WhenSeeded_ClickingACardBodyOpensTheSameProductAsItsTitle()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products"));

        var titleLinks = Html.Hrefs(page, TitleLinks);
        var overlayLinks = Html.Hrefs(page, OverlayLinks);
        Assert.That(titleLinks, Has.Count.EqualTo(28));
        Assert.That(overlayLinks, Is.EqualTo(titleLinks), "the card body opens the product the title names");

        var opened = await app.Client.GetAsync(overlayLinks[0]);
        Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CompassProducts_WhenThereIsAnotherPage_TheNextPageLinkOpensIt_KeepingTheFilters()
    {
        using var app = new FipsApplication(settings: Configured);
        app.Outbound.Respond = r => Serve(r, Scenarios.Seeded);

        // 28 seeded products at 25 a page: a second page exists.
        var page = Html.Parse(await app.Client.GetStringAsync("/compass/products?channel=Web"));

        var href = page.QuerySelector("a.govuk-pagination__link[rel='next']")?.GetAttribute("href");
        Assert.That(href, Is.Not.Null, "the listing offers a next page");
        Assert.That(href, Does.StartWith("/compass/products?").And.Contain("channel=Web").And.Contain("page=2"));

        var response = await app.Client.GetAsync(href);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(app.Outbound.Snapshot(), Has.One.Contains("/products?page=2&"), "the second page is asked of COMPASS");
    }

    [Test]
    public async Task Products_TheCmsBackedListing_StillPointsAtItsOwnPaths()
    {
        using var app = new FipsApplication();

        var html = await app.Client.GetStringAsync("/products");

        Assert.That(html, Does.Contain("action=\"/products\""));
        Assert.That(html, Does.Not.Contain("/compass/"));
    }
}
