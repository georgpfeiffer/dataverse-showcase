# Dataverse Showcase

A standalone showcase of a Dataverse typed `HttpClient` pipeline with
**multi-user credential rotation** and HTTP 429 handling, extracted from a
larger internal integration library.

The goal is to demonstrate everything you can wire up through
`AddDataverseHttpClient<T>` in isolation: the handlers, the user rotation
store, and the activity tagging for observability.

## What's inside

`src/Dataverse.Showcase.Http` — the library:

- `BearerTokenHandler` — single-user bearer token via any `Azure.Core.TokenCredential`
- `DataverseUserRotationHandler` — multi-user rotation that locks a throttled
  user on HTTP 429 and retries with the next available one
- `DataverseActivityTagHandler` + `DataverseRequestOptions` — tag the current
  `Activity` with `DataverseUserName` / `DataverseUserAttempt`
- `UserManagement/` — `DataverseUser`, `IDataverseUserManager` /
  `DataverseUserManager` (round-robin with skip-locked), `IDataverseUserStore` /
  `InMemoryDataverseUserStore` (TTL-based lock store)
- `DataverseClientBase` — base for your typed Dataverse clients; exposes a
  minimal `GetCollectionAsync<T>` OData helper that unwraps the `value` array
  (the full library adds custom-action and batch helpers on top)
- `AddDataverseHttpClient<T>` DI extensions (single-user and multi-user overloads)

`src/Dataverse.Showcase.FunctionApp` — a minimal Azure Functions isolated-worker app
with one HTTP trigger that injects a typed client and calls Dataverse.

`tests/Dataverse.Showcase.Http.Tests` — NUnit + NSubstitute unit tests for the user
manager, rotation handler, and in-memory store.

## Usage — single user (client credentials)

```csharp
services.AddDataverseHttpClient<MyDataverseClient>(
    baseUrl, tenantId, clientId, clientSecret, scope);
```

Authenticates every request with a `ClientSecretCredential` bearer token.

## Usage — multi-user with rotation

```csharp
services.AddDataverseHttpClient<MyDataverseClient>(baseUrl, scope,
    new DataverseUser("user-a", credentialA),
    new DataverseUser("user-b", credentialB));
```

Rotates through the provided users. When Dataverse returns HTTP 429, the
throttled user is locked for the `Retry-After` duration and the request is
retried with the next available user. If all users are locked, a
`DataverseThrottledException` is thrown.

## Activity tagging

```csharp
services.AddDataverseHttpClient<MyDataverseClient>(baseUrl, scope, users)
    .AddDataverseActivityTagHandler();
```

Tags each span with `DataverseUserName` and `DataverseUserAttempt`. On 429
retries each attempt is a separate span showing which user was tried.

## Running the sample

```bash
dotnet build
cp src/Dataverse.Showcase.FunctionApp/local.settings.json.template src/Dataverse.Showcase.FunctionApp/local.settings.json
# fill in Dataverse:BaseUrl, Dataverse:TenantId, and the two user credentials
cd src/Dataverse.Showcase.FunctionApp
func start
curl http://localhost:7071/api/accounts
```

## Running the tests

```bash
dotnet test
```
