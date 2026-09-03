namespace FipsFrontend.Configuration;

/// <summary>
/// The COMPASS service-register API: where it is and the bearer token that reads it. All or nothing;
/// absent means the pages that read COMPASS say so. Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class CompassOptions : IOptionalSection
{
    public const string Section = "Compass";

    public string BaseUrl { get; set; } = "";
    public string ApiToken { get; set; } = "";

    /// <summary>The COMPASS root with a trailing slash (a scenario prefix on the stub is part of it); a placeholder when off.</summary>
    public Uri BaseAddress { get; private set; } = new("http://compass.example.com/");

    public bool IsConfigured => !ConfigurationSections.IsAbsent(BaseUrl) || !ConfigurationSections.IsAbsent(ApiToken);

    public IEnumerable<string> MissingRequired()
    {
        if (ConfigurationSections.IsAbsent(BaseUrl)) yield return nameof(BaseUrl);
        if (ConfigurationSections.IsAbsent(ApiToken)) yield return nameof(ApiToken);
    }

    public static CompassOptions Read(IConfiguration configuration)
    {
        var options = new CompassOptions();
        configuration.GetSection(Section).Bind(options);
        if (!options.IsConfigured) return options;

        ConfigurationSections.RefuseIfPartlySupplied(options, Section, "the pages that read COMPASS then say it is not configured.");
        options.BaseAddress = ConfigurationSections.TryNormaliseBaseUrl(options.BaseUrl)
            ?? throw new InvalidOperationException($"{Section}:{nameof(BaseUrl)} must be an absolute http(s) URL; found '{options.BaseUrl}'.");
        return options;
    }
}
