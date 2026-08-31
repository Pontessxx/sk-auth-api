namespace Skynet.Infra.Repositories;

public class TokenBlacklistRepository : ITokenBlacklistRepository
{
    private readonly AppDbContext _context;
    public TokenBlacklistRepository(AppDbContext context) => _context = context;

    public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var revokedToken = new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt
        };

        await _context.RevokedTokens.AddAsync(revokedToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return await _context.RevokedTokens.AnyAsync(t => t.Jti == jti, cancellationToken);
    }
}
