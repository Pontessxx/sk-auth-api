namespace Skynet.Infra.Settings;

public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public EndpointRateLimitSettings Global { get; set; } = new() { PermitLimit = 100, WindowSeconds = 10 };
    public EndpointRateLimitSettings Login { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    public EndpointRateLimitSettings Register { get; set; } = new() { PermitLimit = 3, WindowSeconds = 60 };
    public EndpointRateLimitSettings Refresh { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}
