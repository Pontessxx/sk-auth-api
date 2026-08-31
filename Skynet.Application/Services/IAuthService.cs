namespace Skynet.Application.Services;

public interface IAuthService
{
    Task<BaseResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}