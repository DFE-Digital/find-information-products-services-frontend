using FiPSAutomation.Configuration;
using Microsoft.Extensions.Configuration;

// Outside the FiPSAutomation namespace on purpose: the suite's [SetUpFixture] lives there and would
// start a browser and open the application for these, which need neither.
namespace FipsFrontend.EndToEnd.ConfigurationTests;

/// <summary>
/// What a person configuring the suite meets: the rules in ConfigurationSections, applied. These
/// need no browser and no running application, so the pipeline runs them as a gate of their own
/// (the category below) and leaves them out of the browser run and its known-green check.
/// </summary>
[TestFixture, Category("Configuration")]
public class SuiteSettingsTests
{
    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    private static SuiteSettings Load(params (string Key, string? Value)[] values) =>
        SuiteSettings.From(Configuration(values), "in-memory");

    [Test]
    public void Suite_WhenApplicationUrlLacksTrailingSlash_NavigationsResolveInsideTheSite()
    {
        var settings = Load(("Target:ApplicationUrl", "https://fips.example.com"));

        Assert.That(settings.ApplicationUrl, Is.EqualTo("https://fips.example.com/"));
    }

    [Test]
    public void Suite_WhenApplicationUrlAbsent_RefusesNamingTheKeyAndWhereItLooked()
    {
        var refusal = Assert.Throws<InvalidOperationException>(() => Load(("Target:ApplicationUrl", "")));

        Assert.That(refusal!.Message, Does.Contain("Target:ApplicationUrl").And.Contain("in-memory").And.Contain("Target__ApplicationUrl"));
    }

    [TestCase("<TEST_APPLICATION_URL>")]
    [TestCase("/products")]
    [TestCase("ftp://fips.example.com/")]
    public void Suite_WhenApplicationUrlIsAPlaceholderOrNotHttp_Refuses(string value)
    {
        var refusal = Assert.Throws<InvalidOperationException>(() => Load(("Target:ApplicationUrl", value)));

        Assert.That(refusal!.Message, Does.Contain(value));
    }

    [TestCase("http://localhost:5505/", true)]
    [TestCase("http://127.0.0.1:5505/", true)]
    [TestCase("https://fips.example.com/", false)]
    public void Suite_KnowsWhetherTheTargetIsOnThisMachine(string url, bool local)
    {
        Assert.That(Load(("Target:ApplicationUrl", url)).TargetIsLocal, Is.EqualTo(local));
    }

    [Test]
    public void Suite_WhenSignInLeftEmpty_RunsWithoutSigningIn()
    {
        var settings = Load(("Target:ApplicationUrl", "http://localhost:5505/"), ("SignIn:UserName", "  "));

        Assert.That(settings.SignIn, Is.Null);
    }

    [Test]
    public void Suite_WhenSignInPartlySupplied_RefusesNamingEveryMissingKey()
    {
        var refusal = Assert.Throws<InvalidOperationException>(() =>
            Load(("Target:ApplicationUrl", "http://localhost:5505/"), ("SignIn:UserName", "someone@example.com")));

        Assert.That(refusal!.Message, Does.Contain("SignIn:OAuthUrl").And.Contain("SignIn:LoginUrl").And.Contain("SignIn:Password").And.Not.Contain("SignIn:UserName"));
    }

    [Test]
    public void Suite_WhenSignInFullySupplied_SignsInWithTheValuesAsGiven()
    {
        var settings = Load(
            ("Target:ApplicationUrl", "https://fips.example.com/"),
            ("SignIn:OAuthUrl", "https://login.example.com/start"),
            ("SignIn:LoginUrl", "https://login.example.com/done"),
            ("SignIn:UserName", "someone@example.com"),
            ("SignIn:Password", "plain, not encoded"));

        Assert.That(settings.SignIn, Is.Not.Null);
        Assert.That(settings.SignIn!.Password, Is.EqualTo("plain, not encoded"));
    }

