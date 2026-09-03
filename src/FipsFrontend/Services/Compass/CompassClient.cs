using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Compass.FipsApi.Contracts;
using Compass.FipsApi.Contracts.Generated;

namespace FipsFrontend.Services.Compass;

public sealed class CompassClient(HttpClient http, IContractObservations observations) : ICompassClient
{
    // The version is part of the contract the records were generated against, so it lives here, not in configuration.
    private const string Api = "api/v1/ServiceRegister/";

    public Task<ServiceRegisterGetFipsConfigurationBundleResponse> GetFipsConfigurationAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ServiceRegisterGetFipsConfigurationBundleResponse>("fips/configuration", Api + "fips/configuration", cancellationToken);

    public Task<ServiceRegisterGetProductsResponse> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken = default) =>
        GetAsync<ServiceRegisterGetProductsResponse>("products", Api + "products" + QueryString(query), cancellationToken);

    public async Task<ServiceRegisterGetProductsResponseDataItem?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var envelope = await GetOrNullAsync<ServiceRegisterGetProductResponse>("products/{id}", $"{Api}products/{id}", cancellationToken, nullOnNotFound: true);
        return envelope?.Data;
    }

    // A collection endpoint has no "not found": the answer is a payload or a CompassUnavailableException, never null.
    private async Task<T> GetAsync<T>(string endpoint, string relativeUrl, CancellationToken cancellationToken)
        where T : class, IContractRecord =>
        await GetOrNullAsync<T>(endpoint, relativeUrl, cancellationToken, nullOnNotFound: false)
        ?? throw new CompassUnavailableException(endpoint, null, "answered nothing");

    private static string QueryString(ProductQuery query)
    {
        var parts = new List<string> { $"page={query.Page}", $"pageSize={query.PageSize}" };
        foreach (var status in query.Status ?? []) parts.Add("status=" + Uri.EscapeDataString(status));
        if (!string.IsNullOrWhiteSpace(query.Keywords)) parts.Add("q=" + Uri.EscapeDataString(query.Keywords));
        Add("categoryIds", query.CategoryIds);
        Add("channelIds", query.ChannelIds);
        Add("typeIds", query.TypeIds);
        Add("businessAreaIds", query.BusinessAreaIds);
        Add("userGroupIds", query.UserGroupIds);
        return "?" + string.Join("&", parts);

        void Add(string name, IReadOnlyList<int>? ids)
        {
            foreach (var id in ids ?? []) parts.Add($"{name}={id}");
        }
    }

    private async Task<T?> GetOrNullAsync<T>(string endpoint, string relativeUrl, CancellationToken cancellationToken, bool nullOnNotFound)
        where T : class, IContractRecord
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(relativeUrl, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            throw new CompassUnavailableException(endpoint, null, "no response", e);
        }
        catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation; the caller's cancellation is not this client's to rename.
            throw new CompassUnavailableException(endpoint, null, $"no response within {http.Timeout.TotalSeconds:0}s", e);
        }

        using (response)
        {
            if (nullOnNotFound && response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
                throw new CompassUnavailableException(endpoint, response.StatusCode, $"answered {(int)response.StatusCode}");

            T? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<T>(CompassJson.Options, cancellationToken);
            }
            catch (JsonException e)
            {
                throw new CompassUnavailableException(endpoint, response.StatusCode, "answered with something that is not the expected JSON", e);
            }
            if (payload is null) throw new CompassUnavailableException(endpoint, response.StatusCode, "answered with an empty body");

            observations.Observe(endpoint, payload);
            return payload;
        }
    }
}
