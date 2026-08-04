using EventManager.Application.Interfaces;
using EventManager.Infrastructure.Configurations;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories.BookingRepository;
using EventManager.Infrastructure.Repositories.EventRepository;
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
                TokenSettingsConfiguration.SectionName))
            .ValidateOnStart();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
