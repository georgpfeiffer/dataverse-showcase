using System.Net.Http.Headers;
using Azure.Core;

namespace Dataverse.Showcase.Http.Middleware;

/// <summary>
///     Attaches a Bearer token from any <see cref="TokenCredential" /> to outgoing HTTP requests.
///     Azure.Identity handles caching, thread safety, and proactive token refresh.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;

    public BearerTokenHandler(TokenCredential credential, string[] scopes)
    {
        _credential = credential;
        _scopes = scopes;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
