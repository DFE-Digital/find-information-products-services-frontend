using Microsoft.Extensions.Configuration;

namespace FipsFrontend.Configuration;

/// <summary>
/// The content source. OPTIONAL, all-or-nothing: leave every value empty and the application runs
/// with no content - every page renders its empty state, nothing is fetched - which is what a
/// first run from a fresh clone gets; supply any and <see cref="BaseUrl"/> (absolute) and
/// <see cref="ReadApiKey"/> are required. <see cref="WriteApiKey"/> is optional: without it,
/// nothing is written to the content source (search-term logging is off).
/// Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class CmsApiOptions : IOptionalSection
{
    public const string Section = "CmsApi";

    public string BaseUrl { get; set; } = "";
    public string ReadApiKey { get; set; } = "";
    public string WriteApiKey { get; set; } = "";

    /// <summary>
    /// The base address, absolute with a trailing slash; set by <see cref="Read"/> once validated.
    /// Without a content source it is a placeholder under the reserved example domain: the clients
    /// need an address to build requests against, and the in-process handler never sends them.
    /// </summary>
    public Uri BaseAddress { get; private set; } = new("http://content-source.example.com/api/");

    public bool CanWrite => !ConfigurationSections.IsAbsent(WriteApiKey);

    public bool IsConfigured =>
        !ConfigurationSections.IsAbsent(BaseUrl) ||
        !ConfigurationSections.IsAbsent(ReadApiKey) ||
        !ConfigurationSections.IsAbsent(WriteApiKey);

    public IEnumerable<string> MissingRequired()
    {
        if (ConfigurationSections.IsAbsent(BaseUrl)) yield return nameof(BaseUrl);
        if (ConfigurationSections.IsAbsent(ReadApiKey)) yield return nameof(ReadApiKey);
    }

    /// <summary>
    /// The section as configured; <see cref="IsConfigured"/> is false when there is no content
    /// source. Refuses a partly supplied section or a relative address.
    /// </summary>
    public static CmsApiOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(Section);
        var options = new CmsApiOptions
        {
            BaseUrl = section[nameof(BaseUrl)] ?? "",
            ReadApiKey = section[nameof(ReadApiKey)] ?? "",
            WriteApiKey = section[nameof(WriteApiKey)] ?? "",
        };
        ConfigurationSections.RefuseIfPartlySupplied(options, Section, "the application then runs with no content.");
        if (!options.IsConfigured) return options;

        options.BaseAddress = ConfigurationSections.TryNormaliseBaseUrl(options.BaseUrl)
            ?? throw new InvalidOperationException(
                $"{Section}:{nameof(BaseUrl)} must be an absolute http(s) URL; found '{options.BaseUrl}'.");
        return options;
    }
}
