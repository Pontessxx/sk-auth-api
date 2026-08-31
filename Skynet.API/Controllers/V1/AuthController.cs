namespace Skynet.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/auth-service")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private const string CookiePath = "/api/v1/auth-service";

    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, JwtSettings jwtSettings)
    {
        _authService = authService;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// Endpoint for user login.
    /// </summary>
    /// <param name="loginRequest">The login request containing username and password.</param>
    /// <returns>The authenticated user's data along with the JWT access token. The refresh token is set as an HttpOnly cookie.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var result = await _authService.LoginAsync(loginRequest);
            AppendRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint for user registration.
    /// </summary>
    /// <param name="registerRequest">The registration request containing user details.</param>
    /// <returns>The created user.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint to exchange a valid refresh token (read from the HttpOnly cookie) for a new access/refresh token pair.
    /// </summary>
    /// <returns>A new JWT access token. The rotated refresh token is set as an HttpOnly cookie.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token ausente." });
        }

        try
        {
            var result = await _authService.RefreshAsync(refreshToken);
            AppendRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint to revoke the current access token and the refresh token stored in the HttpOnly cookie.
    /// </summary>
    [Authorize]
    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue("jti");
        var expClaim = User.FindFirstValue("exp");
        if (string.IsNullOrEmpty(jti) || !long.TryParse(expClaim, out var expUnix))
        {
            return Unauthorized();
        }

        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken);

        var accessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        await _authService.LogoutAsync(jti, accessTokenExpiresAt, refreshToken ?? string.Empty);

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    private void AppendRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = CookiePath
        });
    }
}
