namespace FipsFrontend.Models;

// Bound from the "SAS" configuration section (SAS__BaseUrl, SAS__SecretId as environment variables).
public class SasOptions
{
    public const string SectionName = "SAS";

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
