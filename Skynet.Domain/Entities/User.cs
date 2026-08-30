namespace Skynet.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7(); 
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.User;
}