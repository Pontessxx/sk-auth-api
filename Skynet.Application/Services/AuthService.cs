namespace Skynet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenBlacklistRepository _tokenBlacklistRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenBlacklistRepository tokenBlacklistRepository,
        JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenBlacklistRepository = tokenBlacklistRepository;
        _jwtSettings = jwtSettings;
    }

    public async Task<BaseResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            throw new Exception("User already exists.");
        }
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = Role.User
        };

        await _userRepository.AddAsync(user);

        return new BaseResponse
        {
            Id = user.Id,
            Username = user.Username,
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username) ?? throw new UnauthorizedAccessException("User not found.");

        var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Usuário desativado.");
        }

        return await IssueTokensAsync(user);
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash)
            ?? throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");

        if (!storedRefreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        var user = await _userRepository.GetByIdAsync(storedRefreshToken.UserId)
            ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Usuário desativado.");
        }

        var response = await IssueTokensAsync(user, storedRefreshToken);

        await _refreshTokenRepository.RevokeAsync(storedRefreshToken);

        return response;
    }

    public async Task LogoutAsync(string jti, DateTime accessTokenExpiresAt, string refreshToken)
    {
        await _tokenBlacklistRepository.RevokeAsync(jti, accessTokenExpiresAt);

        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);
        var storedRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (storedRefreshToken is not null && storedRefreshToken.IsActive)
        {
            await _refreshTokenRepository.RevokeAsync(storedRefreshToken);
        }
    }

    private async Task<LoginResponse> IssueTokensAsync(User user, RefreshToken? replacedToken = null)
    {
        var (accessToken, _) = _accessTokenGenerator.Generate(user);
        var rawRefreshToken = _refreshTokenGenerator.Generate();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _refreshTokenGenerator.Hash(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);

        if (replacedToken is not null)
        {
            replacedToken.ReplacedByTokenId = newRefreshToken.Id;
        }

        return new LoginResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken
        };
    }
}
