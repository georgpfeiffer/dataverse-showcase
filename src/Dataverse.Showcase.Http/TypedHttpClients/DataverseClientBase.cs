using System.Net.Http.Json;
using System.Text.Json;
using Dataverse.Showcase.Http.Exceptions;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http.TypedHttpClients;

/// <summary>
///     Base class for typed Dataverse / Dynamics 365 HTTP clients. Subclass this
///     and register your subclass via <c>AddDataverseHttpClient&lt;T&gt;</c>.
///     Exposes a single <see cref="SendAsync" /> entry point and an OData GET
///     helper; subclasses should never touch the underlying <see cref="HttpClient" />
///     directly so that every request flows through the same handler pipeline.
/// </summary>
public abstract class DataverseClientBase
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    /// <summary>
    ///     Logger available to subclasses for request-level diagnostics.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    ///     Creates a new <see cref="DataverseClientBase" /> with the supplied typed
    ///     <see cref="HttpClient" /> and logger.
    /// </summary>
    protected DataverseClientBase(HttpClient client, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _client = client;
        Logger = logger;
    }

    /// <summary>
    ///     Sends an HTTP request through the configured handler pipeline. This is
    ///     the single entry point subclasses use for custom requests instead of
    ///     touching the underlying <see cref="HttpClient" /> directly.
    /// </summary>
    protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
    protected async Task<IReadOnlyList<T>> GetODataAsync<T>(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        using var response = await SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Dataverse OData query {Query} failed with {StatusCode}", query, (int)response.StatusCode);
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
