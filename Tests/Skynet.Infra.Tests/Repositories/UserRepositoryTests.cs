using Microsoft.EntityFrameworkCore;
using Skynet.Domain.Entities;
using Skynet.Domain.Enums;
using Skynet.Infra.Data;
using Skynet.Infra.Repositories;

namespace Skynet.Infra.Tests.Repositories;

public class UserRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetByUsernameAsync_ReturnsTheSameUser()
    {
        await using var context = CreateContext();
        var sut = new UserRepository(context);
        var user = new User { Username = "neo", PasswordHash = "hash", Role = Role.User };

        await sut.AddAsync(user);
        var found = await sut.GetByUsernameAsync("neo");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var context = CreateContext();
        var sut = new UserRepository(context);

        var found = await sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryPersistedUser()
    {
        await using var context = CreateContext();
        var sut = new UserRepository(context);
        await sut.AddAsync(new User { Username = "neo", PasswordHash = "hash", Role = Role.User });
        await sut.AddAsync(new User { Username = "trinity", PasswordHash = "hash", Role = Role.Admin });

        var all = await sut.GetAllAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        await using var context = CreateContext();
        var sut = new UserRepository(context);
        var user = new User { Username = "neo", PasswordHash = "hash", Role = Role.User };
        await sut.AddAsync(user);

        user.Role = Role.Admin;
        await sut.UpdateAsync(user);

        var found = await sut.GetByIdAsync(user.Id);
        Assert.Equal(Role.Admin, found!.Role);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheUser()
    {
        await using var context = CreateContext();
        var sut = new UserRepository(context);
        var user = new User { Username = "neo", PasswordHash = "hash", Role = Role.User };
        await sut.AddAsync(user);

        await sut.DeleteAsync(user);

        var found = await sut.GetByIdAsync(user.Id);
        Assert.Null(found);
    }
}
