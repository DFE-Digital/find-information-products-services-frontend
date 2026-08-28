using FipsFrontend.Configuration;

namespace FipsFrontend.Models;

// Bound from the "SAS" configuration section (SAS__BaseUrl, SAS__SecretId as environment variables).
// OPTIONAL, all-or-nothing: leave both empty and the assessments integration is off (every lookup
// answers empty); supply either and both are required. Turning EnabledFeatures:Assurance on needs it.
// Rules in ConfigurationSections.
public class SasOptions : IOptionalSection
{
    public const string SectionName = "SAS";

    public bool IsConfigured => EffectiveBaseUrl is not null || !ConfigurationSections.IsAbsent(SecretId);

    public IEnumerable<string> MissingRequired()
    {
        if (EffectiveBaseUrl is null) yield return nameof(BaseUrl);
        if (ConfigurationSections.IsAbsent(SecretId)) yield return nameof(SecretId);
    }

    // The section as configured; IsConfigured is false when the integration is off.
    // Refuses a partly supplied section or a relative address.
    public static SasOptions Read(IConfiguration configuration)
    {
        var options = new SasOptions();
        configuration.GetSection(SectionName).Bind(options);
        ConfigurationSections.RefuseIfPartlySupplied(options, SectionName, "the assessments integration is then off.");
        if (options.IsConfigured && ConfigurationSections.TryNormaliseBaseUrl(options.EffectiveBaseUrl) is null)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(BaseUrl)} must be an absolute http(s) URL; found '{options.EffectiveBaseUrl}'.");
        }
        return options;
    }

    // Address of the service assessments service.
    public string? BaseUrl { get; set; }

    // Deprecated name for BaseUrl: the value was always the address, never a tenant.
    // Still read so an instance configured with the old name keeps working; start-up warns when it is relied on.
    // Read only through EffectiveBaseUrl; anything else touching it gets the compiler warning.
    // TODO: remove once every hosted app sets SAS__BaseUrl.
    [Obsolete("SAS:TenantId is read as the service assessments base URL. Set SAS:BaseUrl (SAS__BaseUrl) instead; this name will stop being read.")]
    public string? TenantId { get; set; }

    public string? SecretId { get; set; }

#pragma warning disable CS0618 // The two members below exist to read the obsolete name; nothing else may.
    public bool UsesDeprecatedBaseUrlKey =>
        string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(TenantId);

    public string? EffectiveBaseUrl =>
        !string.IsNullOrWhiteSpace(BaseUrl) ? BaseUrl
        : !string.IsNullOrWhiteSpace(TenantId) ? TenantId
        : null;
#pragma warning restore CS0618
}
