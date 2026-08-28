using System.Text.RegularExpressions;

namespace FipsFrontend.Tests.TestSupport;

/// <summary>Just enough reading of a rendered page for assertions to name what a visitor sees.</summary>
public static class Html
{
    /// <summary>The text of every h1 on the page, tags stripped.</summary>
    public static IReadOnlyList<string> Headings(string html) =>
        Regex.Matches(html, "<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline)
            .Select(m => Text(m.Groups[1].Value))
            .ToList();

    /// <summary>The visible text of the main two-thirds column, whitespace collapsed.</summary>
    public static string MainColumnText(string html)
    {
        var start = html.IndexOf("govuk-grid-column-two-thirds", StringComparison.Ordinal);
        if (start < 0) return "";
        var end = html.IndexOf("govuk-grid-column-one-third", start, StringComparison.Ordinal);
        return Text(end < 0 ? html[start..] : html[start..end]);
    }

    /// <summary>Enough of a page to name the error the developer page rendered.</summary>
    public static string Excerpt(string html)
    {
        var text = Text(html);
        return text.Length > 600 ? text[..600] : text;
    }

    private static string Text(string fragment) =>
        Regex.Replace(Regex.Replace(fragment, "<.*?>", " "), @"\s+", " ").Trim();
}
