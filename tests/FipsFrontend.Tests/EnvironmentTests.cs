using System.Net;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// What the environment name changes. The default is the strict one: an unnamed environment is
/// Production, and a developer's conveniences are opt-in under "local-dev" (a developer's machine)
/// or "Development" (the hosted development platform) - never the other way round by accident.
/// </summary>
[TestFixture]
public class EnvironmentTests
{
    [TestCase("local-dev", "max-age=300; includeSubDomains")]
    [TestCase("Development", "max-age=300; includeSubDomains")]
    [TestCase("ci", "max-age=31536000; includeSubDomains; preload")]
    [TestCase("Production", "max-age=31536000; includeSubDomains; preload")]
    public async Task Response_CarriesTheStrictTransportSecurityOfItsEnvironment(string environment, string expected)
    {
        using var app = new FipsApplication(environment);

        var response = await app.Client.GetAsync("/about");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.GetValues("Strict-Transport-Security").Single(), Is.EqualTo(expected));
    }

    [TestCase("local-dev")]
    [TestCase("Development")]
    [TestCase("ci")]
    public async Task Page_OutsideProduction_SaysWhichEnvironmentThisIs(string environment)
    {
        using var app = new FipsApplication(environment);

        var html = await app.Client.GetStringAsync("/about");

        Assert.That(html, Does.Contain("govuk-phase-banner").And.Contain($">{environment}</strong>"));
    }

    [Test]
    public async Task Page_InProduction_CarriesNoEnvironmentBanner()
    {
        using var app = new FipsApplication("Production");

        var html = await app.Client.GetStringAsync("/about");

        Assert.That(html, Does.Not.Contain("govuk-phase-banner"));
    }

    // Not covered: that the developer exception page and the error page's exception details show
    // under local-dev and Development only. Nothing in the application lets a scenario provoke an
    // unhandled exception - every fault from the content source is swallowed - so the same branch is
    // held by the header cases above, which share the gate.
}
