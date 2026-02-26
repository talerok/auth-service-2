using Auth.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationOptions>(configuration.GetSection("Integration"));
        var integration = configuration.GetSection("Integration").Get<IntegrationOptions>() ?? new IntegrationOptions();

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(integration.PostgreSql.ConnectionString));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenFactory, JwtTokenFactory>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IPermissionBitCache, PermissionBitCache>();
        services.AddScoped<ISearchIndexService, NullSearchIndexService>();

        if (integration.Smtp.Enabled)
            services.AddSingleton<ITwoFactorEmailGateway, SmtpTwoFactorEmailGateway>();
        else
            services.AddSingleton<ITwoFactorEmailGateway, SafeDefaultTwoFactorEmailGateway>();
        services.AddHostedService<TwoFactorDeliveryBackgroundService>();


        return services;
    }
}
