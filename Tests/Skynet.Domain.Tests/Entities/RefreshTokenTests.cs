using Skynet.Domain.Entities;

namespace Skynet.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(token.IsActive);
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };

        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.False(token.IsActive);
    }
}
