namespace Dataverse.Showcase.Http.UserManagement;

/// <summary>
///     Round-robin user selection with skip-locked semantics.
/// </summary>
public class DataverseUserManager : IDataverseUserManager
{
    private readonly DataverseUser[] _users;
    private readonly IDataverseUserStore _store;
    private int _nextIndex;

    public DataverseUserManager(IDataverseUserStore store, params DataverseUser[] users)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentOutOfRangeException.ThrowIfZero(users.Length, nameof(users));

        _store = store;
        _users = users;
    }

    /// <inheritdoc />
    public int UserCount => _users.Length;

    /// <inheritdoc />
    public DataverseUser? GetAvailableUser()
    {
        var startIndex = Interlocked.Increment(ref _nextIndex);

        for (var i = 0; i < _users.Length; i++)
        {
            var user = _users[(startIndex + i) % _users.Length];
            if (_store.IsAvailable(user.Name))
            {
                return user;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void Lock(string userName, TimeSpan retryAfter) => _store.Lock(userName, retryAfter);
}
