namespace Skynet.Application.Services;

public interface IAuthService
{
    Task<BaseResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string jti, DateTime accessTokenExpiresAt, string refreshToken);
}
