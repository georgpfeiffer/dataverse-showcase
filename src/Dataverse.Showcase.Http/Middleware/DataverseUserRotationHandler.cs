using System.Net;
using System.Net.Http.Headers;
using Azure.Core;
using Dataverse.Showcase.Http.Exceptions;
using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http.Middleware;

/// <summary>
///     Attaches Bearer tokens from rotating Dataverse users and handles HTTP 429 (Too Many Requests)
///     by locking the throttled user and retrying with the next available one.
/// </summary>
public class DataverseUserRotationHandler : DelegatingHandler
{
    private readonly IDataverseUserManager _userManager;
    private readonly string[] _scopes;
    private readonly ILogger _logger;

    public DataverseUserRotationHandler(IDataverseUserManager userManager, string[] scopes, ILogger logger)
    {
        _userManager = userManager;
        _scopes = scopes;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _userManager.UserCount; attempt++)
        {
            var user = _userManager.GetAvailableUser() ?? throw new DataverseThrottledException("All Dataverse users are rate-limited");

            var token = await user.Credential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return response;
            }

            var retryAfter = ParseRetryAfter(response);
            _logger.LogWarning("Dataverse user '{UserName}' throttled (429), locking for {RetryAfter}", user.Name, retryAfter);

            _userManager.Lock(user.Name, retryAfter);
            response.Dispose();
        }

        throw new DataverseThrottledException("All Dataverse users are rate-limited");
    }

    private static TimeSpan ParseRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header == null)
        {
            return TimeSpan.FromSeconds(30);
        }

        if (header.Delta.HasValue)
        {
            return header.Delta.Value;
        }

        if (header.Date.HasValue)
        {
            var delay = header.Date.Value - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        return TimeSpan.FromSeconds(30);
    }
}
