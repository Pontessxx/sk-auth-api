namespace Skynet.Application.Models.Auth;

public class LoginResponse : BaseResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public static LoginResponse FromResult(LoginResult result) => new()
    {
        Id = result.Id,
        Username = result.Username,
        AccessToken = result.AccessToken
    };
}
