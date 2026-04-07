using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dataverse.Showcase.FunctionApp;

public class GetAccountsHttpFunctionTrigger
{
    private readonly SampleDataverseClient _client;
    private readonly ILogger<GetAccountsHttpFunctionTrigger> _logger;

    public GetAccountsHttpFunctionTrigger(SampleDataverseClient client, ILogger<GetAccountsHttpFunctionTrigger> logger)
    {
        _client = client;
        _logger = logger;
    }

    [Function("GetAccounts")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData requestData,
        CancellationToken ct)
    {
        _logger.LogInformation("GetAccounts invoked");

        var accounts = await _client.GetTopAccountsAsync(ct);

        var response = requestData.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(accounts, ct);
        return response;
    }
}
