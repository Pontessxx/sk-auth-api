namespace Skynet.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SkynetDB");
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();
        services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();

        return services;
    }
}