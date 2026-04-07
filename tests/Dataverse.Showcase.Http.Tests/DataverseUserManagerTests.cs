using Azure.Core;
using Dataverse.Showcase.Http.UserManagement;
using NSubstitute;

namespace Dataverse.Showcase.Http.Tests;

[TestFixture]
public class DataverseUserManagerTests
{
    private IDataverseUserStore _store = null!;
    private DataverseUser _user1 = null!;
    private DataverseUser _user2 = null!;
    private DataverseUser _user3 = null!;

    [SetUp]
    public void Setup()
    {
        _store = Substitute.For<IDataverseUserStore>();
        _store.IsAvailable(Arg.Any<string>()).Returns(true);

        _user1 = new DataverseUser("user1", Substitute.For<TokenCredential>());
        _user2 = new DataverseUser("user2", Substitute.For<TokenCredential>());
        _user3 = new DataverseUser("user3", Substitute.For<TokenCredential>());
    }

    [Test]
    public void ConstructorThrowsOnEmptyUsers()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DataverseUserManager(_store));
    }

    [Test]
    public void ConstructorThrowsOnNullStore()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DataverseUserManager(null!, _user1));
    }

    [Test]
    public void UserCountReturnsNumberOfRegisteredUsers()
    {
        var manager = new DataverseUserManager(_store, _user1, _user2, _user3);

        Assert.That(manager.UserCount, Is.EqualTo(3));
    }

    [Test]
    public void GetAvailableUserReturnsUserWhenAvailable()
    {
        var manager = new DataverseUserManager(_store, _user1);

        var result = manager.GetAvailableUser();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("user1"));
    }

    [Test]
    public void GetAvailableUserSkipsLockedUser()
    {
        _store.IsAvailable("user1").Returns(false);
        var manager = new DataverseUserManager(_store, _user1, _user2);

        var result = manager.GetAvailableUser();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("user2"));
    }

    [Test]
    public void GetAvailableUserReturnsNullWhenAllLocked()
    {
        _store.IsAvailable(Arg.Any<string>()).Returns(false);
        var manager = new DataverseUserManager(_store, _user1, _user2);

        var result = manager.GetAvailableUser();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAvailableUserRoundRobinsAcrossCalls()
    {
        var manager = new DataverseUserManager(_store, _user1, _user2, _user3);

        var first = manager.GetAvailableUser();
        var second = manager.GetAvailableUser();
        var third = manager.GetAvailableUser();

        Assert.Multiple(() =>
        {
            Assert.That(first!.Name, Is.Not.EqualTo(second!.Name));
            Assert.That(second.Name, Is.Not.EqualTo(third!.Name));
        });
    }

    [Test]
    public void LockDelegatesToStore()
    {
        var manager = new DataverseUserManager(_store, _user1);
        var duration = TimeSpan.FromSeconds(30);

        manager.Lock("user1", duration);

        _store.Received(1).Lock("user1", duration);
    }
}
