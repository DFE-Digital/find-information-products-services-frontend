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
        private readonly List<string> _requests = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_requests) _requests.Add($"{request.Method} {request.RequestUri}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[],"meta":{"pagination":{"page":1,"pageSize":25,"pageCount":0,"total":0}}}""", System.Text.Encoding.UTF8, "application/json"),
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

    // AddHttpClient<T> names each client after T; these are the registrations in Program.cs.
    private static readonly string[] OutboundClients =
        ["CmsApiService", "IOptimizedCmsApiService", "IAirtableService", "IServiceAssessmentsService"];

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
    public FipsApplication(string environment = "Development", IDictionary<string, string?>? settings = null)
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
