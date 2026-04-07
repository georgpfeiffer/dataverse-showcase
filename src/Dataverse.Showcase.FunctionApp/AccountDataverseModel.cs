using System.Text.Json.Serialization;

namespace Dataverse.Showcase.FunctionApp;

/// <summary>
///     Deserialization target for rows from the Dataverse <c>account</c> entity set.
///     Property names mirror the Dataverse Web API field names.
/// </summary>
public sealed record AccountDataverseModel([property: JsonPropertyName("accountid")] Guid AccountId, string? Name);
