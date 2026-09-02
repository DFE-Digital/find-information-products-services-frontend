using System.Net;
using FipsFrontend.Tests.TestSupport;

namespace FipsFrontend.Tests;

/// <summary>
/// What a caller meets at the request limiter: admitted up to the limit, then refused in a way that says so
/// (429 with Retry-After, not a 503 that reads as the service being down), with the limit a setting rather than
/// a constant, so an instance driven by an automated suite can admit its pace.
/// </summary>
[TestFixture]
public class RateLimitingTests
{
    // A page that renders without any content source and counts against the limiter (static files do not).
    private const string Page = "/cookies";

    [Test]
    public async Task RateLimiting_WhenACallerExceedsTheWindow_TheRefusalIs429WithRetryAfter()
    {
        using var app = new FipsApplication(settings: new Dictionary<string, string?> { ["RateLimiting:PermitLimitPerWindow"] = "3", ["RateLimiting:WindowSeconds"] = "60" });

        for (var i = 1; i <= 3; i++)
            Assert.That((await app.Client.GetAsync(Page)).StatusCode, Is.EqualTo(HttpStatusCode.OK), $"request {i} is within the limit");
        var refused = await app.Client.GetAsync(Page);

        Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        Assert.That(refused.Headers.RetryAfter?.Delta, Is.Not.Null.And.LessThanOrEqualTo(TimeSpan.FromSeconds(60)), "the caller is told when the window opens again");
    }

    [Test]
    public async Task RateLimiting_WhenTheLimitIsRaised_ARunPastTheOldConstantIsAdmitted()
    {
        using var app = new FipsApplication(settings: new Dictionary<string, string?> { ["RateLimiting:PermitLimitPerWindow"] = "200" });

        for (var i = 1; i <= 120; i++)
            Assert.That((await app.Client.GetAsync(Page)).StatusCode, Is.EqualTo(HttpStatusCode.OK), $"request {i} of 120 under a limit of 200");
    }

    [Test]
    public async Task RateLimiting_WhenNothingIsConfigured_AHundredAreAdmittedAndTheNextIsRefused()
    {
        using var app = new FipsApplication();

        for (var i = 1; i <= 100; i++)
            Assert.That((await app.Client.GetAsync(Page)).StatusCode, Is.EqualTo(HttpStatusCode.OK), $"request {i} of the default 100");

        Assert.That((await app.Client.GetAsync(Page)).StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests), "the default limit is what the constant was");
    }

    [Test]
    public void RateLimiting_WhenTheLimitIsNotPositive_RefusesToStartNamingTheKey()
    {
        var refusal = FipsApplication.StartupRefusal(new Dictionary<string, string?> { ["RateLimiting:PermitLimitPerWindow"] = "0" });

        Assert.That(refusal, Does.Contain("RateLimiting:PermitLimitPerWindow"));
    }

    [Test]
    public void RateLimiting_WhenTheWindowIsNotANumber_RefusesToStartNamingTheKey()
    {
        var refusal = FipsApplication.StartupRefusal(new Dictionary<string, string?> { ["RateLimiting:WindowSeconds"] = "soon" });

        Assert.That(refusal, Does.Contain("RateLimiting:WindowSeconds"));
    }
}
