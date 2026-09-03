using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace FipsFrontend.Tests.TestSupport;

/// <summary>
/// Just enough reading of a rendered page for assertions to name what a visitor sees.
/// Pages are parsed, never pattern-matched: a regular expression over HTML fixes one serialisation
/// (attribute order, whitespace, nesting) and matches nothing, silently, when the template changes shape.
/// </summary>
public static class Html
{
    private static readonly HtmlParser Parser = new();

    public static IHtmlDocument Parse(string html) => Parser.ParseDocument(html);

    /// <summary>The text of every h1 on the page.</summary>
    public static IReadOnlyList<string> H1Headings(string html) => H1Headings(Parse(html));

    public static IReadOnlyList<string> H1Headings(IHtmlDocument page) =>
        page.QuerySelectorAll("h1").Select(Text).ToList();

    /// <summary>The visible text of the main two-thirds column, whitespace collapsed.</summary>
    public static string MainColumnText(string html) =>
        Text(Parse(html).QuerySelector(".govuk-grid-column-two-thirds"));

    /// <summary>Enough of a page to name the error the developer page rendered.</summary>
    public static string Excerpt(string html)
    {
        var text = Text(Parse(html).Body);
        return text.Length > 600 ? text[..600] : text;
    }

    /// <summary>The href of every element the selector matches, in page order.</summary>
    public static IReadOnlyList<string> Hrefs(IHtmlDocument page, string selector) =>
        page.QuerySelectorAll(selector).Select(e => e.GetAttribute("href") ?? "").ToList();

    /// <summary>Every GOV.UK summary-list row as its key and value text.</summary>
    public static IReadOnlyList<(string Key, string Value)> SummaryRows(IHtmlDocument page) =>
        page.QuerySelectorAll(".govuk-summary-list__row")
            .Select(row => (Text(row.QuerySelector(".govuk-summary-list__key")), Text(row.QuerySelector(".govuk-summary-list__value"))))
            .ToList();

    /// <summary>An element's text with whitespace collapsed; empty when there is no element.</summary>
    public static string Text(INode? node) =>
        string.Join(' ', (node?.TextContent ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
