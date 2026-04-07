using Azure.Identity;
using Dataverse.Showcase.Http.Middleware;
using Dataverse.Showcase.Http.TypedHttpClients;
using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers a typed Dataverse HTTP client with Azure.Identity-based authentication.
    /// </summary>
    public static IHttpClientBuilder AddDataverseHttpClient<T>(this IServiceCollection services,
        string baseUrl,
        string tenantId,
        string clientId,
        string clientSecret,
        string scope)
        where T : DataverseClientBase
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        return services
            .AddHttpClient<T>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler(() => new BearerTokenHandler(credential, [scope]));
    }

    /// <summary>
    ///     Registers a typed Dataverse HTTP client with multi-user credential rotation and HTTP 429 handling.
    ///     When a user is throttled, the handler locks that user and switches to the next available one.
    /// </summary>
    public static IHttpClientBuilder AddDataverseHttpClient<T>(
        this IServiceCollection services,
        string baseUrl,
        string scope,
        params DataverseUser[] users)
        where T : DataverseClientBase
    {
        services.AddMemoryCache();
        services.TryAddSingleton<IDataverseUserStore, InMemoryDataverseUserStore>();
        services.TryAddSingleton<IDataverseUserManager>(sp => new DataverseUserManager(sp.GetRequiredService<IDataverseUserStore>(), users));

        return services
            .AddHttpClient<T>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler(sp => new DataverseUserRotationHandler(
                sp.GetRequiredService<IDataverseUserManager>(),
                [scope],
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DataverseUserRotationHandler>()));
    }
}
