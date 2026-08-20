using Booking.Application.Interfaces;
using Booking.Infrastructure.Configurations;
using Booking.Infrastructure.DataAccess;
using Booking.Infrastructure.Repositories.BookingRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<TokenSettingsConfiguration>()
            .Bind(configuration.GetRequiredSection(
                TokenSettingsConfiguration.SectionName));
        
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
