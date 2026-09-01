namespace Skynet.Domain.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string? PublicKey {get; set;}
    public string? PrivateKey {get; set;}
    public string Issuer { get; set; } = "skynet-api-auth";
    public string Audience { get; set; } = "skynet-api-auth";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int MaxActiveSessionsPerUser { get; set; } = 5;
}