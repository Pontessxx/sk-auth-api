namespace Skynet.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<UserResponse> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId) ?? throw new Exception("User not found.");
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();

        return users.Select(user => new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        });
    }
    
    public async Task<UserResponse> UpdateUserAsync(Guid userId, UserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId) ?? throw new Exception("User not found.");

        user.Username = request.Username;

        await _userRepository.UpdateAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        await _userRepository.DeleteAsync(user);
        return true;
    }
}