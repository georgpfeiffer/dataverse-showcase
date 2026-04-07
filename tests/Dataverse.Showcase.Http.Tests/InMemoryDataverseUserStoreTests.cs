using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.Caching.Memory;

namespace Dataverse.Showcase.Http.Tests;

[TestFixture]
public class InMemoryDataverseUserStoreTests
{
    private MemoryCache _cache = null!;
    private InMemoryDataverseUserStore _store = null!;

    [SetUp]
    public void Setup()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _store = new InMemoryDataverseUserStore(_cache);
    }

    [TearDown]
    public void TearDown() => _cache.Dispose();

    [Test]
    public void IsAvailableReturnsTrueWhenUserNotLocked()
    {
        Assert.That(_store.IsAvailable("user1"), Is.True);
    }

    [Test]
    public void IsAvailableReturnsFalseWhenUserLocked()
    {
        _store.Lock("user1", TimeSpan.FromMinutes(5));

        Assert.That(_store.IsAvailable("user1"), Is.False);
    }

    [Test]
    public void IsAvailableReturnsTrueAfterLockExpires()
    {
        _store.Lock("user1", TimeSpan.FromMilliseconds(50));

        Thread.Sleep(100);

        Assert.That(_store.IsAvailable("user1"), Is.True);
    }

    [Test]
    public void LockDoesNotAffectOtherUsers()
    {
        _store.Lock("user1", TimeSpan.FromMinutes(5));

        Assert.Multiple(() =>
        {
            Assert.That(_store.IsAvailable("user1"), Is.False);
            Assert.That(_store.IsAvailable("user2"), Is.True);
        });
    }

    [Test]
    public void LockOverwritesPreviousLock()
    {
        _store.Lock("user1", TimeSpan.FromMilliseconds(50));
        _store.Lock("user1", TimeSpan.FromMinutes(5));

        Thread.Sleep(100);

        Assert.That(_store.IsAvailable("user1"), Is.False);
    }
}
