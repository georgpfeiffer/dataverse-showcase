namespace Dataverse.Showcase.Http.UserManagement;

/// <summary>
///     Tracks lock state for Dataverse application users.
/// </summary>
public interface IDataverseUserStore
{
    /// <summary>
    ///     Returns true if the user is not currently locked.
    /// </summary>
    bool IsAvailable(string userName);

    /// <summary>
    ///     Locks the user for the specified duration.
    /// </summary>
    void Lock(string userName, TimeSpan duration);
}
