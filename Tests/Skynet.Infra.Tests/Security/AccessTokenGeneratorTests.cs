using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Skynet.Domain.Entities;
using Skynet.Domain.Enums;
using Skynet.Domain.Settings;
using Skynet.Infra.Security;

namespace Skynet.Infra.Tests.Security;

public class AccessTokenGeneratorTests
{
    private static readonly RSA _rsa = RSA.Create(2048);

    private readonly JwtSettings _settings = new()
    {
        PrivateKey = _rsa.ExportRSAPrivateKeyPem(),
        Issuer = "skynet-tests",
        Audience = "skynet-tests",
        AccessTokenExpirationMinutes = 15
    };

    [Fact]
    public void Generate_ReturnsATokenWithExpectedClaimsAndExpiration()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "trinity",
            Role = Role.Admin
        };

        var sut = new AccessTokenGenerator(_settings);

        var (token, expiresAt) = sut.Generate(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow.AddMinutes(14));
        Assert.True(expiresAt <= DateTime.UtcNow.AddMinutes(15));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(_settings.Issuer, jwt.Issuer);
        Assert.Equal(_settings.Audience, jwt.Audiences.Single());
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Username, jwt.Claims.Single(c => c.Type == "username").Value);
        Assert.Equal(user.Role.ToString(), jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.NotNull(jwt.Claims.SingleOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void Generate_ReturnsDifferentJtiOnEachCall()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "neo", Role = Role.User };
        var sut = new AccessTokenGenerator(_settings);
        var handler = new JwtSecurityTokenHandler();

        var (firstToken, _) = sut.Generate(user);
        var (secondToken, _) = sut.Generate(user);

        var firstJti = handler.ReadJwtToken(firstToken).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = handler.ReadJwtToken(secondToken).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(firstJti, secondJti);
    }
}
