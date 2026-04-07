namespace Dataverse.Showcase.Http.Middleware;

/// <summary>
///     Shared <see cref="HttpRequestOptionsKey{T}" /> constants used to pass per-request context
///     from <see cref="DataverseUserRotationHandler" /> through the handler pipeline.
/// </summary>
public static class DataverseRequestOptions
{
    public static readonly HttpRequestOptionsKey<string> UserName = new("DataverseUserName");

    public static readonly HttpRequestOptionsKey<int> Attempt = new("DataverseUserAttempt");
}
