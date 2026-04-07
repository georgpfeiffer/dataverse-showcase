using Microsoft.Extensions.Caching.Memory;

namespace Dataverse.Showcase.Http.UserManagement;

/// <summary>
///     In-memory implementation of <see cref="IDataverseUserStore" /> using <see cref="IMemoryCache" /> with TTL-based expiry.
/// </summary>
public class InMemoryDataverseUserStore : IDataverseUserStore
{
    private readonly IMemoryCache _cache;

    public InMemoryDataverseUserStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public bool IsAvailable(string userName) => !_cache.TryGetValue(userName, out _);

    /// <inheritdoc />
    public void Lock(string userName, TimeSpan duration) =>
        _cache.Set(userName, true, duration);
}
