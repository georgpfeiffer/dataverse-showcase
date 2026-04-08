using Azure.Identity;
using Dataverse.Showcase.Http.Middleware;
using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.Http;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the <see cref="DataverseApiClient" /> facade with Azure.Identity-based authentication.
    ///     Register your own domain classes on top via <c>services.AddTransient&lt;MyClient&gt;()</c>.
    /// </summary>
    public static IHttpClientBuilder AddDataverseApiClient(this IServiceCollection services,
        string baseUrl,
        string tenantId,
        string clientId,
        string clientSecret,
        string scope)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        return services
            .AddHttpClient<DataverseApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler(() => new BearerTokenHandler(credential, [scope]));
    }

    /// <summary>
    ///     Registers the <see cref="DataverseApiClient" /> facade with multi-user credential rotation and HTTP 429 handling.
    ///     When a user is throttled, the handler locks that user and switches to the next available one.
    ///     Register your own domain classes on top via <c>services.AddTransient&lt;MyClient&gt;()</c>.
    /// </summary>
    public static IHttpClientBuilder AddDataverseApiClient(
        this IServiceCollection services,
        string baseUrl,
        string scope,
        params DataverseUser[] users)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<IDataverseUserStore, InMemoryDataverseUserStore>();
        services.TryAddSingleton<IDataverseUserManager>(sp => new DataverseUserManager(sp.GetRequiredService<IDataverseUserStore>(), users));

        return services
            .AddHttpClient<DataverseApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler(sp => new DataverseUserRotationHandler(
                sp.GetRequiredService<IDataverseUserManager>(),
                [scope],
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DataverseUserRotationHandler>()));
    }
}
