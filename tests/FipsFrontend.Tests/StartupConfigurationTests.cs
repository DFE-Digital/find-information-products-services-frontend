using System.Net;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// What a person running the application meets when configuration is missing or wrong. A section
/// left empty switches its feature off; a section partly supplied, or a value of the wrong shape,
/// is refused when the application starts, naming the key - never as a 500 on the first request.
/// </summary>
[TestFixture]
public class StartupConfigurationTests
{
    private static readonly Dictionary<string, string?> NoIdentitySettings = new()
    {
        ["AzureAd:Instance"] = null,
        ["AzureAd:TenantId"] = null,
        ["AzureAd:ClientId"] = null,
        ["AzureAd:ClientSecret"] = null,
    };

    private static readonly Dictionary<string, string?> NoContentSource = new()
    {
        ["CmsApi:BaseUrl"] = null,
        ["CmsApi:ReadApiKey"] = null,
        ["CmsApi:WriteApiKey"] = null,
    };

    /// <summary>A fresh clone: nothing but the committed settings file. The harness's baseline overrides are all lifted.</summary>
    private static readonly Dictionary<string, string?> NothingConfigured = new()
    {
        ["AzureAd:Instance"] = null,
        ["AzureAd:TenantId"] = null,
        ["AzureAd:ClientId"] = null,
        ["AzureAd:ClientSecret"] = null,
        ["CmsApi:BaseUrl"] = null,
        ["CmsApi:ReadApiKey"] = null,
        ["CmsApi:WriteApiKey"] = null,
        ["SAS:BaseUrl"] = null,
        ["SAS:SecretId"] = null,
        // The baseline's boolean overrides stand: Redis off matches the committed file; cache warming is off here
        // where the file has it on, which against no content only changes when the empty answers are first fetched.
    };

    [TestCase("/")]
    [TestCase("/products")]
    [TestCase("/categories")]
    [TestCase("/about")]
    [TestCase("/health")]
    public async Task Application_WhenRunFromAFreshCloneWithNothingConfigured_ServesEveryPage(string path)
    {
        using var app = new FipsApplication(settings: NothingConfigured, replaceOutboundClients: false);

        var response = await app.Client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{path}: {Html.Excerpt(html)}");
    }

    [Test]
    public void Application_WhenTheDistributedCacheIsEnabledWithoutAnAddress_RefusesToStartNamingTheKey()
    {
        var refusal = StartupRefusal(new Dictionary<string, string?> { ["Caching:Redis:Enabled"] = "true", ["Caching:Redis:ConnectionString"] = "" });

        Assert.That(refusal, Does.Contain("Caching:Redis:ConnectionString"));
    }

    [Test]
    public void Application_WhenAssuranceIsOnWithoutTheAssessmentsService_RefusesToStartNamingTheKeys()
    {
        var refusal = StartupRefusal(new Dictionary<string, string?> { ["EnabledFeatures:Assurance"] = "true", ["SAS:BaseUrl"] = null, ["SAS:SecretId"] = null });

        Assert.That(refusal, Does.Contain("SAS:BaseUrl").And.Contain("EnabledFeatures:Assurance"));
    }

    [Test]
    public void Application_WhenTheAssessmentsServiceIsPartlySupplied_RefusesToStartNamingTheMissingKey()
    {
        var refusal = StartupRefusal(new Dictionary<string, string?> { ["SAS:SecretId"] = null });

        Assert.That(refusal, Does.Contain("SAS:SecretId").And.Not.Contain("SAS:BaseUrl"));
    }

    [Test]
    public async Task Application_WhenNoIdentitySettingsAreSupplied_ServesItsPagesWithoutSignIn()
    {
        using var app = new FipsApplication(settings: NoIdentitySettings);

        var response = await app.Client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), Html.Excerpt(html));
        Assert.That(Html.H1Headings(html), Is.Not.Empty);
    }

    [Test]
    public void Application_WhenIdentitySettingsArePartlySupplied_RefusesToStartNamingTheMissingKeys()
    {
        var partly = new Dictionary<string, string?>(NoIdentitySettings) { ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000" };

        var refusal = StartupRefusal(partly);

        Assert.That(refusal, Does.Contain("AzureAd:TenantId").And.Contain("AzureAd:ClientSecret").And.Not.Contain("AzureAd:ClientId"));
    }

    [Test]
    public async Task Application_WhenNoContentSourceIsConfigured_RunsWithNoContent()
    {
        using var app = new FipsApplication(settings: NoContentSource);

        var response = await app.Client.GetAsync("/products");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), Html.Excerpt(html));
        Assert.That(html, Does.Contain("There are no products and services to show."));
    }

    [TestCase("cms/api")]
    [TestCase("<CMS_URL>")]
    public void Application_WhenTheContentSourceAddressIsNotAnAbsoluteUrl_RefusesToStartNamingTheKey(string value)
    {
        var refusal = StartupRefusal(new Dictionary<string, string?> { ["CmsApi:BaseUrl"] = value });

        Assert.That(refusal, Does.Contain("CmsApi:BaseUrl").And.Contain(value));
    }

    /// <summary>Every message on the chain of the exception that stops the application starting; empty if it started.</summary>
    private static string StartupRefusal(IDictionary<string, string?> settings) => FipsApplication.StartupRefusal(settings);
}
