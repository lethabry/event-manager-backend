using Event.Application.Interfaces;
using Event.Infrastructure.Configurations;
using Event.Infrastructure.DataAccess;
using Event.Infrastructure.Kafka.BookingEventsConsumerWorker;
using Event.Infrastructure.Kafka.BookingProducer;
using Event.Infrastructure.Kafka.KafkaTopicInitializerService;
using Event.Infrastructure.Repositories.EventRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Event.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddOptions<TokenSettingsConfiguration>()
            .Bind(configuration.GetRequiredSection( TokenSettingsConfiguration.SectionName));
        services.AddOptions<KafkaConfiguration>()
            .Bind(configuration.GetRequiredSection(KafkaConfiguration.SectionName));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IBookingProducer, BookingProducer>();
        services.AddHostedService<KafkaTopicInitializerService>();
        services.AddHostedService<BookingEventsConsumerWorker>();

        return services;
    }
}
