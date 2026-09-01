namespace Skynet.API.Extensions;

public static class JwtAuthenticationExtension
{
    public static IServiceCollection AddJwtAuthenticationConfig(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, jwtSettings) =>
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(jwtSettings.PublicKey);

                var securityKey = new RsaSecurityKey(rsa);

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirstValue("jti");
                        if (string.IsNullOrEmpty(jti))
                        {
                            context.Fail("Invalid Token");
                            return;
                        }

                        var tokenBlacklistRepository = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenBlacklistRepository>();

                        if (await tokenBlacklistRepository.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                        {
                            context.Fail("Revoked Token");
                        }
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Token missing, invalid or revoked."
                        });
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static WebApplication ValidateJwtSettings(this WebApplication app)
    {
        var jwtSettings = app.Services.GetRequiredService<JwtSettings>();

        if (string.IsNullOrWhiteSpace(jwtSettings.PublicKey))
        {
            throw new InvalidOperationException(
                "Jwt:PublicKey missing or null. define by using the environment variable Jwt__PublicKey — never in appsettings.json.");
        }
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(jwtSettings.PublicKey);
            rsa.Dispose();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Jwt:PublicKey invalid. Check if it is in a valid PEM format.", ex);
        }

        return app;
    }
}