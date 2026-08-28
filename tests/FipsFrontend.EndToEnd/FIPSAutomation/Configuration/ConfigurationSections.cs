namespace FiPSAutomation.Configuration;

/// <summary>
/// The rules every configuration section of the suite follows - the same rules the application's
/// own options follow, so that a person who has configured one has configured the other. Stated
/// once; the options classes and the template's hints point here rather than restating them.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item><b>A key string lives in exactly one place</b>: the <c>Section</c> constant and the
/// property names of its options class. Nothing else in the suite names a configuration key.</item>
/// <item><b>Empty or whitespace-only is absent.</b> The template ships with every key present and
/// empty, so a presence check must not count an empty string as supplied. See <see cref="IsAbsent"/>.</item>
/// <item><b>A section is all-or-nothing.</b> A section with nothing supplied is a deliberate
/// off-switch; a section with some values supplied must have all its required ones, or the run
/// refuses to start naming the missing keys. See <see cref="IOptionalSection"/>.</item>
/// <item><b>Shape validation lives in the options class</b>: a base URL must be absolute and is
/// normalised to a trailing slash once, here, not at each call site.</item>
/// </list>
/// </remarks>
public static class ConfigurationSections
{
    /// <summary>Rule 2: empty or whitespace-only means the value was not supplied.</summary>
    public static bool IsAbsent(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Rule 4 for base URLs: must parse as an absolute http(s) URI; returned with a trailing slash,
    /// so that relative paths appended to it resolve inside it rather than beside it.
    /// </summary>
    public static Uri? TryNormaliseBaseUrl(string? value)
    {
        if (IsAbsent(value)) return null;
        if (!Uri.TryCreate(value!.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
    }

    /// <summary>
    /// Rule 3 applied: an absent section is fine; a partly supplied one is refused with every
    /// missing key named, so a person fixing the configuration fixes it once.
    /// </summary>
    public static string? PartlySupplied(IOptionalSection section, string sectionName)
    {
        if (!section.IsConfigured) return null;
        var missing = section.MissingRequired().ToList();
        return missing.Count == 0
            ? null
            : $"Configuration section '{sectionName}' is partly supplied; it needs all of: {string.Join(", ", missing.Select(k => $"{sectionName}:{k}"))}. " +
              "Supply them, or clear the whole section to switch the feature off.";
    }
}

/// <summary>
/// A section the suite can run without. Absent entirely, it switches its feature off;
/// partly present, it must be complete.
/// </summary>
public interface IOptionalSection
{
    /// <summary>True when any value in the section was supplied (rule 2 applied per value).</summary>
    bool IsConfigured { get; }

    /// <summary>The keys required once the section is in use that are absent. Empty when complete.</summary>
    IEnumerable<string> MissingRequired();
}
