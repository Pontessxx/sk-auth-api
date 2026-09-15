namespace Skynet.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<LoginResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string jti, DateTime accessTokenExpiresAt, string refreshToken);
}
