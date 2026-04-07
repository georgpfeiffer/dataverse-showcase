using System.Net;
using System.Net.Http.Headers;
using Azure.Core;
using Dataverse.Showcase.Http.Exceptions;
using Dataverse.Showcase.Http.Middleware;
using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dataverse.Showcase.Http.Tests;

[TestFixture]
public class DataverseUserRotationHandlerTests
{
    private IDataverseUserManager _userManager = null!;
    private MockHttpMessageHandler _innerHandler = null!;
    private HttpMessageInvoker _invoker = null!;
    private DataverseUser _user1 = null!;
    private DataverseUser _user2 = null!;

    [SetUp]
    public void Setup()
    {
        _userManager = Substitute.For<IDataverseUserManager>();
        _userManager.UserCount.Returns(2);

        _innerHandler = new MockHttpMessageHandler();

        _user1 = new DataverseUser("user1", CreateMockCredential("token-user1"));
        _user2 = new DataverseUser("user2", CreateMockCredential("token-user2"));

        var handler = new DataverseUserRotationHandler(
            _userManager,
            ["https://org.crm4.dynamics.com/.default"],
            Substitute.For<ILogger>())
        {
            InnerHandler = _innerHandler
        };

        _invoker = new HttpMessageInvoker(handler);
    }

    [TearDown]
    public void TearDown()
    {
        _innerHandler.Dispose();
        _invoker.Dispose();
    }

    [Test]
    public async Task SendAsyncAttachesBearerToken()
    {
        _userManager.GetAvailableUser().Returns(_user1);
        _innerHandler.EnqueueResponse(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(_innerHandler.SentRequests[0].Headers.Authorization?.Parameter, Is.EqualTo("token-user1"));
    }

    [Test]
    public async Task SendAsyncReturnsResponseOnSuccess()
    {
        _userManager.GetAvailableUser().Returns(_user1);
        _innerHandler.EnqueueResponse(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SendAsyncReturnsNon429ErrorWithoutRotation()
    {
        _userManager.GetAvailableUser().Returns(_user1);
        _innerHandler.EnqueueResponse(HttpStatusCode.InternalServerError);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        _userManager.DidNotReceive().Lock(Arg.Any<string>(), Arg.Any<TimeSpan>());
    }

    [Test]
    public async Task SendAsyncRotatesUserOn429()
    {
        _userManager.GetAvailableUser().Returns(_user1, _user2);
        _innerHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(10)));
        _innerHandler.EnqueueResponse(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_innerHandler.SentRequests, Has.Count.EqualTo(2));
            Assert.That(_innerHandler.SentRequests[1].Headers.Authorization?.Parameter, Is.EqualTo("token-user2"));
        });
        _userManager.Received(1).Lock("user1", TimeSpan.FromSeconds(10));
    }

    [Test]
    public void SendAsyncThrowsThrottledExceptionWhenAllUsersExhausted()
    {
        _userManager.GetAvailableUser().Returns(_user1, _user2);
        _innerHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(10)));
        _innerHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(10)));

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");

        Assert.ThrowsAsync<DataverseThrottledException>(async () =>
            await _invoker.SendAsync(request, CancellationToken.None));
    }

    [Test]
    public void SendAsyncThrowsThrottledExceptionWhenNoUsersAvailable()
    {
        _userManager.GetAvailableUser().Returns((DataverseUser?)null);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");

        Assert.ThrowsAsync<DataverseThrottledException>(async () =>
            await _invoker.SendAsync(request, CancellationToken.None));
    }

    [Test]
    public async Task SendAsyncParsesRetryAfterSeconds()
    {
        _userManager.GetAvailableUser().Returns(_user1, _user2);
        _innerHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, new RetryConditionHeaderValue(TimeSpan.FromSeconds(42)));
        _innerHandler.EnqueueResponse(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        _userManager.Received(1).Lock("user1", TimeSpan.FromSeconds(42));
    }

    [Test]
    public async Task SendAsyncDefaultsRetryAfterTo30sWhenHeaderMissing()
    {
        _userManager.GetAvailableUser().Returns(_user1, _user2);
        _innerHandler.EnqueueResponse(HttpStatusCode.TooManyRequests);
        _innerHandler.EnqueueResponse(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://org.crm4.dynamics.com/api/data/v9.2/accounts");
        using var response = await _invoker.SendAsync(request, CancellationToken.None);

        _userManager.Received(1).Lock("user1", TimeSpan.FromSeconds(30));
    }

    private static TokenCredential CreateMockCredential(string tokenValue)
    {
        var accessToken = new AccessToken(tokenValue, DateTimeOffset.UtcNow.AddHours(1));
        var credential = Substitute.For<TokenCredential>();
        credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);
        return credential;
    }
}
