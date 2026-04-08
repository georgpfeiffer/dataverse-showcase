# Dataverse Showcase

A standalone showcase of a Dataverse typed `HttpClient` pipeline with
**multi-user credential rotation** and HTTP 429 handling, extracted from a
larger internal integration library.

The goal is to demonstrate everything you can wire up through
`AddDataverseApiClient` in isolation: the handlers and the user rotation
store.

## What's inside

`src/Dataverse.Showcase.Http` — the library:

- `BearerTokenHandler` — single-user bearer token via any `Azure.Core.TokenCredential`
- `DataverseUserRotationHandler` — multi-user rotation that locks a throttled
  user on HTTP 429 and retries with the next available one
- `UserManagement/` — `DataverseUser`, `IDataverseUserManager` /
  `DataverseUserManager` (round-robin with skip-locked), `IDataverseUserStore` /
  `InMemoryDataverseUserStore` (TTL-based lock store)
- `DataverseApiClient` — sealed façade over the typed `HttpClient`; exposes
  `SendAsync` and a minimal `GetODataAsync<T>` helper that unwraps the OData
  `value` array. Domain classes inject this instead of subclassing a base
  (the full library adds custom-action and batch helpers on top)
- `AddDataverseApiClient` DI extensions (single-user and multi-user overloads)

`src/Dataverse.Showcase.FunctionApp` — a minimal Azure Functions isolated-worker app
with one HTTP trigger. It shows the recommended shape: a domain `AccountsClient`
that composes `DataverseApiClient` and exposes account-specific operations.

`tests/Dataverse.Showcase.Http.Tests` — NUnit + NSubstitute unit tests for the user
manager, rotation handler, and in-memory store.

## Usage — single user (client credentials)

```csharp
services.AddDataverseApiClient(baseUrl, tenantId, clientId, clientSecret, scope);
services.AddTransient<MyDomainClient>();
```

Authenticates every request with a `ClientSecretCredential` bearer token.
`MyDomainClient` is your own class that takes `DataverseApiClient` as a
constructor dependency.

## Usage — multi-user with rotation

```csharp
services.AddDataverseApiClient(baseUrl, scope,
    new DataverseUser("user-a", credentialA),
    new DataverseUser("user-b", credentialB));
services.AddTransient<MyDomainClient>();
```

Rotates through the provided users. When Dataverse returns HTTP 429, the
throttled user is locked for the `Retry-After` duration and the request is
retried with the next available user. If all users are locked, a
`DataverseThrottledException` is thrown.

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
