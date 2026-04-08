using Dataverse.Showcase.Http.TypedHttpClients;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.FunctionApp;

/// <summary>
///     Demonstration typed client showing how a subclass of
///     <see cref="DataverseClientBase" /> calls the base-class OData helper
///     instead of touching <c>HttpClient</c> directly.
/// </summary>
public class SampleDataverseClient : DataverseClientBase
{
    public SampleDataverseClient(HttpClient client, ILogger<SampleDataverseClient> logger) : base(client, logger)
    {
    }

    public Task<IReadOnlyList<AccountDataverseModel>> GetTopAccountsAsync(CancellationToken cancellationToken)
    {
        return GetODataAsync<AccountDataverseModel>("api/data/v9.2/accounts?$select=accountid,name&$top=5", cancellationToken);
    }
}
