namespace FiPSAutomation.Configuration;

/// <summary>
/// The application under test. MANDATORY: the suite refuses to start without an absolute
/// <see cref="ApplicationUrl"/>. Rules in <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class TargetOptions
{
    public const string Section = "Target";

    /// <summary>
    /// Base URL of the application; normalised to a trailing slash by <see cref="SuiteSettings"/>,
    /// because every navigation appends a relative path to it.
    /// </summary>
    public string ApplicationUrl { get; set; } = "";
}
