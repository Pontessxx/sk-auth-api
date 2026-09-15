namespace Skynet.Application.Models.Auth;

public class LoginResult : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
