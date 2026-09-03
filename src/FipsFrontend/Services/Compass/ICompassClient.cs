using System.Net;
using Compass.FipsApi.Contracts.Generated;

namespace FipsFrontend.Services.Compass;

/// <summary>The COMPASS service-register API as this application reads it. Every failure is a <see cref="CompassUnavailableException"/> naming the endpoint.</summary>
public interface ICompassClient
{
    /// <summary>Every FIPS vocabulary in one response.</summary>
    Task<ServiceRegisterGetFipsConfigurationBundleResponse> GetFipsConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>One page of products, filtered as COMPASS filters them.</summary>
    Task<ServiceRegisterGetProductsResponse> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken = default);

    /// <summary>One product by its COMPASS id; null when COMPASS does not know it.</summary>
    Task<ServiceRegisterGetProductsResponseDataItem?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// The filters <c>products</c> accepts; ids are COMPASS's, taken from the configuration bundle.
/// Status names are COMPASS's (New, Active, Inactive, Rejected); none means every status.
/// </summary>
public sealed record ProductQuery(
    int Page = 1,
    int PageSize = 100,
    string? Keywords = null,
    IReadOnlyList<string>? Status = null,
    IReadOnlyList<int>? CategoryIds = null,
    IReadOnlyList<int>? ChannelIds = null,
    IReadOnlyList<int>? TypeIds = null,
    IReadOnlyList<int>? BusinessAreaIds = null,
    IReadOnlyList<int>? UserGroupIds = null);

/// <summary>COMPASS did not answer, or answered with something other than the payload asked for.</summary>
public sealed class CompassUnavailableException(string endpoint, HttpStatusCode? statusCode, string reason, Exception? inner = null)
    : Exception($"COMPASS {endpoint}: {reason}", inner)
{
    public string Endpoint { get; } = endpoint;
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
