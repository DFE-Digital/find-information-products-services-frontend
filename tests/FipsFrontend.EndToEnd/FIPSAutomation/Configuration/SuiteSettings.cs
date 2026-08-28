using Microsoft.Extensions.Configuration;

namespace FiPSAutomation.Configuration;

/// <summary>
/// The suite's configuration, bound and validated once before any browser starts, the way the
/// application binds its own. Sources, later ones winning: <c>testsettings.json</c> (the tracked
/// template, every value empty or a default), <c>testsettings.local.json</c> (gitignored, one
/// machine's values), then environment variables (<c>Target__ApplicationUrl</c> and so on, the
/// form a pipeline uses).
/// </summary>
public sealed class SuiteSettings
{
    public const string TemplateFileName = "testsettings.json";
    public const string LocalFileName = "testsettings.local.json";

    /// <summary>Absolute, trailing slash: relative paths appended to it resolve inside the site.</summary>
    public string ApplicationUrl { get; }

    /// <summary>
    /// True when the target is this machine (a loopback address). Derived from the URL rather than
    /// declared, so it cannot be left behind when the URL is re-pointed at a hosted instance.
    /// </summary>
    public bool TargetIsLocal { get; }

    /// <summary>Null when no sign-in is configured: the suite goes straight to the application.</summary>
    public SignInOptions? SignIn { get; }

    public TimeoutOptions Timeouts { get; }

    private SuiteSettings(Uri applicationUrl, SignInOptions? signIn, TimeoutOptions timeouts)
    {
        ApplicationUrl = applicationUrl.AbsoluteUri;
        TargetIsLocal = applicationUrl.IsLoopback;
        SignIn = signIn;
        Timeouts = timeouts;
    }

    /// <summary>Loads from the files beside the test assembly and the process environment.</summary>
    public static SuiteSettings Load(string? directory = null)
    {
        directory ??= AppContext.BaseDirectory;
        var template = Path.Combine(directory, TemplateFileName);
        var local = Path.Combine(directory, LocalFileName);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(template, optional: true)
            .AddJsonFile(local, optional: true)
            .AddEnvironmentVariables()
            .Build();
        return From(configuration, $"{template}; {local} (optional); environment variables");
    }

    /// <summary>
    /// Binds and validates. Refuses with a message naming the key, the value found, and where it
    /// looked, because a wrong or missing value otherwise surfaces as the browser failing to
    /// navigate from inside its own protocol layer, with every test reported as failed.
    /// </summary>
    public static SuiteSettings From(IConfiguration configuration, string sources)
    {
        var target = configuration.GetSection(TargetOptions.Section);
        var applicationUrl = target[nameof(TargetOptions.ApplicationUrl)];
        var baseUrl = ConfigurationSections.TryNormaliseBaseUrl(applicationUrl)
            ?? throw new InvalidOperationException(
                $"{TargetOptions.Section}:{nameof(TargetOptions.ApplicationUrl)} must be an absolute http(s) URL; found '{applicationUrl}'. " +
                $"Sources read: {sources}. For a copy of the application on this machine set it in {LocalFileName} or as the " +
                $"environment variable {TargetOptions.Section}__{nameof(TargetOptions.ApplicationUrl)}.");

        var signInSection = configuration.GetSection(SignInOptions.Section);
        var signIn = new SignInOptions
        {
            OAuthUrl = signInSection[nameof(SignInOptions.OAuthUrl)] ?? "",
            LoginUrl = signInSection[nameof(SignInOptions.LoginUrl)] ?? "",
            UserName = signInSection[nameof(SignInOptions.UserName)] ?? "",
            Password = signInSection[nameof(SignInOptions.Password)] ?? "",
        };
        var partlySupplied = ConfigurationSections.PartlySupplied(signIn, SignInOptions.Section);
        if (partlySupplied is not null)
        {
            throw new InvalidOperationException($"{partlySupplied} Sources read: {sources}.");
        }

        var timeoutsSection = configuration.GetSection(TimeoutOptions.Section);
        var defaults = new TimeoutOptions();
        var timeouts = new TimeoutOptions
        {
            ExpectMs = ReadInt(timeoutsSection, nameof(TimeoutOptions.ExpectMs), defaults.ExpectMs, sources),
            ActionMs = ReadInt(timeoutsSection, nameof(TimeoutOptions.ActionMs), defaults.ActionMs, sources),
            NavigationMs = ReadInt(timeoutsSection, nameof(TimeoutOptions.NavigationMs), defaults.NavigationMs, sources),
        };
        var nonPositive = timeouts.NonPositive().ToList();
        if (nonPositive.Count > 0)
        {
            throw new InvalidOperationException(
                $"Every value in configuration section '{TimeoutOptions.Section}' must be a positive number; not positive: " +
                $"{string.Join(", ", nonPositive.Select(k => $"{TimeoutOptions.Section}:{k}"))}. Sources read: {sources}.");
        }

        return new SuiteSettings(baseUrl, signIn.IsConfigured ? signIn : null, timeouts);
    }

    private static int ReadInt(IConfigurationSection section, string key, int fallback, string sources)
    {
        var value = section[key];
        if (ConfigurationSections.IsAbsent(value)) return fallback;
        return int.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"{section.Path}:{key} must be a whole number; found '{value}'. Sources read: {sources}.");
    }
}
