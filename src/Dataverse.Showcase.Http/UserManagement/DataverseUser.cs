using Azure.Core;

namespace Dataverse.Showcase.Http.UserManagement;

/// <summary>
///     Represents a Dataverse application user (app registration) with its credential.
/// </summary>
public record DataverseUser(string Name, TokenCredential Credential);
