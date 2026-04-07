namespace Dataverse.Showcase.Http.Exceptions;

/// <summary>
///     Thrown when all Dataverse users are rate-limited (HTTP 429).
/// </summary>
public class DataverseThrottledException(string message) : Exception(message);
