using System.Net;
using System.Net.Http.Headers;

namespace FipsFrontend.Configuration;

/// <summary>
/// What the content source's clients talk to when no content source is configured: every request
/// is answered at once with 200 and an empty collection in the shape the content API returns, so
/// every page renders its empty state and nothing leaves the process. A first run from a fresh
/// clone gets a working application with no content rather than a timeout on every page.
/// </summary>
public sealed class NoContentSourceHandler : HttpMessageHandler
{
    private const string EmptyCollection = """{"data":[],"meta":{"pagination":{"page":1,"pageSize":25,"pageCount":0,"total":0}}}""";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(EmptyCollection),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return Task.FromResult(response);
    }
}
