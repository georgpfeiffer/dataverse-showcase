namespace Dataverse.Showcase.Http.UserManagement;

/// <summary>
///     Selects the next available Dataverse user and manages throttle reporting.
/// </summary>
public interface IDataverseUserManager
{
    /// <summary>
    ///     Returns the next available (unlocked) user, or null if all users are locked.
    /// </summary>
    DataverseUser? GetAvailableUser();

    /// <summary>
    ///     Locks the specified user for the given duration after a 429 response.
    /// </summary>
    void Lock(string userName, TimeSpan retryAfter);

    /// <summary>
    ///     The total number of registered users.
    /// </summary>
    int UserCount { get; }
}
