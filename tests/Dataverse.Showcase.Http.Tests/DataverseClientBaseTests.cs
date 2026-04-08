using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Dataverse.Showcase.Http.Exceptions;
using Dataverse.Showcase.Http.TypedHttpClients;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dataverse.Showcase.Http.Tests;

[TestFixture]
public class DataverseClientBaseTests
{
    private MockHttpMessageHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TestDataverseClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://contoso.crm4.dynamics.com/"),
        };
        _client = new TestDataverseClient(_httpClient, Substitute.For<ILogger>());
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Test]
    public async Task GetODataAsyncReturnsDeserializedValueArray()
    {
        EnqueueJson(HttpStatusCode.OK, """
            {
              "value": [
                { "name": "Contoso" },
                { "name": "Fabrikam" }
              ]
            }
            """);

        var result = await _client.InvokeGetODataAsync<AccountRow>("api/data/v9.2/accounts?$select=name", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("Contoso"));
            Assert.That(result[1].Name, Is.EqualTo("Fabrikam"));
        });
    }

    [Test]
    public async Task GetODataAsyncReturnsEmptyListWhenValueMissing()
    {
        EnqueueJson(HttpStatusCode.OK, "{}");

        var result = await _client.InvokeGetODataAsync<AccountRow>("api/data/v9.2/accounts", CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetODataAsyncThrowsDataverseExceptionWithParsedErrorOnFailure()
    {
        EnqueueJson(HttpStatusCode.BadRequest, """
            {
              "error": {
                "code": "0x80040203",
                "message": "Invalid $select clause"
              }
            }
            """);

        var ex = Assert.ThrowsAsync<DataverseException>(async () =>
            await _client.InvokeGetODataAsync<AccountRow>("api/data/v9.2/accounts", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(ex.ErrorCode, Is.EqualTo("0x80040203"));
            Assert.That(ex.Message, Does.Contain("Invalid $select clause"));
        });
    }

    [Test]
    public void GetODataAsyncThrowsDataverseExceptionWhenErrorBodyNotParseable()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("upstream timed out", Encoding.UTF8, "text/plain"),
        };
        _handler.EnqueueResponse(response);

        var ex = Assert.ThrowsAsync<DataverseException>(async () =>
            await _client.InvokeGetODataAsync<AccountRow>("api/data/v9.2/accounts", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(ex.ErrorCode, Is.Null);
            Assert.That(ex.Message, Does.Contain("500"));
        });
    }

    [Test]
    public void GetODataAsyncThrowsDataverseExceptionOnInvalidJsonOnSuccess()
    {
        EnqueueJson(HttpStatusCode.OK, "{ this is not json");

        var ex = Assert.ThrowsAsync<DataverseException>(async () =>
            await _client.InvokeGetODataAsync<AccountRow>("api/data/v9.2/accounts", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ex.InnerException, Is.InstanceOf<System.Text.Json.JsonException>());
        });
    }

    [Test]
    public void GetODataAsyncThrowsOnEmptyQuery()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _client.InvokeGetODataAsync<AccountRow>("   ", CancellationToken.None));
    }

    [Test]
    public async Task SendAsyncForwardsRequestThroughHandlerPipeline()
    {
        _handler.EnqueueResponse(HttpStatusCode.NoContent);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/data/v9.2/accounts");
        using var response = await _client.InvokeSendAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(_handler.SentRequests, Has.Count.EqualTo(1));
            Assert.That(_handler.SentRequests[0], Is.SameAs(request));
        });
    }

    private void EnqueueJson(HttpStatusCode statusCode, string body)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        _handler.EnqueueResponse(response);
    }

    private sealed record AccountRow(string? Name);

    private sealed class TestDataverseClient : DataverseClientBase
    {
        public TestDataverseClient(HttpClient client, ILogger logger) : base(client, logger)
        {
        }

        public Task<IReadOnlyList<T>> InvokeGetODataAsync<T>(string query, CancellationToken cancellationToken)
            => GetODataAsync<T>(query, cancellationToken);

        public Task<HttpResponseMessage> InvokeSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => SendAsync(request, cancellationToken);
    }
}
