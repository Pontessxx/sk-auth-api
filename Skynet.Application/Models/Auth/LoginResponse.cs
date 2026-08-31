namespace Skynet.Application.Models.Auth;

public class LoginResponse : BaseResponse
{
    public Role Role { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}