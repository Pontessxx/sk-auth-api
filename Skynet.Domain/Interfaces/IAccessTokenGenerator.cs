namespace Skynet.Domain.Interfaces;

public interface IAccessTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(User user);
}