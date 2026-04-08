using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dataverse.Showcase.Http.TypedHttpClients;

namespace Dataverse.Showcase.Http.Exceptions;

/// <summary>
///     Thrown when the Dataverse Web API returns a non-success status or a response
///     that cannot be deserialized into the expected shape.
/// </summary>
public class DataverseException : Exception
{
    /// <summary>
    ///     The HTTP status code returned by Dataverse.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    ///     The Dataverse error code parsed from the response body, when available.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    ///     Creates a new <see cref="DataverseException" /> with a status code and message.
    /// </summary>
    public DataverseException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Creates a new <see cref="DataverseException" /> with a status code, message, and inner exception.
    /// </summary>
    public DataverseException(HttpStatusCode statusCode, string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Creates a new <see cref="DataverseException" /> with a status code, parsed Dataverse error code, and message.
    /// </summary>
    public DataverseException(HttpStatusCode statusCode, string? errorCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    internal static async Task<DataverseException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<DataverseErrorEnvelope>(DataverseClientBase.JsonOptions,
                cancellationToken);
            if (envelope?.Error is { } error)
            {
                return new DataverseException(
                    response.StatusCode,
                    error.Code,
                    $"Dataverse request failed with {(int)response.StatusCode} ({response.StatusCode}): {error.Message}");
            }
        }
        catch (JsonException)
        {
            // body was not a Dataverse error envelope; fall through to the generic message
        }
        catch (NotSupportedException)
        {
            // content type was not JSON; fall through to the generic message
        }

        return new DataverseException(
            response.StatusCode,
            $"Dataverse request failed with {(int)response.StatusCode} ({response.StatusCode}).");
    }

    private sealed record DataverseErrorEnvelope(DataverseErrorBody? Error);

    private sealed record DataverseErrorBody(string? Code, string Message);
}