    [Test]
    public void Suite_WhenTimeoutsOmitted_PlaywrightDefaultsApply()
    {
        var settings = Load(("Target:ApplicationUrl", "http://localhost:5505/"));

        Assert.That((settings.Timeouts.ExpectMs, settings.Timeouts.ActionMs, settings.Timeouts.NavigationMs), Is.EqualTo((5_000, 30_000, 30_000)));
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("soon")]
    public void Suite_WhenATimeoutIsNotAPositiveNumber_RefusesNamingIt(string value)
    {
        var refusal = Assert.Throws<InvalidOperationException>(() =>
            Load(("Target:ApplicationUrl", "http://localhost:5505/"), ("Timeouts:ExpectMs", value)));

        Assert.That(refusal!.Message, Does.Contain("Timeouts:ExpectMs"));
    }

    [Test]
    public void Suite_WhenLocalFileSitsBesideTheTemplate_LocalValuesWinAndTheTemplateStillLoads()
    {
        var directory = Directory.CreateTempSubdirectory("testsettings-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, SuiteSettings.TemplateFileName), """{ "Target": { "ApplicationUrl": "" }, "Timeouts": { "ExpectMs": 4000 } }""");
            File.WriteAllText(Path.Combine(directory, SuiteSettings.LocalFileName), """{ "Target": { "ApplicationUrl": "http://localhost:5505" } }""");

            var settings = SuiteSettings.Load(directory, includeEnvironmentVariables: false);

            Assert.That(settings.ApplicationUrl, Is.EqualTo("http://localhost:5505/"));
            Assert.That(settings.Timeouts.ExpectMs, Is.EqualTo(4000), "the template's value survives when the local file does not set it");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void Suite_WhenNoFileHasAUrl_RefusesNamingTheLocalFileToCreate()
    {
        var directory = Directory.CreateTempSubdirectory("testsettings-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(directory, SuiteSettings.TemplateFileName), """{ "Target": { "ApplicationUrl": "" } }""");

            var refusal = Assert.Throws<InvalidOperationException>(() => SuiteSettings.Load(directory, includeEnvironmentVariables: false));

            Assert.That(refusal!.Message, Does.Contain(SuiteSettings.LocalFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// How a pipeline configures the suite without writing a file - and why the two tests above
    /// switch the environment off: in the pipeline these variables are set for the whole job.
    /// </summary>
    [Test]
    public void Suite_WhenEnvironmentVariableSupplied_ItOverridesBothFiles()
    {
        var directory = Directory.CreateTempSubdirectory("testsettings-").FullName;
        var previous = Environment.GetEnvironmentVariable("Timeouts__ExpectMs");
        try
        {
            File.WriteAllText(Path.Combine(directory, SuiteSettings.TemplateFileName), """{ "Target": { "ApplicationUrl": "http://localhost:5505/" }, "Timeouts": { "ExpectMs": 4000 } }""");
            File.WriteAllText(Path.Combine(directory, SuiteSettings.LocalFileName), """{ "Timeouts": { "ExpectMs": 3000 } }""");
            Environment.SetEnvironmentVariable("Timeouts__ExpectMs", "1500");

            var settings = SuiteSettings.Load(directory);

            Assert.That(settings.Timeouts.ExpectMs, Is.EqualTo(1500));
        }
        finally
        {
            Environment.SetEnvironmentVariable("Timeouts__ExpectMs", previous);
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// The tracked template and the options classes describe the same keys: a property absent from
    /// the template is one nobody is told about, and a template key no class reads is a lie.
    /// </summary>
    [Test]
    public void Template_EveryOptionsPropertyHasAKey_AndEveryKeyHasAProperty()
    {
        var template = new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, SuiteSettings.TemplateFileName)).Build();
        var expected = new Dictionary<string, Type>
        {
            [TargetOptions.Section] = typeof(TargetOptions),
            [SignInOptions.Section] = typeof(SignInOptions),
            [TimeoutOptions.Section] = typeof(TimeoutOptions),
        };

        foreach (var (section, type) in expected)
        {
            var keysInTemplate = template.GetSection(section).GetChildren().Select(c => c.Key).Where(k => !k.StartsWith('_')).OrderBy(k => k).ToList();
            var properties = type.GetProperties().Where(p => p.CanWrite).Select(p => p.Name).OrderBy(p => p).ToList();
            Assert.That(keysInTemplate, Is.EqualTo(properties), $"section '{section}' in {SuiteSettings.TemplateFileName} versus {type.Name}");
        }
        Assert.That(template.GetChildren().Select(c => c.Key).Where(k => !k.StartsWith('_')), Is.EquivalentTo(expected.Keys), "sections in the template");
    }
}
