using Microsoft.EntityFrameworkCore;
using Skynet.Domain.Entities;
using Skynet.Infra.Data;
using Skynet.Infra.Repositories;

namespace Skynet.Infra.Tests.Repositories;

public class RefreshTokenRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetByTokenHashAsync_ReturnsTheSameToken()
    {
        await using var context = CreateContext();
        var sut = new RefreshTokenRepository(context);
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await sut.AddAsync(token);
        var found = await sut.GetByTokenHashAsync("hash-123");

        Assert.NotNull(found);
        Assert.Equal(token.Id, found!.Id);
    }

    [Fact]
    public async Task GetByTokenHashAsync_WhenHashIsUnknown_ReturnsNull()
    {
        await using var context = CreateContext();
        var sut = new RefreshTokenRepository(context);

        var found = await sut.GetByTokenHashAsync("unknown-hash");

        Assert.Null(found);
    }

    [Fact]
    public async Task RevokeAsync_SetsRevokedAtAndPersistsIt()
    {
        await using var context = CreateContext();
        var sut = new RefreshTokenRepository(context);
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await sut.AddAsync(token);

        await sut.RevokeAsync(token);

        var found = await sut.GetByTokenHashAsync("hash-123");
        Assert.NotNull(found!.RevokedAt);
        Assert.False(found.IsActive);
    }
}
