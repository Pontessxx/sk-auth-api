namespace Skynet.Infra.Settings;

public class EndpointRateLimitSettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
