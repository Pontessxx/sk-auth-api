namespace Skynet.Application.Models.User;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public Role Role { get; set; }
}