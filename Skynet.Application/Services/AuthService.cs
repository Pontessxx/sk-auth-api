namespace Skynet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
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

        return new LoginResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            AccessToken = "TODO",
            RefreshToken = "TODO"
        };
    }
}