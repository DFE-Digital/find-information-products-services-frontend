namespace FiPSAutomation.Configuration;

/// <summary>
/// Credentials for an application fronted by an identity provider. OPTIONAL, all-or-nothing:
/// leave every value empty and the suite navigates straight to the application (a copy on this
/// machine, or one whose platform sign-in is off); supply any and all four are required. Values
/// are plain: a gitignored file or an environment variable needs no encoding, and the base64
/// layer the suite used to carry was obfuscation, not protection. Rules in
/// <see cref="ConfigurationSections"/>.
/// </summary>
public sealed class SignInOptions : IOptionalSection
{
    public const string Section = "SignIn";

    /// <summary>Where the identity provider's sign-in form is first opened.</summary>
    public string OAuthUrl { get; set; } = "";

    /// <summary>The URL the provider lands on after the password step, before the application.</summary>
    public string LoginUrl { get; set; } = "";

    public string UserName { get; set; } = "";

    public string Password { get; set; } = "";

    public bool IsConfigured =>
        !ConfigurationSections.IsAbsent(OAuthUrl) ||
        !ConfigurationSections.IsAbsent(LoginUrl) ||
        !ConfigurationSections.IsAbsent(UserName) ||
        !ConfigurationSections.IsAbsent(Password);

    public IEnumerable<string> MissingRequired()
    {
        if (ConfigurationSections.IsAbsent(OAuthUrl)) yield return nameof(OAuthUrl);
        if (ConfigurationSections.IsAbsent(LoginUrl)) yield return nameof(LoginUrl);
        if (ConfigurationSections.IsAbsent(UserName)) yield return nameof(UserName);
        if (ConfigurationSections.IsAbsent(Password)) yield return nameof(Password);
    }
}
