using System.Net.Http.Json;
using System.Text.Json;
using Dataverse.Showcase.Http.Exceptions;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http;

/// <summary>
///     Sealed Dataverse Web API facade. Inject this into your domain classes and
///     call <see cref="SendAsync" /> or <see cref="GetODataAsync{T}" /> instead of
///     touching the underlying <see cref="HttpClient" /> directly, so every request
///     flows through the configured handler pipeline.
/// </summary>
public sealed class DataverseApiClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;
    private readonly ILogger<DataverseApiClient> _logger;

    /// <summary>
    ///     Creates a new <see cref="DataverseApiClient" /> with the typed
    ///     <see cref="HttpClient" /> supplied by DI.
    /// </summary>
    public DataverseApiClient(HttpClient client, ILogger<DataverseApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        _logger = logger;
    }

    /// <summary>
    ///     Sends an HTTP request through the configured handler pipeline.
    /// </summary>
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    ///     Runs an OData GET query against the Dataverse Web API and returns the
    ///     deserialized <c>value</c> array. <paramref name="query" /> is the relative
    ///     URL, e.g. <c>api/data/v9.2/accounts?$select=name&amp;$top=5</c>.
    /// </summary>
    /// <exception cref="DataverseException">
    ///     Thrown when Dataverse returns a non-success status or the response body
    ///     cannot be deserialized into the expected shape.
    /// </exception>
    public async Task<IReadOnlyList<T>> GetODataAsync<T>(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        using var response = await SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Dataverse OData query {Query} failed with {StatusCode}", query, (int)response.StatusCode);
            throw await DataverseException.FromResponseAsync(response, cancellationToken);
        }

        var collectionResponse = await ReadCollectionResponse<T>(response, cancellationToken);
        if (collectionResponse is null)
        {
            throw new DataverseException(response.StatusCode, "Dataverse returned an empty response body.");
        }

        return collectionResponse.Value ?? [];
    }

    private static async Task<ODataCollectionResponse<T>?> ReadCollectionResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ODataCollectionResponse<T>>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new DataverseException(response.StatusCode, "Failed to deserialize Dataverse OData response.", ex);
        }
    }

    private sealed record ODataCollectionResponse<T>(IReadOnlyList<T>? Value);
}
