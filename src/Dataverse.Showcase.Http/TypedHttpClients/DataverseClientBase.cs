using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http.TypedHttpClients;

/// <summary>
///     Base class for typed Dataverse / Dynamics 365 HTTP clients. Subclass this
///     and register your subclass via <c>AddDataverseHttpClient&lt;T&gt;</c>.
///     Provides a minimal GET OData helper; the full library adds custom-action
///     and batch helpers on top.
/// </summary>
public abstract class DataverseClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected HttpClient Client { get; }

    protected ILogger Logger { get; }

    protected DataverseClientBase(HttpClient client, ILogger logger)
    {
        Client = client;
        Logger = logger;
    }

    /// <summary>
    ///     GETs an OData collection at the given relative URL and returns the
    ///     deserialized <c>value</c> array. The URL must be relative to the
    ///     <see cref="HttpClient.BaseAddress" />, e.g.
    ///     <c>api/data/v9.2/accounts?$select=name&amp;$top=5</c>.
    /// </summary>
    protected async Task<IReadOnlyList<T>> GetCollectionAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await Client.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ODataCollectionResponse<T>>(JsonOptions, cancellationToken);
        return payload?.Value ?? [];
    }

    private sealed record ODataCollectionResponse<T>(IReadOnlyList<T> Value);
}
