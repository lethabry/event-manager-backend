using Auth.Application.Interfaces;
using Auth.Infrastructure.Configurations;
using Auth.Infrastructure.DataAccess;
using Auth.Infrastructure.Repositories.UserRepository;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<TokenSettingsConfiguration>()
            .Bind(configuration.GetRequiredSection(
                TokenSettingsConfiguration.SectionName));
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IHasher, Hasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGeneratorService>();

        return services;
    }
}
