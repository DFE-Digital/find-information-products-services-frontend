namespace FipsFrontend.Models;

// Bound from the "AccessibilityStatement" configuration section (AccessibilityStatement__Url as an environment variable).
public class AccessibilityStatementOptions
{
    public const string SectionName = "AccessibilityStatement";

    // The service's published accessibility statement, linked from every page's footer.
    public const string DefaultUrl = "https://accessibility-statements.education.gov.uk/s/43";

    // Where the statement lives for this instance. Blank means the departmental statement above: a footer without an
    // accessibility statement is not an option, so this section switches the address, never the link.
    public string? Url { get; set; } = DefaultUrl;

    public string EffectiveUrl => string.IsNullOrWhiteSpace(Url) ? DefaultUrl : Url;
}
