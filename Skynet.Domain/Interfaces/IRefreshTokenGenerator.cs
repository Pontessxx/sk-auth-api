namespace Skynet.Domain.Interfaces;

public interface IRefreshTokenGenerator
{
    string Generate();
    string Hash(string rawToken);
}