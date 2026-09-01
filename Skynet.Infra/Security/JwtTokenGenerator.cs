namespace Skynet.Infra.Security;

public class AccessTokenGenerator : IAccessTokenGenerator, IDisposable
{
    private readonly JwtSettings _settings;
    private readonly RSA _rsa;

    public AccessTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(_settings.PrivateKey);
    }

    public (string Token, DateTime ExpiresAt) Generate(User user)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("username", user.Username),
            new Claim("role", user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_rsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public void Dispose()
    {
        _rsa.Dispose();
        GC.SuppressFinalize(this);
    }
}