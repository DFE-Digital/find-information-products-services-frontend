using Microsoft.Extensions.Configuration;

namespace FipsFrontend.Configuration;

/// <summary>
/// Sign-in through the identity provider. OPTIONAL, all-or-nothing: leave the tenant, client, and
/// secret empty and the application serves its pages without sign-in (a developer's machine, the
/// pipeline's copy); supply any and all three are required. <see cref="Instance"/> has a default
/// (the public cloud) and does not count as supplying the section. The identity library binds the
/// same section itself and validates it on the first request, which is too late and too vague:
/// this class decides at start-up whether sign-in is registered at all.
/// Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class AzureAdOptions : IOptionalSection
{
    public const string Section = "AzureAd";

    public string Instance { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    public bool IsConfigured =>
        !ConfigurationSections.IsAbsent(TenantId) ||
        !ConfigurationSections.IsAbsent(ClientId) ||
        !ConfigurationSections.IsAbsent(ClientSecret);

    public IEnumerable<string> MissingRequired()
    {
        if (ConfigurationSections.IsAbsent(TenantId)) yield return nameof(TenantId);
        if (ConfigurationSections.IsAbsent(ClientId)) yield return nameof(ClientId);
        if (ConfigurationSections.IsAbsent(ClientSecret)) yield return nameof(ClientSecret);
    }

    /// <summary>The section as configured, or null when sign-in is off. Refuses a partly supplied section.</summary>
    public static AzureAdOptions? Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(Section);
        var options = new AzureAdOptions
        {
            Instance = section[nameof(Instance)] ?? "",
            TenantId = section[nameof(TenantId)] ?? "",
            ClientId = section[nameof(ClientId)] ?? "",
            ClientSecret = section[nameof(ClientSecret)] ?? "",
        };
        ConfigurationSections.RefuseIfPartlySupplied(options, Section, "the application then runs without sign-in.");
        return options.IsConfigured ? options : null;
    }
}
