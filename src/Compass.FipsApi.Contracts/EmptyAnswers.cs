namespace Compass.FipsApi.Contracts;

/// <summary>
/// What COMPASS answers when it holds nothing, in its own envelope shapes: the configuration bundle with every
/// vocabulary present and empty, a product page with zero records, a plain empty list, and 404 for a product asked
/// for by id. Stated once, here, because two things must agree on it: the stub's <c>empty</c> scenario and the
/// application's stand-in for an unconfigured COMPASS. A CMS-shaped empty answer (<c>meta.pagination</c>) is not a
/// COMPASS answer: the records would read no pagination and log the envelope as drift.
/// </summary>
public static class EmptyAnswers
{
    public const string Bundle = """{"channels":[],"types":[],"businessAreas":[],"userGroups":[],"contactRoles":[],"categorisationGroups":[]}""";
    public const string Page = """{"data":[],"pagination":{"currentPage":1,"pageSize":100,"totalPages":0,"totalRecords":0}}""";
    public const string List = """{"data":[]}""";
    public const string NotFound = """{"error":"no product has this id"}""";

    // A base for parsing a request path on its own; only the path's segments are read, never this host.
    private static readonly Uri AnyHost = new("http://compass.invalid/");

    /// <summary>The status and body for a request: decided by the path's segments, so a query string or a scenario prefix changes nothing.</summary>
    public static (int Status, string Body) For(Uri request) => For(Segments(request));

    /// <summary>The same, for a path on its own (with or without a leading slash, with or without a query string).</summary>
    public static (int Status, string Body) For(string path) => For(new Uri(AnyHost, path));

    private static (int Status, string Body) For(string[] segments)
    {
        if (segments.Length > 0 && segments[^1] is "configuration" or "fips") return (200, Bundle);
        if (IsProductById(segments)) return (404, NotFound);
        if (segments.Contains("products")) return (200, Page);
        return (200, List);
    }

    /// <summary>True for <c>.../products/{guid}</c>: one product asked for by id.</summary>
    private static bool IsProductById(string[] segments) =>
        segments.Length >= 2 && segments[^2] == "products" && Guid.TryParse(segments[^1], out _);

    // Uri.Segments gives each path segment with its trailing slash ("api/", "products/", "{id}"), still escaped.
    private static string[] Segments(Uri request) =>
        request.Segments.Select(s => Uri.UnescapeDataString(s.TrimEnd('/'))).Where(s => s.Length > 0).ToArray();
}
