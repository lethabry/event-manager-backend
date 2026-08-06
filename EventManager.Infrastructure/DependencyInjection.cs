using EventManager.Application.Interfaces;
using EventManager.Infrastructure.Configurations;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories.BookingRepository;
using EventManager.Infrastructure.Repositories.EventRepository;
using EventManager.Infrastructure.Repositories.UserRepository;
using EventManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EventManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<TokenSettingsConfiguration>()
            .Bind(configuration.GetRequiredSection(
                TokenSettingsConfiguration.SectionName));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IHasher, Hasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGeneratorService>();

        return services;
    }
}
