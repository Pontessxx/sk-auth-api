namespace Skynet.Application.Models.Auth;

public class BaseResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
}