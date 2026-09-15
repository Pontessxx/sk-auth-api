namespace Skynet.API.Extensions;

public static class RateLimitingExtension
{
    public const string LoginPolicy = "login";
    public const string RegisterPolicy = "register";
    public const string RefreshPolicy = "refresh";

    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many attempts. Try again later."
                }, cancellationToken);
            };

            // Applies to every request: a general per-IP flood guard for the whole API.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => SlidingWindowOptions(settings.Global.PermitLimit, TimeSpan.FromSeconds(settings.Global.WindowSeconds))));

            // Stacks on top of the global limiter: a tighter per-IP limit specifically for
            // brute-force-prone auth endpoints. A request must pass both to go through.
            options.AddPolicy(LoginPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => SlidingWindowOptions(settings.Login.PermitLimit, TimeSpan.FromSeconds(settings.Login.WindowSeconds))));

            options.AddPolicy(RegisterPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => SlidingWindowOptions(settings.Register.PermitLimit, TimeSpan.FromSeconds(settings.Register.WindowSeconds))));

            options.AddPolicy(RefreshPolicy, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => SlidingWindowOptions(settings.Refresh.PermitLimit, TimeSpan.FromSeconds(settings.Refresh.WindowSeconds))));
        });

        return services;
    }

    private static string PartitionKeyFor(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static SlidingWindowRateLimiterOptions SlidingWindowOptions(int permitLimit, TimeSpan window) => new()
    {
        PermitLimit = permitLimit,
        Window = window,
        SegmentsPerWindow = 4,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    };
}
