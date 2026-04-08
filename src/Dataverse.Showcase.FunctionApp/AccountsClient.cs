using Dataverse.Showcase.Http;

namespace Dataverse.Showcase.FunctionApp;

/// <summary>
///     Domain client for Dataverse <c>account</c> operations. Composes the shared
///     <see cref="DataverseApiClient" /> facade instead of inheriting from a base.
/// </summary>
public sealed class AccountsClient
{
    private readonly DataverseApiClient _dataverse;

    /// <summary>
    ///     Creates a new <see cref="AccountsClient" /> with the injected Dataverse facade.
    /// </summary>
    public AccountsClient(DataverseApiClient dataverse)
    {
        _dataverse = dataverse;
    }

    /// <summary>
    ///     Returns the top 5 accounts with just the id and name populated.
    /// </summary>
    public Task<IReadOnlyList<AccountDataverseModel>> GetTopAsync(CancellationToken cancellationToken)
    {
        return _dataverse.GetODataAsync<AccountDataverseModel>("api/data/v9.2/accounts?$select=accountid,name&$top=5", cancellationToken);
    }
}
