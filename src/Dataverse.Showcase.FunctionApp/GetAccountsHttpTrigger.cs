using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.FunctionApp;

public class GetAccountsHttpFunctionTrigger
{
    private readonly AccountsClient _accounts;
    private readonly ILogger<GetAccountsHttpFunctionTrigger> _logger;

    public GetAccountsHttpFunctionTrigger(AccountsClient accounts, ILogger<GetAccountsHttpFunctionTrigger> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }

    [Function("GetAccounts")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData requestData,
        CancellationToken ct)
    {
        _logger.LogInformation("GetAccounts invoked");

        var accounts = await _accounts.GetTopAsync(ct);

        var response = requestData.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(accounts, ct);
        return response;
    }
}
