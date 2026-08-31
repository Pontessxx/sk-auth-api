using Skynet.Infra.Security;

namespace Skynet.Infra.Tests.Security;

public class RefreshTokenGeneratorTests
{
    private readonly RefreshTokenGenerator _sut = new();

    [Fact]
    public void Generate_ReturnsAUrlSafeToken()
    {
        var token = _sut.Generate();

        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public void Generate_ReturnsDifferentTokensOnEachCall()
    {
        var first = _sut.Generate();
        var second = _sut.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_IsDeterministicForTheSameInput()
    {
        var token = _sut.Generate();

        var firstHash = _sut.Hash(token);
        var secondHash = _sut.Hash(token);

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForDifferentInputs()
    {
        var hashA = _sut.Hash("token-a");
        var hashB = _sut.Hash("token-b");

        Assert.NotEqual(hashA, hashB);
    }
}
