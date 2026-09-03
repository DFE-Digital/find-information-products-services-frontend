using System.Net;
using System.Net.Http.Headers;
using Compass.FipsApi.Contracts;

namespace FipsFrontend.Configuration;

/// <summary>
/// What the COMPASS client talks to when no COMPASS is configured: every request is answered at once, in COMPASS's
/// own shapes, as an instance holding nothing (<see cref="EmptyAnswers"/>), so anything that reaches the client
/// reads empty vocabularies and no products rather than a timeout, and the records log no drift. The pages check
/// the configuration before calling the client and say COMPASS is off; this is the answer for whatever does not.
/// </summary>
public sealed class NoCompassHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (status, body) = request.RequestUri is { } address ? EmptyAnswers.For(address) : EmptyAnswers.For("");
        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            RequestMessage = request,
            Content = new StringContent(body),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return Task.FromResult(response);
    }
}
