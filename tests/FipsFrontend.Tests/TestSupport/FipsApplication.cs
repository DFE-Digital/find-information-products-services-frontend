using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FipsFrontend.Tests.TestSupport;

/// <summary>
/// The application hosted in-process, with every outbound HTTP client replaced by a stand-in that
/// answers as an empty system would and records what was asked of it, and a client that keeps
/// cookies between requests.
/// The stand-in answers 200 with an empty collection rather than an error:
/// the application's retry policy retries any unsuccessful status with exponential back-off,
/// which turned one page request into a minute and a half of waiting.
/// </summary>
public sealed class FipsApplication : IDisposable
{
    /// <summary>Records every request an outbound client tried to make, as "METHOD url".</summary>
    public sealed class RecordingStandIn : HttpMessageHandler
    {
        private const string EmptyCollection = """{"data":[],"meta":{"pagination":{"page":1,"pageSize":25,"pageCount":0,"total":0}}}""";
        private readonly List<string> _requests = [];

        /// <summary>
        /// What to answer a request with, when a scenario needs something other than the empty
        /// collection: return a body, or null to fall back to it. Always 200; the scenarios that
        /// matter here are about what the application makes of an answer, not of a refusal.
        /// </summary>
        public Func<HttpRequestMessage, string?>? Answer { get; set; }

        /// <summary>
        /// A status and body for a request, for scenarios about a refusal (a 503, a 404): return null to fall
        /// back to <see cref="Answer"/>. Only clients without a retry policy should be refused here.
        /// </summary>
        public Func<HttpRequestMessage, (HttpStatusCode Status, string Body)?>? Respond { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_requests) _requests.Add($"{request.Method} {request.RequestUri}");
            var (status, body) = Respond?.Invoke(request) ?? (HttpStatusCode.OK, Answer?.Invoke(request) ?? EmptyCollection);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
        }

        public string[] Snapshot()
        {
            lock (_requests) return _requests.ToArray();
        }

        public void Clear()
        {
            lock (_requests) _requests.Clear();
        }
    }

    /// <summary>
    /// What the application says when it refuses to start under these settings: every exception message in the
    /// chain, joined; empty when it starts. For the scenarios where a bad setting must be named at start-up.
    /// </summary>
    public static string StartupRefusal(IDictionary<string, string?> settings)
    {
        try
        {
            using var app = new FipsApplication(settings: settings);
            return "";
        }
        catch (Exception ex)
        {
            var messages = new List<string>();
            for (var e = ex; e is not null; e = e.InnerException) messages.Add(e.Message);
            return string.Join(" | ", messages);
        }
    }

    // AddHttpClient<T> names each client after T; these are the registrations in Program.cs.
    private static readonly string[] OutboundClients =
        ["CmsApiService", "IOptimizedCmsApiService", "IAirtableService", "IServiceAssessmentsService", "ICompassClient"];

    // The least configuration under which every controller can be constructed.
    // Values are inert: nothing leaves the process, because every outbound client is the stand-in.
    private static readonly Dictionary<string, string?> Baseline = new()
    {
        ["Caching:Performance:EnableWarming"] = "false",
        ["Caching:Redis:Enabled"] = "false",
        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
        ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
        ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
        ["AzureAd:ClientSecret"] = "test-only",
        ["CmsApi:BaseUrl"] = "http://cms.example.com/api",
        ["CmsApi:ReadApiKey"] = "test-only",
        ["CmsApi:WriteApiKey"] = "test-only",
        ["SAS:BaseUrl"] = "http://assessments.example.com/",
        ["SAS:SecretId"] = "test-only",
    };

    public RecordingStandIn Outbound { get; } = new();
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }

    /// <param name="environment">The environment name the application believes it runs under.</param>
    /// <param name="settings">Configuration for the scenario, layered over the baseline.</param>
    /// <param name="replaceOutboundClients">
    /// True (the default) swaps every outbound client's handler for the recording stand-in. False leaves the
    /// application's own handlers in place, for scenarios about what the application does when nothing is
    /// configured - its in-process no-content handler is then the thing under test, not hidden by the stand-in.
    /// </param>
    public FipsApplication(string environment = "Development", IDictionary<string, string?>? settings = null, bool replaceOutboundClients = true)
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            foreach (var (key, value) in Baseline)
            {
                host.UseSetting(key, value);
            }
            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                host.UseSetting(key, value);
            }
            host.ConfigureTestServices(services =>
            {
                if (!replaceOutboundClients) return;
                // Registered after the application's own, so this primary handler is the one each named client gets.
                foreach (var name in OutboundClients)
                {
                    services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => Outbound);
                }
            });
        });
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
