using Compass.FipsApi.Contracts.Generated;
using FipsFrontend.Configuration;
using FipsFrontend.Models;
using FipsFrontend.Services.Compass;
using Microsoft.AspNetCore.Mvc;

namespace FipsFrontend.Controllers;

/// <summary>
/// The products listing and product page read from COMPASS instead of the CMS, rendered through the same views under
/// /compass/... . Filters come from COMPASS's configuration bundle; filtering and paging are COMPASS's. A failure is
/// logged and the page says COMPASS could not be reached; nothing technical reaches the visitor.
/// </summary>
[Route("compass")]
public class CompassProductsController(ICompassClient compass, CompassOptions options, ILogger<CompassProductsController> logger) : Controller
{
    private const int PageSize = 25;
    private const string ListingPath = "/compass/products";
    private const string ProductPath = "/compass/product";

    [HttpGet("products")]
    public async Task<IActionResult> Index(string? keywords, string[]? phase, string[]? group, string[]? channel, string[]? type, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) return View("State", CompassState.NotConfigured);

        var model = new ProductsViewModel
        {
            PageTitle = "Search and filter products and services (from COMPASS)",
            ListingPath = ListingPath,
            ProductPath = ProductPath,
            Keywords = keywords,
            KeywordTerms = Terms(keywords),
            SelectedPhases = List(phase),
            SelectedGroups = List(group),
            SelectedChannels = List(channel),
            SelectedTypes = List(type),
            CurrentPage = Math.Max(page, 1),
            PageSize = PageSize,
            // What this listing cannot do the way the CMS-backed one does, as COMPASS's API stands at the commit the
            // contracts were generated from (named in the stub's scenarios README).
            Notices =
            [
                "[NOT AVAILABLE VIA COMPASS] FIPS IDs: product links use COMPASS's own identifier instead.",
                "[NOT AVAILABLE VIA COMPASS] Short descriptions: COMPASS supplies one description per product.",
                "[DIFFERS VIA COMPASS] Two taxonomies: the channel, type and business-area filters use COMPASS's dedicated lookups, which each product names for channels and types but only by name for business areas; the categorisation groups tag products separately.",
                "[DIFFERS VIA COMPASS] Phase: COMPASS cannot filter products by the phase it shows on them; this filter uses its categorisation group \"Phase\" instead, which matches only products tagged in that group.",
                "[DIFFERS VIA COMPASS] Nesting: COMPASS nests user groups (children by name only), not business areas, so sub-group filtering is not offered.",
                "[NOT AVAILABLE VIA COMPASS] CMDB status and parent filters.",
                "[DIFFERS VIA COMPASS] Ordering: COMPASS sorts titles without regard to case, so a lower-case title sorts among the capitalised ones; the CMS sorts capitals first.",
            ],
        };

