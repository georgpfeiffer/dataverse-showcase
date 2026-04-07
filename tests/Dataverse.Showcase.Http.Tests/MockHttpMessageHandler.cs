using System.Net;
using System.Net.Http.Headers;

namespace Dataverse.Showcase.Http.Tests;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<HttpRequestMessage> SentRequests { get; } = [];

    public void EnqueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

    public void EnqueueResponse(HttpStatusCode statusCode, RetryConditionHeaderValue? retryAfter = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (retryAfter != null)
        {
            response.Headers.RetryAfter = retryAfter;
        }

        _responses.Enqueue(response);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        var response = _responses.Dequeue();
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
