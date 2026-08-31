using Microsoft.EntityFrameworkCore;
using Skynet.Infra.Data;
using Skynet.Infra.Repositories;

namespace Skynet.Infra.Tests.Repositories;

public class TokenBlacklistRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RevokeAsync_ThenIsRevokedAsync_ReturnsTrueForTheSameJti()
    {
        await using var context = CreateContext();
        var sut = new TokenBlacklistRepository(context);

        await sut.RevokeAsync("jti-123", DateTime.UtcNow.AddMinutes(15));

        Assert.True(await sut.IsRevokedAsync("jti-123"));
    }

    [Fact]
    public async Task IsRevokedAsync_WhenJtiWasNeverRevoked_ReturnsFalse()
    {
        await using var context = CreateContext();
        var sut = new TokenBlacklistRepository(context);

        Assert.False(await sut.IsRevokedAsync("unknown-jti"));
    }
}