        try
        {
            var bundle = await compass.GetFipsConfigurationAsync(cancellationToken);
            var phases = Vocabulary(bundle.CategorisationGroups?.FirstOrDefault(g => Is(g.Name, "Phase"))?.Items?.Where(i => i.Active != false).Select(i => (i.Id, i.Name)));
            var channels = Vocabulary(bundle.Channels?.Where(c => c.Active != false).Select(c => (c.Id, c.Name)));
            var types = Vocabulary(bundle.Types?.Where(t => t.Active != false).Select(t => (t.Id, t.Name)));
            var areas = Vocabulary(bundle.BusinessAreas?.Where(b => b.Active != false).Select(b => (b.Id, b.Name)));

            model.PhaseOptions = Options(phases, model.SelectedPhases);
            model.ChannelOptions = Options(channels, model.SelectedChannels);
            model.TypeOptions = Options(types, model.SelectedTypes);
            model.GroupOptions = Options(areas, model.SelectedGroups);
            model.SelectedFilters = Chips(model);

            // The view's filter values are names, as on the CMS-backed page; COMPASS filters by id. A name COMPASS's
            // vocabulary does not hold (a stale link, a typed url) can match no product, so nothing is listed and the
            // choice stays on the page as a removable badge. Sending the request without that filter would list
            // everything as though the choice had been honoured.
            if (Unknown(phases, model.SelectedPhases) || Unknown(channels, model.SelectedChannels) || Unknown(types, model.SelectedTypes) || Unknown(areas, model.SelectedGroups))
            {
                model.Products = [];
                model.FilteredCount = model.TotalCount = 0;
                return View("~/Views/Products/Index.cshtml", model);
            }

            // The public listing shows Active products only, as the CMS-backed one does: COMPASS also holds New, Inactive and Rejected.
            var products = await compass.GetProductsAsync(new ProductQuery(
                Page: model.CurrentPage,
                PageSize: PageSize,
                Keywords: keywords,
                Status: ["Active"],
                CategoryIds: Ids(phases, model.SelectedPhases),
                ChannelIds: Ids(channels, model.SelectedChannels),
                TypeIds: Ids(types, model.SelectedTypes),
                BusinessAreaIds: Ids(areas, model.SelectedGroups)), cancellationToken);

            model.Products = (products.Data ?? []).Select(Product).ToList();
            model.FilteredCount = model.TotalCount = products.Pagination?.TotalRecords ?? model.Products.Count; // the view derives the page count
        }
        catch (CompassUnavailableException e)
        {
            logger.LogError(e, "COMPASS could not serve the products listing: {Endpoint}", e.Endpoint);
            return View("State", CompassState.Unavailable);
        }

