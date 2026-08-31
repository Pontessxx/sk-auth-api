using Moq;
using Skynet.Application.Models.Auth;
using Skynet.Application.Services;
using Skynet.Domain.Entities;
using Skynet.Domain.Enums;
using Skynet.Domain.Interfaces;
using Skynet.Domain.Settings;

namespace Skynet.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAccessTokenGenerator> _accessTokenGenerator = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenBlacklistRepository> _tokenBlacklistRepository = new();
    private readonly JwtSettings _jwtSettings = new() { RefreshTokenExpirationDays = 7 };

    private AuthService CreateSut() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _accessTokenGenerator.Object,
        _refreshTokenGenerator.Object,
        _refreshTokenRepository.Object,
        _tokenBlacklistRepository.Object,
        _jwtSettings);

    private static User CreateUser(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Username = "neo",
        PasswordHash = "hashed-password",
        Role = Role.User,
        IsActive = isActive
    };

    [Fact]
    public async Task RegisterAsync_WhenUsernameAlreadyExists_Throws()
    {
        var existingUser = CreateUser();
        _userRepository.Setup(r => r.GetByUsernameAsync(existingUser.Username)).ReturnsAsync(existingUser);

        var sut = CreateSut();

        await Assert.ThrowsAsync<Exception>(() => sut.RegisterAsync(new RegisterRequest
        {
            Username = existingUser.Username,
            Password = "P@ssw0rd",
            ConfirmPassword = "P@ssw0rd"
        }));

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameIsFree_HashesPasswordAndPersistsUser()
    {
        _userRepository.Setup(r => r.GetByUsernameAsync("neo")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.HashPassword("P@ssw0rd")).Returns("hashed-password");

        var sut = CreateSut();

        var result = await sut.RegisterAsync(new RegisterRequest
        {
            Username = "neo",
            Password = "P@ssw0rd",
            ConfirmPassword = "P@ssw0rd"
        });

        Assert.Equal("neo", result.Username);
        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "neo" &&
            u.PasswordHash == "hashed-password" &&
            u.Role == Role.User)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsUnauthorized()
    {
        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = "ghost", Password = "irrelevant" }));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsUnauthorized()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByUsernameAsync(user.Username)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "wrong" }));
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ThrowsUnauthorized()
    {
        var user = CreateUser(isActive: false);
        _userRepository.Setup(r => r.GetByUsernameAsync(user.Username)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "correct" }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_IssuesAccessAndRefreshTokens()
    {
        var user = CreateUser();
        _userRepository.Setup(r => r.GetByUsernameAsync(user.Username)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("correct", user.PasswordHash)).Returns(true);
        _accessTokenGenerator.Setup(g => g.Generate(user)).Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns("raw-refresh-token");
        _refreshTokenGenerator.Setup(g => g.Hash("raw-refresh-token")).Returns("hashed-refresh-token");

        var sut = CreateSut();

        var result = await sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "correct" });

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("raw-refresh-token", result.RefreshToken);
        Assert.Equal(user.Id, result.Id);

        _refreshTokenRepository.Verify(r => r.AddAsync(It.Is<RefreshToken>(t =>
            t.UserId == user.Id && t.TokenHash == "hashed-refresh-token"), default), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenDoesNotExist_ThrowsUnauthorized()
    {
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", default)).ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RefreshAsync("raw-token"));
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsRevoked_ThrowsUnauthorized()
    {
        var storedToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", default)).ReturnsAsync(storedToken);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RefreshAsync("raw-token"));
    }

    [Fact]
    public async Task RefreshAsync_WhenUserIsInactive_ThrowsUnauthorized()
    {
        var user = CreateUser(isActive: false);
        var storedToken = new RefreshToken
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", default)).ReturnsAsync(storedToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RefreshAsync("raw-token"));
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesRefreshTokenAndRevokesOldOne()
    {
        var user = CreateUser();
        var storedToken = new RefreshToken
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenGenerator.Setup(g => g.Hash("old-raw-token")).Returns("old-hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("old-hash", default)).ReturnsAsync(storedToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _accessTokenGenerator.Setup(g => g.Generate(user)).Returns(("new-access-token", DateTime.UtcNow.AddMinutes(15)));
        _refreshTokenGenerator.Setup(g => g.Generate()).Returns("new-raw-token");
        _refreshTokenGenerator.Setup(g => g.Hash("new-raw-token")).Returns("new-hash");

        var sut = CreateSut();

        var result = await sut.RefreshAsync("old-raw-token");

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-raw-token", result.RefreshToken);

        _refreshTokenRepository.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "new-hash"), default), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAsync(storedToken, default), Times.Once);
        Assert.NotNull(storedToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task LogoutAsync_BlacklistsAccessTokenAndRevokesActiveRefreshToken()
    {
        var storedToken = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        _refreshTokenGenerator.Setup(g => g.Hash("raw-token")).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", default)).ReturnsAsync(storedToken);

        var sut = CreateSut();

        await sut.LogoutAsync("jti-123", expiresAt, "raw-token");

        _tokenBlacklistRepository.Verify(r => r.RevokeAsync("jti-123", expiresAt, default), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAsync(storedToken, default), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WhenRefreshTokenIsUnknown_OnlyBlacklistsAccessToken()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hash");
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync("hash", default)).ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        await sut.LogoutAsync("jti-123", expiresAt, "unknown-token");

        _tokenBlacklistRepository.Verify(r => r.RevokeAsync("jti-123", expiresAt, default), Times.Once);
        _refreshTokenRepository.Verify(r => r.RevokeAsync(It.IsAny<RefreshToken>(), default), Times.Never);
    }
}
