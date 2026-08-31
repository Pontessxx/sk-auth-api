using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Skynet.API.Controllers.V1;
using Skynet.Application.Models.Auth;
using Skynet.Application.Services;
using Skynet.Domain.Settings;

namespace Skynet.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly JwtSettings _jwtSettings = new() { RefreshTokenExpirationDays = 7 };

    private AuthController CreateSut()
    {
        var controller = new AuthController(_authService.Object, _jwtSettings)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndSetsRefreshTokenCookie()
    {
        var response = new LoginResponse
        {
            Id = Guid.NewGuid(),
            Username = "neo",
            AccessToken = "access-token",
            RefreshToken = "raw-refresh-token"
        };
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(response);

        var sut = CreateSut();

        var result = await sut.Login(new LoginRequest { Username = "neo", Password = "P@ssw0rd" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);

        var setCookie = sut.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("refreshToken=raw-refresh-token", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WhenServiceThrowsUnauthorized_ReturnsUnauthorized()
    {
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid username or password."));

        var sut = CreateSut();

        var result = await sut.Login(new LoginRequest { Username = "neo", Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenCookieIsMissing_ReturnsUnauthorized()
    {
        var sut = CreateSut();

        var result = await sut.Refresh();

        Assert.IsType<UnauthorizedObjectResult>(result);
        _authService.Verify(s => s.RefreshAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ForwardsTokenAndRotatesCookie()
    {
        var response = new LoginResponse
        {
            Id = Guid.NewGuid(),
            Username = "neo",
            AccessToken = "new-access-token",
            RefreshToken = "new-raw-refresh-token"
        };
        _authService.Setup(s => s.RefreshAsync("old-raw-refresh-token")).ReturnsAsync(response);

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers.Append("Cookie", "refreshToken=old-raw-refresh-token");

        var result = await sut.Refresh();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);

        var setCookie = sut.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("refreshToken=new-raw-refresh-token", setCookie);
    }

    [Fact]
    public async Task Refresh_WhenServiceThrowsUnauthorized_DeletesCookieAndReturnsUnauthorized()
    {
        _authService.Setup(s => s.RefreshAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Refresh token inválido ou expirado."));

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.Request.Headers.Append("Cookie", "refreshToken=expired-token");

        var result = await sut.Refresh();

        Assert.IsType<UnauthorizedObjectResult>(result);
        var setCookie = sut.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("refreshToken=", setCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WithoutJtiOrExpClaims_ReturnsUnauthorized()
    {
        var sut = CreateSut();
        sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await sut.Logout();

        Assert.IsType<UnauthorizedResult>(result);
        _authService.Verify(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithValidClaimsAndCookie_RevokesTokensAndDeletesCookie()
    {
        var expiresAtUnix = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var claims = new[]
        {
            new Claim("jti", "jti-123"),
            new Claim("exp", expiresAtUnix.ToString())
        };

        var sut = CreateSut();
        sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
        sut.ControllerContext.HttpContext.Request.Headers.Append("Cookie", "refreshToken=raw-refresh-token");

        var result = await sut.Logout();

        Assert.IsType<NoContentResult>(result);
        _authService.Verify(s => s.LogoutAsync(
            "jti-123",
            It.Is<DateTime>(d => Math.Abs((d - DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime).TotalSeconds) < 1),
            "raw-refresh-token"), Times.Once);

        var setCookie = sut.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}