        return View("~/Views/Products/Index.cshtml", model);
    }

    [HttpGet("product/{id:guid}")]
    public async Task<IActionResult> Product(Guid id, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured) return View("State", CompassState.NotConfigured);
        try
        {
            // Any product COMPASS knows renders, whatever its status: the listing shows Active products only, but the
            // CMS-backed product page renders any product found by id, and this page follows it. A product that has
            // left the listing stays reachable by its link, with its status shown on the page.
            var product = await compass.GetProductAsync(id, cancellationToken);
            if (product is null) return NotFound();
            return View("Product", new CompassProductViewModel(Product(product), product.Contacts ?? [], product.LastUpdated) { PageTitle = product.ProductName });
        }
        catch (CompassUnavailableException e)
        {
            logger.LogError(e, "COMPASS could not serve product {Id}: {Endpoint}", id, e.Endpoint);
            return View("State", CompassState.Unavailable);
        }
    }

    private static bool Is(string? a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static List<string> List(string[]? values) => (values ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
    private static List<string> Terms(string? keywords) => (keywords ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>A vocabulary's usable rows: those with both an id to filter by and a name to show.</summary>
    private static List<(int Id, string Name)> Vocabulary(IEnumerable<(int? Id, string? Name)>? rows)
    {
        var usable = new List<(int Id, string Name)>();
        foreach (var (id, name) in rows ?? [])
        {
            if (id is int known && !string.IsNullOrWhiteSpace(name)) usable.Add((known, name));
        }
        return usable;
    }

    private static List<int>? Ids(List<(int Id, string Name)> vocabulary, List<string> selectedNames)
    {
        var ids = vocabulary.Where(v => selectedNames.Contains(v.Name, StringComparer.OrdinalIgnoreCase)).Select(v => v.Id).ToList();
        return ids.Count == 0 ? null : ids;
    }

    /// <summary>True when a selected name is not in the vocabulary, so no id could stand for it.</summary>
    private static bool Unknown(List<(int Id, string Name)> vocabulary, List<string> selectedNames) =>
        selectedNames.Any(name => !vocabulary.Any(v => Is(v.Name, name)));

    private static List<FilterOption> Options(List<(int Id, string Name)> vocabulary, List<string> selected) =>
        vocabulary.Select(v => new FilterOption { Value = v.Name, Text = v.Name, IsSelected = selected.Contains(v.Name, StringComparer.OrdinalIgnoreCase) }).ToList();

    private static List<SelectedFilter> Chips(ProductsViewModel model)
    {
        var chips = new List<SelectedFilter>();
        Add("Phase", model.SelectedPhases, "phase");
        Add("Business area", model.SelectedGroups, "group");
        Add("Channel", model.SelectedChannels, "channel");
        Add("Type", model.SelectedTypes, "type");
        return chips;

        void Add(string category, List<string> values, string parameter)
        {
            foreach (var value in values)
            {
                var remaining = new List<string> { $"keywords={Uri.EscapeDataString(model.Keywords ?? "")}" };
                remaining.AddRange(model.SelectedPhases.Where(v => !(parameter == "phase" && v == value)).Select(v => "phase=" + Uri.EscapeDataString(v)));
                remaining.AddRange(model.SelectedGroups.Where(v => !(parameter == "group" && v == value)).Select(v => "group=" + Uri.EscapeDataString(v)));
                remaining.AddRange(model.SelectedChannels.Where(v => !(parameter == "channel" && v == value)).Select(v => "channel=" + Uri.EscapeDataString(v)));
                remaining.AddRange(model.SelectedTypes.Where(v => !(parameter == "type" && v == value)).Select(v => "type=" + Uri.EscapeDataString(v)));
                chips.Add(new SelectedFilter { Category = category, Value = value, DisplayText = value, RemoveUrl = ListingPath + "?" + string.Join("&", remaining) });
            }
        }
    }

    /// <summary>A COMPASS row as the views' product: the id is COMPASS's, carried in DocumentId so the product link uses it.</summary>
    private static Product Product(ServiceRegisterGetProductsResponseDataItem p) => new()
    {
        DocumentId = p.Id?.ToString(),
        Title = p.ProductName ?? "(unnamed)",
        LongDescription = p.LongDescription,
        ShortDescription = p.LongDescription ?? "",
        ProductUrl = p.ProductUrl,
        State = p.Status ?? "",
        UpdatedAt = p.LastUpdated,
        CategoryValues = Categories(p),
    };

    /// <summary>
    /// A product's category values from everything COMPASS says about it: its categorisation tags, the channel and
    /// type lookups it is linked to, and the phase and business area named on the row itself. COMPASS can say the
    /// same thing twice (a "Channel: Web" tag and a link to the Web channel; a "Phase: Live" tag and phase "Live" on
    /// the row), and that shows once. Where two sources disagree (the row says one phase, the tag another) both
    /// show, because hiding either would decide a conflict in COMPASS's data silently on its behalf.
    /// </summary>
    private static List<CategoryValue> Categories(ServiceRegisterGetProductsResponseDataItem p)
    {
        var values = (p.Categories ?? [])
            .Select(c => new CategoryValue { Id = c.Id ?? 0, Name = c.Name ?? "", CategoryType = new CategoryType { Name = c.GroupName ?? "" } })
            .ToList();
        values.AddRange((p.Channels ?? []).Select(c => new CategoryValue { Id = c.Id ?? 0, Name = c.Name ?? "", CategoryType = new CategoryType { Name = "Channel" } }));
        values.AddRange((p.Types ?? []).Select(t => new CategoryValue { Id = t.Id ?? 0, Name = t.Name ?? "", CategoryType = new CategoryType { Name = "Type" } }));
        if (!string.IsNullOrWhiteSpace(p.Phase))
            values.Add(new CategoryValue { Name = p.Phase, CategoryType = new CategoryType { Name = "Phase" } });
        if (!string.IsNullOrWhiteSpace(p.BusinessArea))
            values.Add(new CategoryValue { Name = p.BusinessArea, CategoryType = new CategoryType { Name = "Business area" } });
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .DistinctBy(v => (Type: (v.CategoryType?.Name ?? "").ToUpperInvariant(), Name: v.Name.ToUpperInvariant()))
            .ToList();
    }
}

public enum CompassState { NotConfigured, Unavailable }

public sealed class CompassProductViewModel(Product product, List<ServiceRegisterGetProductsResponseDataItemContact> contacts, DateTime? lastUpdated) : BaseViewModel
{
    public Product Product { get; } = product;
    public List<ServiceRegisterGetProductsResponseDataItemContact> Contacts { get; } = contacts;
    public DateTime? LastUpdated { get; } = lastUpdated;
}
