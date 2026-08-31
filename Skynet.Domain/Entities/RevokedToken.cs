namespace Skynet.Domain.Entities;

public class RevokedToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Jti { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
