using Skynet.Infra.Security;

namespace Skynet.Infra.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ProducesAHashDifferentFromThePlainPassword()
    {
        var hash = _sut.HashPassword("P@ssw0rd");

        Assert.NotEqual("P@ssw0rd", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.HashPassword("P@ssw0rd");

        Assert.True(_sut.Verify("P@ssw0rd", hash));
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("P@ssw0rd");

        Assert.False(_sut.Verify("wrong-password", hash));
    }
}
