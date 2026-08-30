namespace Skynet.Application.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);
    Task<BaseResponse> LoginAsync(LoginRequest request);
}