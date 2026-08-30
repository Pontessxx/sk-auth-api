namespace Skynet.Application.Services;

public interface IUserService
{
    Task<UserResponse> GetUserByIdAsync(Guid userId);
    Task<UserResponse> UpdateUserAsync(Guid userId, UserRequest request);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(Guid userId);
}